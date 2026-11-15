using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
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
    private readonly PickerSessionController _pickerSession = new();
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
            _vm.RefreshSnippetHotkeysAsync = RefreshSnippetHotkeysAsync;
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
            desktop.Exit += (_, _) =>
            {
                IsExiting = true;
                CancelSnippetInsertion();
                CancelPickerInsertion();
                _snippetHotkeys?.Dispose();
                _pickerHotkey?.Dispose();
            };
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
                if (_snippetSnapshot is not null) await RefreshSnippetHotkeysAsync(_snippetSnapshot);
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
        CancelSnippetInsertion();
        CancelPickerInsertion();
        if (_main is null) return;
        if (!_main.IsVisible) _main.Show();
        _main.WindowState = WindowState.Normal;
        _main.Activate();
    }

    internal async void ShowPicker()
    {
        if (IsExiting || _picker is null || _pickerVm is null) return;
        // Ignore delayed/repeated hotkey notifications during an in-flight Insert.
        // They must not cancel the request between hiding the picker and SendInput.
        if (_pickerSession.IsInsertionInProgress || _directInsertRunning) return;
        // Repeated global hotkey while the picker itself owns focus must not
        // recapture Ristretto as the external recipient or erase the target.
        // Reinvoking from a different foreground app starts a fresh session.
        var opening = _pickerSession.Open(_picker.IsVisible, _picker.IsActive, () =>
        {
            if (!OperatingSystem.IsWindows()) return null;
            _pickerPaste ??= new WindowsPickerPasteService();
            var target = _pickerPaste.CaptureExternalForeground();
            return target is null ? null : new WindowsPickerInsertRequest(_pickerPaste, target);
        });
        if (opening == PickerSessionOpenResult.InsertionInProgress) return;
        if (opening == PickerSessionOpenResult.Reused)
        {
            _picker.Activate();
            _picker.FocusSearch();
            return;
        }

        _pickerVm.CancelReload();
        _pickerVm.SetInsertAvailable(_pickerSession.Request is not null);
        var generation = _pickerSession.Generation;
        if (!_picker.IsVisible) _picker.Show();
        if (_picker.WindowState == WindowState.Minimized)
            _picker.WindowState = WindowState.Normal;
        _picker.Activate();
        _picker.FocusSearch();
        if (!await _pickerVm.ReloadAsync()) return;
        if (!_pickerSession.IsCurrent(generation) || IsExiting) return;
        if (OperatingSystem.IsWindows() && _pickerSession.Request is null)
            _pickerVm.Status = _pickerVm.Texts.PickerInsertNoTarget;
        await _picker.CompleteDeferredEnterAsync();
    }

    internal void CancelPickerInsertion()
    {
        _pickerSession.Cancel();
        _pickerVm?.CancelReload();
        _pickerVm?.SetInsertAvailable(false);
    }

    internal async Task InsertFromPickerAsync(string content)
    {
        if (_directInsertRunning) return;
        if (_picker is null || _pickerVm is null || _pickerPaste is null || _pickerSession.Request is null)
        {
            if (_pickerVm is not null) _pickerVm.Status = _pickerVm.Texts.PickerInsertNoTarget;
            return;
        }
        var request = _pickerSession.Request!;
        var generation = _pickerSession.Generation;
        var clipboard = _picker.Clipboard;
        if (clipboard is null)
        {
            _pickerVm.Status = _pickerVm.Texts.PickerClipboardUnavailable;
            return;
        }
        // Reserve the current session before hiding the picker. A queued hotkey
        // cannot recapture/cancel the destination during this transition.
        if (!_pickerSession.TryBeginInsertion(generation, request)) return;
        _pickerVm.SetInsertAvailable(false);
        try
        {
            _picker.Hide();
            // Explicit insertion uses clipboard paste to retain multiline text and Unicode.
            // The request waits for the external focus, revalidates the target and
            // clipboard sequence, and ONLY THEN calls SetTextAsync. It revalidates
            // everything again just before Ctrl+V. Clipboard is not restored.
            var result = await request.ExecuteAsync(async () =>
            {
                // Do not send Ctrl+V when SetTextAsync returned without an
                // observable clipboard change: that could paste stale data.
                var before = _pickerPaste.ClipboardSequence();
                await clipboard.SetTextAsync(content);
                var afterWrite = _pickerPaste.ClipboardSequence();
                // Read back exactly what the OS clipboard exposes. A changed
                // sequence is not evidence that our selected snippet was copied.
                // Do not record clipboard contents in diagnostics.
                if (!WindowsPickerPasteService.HasClipboardAdvanced(before, afterWrite))
                    return 0u;
                var observed = await clipboard.TryGetTextAsync();
                var afterRead = _pickerPaste.ClipboardSequence();
                return PickerClipboardReadbackVerifier.VerifiedSequence(
                    content, observed, before, afterWrite, afterRead);
            }, () => Task.Delay(60));
            // An old continuation must not reopen or change a newer picker session.
            if (!_pickerSession.IsCurrent(generation, request) || IsExiting) return;
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
                    // Native reason codes are fixed, privacy-safe diagnostics.
                    RestorePickerAfterInsertError(
                        $"{_pickerVm.Texts.PickerInsertFocusChanged} [{result.Reason}]");
                    return;
            }
        }
        catch (Exception)
        {
            // Clipboard or native failure. Never retry: an earlier paste may have succeeded.
            if (_pickerSession.IsCurrent(generation, request) && !IsExiting)
                RestorePickerAfterInsertError(_pickerVm.Texts.PickerInsertUncertain);
        }
        finally
        {
            _pickerSession.CompleteInsertion(generation, request);
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
        CancelSnippetInsertion();
        CancelPickerInsertion();
        _desktop?.Shutdown();
    }
}
