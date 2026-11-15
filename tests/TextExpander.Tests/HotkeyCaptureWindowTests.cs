using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Styling;
using Avalonia.VisualTree;
using TextExpander.Application.Features.Hotkeys;
using TextExpander.Core;
using TextExpander.Desktop;
using TextExpander.Desktop.Localization;
using TextExpander.Desktop.ViewModels;
using TextExpander.Infrastructure.Storage;
using Xunit;

namespace TextExpander.Tests;

public sealed class HotkeyCaptureWindowTests
{
    private static readonly UiStrings Texts = UiStrings.ForLanguage("it");
    private static Button Button(Window window, string text) =>
        window.GetVisualDescendants().OfType<Button>().Single(b => (string?)b.Content == text);
    private static string Display(HotkeyCaptureWindow window) =>
        window.GetVisualDescendants().OfType<TextBlock>()
            .Single(t => AutomationProperties.GetAutomationId(t) == "HotkeyCaptureDisplay").Text ?? "";
    private static Window Owner()
    {
        Assert.IsType<HeadlessTestApp>(Avalonia.Application.Current);
        var owner = new Window();
        owner.Show();
        return owner;
    }
    private static void Down(HotkeyCaptureWindow window, Key key, RawInputModifiers modifiers) =>
        window.KeyPress(key, modifiers, PhysicalKey.None, null);
    private static void Up(HotkeyCaptureWindow window, Key key, RawInputModifiers modifiers) =>
        window.KeyRelease(key, modifiers, PhysicalKey.None, null);
    private static void Click(Window window, Control control)
    {
        var point = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Capture(HotkeyCaptureWindow window)
    {
        Down(window, Key.LeftAlt, RawInputModifiers.Alt);
        Down(window, Key.LeftCtrl, RawInputModifiers.Alt | RawInputModifiers.Control);
        Down(window, Key.LeftShift, RawInputModifiers.Alt | RawInputModifiers.Control | RawInputModifiers.Shift);
        Down(window, Key.B, RawInputModifiers.Alt | RawInputModifiers.Control | RawInputModifiers.Shift);
        Up(window, Key.B, RawInputModifiers.Alt | RawInputModifiers.Control | RawInputModifiers.Shift);
        Up(window, Key.LeftShift, RawInputModifiers.Alt | RawInputModifiers.Control);
        Up(window, Key.LeftCtrl, RawInputModifiers.Alt);
        Up(window, Key.LeftAlt, RawInputModifiers.None);
    }

    [AvaloniaFact]
    public async Task RealModalReceivesChordAndReturnsCanonicalValueWhenClicked()
    {
        var owner = Owner();
        var window = new HotkeyCaptureWindow(Texts);
        try
        {
            owner.IsEnabled = false; // Same preparation as the production MainWindow handler.
            var result = window.ShowDialog<string?>(owner);
            Dispatcher.UIThread.RunJobs();
            Assert.False(owner.IsEnabled);
            Capture(window);
            Assert.Equal("ALT + CTRL + SHIFT + B", Display(window));
            Assert.True(Button(window, Texts.HotkeyCaptureUse).IsEnabled);
            Click(window, Button(window, Texts.HotkeyCaptureUse));
            Assert.Equal("Ctrl+Alt+Shift+B", await result.WaitAsync(TimeSpan.FromSeconds(2)));
            owner.IsEnabled = true; // Restored by the caller after registrations, not by ShowDialog.
            Assert.True(owner.IsEnabled);
        }
        finally { window.Close(); owner.Close(); }
    }

    [AvaloniaFact]
    public async Task EscapeCancelsAndDoesNotReturnTheCandidate()
    {
        var owner = Owner();
        var window = new HotkeyCaptureWindow(Texts);
        try
        {
            var result = window.ShowDialog<string?>(owner);
            Capture(window);
            Assert.Equal("ALT + CTRL + SHIFT + B", Display(window));
            Down(window, Key.Escape, RawInputModifiers.None);
            Assert.Null(await result.WaitAsync(TimeSpan.FromSeconds(2)));
        }
        finally { window.Close(); owner.Close(); }
    }

    [AvaloniaFact]
    public async Task ShiftTabPreservesTheCapturedChordWhileNavigatingToConfirmation()
    {
        var owner = Owner();
        var window = new HotkeyCaptureWindow(Texts);
        try
        {
            var result = window.ShowDialog<string?>(owner);
            Capture(window);
            Down(window, Key.LeftShift, RawInputModifiers.Shift);
            Down(window, Key.Tab, RawInputModifiers.Shift);
            Up(window, Key.Tab, RawInputModifiers.Shift);
            Up(window, Key.LeftShift, RawInputModifiers.None);
            Assert.Equal("ALT + CTRL + SHIFT + B", Display(window));
            Assert.True(Button(window, Texts.HotkeyCaptureUse).IsEnabled);
            Click(window, Button(window, Texts.HotkeyCaptureUse));
            Assert.Equal("Ctrl+Alt+Shift+B", await result.WaitAsync(TimeSpan.FromSeconds(2)));
        }
        finally { window.Close(); owner.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("cancel")]
    [InlineData("clear")]
    [InlineData("close")]
    public async Task DialogActionsReturnCancelOrExplicitRemoval(string action)
    {
        var owner = Owner();
        var window = new HotkeyCaptureWindow(Texts);
        try
        {
            var result = window.ShowDialog<string?>(owner);
            Capture(window);
            if (action == "close") window.Close();
            else Click(window, Button(window, action == "clear" ? Texts.HotkeyCaptureClear : Texts.Cancel));
            Assert.Equal(action == "clear" ? "" : null, await result.WaitAsync(TimeSpan.FromSeconds(2)));
        }
        finally { window.Close(); owner.Close(); }
    }

    [AvaloniaFact]
    public async Task ConfirmationRequiresReleaseAndUnsupportedKeyCannotBeConfirmed()
    {
        var owner = Owner();
        var window = new HotkeyCaptureWindow(Texts);
        try
        {
            var result = window.ShowDialog<string?>(owner);
            Down(window, Key.B, RawInputModifiers.Control);
            Assert.False(Button(window, Texts.HotkeyCaptureUse).IsEnabled);
            Up(window, Key.B, RawInputModifiers.None);
            Assert.True(Button(window, Texts.HotkeyCaptureUse).IsEnabled);
            Down(window, Key.F12, RawInputModifiers.Control);
            Up(window, Key.F12, RawInputModifiers.None);
            Assert.False(Button(window, Texts.HotkeyCaptureUse).IsEnabled);
            Assert.Contains("F12", Display(window));
            Click(window, Button(window, Texts.HotkeyCaptureUse));
            Assert.False(result.IsCompleted);
            window.Close();
            Assert.Null(await result.WaitAsync(TimeSpan.FromSeconds(2)));
        }
        finally { window.Close(); owner.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("it", 440)]
    [InlineData("it", 540)]
    [InlineData("en", 440)]
    [InlineData("en", 540)]
    [InlineData("fr", 440)]
    [InlineData("fr", 540)]
    [InlineData("de", 440)]
    [InlineData("de", 540)]
    [InlineData("es", 440)]
    [InlineData("es", 540)]
    public async Task DialogUsesProductionResourcesAndAllActionsFitItsClientArea(string language, double width)
    {
        var texts = UiStrings.ForLanguage(language);
        var owner = Owner();
        var window = new HotkeyCaptureWindow(texts) { Width = width };
        try
        {
            var result = window.ShowDialog<string?>(owner);
            Assert.Equal(texts.HotkeyCaptureTitle, window.Title);
            foreach (var theme in new[] { ThemeVariant.Light, ThemeVariant.Dark })
            {
                window.RequestedThemeVariant = theme;
                Dispatcher.UIThread.RunJobs();
                foreach (var text in new[] { texts.Cancel, texts.HotkeyCaptureClear, texts.HotkeyCaptureUse })
                {
                    var button = Button(window, text);
                    var at = button.TranslatePoint(new Point(), window)!.Value;
                    Assert.True(at.X >= 0 && at.Y >= 0, language + ": " + text);
                    Assert.True(at.X + button.Bounds.Width <= window.ClientSize.Width + 1, language + ": " + text);
                    Assert.True(at.Y + button.Bounds.Height <= window.ClientSize.Height + 1, language + ": " + text);
                }
            }
            Click(window, Button(window, texts.Cancel));
            Assert.Null(await result.WaitAsync(TimeSpan.FromSeconds(2)));
        }
        finally { window.Close(); owner.Close(); }
    }

    [AvaloniaFact]
    public async Task ForwardTabAndEnterCanConfirmWithoutTypingIntoAnActionButton()
    {
        var owner = Owner();
        var window = new HotkeyCaptureWindow(Texts);
        try
        {
            var result = window.ShowDialog<string?>(owner);
            Capture(window);
            for (var step = 0; step < 4 && !Button(window, Texts.HotkeyCaptureUse).IsFocused; step++)
            {
                Down(window, Key.Tab, RawInputModifiers.None);
                Up(window, Key.Tab, RawInputModifiers.None);
            }
            Assert.True(Button(window, Texts.HotkeyCaptureUse).IsFocused);
            Assert.Equal("ALT + CTRL + SHIFT + B", Display(window));
            Down(window, Key.Return, RawInputModifiers.None);
            Assert.Equal("Ctrl+Alt+Shift+B", await result.WaitAsync(TimeSpan.FromSeconds(2)));
        }
        finally { window.Close(); owner.Close(); }
    }

    private sealed class LibraryWindow : MainWindow
    {
        public HotkeyCaptureCoordinator Coordinator { get; } = new();
        public HotkeyCaptureWindow? Dialog { get; private set; }
        public TaskCompletionSource? RestoreGate { get; init; }
        public bool ThrowOnOpen { get; init; }
        public int Pauses { get; private set; }
        public int Restores { get; private set; }

        protected override Task<string?> ShowSnippetHotkeyCaptureAsync(UiStrings texts) =>
            Coordinator.RunAsync(() => { Pauses++; return Task.CompletedTask; },
                async () => { Restores++; if (RestoreGate is not null) await RestoreGate.Task; }, () =>
                {
                    if (ThrowOnOpen) throw new IOException("Synthetic dialog failure");
                    Dialog = new HotkeyCaptureWindow(texts);
                    return Dialog.ShowDialog<string?>(this);
                });
    }

    private sealed class LibraryFixture : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "ristretto-ui-tests", Guid.NewGuid().ToString("N"));
        public SqliteLibraryStore Store => new(Path.Combine(Root, "library.db"));
        public async Task<MainViewModel> CreateViewModelAsync()
        {
            await Store.InitializeAsync();
            var group = await Store.CreateGroupAsync("Synthetic group");
            await Store.SaveSnippetAsync(new SnippetDraft(null, group.Id, "Synthetic snippet", ";test",
                "Caffè 🧑🏽‍💻\nمرحبا", "", true, null, "Ctrl+Alt+F"));
            return new MainViewModel(Store, Root);
        }
        public void Dispose()
        {
            var parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "ristretto-ui-tests")) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(Root).StartsWith(parent, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException();
            if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
        }
    }

    private static async Task Until(Func<bool> condition)
    {
        var deadline = Environment.TickCount64 + 3000;
        while (!condition())
        {
            if (Environment.TickCount64 > deadline) throw new TimeoutException("Headless UI did not finish");
            await Task.Delay(10);
        }
    }

    [AvaloniaFact]
    public async Task LibraryButtonOpensModalWaitsForRestoreAndChangesOnlyTheDraftUntilSave()
    {
        Assert.IsType<HeadlessTestApp>(Avalonia.Application.Current);
        using var fixture = new LibraryFixture();
        var vm = await fixture.CreateViewModelAsync();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var window = new LibraryWindow { DataContext = vm, RestoreGate = release };
        window.Show();
        try
        {
            await Until(() => window.FindControl<Button>("CaptureSnippetHotkeyButton")!.IsEnabled);
            Click(window, window.FindControl<Button>("CaptureSnippetHotkeyButton")!);
            Assert.NotNull(window.Dialog);
            Assert.False(window.IsEnabled);
            Assert.True(window.Coordinator.IsActive);
            Capture(window.Dialog);
            Click(window.Dialog, Button(window.Dialog, vm.Texts.HotkeyCaptureUse));
            Assert.False(window.IsEnabled);
            Assert.Equal("Ctrl+Alt+F", vm.EditorHotkey);
            release.SetResult();
            await Until(() => window.IsEnabled);
            Assert.Equal("Ctrl+Alt+Shift+B", vm.EditorHotkey);
            Assert.True(vm.HasUnsavedChanges);
            Assert.Equal("Ctrl+Alt+F", Assert.Single((await fixture.Store.ReadAsync()).Snippets).Hotkey);
            await vm.SaveAsync();
            Assert.Equal("Ctrl+Alt+Shift+B", Assert.Single((await fixture.Store.ReadAsync()).Snippets).Hotkey);
            Assert.False(vm.HasUnsavedChanges);
            Assert.False(window.Coordinator.IsActive);
            Assert.Equal(1, window.Pauses);
            Assert.Equal(1, window.Restores);
        }
        finally { release.TrySetResult(); window.Dialog?.Close(); window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LibraryCancellationPreservesDraftAndClearRequiresSave(bool clear)
    {
        Assert.IsType<HeadlessTestApp>(Avalonia.Application.Current);
        using var fixture = new LibraryFixture();
        var vm = await fixture.CreateViewModelAsync();
        var window = new LibraryWindow { DataContext = vm };
        window.Show();
        try
        {
            await Until(() => window.FindControl<Button>("CaptureSnippetHotkeyButton")!.IsEnabled);
            Click(window, window.FindControl<Button>("CaptureSnippetHotkeyButton")!);
            Assert.NotNull(window.Dialog);
            Capture(window.Dialog);
            Click(window.Dialog, Button(window.Dialog, clear ? vm.Texts.HotkeyCaptureClear : vm.Texts.Cancel));
            await Until(() => window.IsEnabled);
            Assert.Equal(clear ? "" : "Ctrl+Alt+F", vm.EditorHotkey);
            Assert.Equal(clear, vm.HasUnsavedChanges);
            Assert.Equal("Ctrl+Alt+F", Assert.Single((await fixture.Store.ReadAsync()).Snippets).Hotkey);
            Assert.Equal(1, window.Restores);
        }
        finally { window.Dialog?.Close(); window.Close(); }
    }

    [AvaloniaFact]
    public async Task DialogFailureReenablesTheLibraryAndLeavesTheSavedHotkeyIntact()
    {
        Assert.IsType<HeadlessTestApp>(Avalonia.Application.Current);
        using var fixture = new LibraryFixture();
        var vm = await fixture.CreateViewModelAsync();
        var window = new LibraryWindow { DataContext = vm, ThrowOnOpen = true };
        window.Show();
        try
        {
            await Until(() => window.FindControl<Button>("CaptureSnippetHotkeyButton")!.IsEnabled);
            Click(window, window.FindControl<Button>("CaptureSnippetHotkeyButton")!);
            await Until(() => window.IsEnabled);
            Assert.Equal("Ctrl+Alt+F", vm.EditorHotkey);
            Assert.Contains(vm.Texts.PickerHotkeyUnavailable, vm.StatusMessage);
            Assert.False(window.Coordinator.IsActive);
            Assert.Equal(1, window.Restores);
        }
        finally { window.Close(); }
    }
}
