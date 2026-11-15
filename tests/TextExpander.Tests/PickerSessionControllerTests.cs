using TextExpander.Desktop;
using TextExpander.Infrastructure.Platform.Windows;
using Xunit;

namespace TextExpander.Tests;

/// <summary>
/// Exercises the production picker session coordinator together with the
/// production one-shot Windows paste request. Native calls are injectable:
/// these tests never activate a real window or touch the user's clipboard.
/// </summary>
public sealed class PickerSessionControllerTests
{
    private sealed class FakeWindows : IWindowsPickerPasteApi
    {
        public nint Root { get; set; } = 0x100;
        public nint Focus { get; set; } = 0x101;
        public nint Foreground { get; set; } = 0x100;
        public uint Process { get; set; } = 42;
        public bool Available { get; set; } = true;
        public bool FocusAvailable { get; set; } = true;
        public uint? FocusProcessId { get; set; }
        public bool Protected { get; set; }
        public bool Modifiers { get; set; }
        public bool Activate { get; set; } = true;
        public uint Clipboard { get; set; } = 17;
        public int AcceptedInputs { get; set; } = 4;
        public Action? BeforeModifierCheck { get; set; }
        public int Sends { get; private set; }
        public int Activations { get; private set; }
        public nint ForegroundWindow() => Foreground;
        public uint WindowProcessId(nint window) => window == Root ? Process :
            window == Focus ? (FocusProcessId ?? Process) : 0;
        public nint FocusedControl(nint window) => window == Root ? Focus : 0;
        public bool IsWindowAvailable(nint window) => Available &&
            (window == Root || (FocusAvailable && window == Focus));
        public bool IsSameOrChild(nint root, nint child) => root == Root && (child == root || child == Focus);
        public bool IsProtectedEditor(nint control) => Protected;
        public bool ActivateWindow(nint window)
        {
            Activations++;
            if (!Activate) return false;
            Foreground = window;
            return true;
        }
        public bool IsAnyModifierPressed()
        {
            BeforeModifierCheck?.Invoke();
            return Modifiers;
        }
        public uint ClipboardSequence() => Clipboard;
        public int SendPasteShortcut() { Sends++; return AcceptedInputs; }
    }

    private static WindowsPickerPasteService Service(FakeWindows native) => new(native, () => true, 900);

    private static Func<WindowsPickerInsertRequest?> Capture(WindowsPickerPasteService service) => () =>
    {
        var target = service.CaptureExternalForeground();
        return target is null ? null : new WindowsPickerInsertRequest(service, target);
    };

    [Fact]
    public void RepeatedHotkeyInActivePickerPreservesOriginalTargetAndGeneration()
    {
        var native = new FakeWindows();
        var service = Service(native);
        var sessions = new PickerSessionController();
        Assert.Equal(PickerSessionOpenResult.Started, sessions.Open(false, false, Capture(service)));
        var original = Assert.IsType<WindowsPickerInsertRequest>(sessions.Request);
        var version = sessions.Generation;
        native.Foreground = 0x900; // The picker now has focus, not the external editor.
        Assert.Equal(PickerSessionOpenResult.Reused, sessions.Open(true, true,
            () => throw new InvalidOperationException("Must not recapture picker")));
        Assert.Same(original, sessions.Request);
        Assert.True(sessions.IsCurrent(version, original));
        Assert.Equal(version, sessions.Generation);
    }

    [Fact]
    public async Task ReopeningFromAnotherSessionDuringLastPreflightPreventsOldDispatch()
    {
        var native = new FakeWindows();
        var service = Service(native);
        var sessions = new PickerSessionController();
        sessions.Open(false, false, Capture(service));
        var oldRequest = Assert.IsType<WindowsPickerInsertRequest>(sessions.Request);
        var oldGeneration = sessions.Generation;
        var modifierChecks = 0;
        native.BeforeModifierCheck = () =>
        {
            if (++modifierChecks != 2) return; // final preflight in Paste
            Assert.Equal(PickerSessionOpenResult.Started,
                sessions.Open(true, false, Capture(service)));
        };
        var oldResult = await oldRequest.ExecuteAsync(() => Task.FromResult(native.Clipboard),
            () => Task.CompletedTask);
        Assert.Equal(PickerPasteStatus.Rejected, oldResult.Status);
        Assert.Equal("request_cancelled", oldResult.Reason);
        Assert.False(sessions.IsCurrent(oldGeneration, oldRequest));
        Assert.Equal(0, native.Sends);

        // New captured session is independent and may be used only once.
        native.BeforeModifierCheck = null;
        var currentRequest = Assert.IsType<WindowsPickerInsertRequest>(sessions.Request);
        Assert.NotSame(oldRequest, currentRequest);
        var generation = sessions.Generation;
        Assert.True(sessions.IsCurrent(generation, currentRequest));
        Assert.Equal(PickerPasteStatus.SubmittedUnverified,
            (await currentRequest.ExecuteAsync(() => Task.FromResult(native.Clipboard),
                () => Task.CompletedTask)).Status);
        Assert.Equal(1, native.Sends);
    }

    [Fact]
    public async Task RepeatedHotkeyDoesNotDisableInsertIntoOriginalEditor()
    {
        var native = new FakeWindows();
        var service = Service(native);
        var sessions = new PickerSessionController();
        sessions.Open(false, false, Capture(service));
        native.Foreground = 0x900;
        sessions.Open(true, true, Capture(service));
        var request = Assert.IsType<WindowsPickerInsertRequest>(sessions.Request);
        var result = await request.ExecuteAsync(() => Task.FromResult(native.Clipboard), () => Task.CompletedTask);
        Assert.Equal(PickerPasteStatus.SubmittedUnverified, result.Status);
        Assert.Equal(1, native.Sends);
    }

    [Fact]
    public async Task SwitchingFromOneEditorToAnotherCancelsOriginalRequest()
    {
        var native = new FakeWindows();
        var service = Service(native);
        var sessions = new PickerSessionController();
        sessions.Open(false, false, Capture(service));
        var old = Assert.IsType<WindowsPickerInsertRequest>(sessions.Request);
        var oldVersion = sessions.Generation;
        native.Root = 0x200;
        native.Focus = 0x201;
        native.Process = 43;
        native.Foreground = native.Root;
        Assert.Equal(PickerSessionOpenResult.Started, sessions.Open(true, false, Capture(service)));
        var current = Assert.IsType<WindowsPickerInsertRequest>(sessions.Request);
        Assert.NotSame(old, current);
        Assert.False(sessions.IsCurrent(oldVersion, old));
        Assert.Equal("request_cancelled", (await old.ExecuteAsync(() => Task.FromResult(17u),
            () => Task.CompletedTask)).Reason);
        Assert.Equal(PickerPasteStatus.SubmittedUnverified,
            (await current.ExecuteAsync(() => Task.FromResult(17u), () => Task.CompletedTask)).Status);
        Assert.Equal(1, native.Sends);
    }

    [Fact]
    public async Task SwitchingToOwnProcessClearsOldInsertPermission()
    {
        var native = new FakeWindows();
        var service = Service(native);
        var sessions = new PickerSessionController();
        sessions.Open(false, false, Capture(service));
        var old = Assert.IsType<WindowsPickerInsertRequest>(sessions.Request);
        native.Process = 900;
        sessions.Open(true, false, Capture(service));
        Assert.Null(sessions.Request);
        Assert.Equal("request_cancelled", (await old.ExecuteAsync(() => Task.FromResult(17u),
            () => Task.CompletedTask)).Reason);
        Assert.Equal(0, native.Sends);
    }

    [Fact]
    public void NewUnqualifiedSessionInvalidatesPriorNullSessionGeneration()
    {
        var native = new FakeWindows { Process = 900 };
        var sessions = new PickerSessionController();
        var capture = Capture(Service(native));
        sessions.Open(false, false, capture);
        var priorGeneration = sessions.Generation;
        Assert.Null(sessions.Request);
        sessions.Open(false, false, capture);
        Assert.Null(sessions.Request);
        Assert.False(sessions.IsCurrent(priorGeneration));
    }

    [Fact]
    public async Task DismissalDuringFocusWaitPreventsOldPaste()
    {
        var native = new FakeWindows();
        var service = Service(native);
        var sessions = new PickerSessionController();
        sessions.Open(false, false, Capture(service));
        var request = Assert.IsType<WindowsPickerInsertRequest>(sessions.Request);
        var wait = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = request.ExecuteAsync(() => Task.FromResult(17u), () => wait.Task);
        sessions.Cancel();
        wait.SetResult();
        Assert.Equal("request_cancelled", (await pending).Reason);
        Assert.Null(sessions.Request);
        Assert.Equal(0, native.Sends);
    }

    [Fact]
    public async Task OldAsyncCompletionCannotBeConsideredCurrentAfterReopening()
    {
        var native = new FakeWindows();
        var service = Service(native);
        var sessions = new PickerSessionController();
        sessions.Open(false, false, Capture(service));
        var request = Assert.IsType<WindowsPickerInsertRequest>(sessions.Request);
        var generation = sessions.Generation;
        var wait = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = request.ExecuteAsync(() => wait.Task, () => Task.CompletedTask);
        sessions.Open(false, false, Capture(service));
        wait.SetResult(17u);
        Assert.Equal("request_cancelled", (await operation).Reason);
        Assert.False(sessions.IsCurrent(generation, request));
        Assert.NotSame(request, sessions.Request);
        Assert.Equal(0, native.Sends);
    }

    [Fact]
    public async Task ChangedFocusAtDispatchIsRejectedWithoutSending()
    {
        var native = new FakeWindows();
        var service = Service(native);
        var sessions = new PickerSessionController();
        sessions.Open(false, false, Capture(service));
        var request = Assert.IsType<WindowsPickerInsertRequest>(sessions.Request);
        var result = await request.ExecuteAsync(() => Task.FromResult(17u), () =>
        {
            native.Focus = 0x102;
            return Task.CompletedTask;
        });
        Assert.Equal(PickerPasteStatus.Rejected, result.Status);
        Assert.Equal(0, native.Sends);
    }

    [Fact]
    public async Task ClipboardMutationWhileWaitingForFocusIsRejectedBeforePreparingSnippet()
    {
        var native = new FakeWindows();
        var service = Service(native);
        var sessions = new PickerSessionController();
        sessions.Open(false, false, Capture(service));
        var request = Assert.IsType<WindowsPickerInsertRequest>(sessions.Request);
        var result = await request.ExecuteAsync(() => Task.FromResult(17u), () =>
        {
            native.Clipboard = 18;
            return Task.CompletedTask;
        });
        Assert.Equal("clipboard_changed_before_preparation", result.Reason);
        Assert.Equal(0, native.Sends);
    }

    [Fact]
    public async Task PartialPasteLeavesRequestConsumed()
    {
        var native = new FakeWindows { AcceptedInputs = 2 };
        var service = Service(native);
        var sessions = new PickerSessionController();
        sessions.Open(false, false, Capture(service));
        var request = Assert.IsType<WindowsPickerInsertRequest>(sessions.Request);
        Assert.Equal(PickerPasteStatus.PartialOrUnknown,
            (await request.ExecuteAsync(() => Task.FromResult(17u), () => Task.CompletedTask)).Status);
        Assert.Equal("request_already_used",
            (await request.ExecuteAsync(() => Task.FromResult(17u), () => Task.CompletedTask)).Reason);
        Assert.Equal(1, native.Sends);
    }

    [Fact]
    public void ReusingActivePickerWithoutExternalTargetDoesNotAttemptToRecapture()
    {
        var native = new FakeWindows { Process = 900 };
        var sessions = new PickerSessionController();
        sessions.Open(false, false, Capture(Service(native)));
        var generation = sessions.Generation;
        Assert.Null(sessions.Request);
        Assert.Equal(PickerSessionOpenResult.Reused,
            sessions.Open(true, true, () => throw new InvalidOperationException("recapture")));
        Assert.True(sessions.IsCurrent(generation));
        Assert.Null(sessions.Request);
    }

    [Fact]
    public void ReopenFromExternalEditorAfterNoTargetCanAcquireAnInsertSession()
    {
        var native = new FakeWindows { Process = 900 };
        var service = Service(native);
        var sessions = new PickerSessionController();
        sessions.Open(false, false, Capture(service));
        var generation = sessions.Generation;
        native.Process = 44;
        Assert.Equal(PickerSessionOpenResult.Started, sessions.Open(true, false, Capture(service)));
        Assert.NotNull(sessions.Request);
        Assert.False(sessions.IsCurrent(generation));
    }

    [Fact]
    public void CaptureExceptionsDoNotLeaveAnOldTargetAvailable()
    {
        var native = new FakeWindows();
        var sessions = new PickerSessionController();
        sessions.Open(false, false, Capture(Service(native)));
        var oldGeneration = sessions.Generation;
        Assert.Equal(PickerSessionOpenResult.Started, sessions.Open(true, false,
            () => throw new InvalidOperationException("test capture error")));
        Assert.Null(sessions.Request);
        Assert.False(sessions.IsCurrent(oldGeneration));
    }

    [Fact]
    public async Task LateHotkeyCannotCancelInsertionAlreadyStartedByEnter()
    {
        var native = new FakeWindows();
        var service = Service(native);
        var sessions = new PickerSessionController();
        sessions.Open(false, false, Capture(service));
        var request = Assert.IsType<WindowsPickerInsertRequest>(sessions.Request);
        var generation = sessions.Generation;
        Assert.True(sessions.TryBeginInsertion(generation, request));
        Assert.False(sessions.TryBeginInsertion(generation, request));

        // Simulate the picker having been hidden for the active Insert, then a
        // delayed global-hotkey callback arriving on the UI dispatcher.
        var captureCalls = 0;
        Assert.Equal(PickerSessionOpenResult.InsertionInProgress,
            sessions.Open(false, false, () => { captureCalls++; return null; }));
        Assert.Equal(0, captureCalls);
        Assert.True(sessions.IsCurrent(generation, request));
        Assert.Same(request, sessions.Request);
        Assert.Equal(PickerPasteStatus.SubmittedUnverified,
            (await request.ExecuteAsync(() => Task.FromResult(native.Clipboard),
                () => Task.CompletedTask)).Status);
        Assert.Equal(1, native.Sends);
        sessions.CompleteInsertion(generation, request);
        Assert.False(sessions.IsInsertionInProgress);
    }

    [Fact]
    public async Task ExplicitCancellationStillWinsEvenWithInsertionInProgress()
    {
        var native = new FakeWindows();
        var service = Service(native);
        var sessions = new PickerSessionController();
        sessions.Open(false, false, Capture(service));
        var request = Assert.IsType<WindowsPickerInsertRequest>(sessions.Request);
        var generation = sessions.Generation;
        Assert.True(sessions.TryBeginInsertion(generation, request));
        sessions.Cancel();
        Assert.False(sessions.IsInsertionInProgress);
        Assert.Equal("request_cancelled",
            (await request.ExecuteAsync(() => Task.FromResult(native.Clipboard),
                () => Task.CompletedTask)).Reason);
        Assert.Equal(0, native.Sends);
        Assert.False(sessions.IsCurrent(generation, request));
    }

    [Fact]
    public void OldInsertCompletionCannotClearAnActiveNewSession()
    {
        var native = new FakeWindows();
        var service = Service(native);
        var sessions = new PickerSessionController();
        sessions.Open(false, false, Capture(service));
        var oldRequest = Assert.IsType<WindowsPickerInsertRequest>(sessions.Request);
        var oldGeneration = sessions.Generation;
        Assert.True(sessions.TryBeginInsertion(oldGeneration, oldRequest));
        sessions.Cancel();
        sessions.Open(false, false, Capture(service));
        var fresh = Assert.IsType<WindowsPickerInsertRequest>(sessions.Request);
        var freshGeneration = sessions.Generation;
        Assert.True(sessions.TryBeginInsertion(freshGeneration, fresh));
        sessions.CompleteInsertion(oldGeneration, oldRequest);
        Assert.True(sessions.IsInsertionInProgress);
        sessions.CompleteInsertion(freshGeneration, fresh);
        Assert.False(sessions.IsInsertionInProgress);
    }
}
