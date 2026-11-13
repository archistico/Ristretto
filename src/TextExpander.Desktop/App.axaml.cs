using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using TextExpander.Application.Features.Hotkeys;
using TextExpander.Infrastructure.Platform.Windows;
using System.Threading;
using TextExpander.Desktop.ViewModels;
using TextExpander.Infrastructure.Storage;

namespace TextExpander.Desktop;

public partial class App : Avalonia.Application
{
    private IClassicDesktopStyleApplicationLifetime? _desktop;
    private MainWindow? _main;
    private PickerWindow? _picker;
    private PickerViewModel? _pickerVm;
    private MainViewModel? _vm;
    private TrayIcon? _tray;
    private WindowsPickerHotkeyService? _pickerHotkey;
    private WindowsPickerPasteService? _pickerPaste;
    private WindowsPickerInsertRequest? _pickerRequest;
    private readonly SemaphoreSlim _pickerHotkeyLock = new(1, 1);
    private string _activePickerHotkey = "";
    private bool _keepRunningInTray;
    private bool _closeApproved;
    internal bool IsExiting { get; private set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _desktop = desktop;
            var profile = Program.Profile ?? throw new InvalidOperationException("Profilo di avvio non configurato.");
            var store = new SqliteLibraryStore(profile.DatabasePath);
            _vm = new MainViewModel(store, profile.DataDirectory);
            _vm.ThemeModeChanged += (_, mode) => RequestedThemeVariant = mode switch
            {
                "Light" => ThemeVariant.Light,
                "Dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };
            _vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(MainViewModel.Texts)) return;
                _pickerVm?.UpdateLanguage(_vm.Texts);
                UpdateTrayMenu();
            };
            _main = new MainWindow { DataContext = _vm };
            desktop.MainWindow = _main;
            _keepRunningInTray = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();
            desktop.ShutdownMode = _keepRunningInTray
                ? ShutdownMode.OnExplicitShutdown : ShutdownMode.OnMainWindowClose;
            _main.Closing += async (_, e) =>
            {
                if (IsExiting || _closeApproved) return;
                if (_keepRunningInTray)
                {
                    e.Cancel = true;
                    _main.Hide(); // Il draft rimane in memoria quando la finestra è nascosta.
                    return;
                }
                if (_vm?.HasUnsavedChanges != true) return;
                e.Cancel = true;
                if (await _main.ConfirmPendingEditsAsync())
                {
                    _closeApproved = true;
                    _main.Close();
                }
            };
            _pickerVm = new PickerViewModel(store, _vm.Texts);
            _picker = new PickerWindow(_pickerVm);
            CreateTray();
            desktop.Exit += (_, _) => _pickerHotkey?.Dispose();
            Program.Instance?.StartListening(() => Dispatcher.UIThread.Post(ShowMainWindow));
        }
        base.OnFrameworkInitializationCompleted();
    }

    internal Task InitializePickerHotkeyAsync(MainViewModel vm) => ConfigurePickerHotkeyAsync(vm, persist: false);
    internal Task SavePickerHotkeyAsync(MainViewModel vm) => ConfigurePickerHotkeyAsync(vm, persist: true);

    private async Task ConfigurePickerHotkeyAsync(MainViewModel vm, bool persist)
    {
        await _pickerHotkeyLock.WaitAsync();
        try
        {
            if (!PickerHotkey.TryParse(vm.PickerHotkeyDraft, out var gesture))
            {
                vm.SetPickerHotkeyState(_activePickerHotkey, vm.Texts.PickerHotkeyInvalid);
                return;
            }
            if (!OperatingSystem.IsWindows())
            {
                vm.SetPickerHotkeyState(_activePickerHotkey, vm.Texts.PickerHotkeyUnsupported);
                return;
            }
            _pickerHotkey ??= new WindowsPickerHotkeyService(
                () => Dispatcher.UIThread.Post(ShowPicker));
            if (!await _pickerHotkey.PrepareAsync(gesture))
            {
                vm.SetPickerHotkeyState(_activePickerHotkey, vm.Texts.PickerHotkeyConflict);
                return;
            }
            var saved = false;
            var previousCanonical = string.IsNullOrEmpty(_activePickerHotkey) ? "None" : _activePickerHotkey;
            try
            {
                if (persist)
                {
                    await vm.SavePickerHotkeyAsync(gesture.Canonical);
                    saved = true;
                }
                if (!await _pickerHotkey.CommitAsync())
                    throw new InvalidOperationException("Windows hotkey commit failed.");
                _activePickerHotkey = gesture.IsDisabled ? "" : gesture.Canonical;
                vm.SetPickerHotkeyState(_activePickerHotkey,
                    gesture.IsDisabled ? vm.Texts.PickerHotkeyDisabled :
                    string.Format(vm.Texts.PickerHotkeyRegistered, gesture.Canonical));
            }
            catch (Exception)
            {
                await _pickerHotkey.CancelAsync();
                if (saved)
                {
                    try { await vm.SavePickerHotkeyAsync(previousCanonical); }
                    catch { /* Retain the runtime registration; report persistence failure. */ }
                }
                vm.SetPickerHotkeyState(_activePickerHotkey, vm.Texts.PickerHotkeySaveFailed);
            }
        }
        catch (Exception)
        {
            vm.SetPickerHotkeyState(_activePickerHotkey, vm.Texts.PickerHotkeyUnavailable);
        }
        finally { _pickerHotkeyLock.Release(); }
    }

    private void CreateTray()
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://Ristretto/Assets/app-icon.ico"));
            _tray = new TrayIcon { Icon = new WindowIcon(stream), ToolTipText = "Ristretto" };
            _tray.Clicked += (_, _) => ShowMainWindow();
            UpdateTrayMenu();
            TrayIcon.SetIcons(this, new TrayIcons { _tray });
        }
        catch
        {
            // Senza icona funzionante non nascondere la finestra: evitare processi invisibili.
            _keepRunningInTray = false;
            if (_desktop is not null) _desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
    }

    private void UpdateTrayMenu()
    {
        if (_tray is null || _vm is null) return;
        var strings = _vm.Texts;
        var menu = new NativeMenu();
        var open = new NativeMenuItem { Header = strings.TrayOpen };
        open.Click += (_, _) => ShowMainWindow();
        var picker = new NativeMenuItem { Header = strings.TrayPicker };
        picker.Click += (_, _) => ShowPicker();
        var quit = new NativeMenuItem { Header = strings.TrayExit };
        quit.Click += (_, _) => Quit();
        menu.Add(open);
        menu.Add(picker);
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(quit);
        _tray.Menu = menu;
    }

    internal void ShowMainWindow()
    {
        CancelPickerInsertion();
        if (_main is null) return;
        if (!_main.IsVisible) _main.Show();
        _main.WindowState = WindowState.Normal;
        _main.Activate();
    }

    internal async void ShowPicker()
    {
        if (IsExiting || _picker is null || _pickerVm is null) return;
        CancelPickerInsertion();
        // Capture before the picker activates: afterwards foreground would be Ristretto itself.
        if (OperatingSystem.IsWindows())
        {
            _pickerPaste ??= new WindowsPickerPasteService();
            var target = _pickerPaste.CaptureExternalForeground();
            _pickerRequest = target is null ? null : new WindowsPickerInsertRequest(_pickerPaste, target);
        }
        _pickerVm.SetInsertAvailable(_pickerRequest is not null);
        var request = _pickerRequest;
        if (!_picker.IsVisible) _picker.Show();
        if (_picker.WindowState == WindowState.Minimized)
            _picker.WindowState = WindowState.Normal;
        _picker.Activate();
        _picker.FocusSearch();
        if (!await _pickerVm.ReloadAsync()) return;
        if (!ReferenceEquals(request, _pickerRequest) || IsExiting) return;
        if (OperatingSystem.IsWindows() && request is null)
            _pickerVm.Status = _pickerVm.Texts.PickerInsertNoTarget;
    }

    internal void CancelPickerInsertion()
    {
        _pickerRequest?.Cancel();
        _pickerRequest = null;
        _pickerVm?.CancelReload();
        _pickerVm?.SetInsertAvailable(false);
    }

    internal async Task InsertFromPickerAsync(string content)
    {
        if (_picker is null || _pickerVm is null || _pickerPaste is null || _pickerRequest is null)
        {
            if (_pickerVm is not null) _pickerVm.Status = _pickerVm.Texts.PickerInsertNoTarget;
            return;
        }
        var request = _pickerRequest;
        var clipboard = _picker.Clipboard;
        if (clipboard is null)
        {
            _pickerVm.Status = _pickerVm.Texts.PickerClipboardUnavailable;
            return;
        }
        _pickerVm.SetInsertAvailable(false);
        try
        {
            _picker.Hide();
            // Explicit insertion uses clipboard paste to retain multiline text and Unicode.
            // This operation changes the clipboard, as documented in the picker help.
            var result = await request.ExecuteAsync(async () =>
            {
                await clipboard.SetTextAsync(content);
                return _pickerPaste.ClipboardSequence();
            }, () => Task.Delay(60));
            // An old continuation must not reopen or change a newer picker session.
            if (!ReferenceEquals(request, _pickerRequest) || IsExiting) return;
            switch (result.Status)
            {
                case PickerPasteStatus.SubmittedUnverified:
                    _pickerVm.Status = _pickerVm.Texts.PickerInsertSubmitted;
                    // Do not reactivate the picker: leave the external editor focused.
                    return;
                case PickerPasteStatus.PartialOrUnknown:
                    RestorePickerAfterInsertError(_pickerVm.Texts.PickerInsertUncertain);
                    return;
                default:
                    RestorePickerAfterInsertError(_pickerVm.Texts.PickerInsertFocusChanged);
                    return;
            }
        }
        catch (Exception)
        {
            // Clipboard or native failure. Never retry: an earlier paste may have succeeded.
            if (ReferenceEquals(request, _pickerRequest) && !IsExiting)
                RestorePickerAfterInsertError(_pickerVm.Texts.PickerInsertUncertain);
        }
    }

    private void RestorePickerAfterInsertError(string message)
    {
        if (_picker is null || _pickerVm is null) return;
        if (!_picker.IsVisible) _picker.Show();
        _picker.Activate();
        _picker.FocusSearch();
        _pickerVm.Status = message;
    }

    private async void Quit()
    {
        if (_main is not null && !await _main.ConfirmPendingEditsAsync()) return;
        IsExiting = true;
        CancelPickerInsertion();
        _desktop?.Shutdown();
    }
}
