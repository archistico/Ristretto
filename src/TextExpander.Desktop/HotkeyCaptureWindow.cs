using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using TextExpander.Desktop.Localization;

namespace TextExpander.Desktop;

public sealed class HotkeyCaptureWindow : Window
{
    private readonly HotkeyCaptureState _state = new();
    private readonly UiStrings _texts;
    private readonly TextBlock _display = new() { FontSize = 23, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _feedback = new() { TextWrapping = TextWrapping.Wrap, FontSize = 12 };
    private readonly Border _captureArea;
    private readonly Button _use;

    public HotkeyCaptureWindow(UiStrings texts)
    {
        _texts = texts;
        Title = texts.HotkeyCaptureTitle;
        Width = 540;
        MinWidth = 440;
        CanResize = false;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Classes.Add("ristretto-dialog");
        var title = new TextBlock { Text = texts.HotkeyCaptureTitle };
        title.Classes.Add("dialog-title");
        var header = new Border { Padding = new Thickness(24, 18), Child = title };
        header.Classes.Add("dialog-hero");

        _captureArea = new Border
        {
            Focusable = true,
            Padding = new Thickness(18),
            MinHeight = 85,
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(2),
            Child = _display
        };
        _captureArea.Bind(Border.BorderBrushProperty, this.GetResourceObservable("RPrimary"));
        AutomationProperties.SetName(_captureArea, texts.HotkeyCaptureTitle);
        _captureArea.PointerPressed += (_, _) => _captureArea.Focus();
        var body = new StackPanel
        {
            Margin = new Thickness(24, 20), Spacing = 14,
            Children =
            {
                new TextBlock { Text = texts.HotkeyCaptureInstructions, TextWrapping = TextWrapping.Wrap },
                _captureArea, _feedback
            }
        };
        var cancel = new Button { Content = texts.Cancel };
        cancel.Classes.Add("ghost");
        cancel.Click += (_, _) => Close((string?)null);
        var clear = new Button { Content = texts.HotkeyCaptureClear };
        clear.Classes.Add("ghost");
        clear.Click += (_, _) => Close("");
        _use = new Button { Content = texts.HotkeyCaptureUse, IsEnabled = false };
        _use.Classes.Add("primary");
        _use.Click += (_, _) => { if (_state.CanConfirm) Close(_state.Hotkey); };
        var footer = new Border
        {
            Padding = new Thickness(22, 13),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8,
                Children = { clear, cancel, _use }
            }
        };
        footer.Classes.Add("dialog-footer");
        var layout = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,Auto") };
        Grid.SetRow(body, 1);
        Grid.SetRow(footer, 2);
        layout.Children.Add(header);
        layout.Children.Add(body);
        layout.Children.Add(footer);
        Content = layout;
        Opened += (_, _) => _captureArea.Focus();
        Deactivated += (_, _) => { _state.Reset(); Refresh(); };
        AddHandler(KeyDownEvent, OnCaptureKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnCaptureKeyUp, RoutingStrategies.Tunnel);
        Refresh();
    }

    private void OnCaptureKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && e.KeyModifiers == KeyModifiers.None)
        {
            e.Handled = true;
            Close((string?)null);
            return;
        }
        if (!_captureArea.IsFocused) return;
        if (e.Key == Key.Tab && e.KeyModifiers is KeyModifiers.None or KeyModifiers.Shift) return;
        e.Handled = true;
        _state.KeyDown(e.Key, e.KeyModifiers);
        Refresh();
    }

    private void OnCaptureKeyUp(object? sender, KeyEventArgs e)
    {
        _state.KeyUp(e.Key, e.KeyModifiers);
        Refresh();
    }

    private void Refresh()
    {
        _display.Text = _state.DisplayText.Length == 0 ? _texts.HotkeyCaptureWaiting : _state.DisplayText;
        _feedback.Text = _state.IsInvalid ? _texts.PickerHotkeyInvalid : _state.CanConfirm
            ? _texts.HotkeyCaptureReady : _state.Hotkey is not null
                ? _texts.HotkeyCaptureRelease : _texts.HotkeyCaptureInstructions;
        _use.IsEnabled = _state.CanConfirm;
    }
}
