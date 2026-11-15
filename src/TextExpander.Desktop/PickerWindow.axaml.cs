using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using TextExpander.Desktop.ViewModels;

namespace TextExpander.Desktop;

public partial class PickerWindow : Window
{
    private PickerViewModel? _viewModel;
    private readonly PickerEnterGesture _enterGesture = new();

    // Necessario per il loader XAML Avalonia; la normale creazione da App
    // usa sempre il costruttore con ViewModel.
    public PickerWindow()
    {
        InitializeComponent();
        Opened += (_, _) => SearchBox.Focus();
        KeyDown += OnPickerKeyDown;
        // Le frecce funzionano anche quando il focus è nel campo di ricerca.
        AddHandler(KeyDownEvent, OnPickerNavigationKeyDown, RoutingStrategies.Tunnel);
        // Intercept Enter before a focused Copy button can invoke its default action.
        AddHandler(KeyDownEvent, OnPickerEnterKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnPickerEnterKeyUp, RoutingStrategies.Tunnel);
        Deactivated += (_, _) => ResetEnterGesture();
        Closing += (_, e) =>
        {
            if ((Avalonia.Application.Current as App)?.IsExiting == true) return;
            e.Cancel = true;
            ResetEnterGesture();
            (Avalonia.Application.Current as App)?.CancelPickerInsertion();
            Hide();
        };
    }

    public PickerWindow(PickerViewModel viewModel) : this()
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    public void FocusSearch()
    {
        ResetEnterGesture();
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void OnPickerKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            ResetEnterGesture();
            (Avalonia.Application.Current as App)?.CancelPickerInsertion();
            Hide();
            e.Handled = true;
        }
    }

    private void OnPickerNavigationKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Down or Key.Up) || e.KeyModifiers != KeyModifiers.None ||
            _viewModel is not { } vm || vm.IsLoading || vm.IsClipboardBusy || vm.Results.Count == 0)
            return;
        vm.MoveSelection(e.Key == Key.Down ? 1 : -1);
        e.Handled = true; // evita che SearchBox o ListBox interpretino la freccia due volte
    }

    private void OnPickerEnterKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (e.KeyModifiers != KeyModifiers.None && !_enterGesture.IsDown) return;
        e.Handled = true;
        // Readiness is intentionally checked at KeyUp: a quick Enter while
        // the picker finishes loading must not permanently disarm the gesture.
        _enterGesture.Press(e.KeyModifiers == KeyModifiers.None);
    }

    private async void OnPickerEnterKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        var observedPress = _enterGesture.IsDown;
        var submit = _enterGesture.Release(e.KeyModifiers == KeyModifiers.None);
        if (observedPress) e.Handled = true;
        if (!submit || _viewModel is not { } vm) return;
        // A quick Enter can arrive before the async search index is published.
        // Defer only within this same focused picker/query; never send after
        // deactivation, dismissal, a new session or a changed search query.
        if (vm.IsLoading)
        {
            _enterGesture.DeferUntilLoaded(vm.Query);
            return;
        }
        // Keep focus in the picker until Enter is released, so key-repeat cannot
        // send Return characters into the external editor during focus restoration.
        if (vm.CanInsert && Avalonia.Application.Current is App app)
            await vm.InsertForEnterAsync(app.InsertFromPickerAsync);
    }

    internal async Task CompleteDeferredEnterAsync()
    {
        if (_viewModel is not { } vm) return;
        if (!_enterGesture.ConsumeDeferred(vm.Query,
                pickerActive: IsVisible && IsActive,
                canInsert: vm.CanInsert && !vm.IsLoading)) return;
        if (Avalonia.Application.Current is App app)
            await vm.InsertForEnterAsync(app.InsertFromPickerAsync);
    }

    private void ResetEnterGesture() => _enterGesture.Reset();

    private async Task CopySelectedAsync()
    {
        var vm = _viewModel;
        if (vm is null) return;
        await vm.CopySelectedAsync(async content =>
        {
            var clipboard = Clipboard;
            if (clipboard is null) throw new InvalidOperationException(vm.Texts.PickerClipboardUnavailable);
            await clipboard.SetTextAsync(content);
        });
    }

    private async void Copy_Click(object? sender, RoutedEventArgs e) => await CopySelectedAsync();

    private async void Insert_Click(object? sender, RoutedEventArgs e)
    {
        var vm = _viewModel;
        if (vm is null || Avalonia.Application.Current is not App app) return;
        await vm.InsertSelectedAsync(app.InsertFromPickerAsync);
    }

    private void OpenLibrary_Click(object? sender, RoutedEventArgs e)
    {
        ResetEnterGesture();
        (Avalonia.Application.Current as App)?.ShowMainWindow();
        Hide();
    }
}
