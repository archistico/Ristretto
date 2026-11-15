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
    private readonly TextBlock _display = new() { TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _feedback = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Border _captureArea;
    private readonly Button _use;

    public HotkeyCaptureWindow(UiStrings texts)
    {
        _texts = texts;
        Title = texts.HotkeyCaptureTitle;
        Width = 500;
        MinWidth = 440;
        CanResize = false;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Classes.Add("ristretto-dialog");
        Classes.Add("compact");
        var title = new TextBlock { Text = texts.HotkeyCaptureTitle };
        title.Classes.Add("dialog-title");
        var header = new Border { Child = title };
        header.Classes.Add("dialog-hero");

        _captureArea = new Border
        {
            Focusable = true,
            Child = _display
        };
        _captureArea.Classes.Add("hotkey-field");
        _feedback.Classes.Add("field-hint");
        _feedback.Classes.Add("hotkey-feedback");
        _feedback.Classes.Add("muted");
        AutomationProperties.SetAutomationId(_captureArea, "HotkeyCaptureArea");
        AutomationProperties.SetAutomationId(_display, "HotkeyCaptureDisplay");
        AutomationProperties.SetAutomationId(_feedback, "HotkeyCaptureFeedback");
        AutomationProperties.SetName(_captureArea, texts.HotkeyCaptureTitle);
        _captureArea.PointerPressed += (_, _) => _captureArea.Focus();
        var instructions = new TextBlock { Text = texts.HotkeyCaptureInstructions, TextWrapping = TextWrapping.Wrap };
        instructions.Classes.Add("muted");
        var body = new Border
        {
            Child = new StackPanel
            {
                Spacing = 8,
                Children = { instructions, _captureArea, _feedback }
            }
        };
        body.Classes.Add("dialog-body");
        var cancel = new Button { Content = texts.Cancel };
        AutomationProperties.SetAutomationId(cancel, "HotkeyCaptureCancel");
        cancel.Classes.Add("ghost");
        cancel.Click += (_, _) => Close((string?)null);
        var clear = new Button { Content = texts.HotkeyCaptureClear };
        AutomationProperties.SetAutomationId(clear, "HotkeyCaptureClear");
        clear.Classes.Add("ghost");
        clear.Click += (_, _) => Close("");
        _use = new Button { Content = texts.HotkeyCaptureUse, IsEnabled = false };
        AutomationProperties.SetAutomationId(_use, "HotkeyCaptureConfirm");
        _use.Classes.Add("primary");
        _use.Click += (_, _) => { if (_state.CanConfirm) Close(_state.Hotkey); };
        var actions = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            ItemSpacing = 6,
            LineSpacing = 6,
            Children = { clear, cancel, _use }
        };
        var footer = new Border
        {
            Child = actions
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
        if (e.Key == Key.Tab && e.KeyModifiers is KeyModifiers.None or KeyModifiers.Shift && !_state.PrimaryKeyHeld)
        {
            _state.Navigate();
            Refresh();
            return;
        }
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
                ? _texts.HotkeyCaptureRelease : "";
        _display.Classes.Set("muted", _state.DisplayText.Length == 0);
        _use.IsEnabled = _state.CanConfirm;
    }
}
