using TextExpander.Infrastructure.Platform.Windows;
using Xunit;

namespace TextExpander.Tests;

public sealed class WindowsSnippetInsertRequestTests
{
    private static readonly WindowsPickerPasteTarget Target = new(100, 200, 101);
    private sealed class Api : IWindowsPickerPasteApi
    {
        public bool Modifiers, Protected;
        public nint Foreground = 100, Focus = 101;
        public uint Sequence = 10;
        public int Activations, Pastes, Sent = 4;
        public nint ForegroundWindow() => Foreground;
        public uint WindowProcessId(nint window) => 200;
        public nint FocusedControl(nint window) => Focus;
        public bool IsWindowAvailable(nint window) => true;
        public bool IsSameOrChild(nint root, nint candidate) => candidate == 101;
        public bool IsProtectedEditor(nint control) => Protected;
        public bool ActivateWindow(nint window) { Activations++; return true; }
        public bool IsAnyModifierPressed() => Modifiers;
        public uint ClipboardSequence() => Sequence;
        public int SendPasteShortcut() { Pastes++; return Sent; }
    }
    private static WindowsSnippetInsertRequest Request(Api api) => new(new(api, () => true, 999), Target, 10);
    private static Task<uint> Prepare(Api api) => Task.FromResult(++api.Sequence);

    [Fact]
    public async Task DirectHotkeyWaitsForReleaseAndNeverActivatesAWindowOrRetries()
    {
        var api = new Api { Modifiers = true };
        var request = Request(api);
        var writes = 0;
        var down = true;
        var result = await request.ExecuteAsync(() => { writes++; return Prepare(api); }, () => down,
            () => { Assert.Equal(0, writes); api.Modifiers = down = false; return Task.CompletedTask; });
        Assert.Equal(PickerPasteStatus.SubmittedUnverified, result.Status);
        Assert.Equal(1, writes);
        Assert.Equal(1, api.Pastes);
        Assert.Equal(0, api.Activations);
        await request.ExecuteAsync(() => Prepare(api), () => false, () => Task.CompletedTask);
        Assert.Equal(1, api.Pastes);
    }

    [Theory]
    [InlineData("focus")]
    [InlineData("clipboard")]
    [InlineData("protected")]
    [InlineData("cancel")]
    public async Task ChangesWhileReleasingNeverWriteClipboardOrPaste(string change)
    {
        var api = new Api { Modifiers = true };
        var request = Request(api);
        var writes = 0;
        var result = await request.ExecuteAsync(() => { writes++; return Prepare(api); }, () => false, () =>
        {
            api.Modifiers = false;
            if (change == "focus") api.Focus = 102;
            if (change == "clipboard") api.Sequence++;
            if (change == "protected") api.Protected = true;
            if (change == "cancel") request.Cancel();
            return Task.CompletedTask;
        });
        Assert.Equal(PickerPasteStatus.Rejected, result.Status);
        Assert.Equal(0, writes);
        Assert.Equal(0, api.Pastes);
        Assert.Equal(0, api.Activations);
    }

    [Fact]
    public async Task HeldTriggerTimesOutInsteadOfQueuingAnInsertion()
    {
        var api = new Api();
        var result = await Request(api).ExecuteAsync(() => throw new Exception(), () => true, () => Task.CompletedTask);
        Assert.Equal("hotkey_release_timeout", result.Reason);
        Assert.Equal(0, api.Pastes);
    }

    [Fact]
    public async Task SlowUiCannotExtendTheReleaseDeadlineAndPasteALateCommand()
    {
        var api = new Api { Modifiers = true };
        long clock = 0;
        var request = new WindowsSnippetInsertRequest(new(api, () => true, 999), Target, 10, () => clock);
        var result = await request.ExecuteAsync(() => throw new Exception(), () => false, () =>
        {
            clock = 3000;
            api.Modifiers = false;
            return Task.CompletedTask;
        });
        Assert.Equal("hotkey_release_timeout", result.Reason);
        Assert.Equal(0, api.Pastes);
    }

    [Fact]
    public async Task CancellationDuringClipboardWritePreventsSendInput()
    {
        var api = new Api();
        var request = Request(api);
        var result = await request.ExecuteAsync(() => { request.Cancel(); return Prepare(api); },
            () => false, () => Task.CompletedTask);
        Assert.Equal(PickerPasteStatus.Rejected, result.Status);
        Assert.Equal(0, api.Pastes);
    }

    [Fact]
    public async Task PartialPasteHasUnknownEffectAndCannotBeRetried()
    {
        var api = new Api { Sent = 2 };
        var request = Request(api);
        Assert.Equal(PickerPasteStatus.PartialOrUnknown,
            (await request.ExecuteAsync(() => Prepare(api), () => false, () => Task.CompletedTask)).Status);
        Assert.Equal("request_already_used",
            (await request.ExecuteAsync(() => throw new Exception(), () => false, () => Task.CompletedTask)).Reason);
        Assert.Equal(1, api.Pastes);
    }

    [Fact]
    public async Task ClipboardChangesBetweenReleaseAndWriteAreNotOverwritten()
    {
        var api = new Api();
        var request = Request(api);
        var result = await request.ExecuteAsync(() => throw new Exception(),
            () => { api.Sequence++; return false; }, () => Task.CompletedTask);
        Assert.Equal(PickerPasteStatus.Rejected, result.Status);
        Assert.Equal(0, api.Pastes);
    }
}
