using System.Reflection;
using System.Runtime.InteropServices;
using TextExpander.Infrastructure.Platform.Windows;
using Xunit;

namespace TextExpander.Tests;

public sealed class WindowsPickerPasteTests
{
    [Fact]
    public void NativeFocusStructureMatchesWindowsAbi()
    {
        // GUITHREADINFO has two DWORDs, six HWNDs, then a RECT. A missing HWND
        // makes cbSize invalid, so GetGUIThreadInfo cannot capture an editor.
        var api = typeof(WindowsPickerPasteService).Assembly.GetType(
            "TextExpander.Infrastructure.Platform.Windows.Win32PickerPasteApi", throwOnError: true)!;
        var info = api.GetNestedType("GuiThreadInfo", BindingFlags.NonPublic)!;
        Assert.NotNull(info);
        Assert.Equal(24 + 6 * IntPtr.Size, Marshal.SizeOf(info));
        Assert.Equal(8 + IntPtr.Size, Marshal.OffsetOf(info, "Focus").ToInt32());
        Assert.Equal(8 + 6 * IntPtr.Size, Marshal.OffsetOf(info, "CaretRect").ToInt32());
    }

    private sealed class FakeApi : IWindowsPickerPasteApi
    {
        public nint Root { get; set; } = 0x100;
        public nint Focus { get; set; } = 0x200;
        public nint CurrentForeground { get; set; } = 0x100;
        public uint Process { get; set; } = 123;
        public bool Visible { get; set; } = true;
        public bool FocusAvailable { get; set; } = true;
        public uint? FocusProcessId { get; set; }
        public bool IsChild { get; set; } = true;
        public HashSet<nint> Children { get; } = [0x200, 0x333];
        public bool Protected { get; set; }
        public bool ActivateWorks { get; set; } = true;
        public bool ModifiersPressed { get; set; }
        public uint Sequence { get; set; } = 17;
        public int Accepted { get; set; } = 4;
        public Action? BeforeFinalClipboardRead { get; set; }
        public Action? BeforeModifierCheck { get; set; }
        public Action? DuringSend { get; set; }
        public int Sent { get; private set; }
        public int Activations { get; private set; }
        public string? ThrowAt { get; set; }
        public Exception Failure { get; set; } = new InvalidOperationException("Synthetic native failure");

        private void MaybeFail(string operation)
        {
            if (ThrowAt == operation) throw Failure;
        }

        public nint ForegroundWindow() => CurrentForeground;
        public uint WindowProcessId(nint hwnd) => hwnd == Root ? Process :
            Children.Contains(hwnd) ? (FocusProcessId ?? Process) : 0;
        public nint FocusedControl(nint hwnd) => hwnd == Root ? Focus : 0;
        public bool IsWindowAvailable(nint hwnd)
        {
            MaybeFail("validate");
            return Visible && (hwnd == Root || (FocusAvailable && Children.Contains(hwnd)));
        }
        public bool IsSameOrChild(nint root, nint candidate) =>
            root == Root && (candidate == root || (Children.Contains(candidate) && IsChild));
        public bool IsProtectedEditor(nint focused) => Protected;
        public bool ActivateWindow(nint hwnd)
        {
            Activations++;
            MaybeFail("activate");
            if (ActivateWorks) CurrentForeground = hwnd;
            return ActivateWorks;
        }
        public bool IsAnyModifierPressed()
        {
            BeforeModifierCheck?.Invoke();
            return ModifiersPressed;
        }
        public uint ClipboardSequence()
        {
            BeforeFinalClipboardRead?.Invoke();
            return Sequence;
        }
        public int SendPasteShortcut()
        {
            Sent++;
            DuringSend?.Invoke();
            MaybeFail("send");
            return Accepted;
        }
    }

    [Theory]
    [InlineData(12u, 13u, true)]
    [InlineData(12u, 12u, false)]
    [InlineData(0u, 15u, false)]
    [InlineData(15u, 0u, false)]
    [InlineData(0u, 0u, false)]
    [InlineData(4294967295u, 1u, true)]
    public void ClipboardMustActuallyAdvanceBeforeAPasteIsPrepared(uint before, uint after, bool expected)
    {
        Assert.Equal(expected, WindowsPickerPasteService.HasClipboardAdvanced(before, after));
    }

    [Fact]
    public async Task ClipboardNotUpdatedNeverDispatchesPaste()
    {
        var fake = new FakeApi();
        var request = Request(fake);
        var result = await request.ExecuteAsync(() => Task.FromResult(
            WindowsPickerPasteService.HasClipboardAdvanced(fake.Sequence, fake.Sequence)
                ? fake.Sequence : 0u), () => Task.CompletedTask);
        Assert.Equal(PickerPasteStatus.Rejected, result.Status);
        Assert.Equal("clipboard_unverified", result.Reason);
        Assert.Equal(0, fake.Sent);
    }

    private static WindowsPickerPasteService Service(FakeApi api) => new(api, () => true, 99);

    private static WindowsPickerInsertRequest Request(FakeApi api)
    {
        var service = Service(api);
        return new(service, service.CaptureExternalForeground()!);
    }

    [Fact]
    public async Task RequestSettlesAndChecksFocusBeforePreparingClipboardThenSubmitsOnce()
    {
        var fake = new FakeApi();
        var request = Request(fake);
        fake.CurrentForeground = 0x999;
        var steps = new List<string>();
        var result = await request.ExecuteAsync(() =>
        {
            Assert.Equal(fake.Root, fake.CurrentForeground);
            steps.Add("clipboard");
            return Task.FromResult(fake.Sequence);
        }, () =>
        {
            steps.Add("focus");
            Assert.Equal(0, fake.Sent);
            return Task.CompletedTask;
        });
        Assert.Equal(new[] { "focus", "clipboard" }, steps);
        Assert.Equal(PickerPasteStatus.SubmittedUnverified, result.Status);
        Assert.Equal(1, fake.Sent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task CompletedRequestCannotBeReusedEvenAfterPartialInput(int accepted)
    {
        var fake = new FakeApi { Accepted = accepted };
        var request = Request(fake);
        var writes = 0;
        Task<uint> Clipboard() { writes++; return Task.FromResult(fake.Sequence); }
        var first = await request.ExecuteAsync(Clipboard, () => Task.CompletedTask);
        Assert.Equal(accepted == 4 ? PickerPasteStatus.SubmittedUnverified : PickerPasteStatus.PartialOrUnknown,
            first.Status);
        Assert.Equal("request_already_used", (await request.ExecuteAsync(Clipboard, () => Task.CompletedTask)).Reason);
        Assert.Equal(1, writes);
        Assert.Equal(1, fake.Activations);
        Assert.Equal(1, fake.Sent);
    }

    [Fact]
    public async Task ActivationRefusalDoesNotTouchClipboardAndConsumesRequest()
    {
        var fake = new FakeApi { ActivateWorks = false };
        var request = Request(fake);
        Task<uint> Clipboard() => throw new Xunit.Sdk.XunitException("Clipboard must not be entered");
        var result = await request.ExecuteAsync(Clipboard, () => Task.CompletedTask);
        Assert.Equal("activation_failed", result.Reason);
        Assert.Equal("request_already_used", (await request.ExecuteAsync(Clipboard, () => Task.CompletedTask)).Reason);
        Assert.Equal(0, fake.Sent);
    }

    [Fact]
    public async Task CancelBeforeClickHasNoNativeOrClipboardEffects()
    {
        var fake = new FakeApi();
        var request = Request(fake);
        request.Cancel();
        var writes = 0;
        var result = await request.ExecuteAsync(() => { writes++; return Task.FromResult(17u); }, () => Task.CompletedTask);
        Assert.Equal("request_cancelled", result.Reason);
        Assert.Equal(0, writes);
        Assert.Equal(0, fake.Activations);
        Assert.Equal(0, fake.Sent);
    }

    [Fact]
    public async Task DismissalDuringClipboardPreparationPreventsInput()
    {
        var fake = new FakeApi();
        var request = Request(fake);
        var clipboard = new TaskCompletionSource<uint>();
        var waits = 0;
        var pending = request.ExecuteAsync(() => clipboard.Task, () => { waits++; return Task.CompletedTask; });
        request.Cancel();
        clipboard.SetResult(17);
        Assert.Equal("request_cancelled", (await pending).Reason);
        Assert.Equal(1, waits);
        Assert.Equal(0, fake.Sent);
    }

    [Fact]
    public async Task CancelDuringFocusWaitPreventsInput()
    {
        var fake = new FakeApi();
        var request = Request(fake);
        var focus = new TaskCompletionSource();
        var writes = 0;
        var pending = request.ExecuteAsync(() => { writes++; return Task.FromResult(17u); }, () => focus.Task);
        request.Cancel();
        focus.SetResult();
        Assert.Equal("request_cancelled", (await pending).Reason);
        Assert.Equal(0, writes);
        Assert.Equal(0, fake.Sent);
    }

    [Fact]
    public async Task ConcurrentClickCannotStartASecondClipboardWrite()
    {
        var fake = new FakeApi();
        var request = Request(fake);
        var clipboard = new TaskCompletionSource<uint>();
        var writes = 0;
        Task<uint> Prepare() { writes++; return clipboard.Task; }
        var first = request.ExecuteAsync(Prepare, () => Task.CompletedTask);
        Assert.Equal("request_already_used", (await request.ExecuteAsync(Prepare, () => Task.CompletedTask)).Reason);
        clipboard.SetResult(17);
        Assert.Equal(PickerPasteStatus.SubmittedUnverified, (await first).Status);
        Assert.Equal(1, writes);
        Assert.Equal(1, fake.Activations);
        Assert.Equal(1, fake.Sent);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClipboardOrFocusWaitFailureIsRejectedWithoutInput(bool failInFocusWait)
    {
        var fake = new FakeApi();
        var request = Request(fake);
        var result = await request.ExecuteAsync(
            () => failInFocusWait ? Task.FromResult(17u) : Task.FromException<uint>(new IOException()),
            () => failInFocusWait ? Task.FromException(new IOException()) : Task.CompletedTask);
        Assert.Equal(PickerPasteStatus.Rejected, result.Status);
        Assert.Equal("preparation_failed", result.Reason);
        Assert.Equal(0, fake.Sent);
    }

    [Fact]
    public async Task CancellationDuringFinalFocusPreflightWinsBeforeNativeDispatch()
    {
        var fake = new FakeApi();
        var request = Request(fake);
        // The request has passed all async boundaries. A native callback
        // cancels just BEFORE the last clipboard comparison and SendInput.
        var checks = 0;
        fake.BeforeModifierCheck = () =>
        {
            if (++checks == 2) request.Cancel(); // first: pre-clipboard, second: final Paste
        };
        var result = await request.ExecuteAsync(() => Task.FromResult(fake.Sequence),
            () => Task.CompletedTask);
        Assert.Equal(PickerPasteStatus.Rejected, result.Status);
        Assert.Equal("request_cancelled", result.Reason);
        Assert.Equal(0, fake.Sent);
        Assert.Equal("request_already_used", (await request.ExecuteAsync(
            () => Task.FromResult(fake.Sequence), () => Task.CompletedTask)).Reason);
    }

    [Fact]
    public async Task CancellationDuringFinalClipboardReadCannotDispatch()
    {
        var fake = new FakeApi();
        var request = Request(fake);
        var reads = 0;
        fake.BeforeFinalClipboardRead = () =>
        {
            // before-focus, still-at-before-focus, final Paste clipboard check
            if (++reads == 3) request.Cancel();
        };
        var result = await request.ExecuteAsync(() => Task.FromResult(fake.Sequence),
            () => Task.CompletedTask);
        Assert.Equal("request_cancelled", result.Reason);
        Assert.Equal(PickerPasteStatus.Rejected, result.Status);
        Assert.Equal(0, fake.Sent);
    }

    [Theory]
    [InlineData(4, PickerPasteStatus.SubmittedUnverified)]
    [InlineData(2, PickerPasteStatus.PartialOrUnknown)]
    public async Task CancellationAfterDispatchReservationNeverRetriesOrClaimsRevocation(
        int accepted, PickerPasteStatus expected)
    {
        var fake = new FakeApi { Accepted = accepted };
        var request = Request(fake);
        fake.DuringSend = request.Cancel; // SendInput has already been entered.
        var result = await request.ExecuteAsync(() => Task.FromResult(fake.Sequence),
            () => Task.CompletedTask);
        Assert.Equal(expected, result.Status);
        Assert.Equal(1, fake.Sent);
        Assert.Equal("request_already_used", (await request.ExecuteAsync(
            () => Task.FromResult(fake.Sequence), () => Task.CompletedTask)).Reason);
        Assert.Equal(1, fake.Sent);
    }

    [Fact]
    public async Task CancellationDuringPreflightDoesNotGetReportedAsAnUnknownPaste()
    {
        var fake = new FakeApi();
        var request = Request(fake);
        var checks = 0;
        fake.BeforeModifierCheck = () =>
        {
            if (++checks == 2) request.Cancel();
        };
        var result = await request.ExecuteAsync(() => Task.FromResult(fake.Sequence),
            () => Task.CompletedTask);
        Assert.Equal(PickerPasteStatus.Rejected, result.Status);
        Assert.NotEqual("native_error_unknown_effect", result.Reason);
        Assert.Equal(0, fake.Sent);
    }

    [Fact]
    public async Task UnverifiedClipboardWriteRejectsWithoutDispatch()
    {
        var fake = new FakeApi();
        var request = Request(fake);
        var waits = 0;
        var result = await request.ExecuteAsync(() => Task.FromResult(0u), () => { waits++; return Task.CompletedTask; });
        Assert.Equal("clipboard_unverified", result.Reason);
        Assert.Equal(1, waits);
        Assert.Equal(0, fake.Sent);
    }

    [Fact]
    public async Task ClipboardChangedDuringFocusWaitIsStillRejected()
    {
        var fake = new FakeApi();
        var request = Request(fake);
        var result = await request.ExecuteAsync(() => Task.FromResult(fake.Sequence), () =>
        {
            fake.Sequence++;
            return Task.CompletedTask;
        });
        Assert.Equal("clipboard_changed_before_preparation", result.Reason);
        Assert.Equal(0, fake.Sent);
    }

    [Fact]
    public async Task UnavailableInitialClipboardSequenceNeverTouchesClipboardOrWaits()
    {
        var native = new FakeApi { Sequence = 0 };
        var request = Request(native);
        var writes = 0;
        var waits = 0;
        var result = await request.ExecuteAsync(() => { writes++; return Task.FromResult(1u); }, () =>
        {
            waits++;
            return Task.CompletedTask;
        });
        Assert.Equal("clipboard_unverified", result.Reason);
        Assert.Equal(0, writes);
        Assert.Equal(0, waits);
        Assert.Equal(0, native.Sent);
    }

    [Fact]
    public async Task NativeFailureAtPreClipboardPreflightRejectsWithoutClipboardWrite()
    {
        var native = new FakeApi();
        var request = Request(native);
        var writes = 0;
        var result = await request.ExecuteAsync(() => { writes++; return Task.FromResult(18u); }, () =>
        {
            native.ThrowAt = "validate";
            return Task.CompletedTask;
        });
        Assert.Equal("native_preflight_failed", result.Reason);
        Assert.Equal(0, writes);
        Assert.Equal(0, native.Sent);
    }

    [Fact]
    public async Task FocusMovedToAnotherAppDuringRestoreDoesNotReplaceClipboard()
    {
        var native = new FakeApi();
        var request = Request(native);
        var writes = 0;
        var result = await request.ExecuteAsync(() => { writes++; return Task.FromResult(19u); }, () =>
        {
            native.CurrentForeground = 0x999;
            return Task.CompletedTask;
        });
        Assert.Equal("focus_changed", result.Reason);
        Assert.Equal(0, writes);
        Assert.Equal(0, native.Sent);
    }

    [Fact]
    public async Task FocusChangedWithinSameEditorBeforeClipboardDoesNotReplaceClipboard()
    {
        var native = new FakeApi();
        var request = Request(native);
        var writes = 0;
        var result = await request.ExecuteAsync(() => { writes++; return Task.FromResult(19u); }, () =>
        {
            native.Focus = 0x333;
            return Task.CompletedTask;
        });
        Assert.Equal("focus_changed", result.Reason);
        Assert.Equal(0, writes);
        Assert.Equal(0, native.Sent);
    }

    [Theory]
    [InlineData("closed")]
    [InlineData("different_process")]
    [InlineData("protected")]
    [InlineData("modifier")]
    public async Task InvalidTargetAfterFocusTransitionNeverTouchesClipboard(string caseName)
    {
        var native = new FakeApi();
        var request = Request(native);
        var writes = 0;
        var result = await request.ExecuteAsync(() => { writes++; return Task.FromResult(18u); }, () =>
        {
            switch (caseName)
            {
                case "closed": native.FocusAvailable = false; break;
                case "different_process": native.FocusProcessId = 555; break;
                case "protected": native.Protected = true; break;
                case "modifier": native.ModifiersPressed = true; break;
            }
            return Task.CompletedTask;
        });
        Assert.Equal(caseName switch
        {
            "closed" or "different_process" => "target_unavailable",
            "protected" => "protected_editor",
            _ => "modifier_pressed"
        }, result.Reason);
        Assert.Equal(0, writes);
        Assert.Equal(0, native.Sent);
    }

    [Fact]
    public async Task ConcurrentClipboardUpdateDuringFocusTransitionIsNotOverwritten()
    {
        var native = new FakeApi { Sequence = 100 };
        var request = Request(native);
        var writes = 0;
        var result = await request.ExecuteAsync(() => { writes++; return Task.FromResult(102u); }, () =>
        {
            native.Sequence = 101;
            return Task.CompletedTask;
        });
        Assert.Equal("clipboard_changed_before_preparation", result.Reason);
        Assert.Equal(0, writes);
        Assert.Equal(0, native.Sent);
    }

    [Fact]
    public async Task FocusLostWhileClipboardIsWritingNeverDispatches()
    {
        var native = new FakeApi();
        var request = Request(native);
        var writing = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = request.ExecuteAsync(() => writing.Task, () => Task.CompletedTask);
        native.CurrentForeground = 0x999;
        writing.SetResult(17);
        var result = await pending;
        Assert.Equal("focus_changed", result.Reason);
        Assert.Equal(0, native.Sent);
    }

    [Fact]
    public async Task ClipboardLostWhileWritingNeverDispatches()
    {
        var native = new FakeApi();
        var request = Request(native);
        var writing = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = request.ExecuteAsync(() => writing.Task, () => Task.CompletedTask);
        native.Sequence = 44;
        writing.SetResult(17);
        var result = await pending;
        Assert.Equal("clipboard_changed", result.Reason);
        Assert.Equal(0, native.Sent);
    }

    [Fact]
    public async Task UnexpectedDispatchExceptionIsUnknownAndCannotBeRetried()
    {
        var fake = new FakeApi();
        var request = Request(fake);
        fake.ThrowAt = "send";
        fake.Failure = new IOException();
        var result = await request.ExecuteAsync(() => Task.FromResult(17u), () => Task.CompletedTask);
        Assert.Equal(PickerPasteStatus.PartialOrUnknown, result.Status);
        Assert.Equal("request_already_used",
            (await request.ExecuteAsync(() => Task.FromResult(17u), () => Task.CompletedTask)).Reason);
        Assert.Equal(1, fake.Sent);
    }

    [Fact]
    public async Task ReplacingAPickerSessionCancelsOnlyItsOwnPendingRequest()
    {
        var fake = new FakeApi();
        var oldRequest = Request(fake);
        var focus = new TaskCompletionSource();
        var oldPending = oldRequest.ExecuteAsync(() => Task.FromResult(17u), () => focus.Task);
        oldRequest.Cancel();
        var nextRequest = Request(fake);
        var nextResult = await nextRequest.ExecuteAsync(() => Task.FromResult(17u), () => Task.CompletedTask);
        focus.SetResult();
        Assert.Equal(PickerPasteStatus.SubmittedUnverified, nextResult.Status);
        Assert.Equal("request_cancelled", (await oldPending).Reason);
        Assert.Equal(1, fake.Sent);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeValidationErrorsAreContainedBeforeAnyInput(bool win32Exception)
    {
        var fake = new FakeApi();
        var service = Service(fake);
        var target = service.CaptureExternalForeground();
        fake.ThrowAt = "validate";
        if (win32Exception) fake.Failure = new System.ComponentModel.Win32Exception(5);
        Assert.Null(service.CaptureExternalForeground());
        Assert.False(service.TryActivate(target));
        var result = service.Paste(target, 17);
        Assert.Equal(PickerPasteStatus.Rejected, result.Status);
        Assert.Equal("native_preflight_failed", result.Reason);
        Assert.Equal(0, fake.Sent);
        Assert.Equal(0, fake.Activations);
    }

    [Fact]
    public void ErrorDuringNativeDispatchHasUnknownEffectAndNoRetry()
    {
        var fake = new FakeApi();
        var service = Service(fake);
        var target = service.CaptureExternalForeground();
        fake.ThrowAt = "send";
        Assert.Equal(PickerPasteStatus.PartialOrUnknown, service.Paste(target, 17).Status);
        Assert.Equal(1, fake.Sent);
    }

    [Fact]
    public void CaptureStoresOriginalWindowAndFocusedControl()
    {
        var fake = new FakeApi();
        Assert.Equal(new WindowsPickerPasteTarget(fake.Root, fake.Process, fake.Focus),
            Service(fake).CaptureExternalForeground());
    }

    [Fact]
    public void NeverCaptureOwnProcess()
    {
        var fake = new FakeApi { Process = 99 };
        Assert.Null(Service(fake).CaptureExternalForeground());
    }

    [Theory]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(true, true, true, true)]
    public void CaptureRejectsNonEditorContexts(bool visible, bool child, bool hasFocus, bool protectedControl)
    {
        var fake = new FakeApi
        {
            Visible = visible, IsChild = child,
            Focus = hasFocus ? 0x200 : 0,
            Protected = protectedControl
        };
        Assert.Null(Service(fake).CaptureExternalForeground());
    }

    [Fact]
    public void CaptureRejectsFocusedControlFromDifferentProcess()
    {
        var fake = new FakeApi { FocusProcessId = 444 };
        Assert.Null(Service(fake).CaptureExternalForeground());
        Assert.Equal(0, fake.Sent);
    }

    [Fact]
    public void CaptureRejectsUnavailableFocusedControl()
    {
        var fake = new FakeApi { FocusAvailable = false };
        Assert.Null(Service(fake).CaptureExternalForeground());
        Assert.Equal(0, fake.Sent);
    }

    [Fact]
    public void FocusedControlDestroyedAfterCapturePreventsActivationAndPaste()
    {
        var fake = new FakeApi();
        var service = Service(fake);
        var target = service.CaptureExternalForeground();
        fake.FocusAvailable = false;
        Assert.False(service.TryActivate(target));
        Assert.Equal("target_unavailable", service.Paste(target, 17).Reason);
        Assert.Equal(0, fake.Activations);
        Assert.Equal(0, fake.Sent);
    }

    [Fact]
    public void FocusedControlMovedToAnotherProcessAfterCapturePreventsInput()
    {
        var fake = new FakeApi();
        var service = Service(fake);
        var target = service.CaptureExternalForeground();
        fake.FocusProcessId = 555;
        Assert.False(service.TryActivate(target));
        Assert.Equal("target_unavailable", service.Paste(target, 17).Reason);
        Assert.Equal(0, fake.Sent);
    }

    [Fact]
    public async Task TargetClosedWhileClipboardWriteIsPendingCannotDispatch()
    {
        var fake = new FakeApi();
        var request = Request(fake);
        var clipboardWrite = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = request.ExecuteAsync(() => clipboardWrite.Task, () => Task.CompletedTask);
        fake.FocusAvailable = false;
        clipboardWrite.SetResult(18);
        var result = await pending;
        Assert.Equal("target_unavailable", result.Reason);
        Assert.Equal(0, fake.Sent);
    }

    [Fact]
    public async Task ClipboardReplacedByOtherAppDuringAsyncTransitionCannotDispatch()
    {
        var fake = new FakeApi { Sequence = 51 };
        var request = Request(fake);
        var wait = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = request.ExecuteAsync(() => Task.FromResult(51u), () => wait.Task);
        fake.Sequence = 52;
        wait.SetResult();
        Assert.Equal("clipboard_changed_before_preparation", (await pending).Reason);
        Assert.Equal(0, fake.Sent);
    }

    [Fact]
    public void ChangingProcessBeforeActivationPreventsWindowRestore()
    {
        var fake = new FakeApi();
        var target = Service(fake).CaptureExternalForeground();
        fake.Process = 124;
        Assert.False(Service(fake).TryActivate(target));
        Assert.Equal(0, fake.Activations);
    }

    [Fact]
    public void CanRestoreExactlyTheCapturedWindow()
    {
        var fake = new FakeApi { CurrentForeground = 0x999 };
        var target = new WindowsPickerPasteTarget(fake.Root, fake.Process, fake.Focus);
        Assert.True(Service(fake).TryActivate(target));
        Assert.Equal(fake.Root, fake.CurrentForeground);
    }

    [Fact]
    public void PasteSubmitsOnceButNeverClaimsVerifiedDocumentContents()
    {
        var fake = new FakeApi();
        var target = Service(fake).CaptureExternalForeground();
        var result = Service(fake).Paste(target, fake.Sequence);
        Assert.Equal(PickerPasteStatus.SubmittedUnverified, result.Status);
        Assert.Equal(1, fake.Sent);
    }

    [Fact]
    public void RejectsFocusChangeWithoutSendingAnyInput()
    {
        var fake = new FakeApi();
        var target = Service(fake).CaptureExternalForeground();
        fake.Focus = 0x333;
        Assert.Equal("focus_changed", Service(fake).Paste(target, 17).Reason);
        Assert.Equal(0, fake.Sent);
    }

    [Fact]
    public void RejectsForegroundChangeWithoutSendingAnyInput()
    {
        var fake = new FakeApi();
        var target = Service(fake).CaptureExternalForeground();
        fake.CurrentForeground = 0x999;
        Assert.Equal("focus_changed", Service(fake).Paste(target, 17).Reason);
        Assert.Equal(0, fake.Sent);
    }

    [Fact]
    public void RejectsDestroyedControlWithoutSendingAnyInput()
    {
        var fake = new FakeApi();
        var target = Service(fake).CaptureExternalForeground()!;
        fake.Children.Remove(target.FocusedControl);
        Assert.Equal("target_unavailable", Service(fake).Paste(target, 17).Reason);
        Assert.False(Service(fake).TryActivate(target));
        Assert.Equal(0, fake.Activations);
        Assert.Equal(0, fake.Sent);
    }

    [Fact]
    public void RejectsModifiedClipboardToPreventWrongTextPasting()
    {
        var fake = new FakeApi();
        var target = Service(fake).CaptureExternalForeground();
        fake.Sequence = 18;
        Assert.Equal("clipboard_changed", Service(fake).Paste(target, 17).Reason);
        Assert.Equal(0, fake.Sent);
    }

    [Fact]
    public void RejectsUnknownClipboardSequence()
    {
        var fake = new FakeApi();
        var target = Service(fake).CaptureExternalForeground();
        Assert.Equal("clipboard_unverified", Service(fake).Paste(target, 0).Reason);
        Assert.Equal(0, fake.Sent);
    }

    [Fact]
    public void RejectsHeldModifiersAndProtectedField()
    {
        var fake = new FakeApi();
        var target = Service(fake).CaptureExternalForeground();
        fake.ModifiersPressed = true;
        Assert.Equal("modifier_pressed", Service(fake).Paste(target, 17).Reason);
        fake.ModifiersPressed = false;
        fake.Protected = true;
        Assert.Equal("protected_editor", Service(fake).Paste(target, 17).Reason);
        Assert.Equal(0, fake.Sent);
    }

    [Fact]
    public void DoesNotRetryAPartialInput()
    {
        var fake = new FakeApi { Accepted = 2 };
        var target = Service(fake).CaptureExternalForeground();
        Assert.Equal(PickerPasteStatus.PartialOrUnknown, Service(fake).Paste(target, 17).Status);
        Assert.Equal(1, fake.Sent);
    }

    [Fact]
    public void UnsupportedPlatformNeverAccessesWindowsApi()
    {
        var fake = new FakeApi();
        var service = new WindowsPickerPasteService(fake, () => false, 99);
        Assert.Null(service.CaptureExternalForeground());
        Assert.False(service.TryActivate(new WindowsPickerPasteTarget(fake.Root, fake.Process, fake.Focus)));
        Assert.Equal(PickerPasteStatus.Rejected,
            service.Paste(new WindowsPickerPasteTarget(fake.Root, fake.Process, fake.Focus), 17).Status);
        Assert.Equal(0, fake.Sent);
    }
}
