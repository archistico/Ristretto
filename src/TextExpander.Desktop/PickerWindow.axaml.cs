using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using TextExpander.Desktop.ViewModels;

namespace TextExpander.Desktop;

public partial class PickerWindow : Window
{
    private PickerViewModel? _viewModel;
    private bool _enterArmed;
    private bool _enterDown;

    // Necessario per il loader XAML Avalonia; la normale creazione da App
    // usa sempre il costruttore con ViewModel.
    public PickerWindow()
    {
        InitializeComponent();
        Opened += (_, _) => SearchBox.Focus();
        KeyDown += OnPickerKeyDown;
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
        else if ((e.Key == Key.Down || e.Key == Key.Up) && _viewModel is { } vm && vm.Results.Count > 0)
        {
            var delta = e.Key == Key.Down ? 1 : -1;
            var next = Math.Clamp(ResultsList.SelectedIndex + delta, 0, vm.Results.Count - 1);
            ResultsList.SelectedIndex = next;
            e.Handled = true;
        }
    }

    private void OnPickerEnterKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || e.KeyModifiers != KeyModifiers.None) return;
        e.Handled = true;
        if (_enterDown) return;
        _enterDown = true;
        _enterArmed = _viewModel is { IsLoading: false, IsClipboardBusy: false } vm && vm.Results.Count > 0;
    }

    private async void OnPickerEnterKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        var armed = _enterArmed;
        ResetEnterGesture();
        if (armed || e.KeyModifiers == KeyModifiers.None) e.Handled = true;
        if (!armed || e.KeyModifiers != KeyModifiers.None) return;
        // Keep focus in the picker until Enter is released, so key-repeat cannot
        // send Return characters into the external editor during focus restoration.
        if (_viewModel is { } vm && Avalonia.Application.Current is App app)
            await vm.InsertFirstAsync(app.InsertFromPickerAsync);
    }

    private void ResetEnterGesture()
    {
        _enterArmed = false;
        _enterDown = false;
    }

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
