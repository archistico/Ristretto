using Avalonia.Input;
using TextExpander.Application.Features.Hotkeys;
using TextExpander.Desktop;
using Xunit;

namespace TextExpander.Tests;

// Production capture state and pause/restore coordinator, without an Avalonia window or native hotkeys.
public sealed class HotkeyCaptureTests
{
    [Fact]
    public void AltCtrlShiftBRemainsVisibleAfterReleaseAndRequiresAllKeysUp()
    {
        var state = new HotkeyCaptureState();
        state.KeyDown(Key.LeftAlt, KeyModifiers.Alt);
        state.KeyDown(Key.LeftCtrl, KeyModifiers.Alt | KeyModifiers.Control);
        state.KeyDown(Key.LeftShift, KeyModifiers.Alt | KeyModifiers.Control | KeyModifiers.Shift);
        state.KeyDown(Key.B, KeyModifiers.Alt | KeyModifiers.Control | KeyModifiers.Shift);
        Assert.Equal("ALT + CTRL + SHIFT + B", state.DisplayText);
        Assert.Equal("Ctrl+Alt+Shift+B", state.Hotkey);
        Assert.False(state.CanConfirm);
        state.KeyUp(Key.B, KeyModifiers.Alt | KeyModifiers.Control | KeyModifiers.Shift);
        state.KeyUp(Key.LeftShift, KeyModifiers.Alt | KeyModifiers.Control);
        state.KeyUp(Key.LeftCtrl, KeyModifiers.Alt);
        Assert.False(state.CanConfirm);
        state.KeyUp(Key.LeftAlt, KeyModifiers.None);
        Assert.True(state.CanConfirm);
        Assert.Equal("ALT + CTRL + SHIFT + B", state.DisplayText);
    }

    [Theory]
    [InlineData(Key.Space, "CTRL + SPACE", "Ctrl+Space")]
    [InlineData(Key.D0, "CTRL + 0", "Ctrl+0")]
    [InlineData(Key.D9, "CTRL + 9", "Ctrl+9")]
    [InlineData(Key.F1, "CTRL + F1", "Ctrl+F1")]
    [InlineData(Key.F11, "CTRL + F11", "Ctrl+F11")]
    public void SupportedPrimaryKeysMatchTheExistingRegistrationContract(Key key, string display, string canonical)
    {
        var state = new HotkeyCaptureState();
        state.KeyDown(key, KeyModifiers.Control);
        Assert.Equal(display, state.DisplayText);
        Assert.Equal(canonical, state.Hotkey);
        state.KeyUp(key, KeyModifiers.None);
        Assert.True(state.CanConfirm);
    }

    [Theory]
    [InlineData(Key.Return)]
    [InlineData(Key.F12)]
    [InlineData(Key.NumPad1)]
    [InlineData(Key.Delete)]
    [InlineData(Key.Tab)]
    public void UnsupportedKeysAreShownButCannotBeConfirmed(Key key)
    {
        var state = new HotkeyCaptureState();
        state.KeyDown(key, KeyModifiers.Control);
        state.KeyUp(key, KeyModifiers.None);
        Assert.StartsWith("CTRL + ", state.DisplayText);
        Assert.True(state.IsInvalid);
        Assert.Null(state.Hotkey);
        Assert.False(state.CanConfirm);
    }

    [Theory]
    [InlineData(KeyModifiers.None)]
    [InlineData(KeyModifiers.Shift)]
    public void PlainTypingAndShiftOnlyCannotCreateAGlobalHotkey(KeyModifiers modifiers)
    {
        var state = new HotkeyCaptureState();
        state.KeyDown(Key.B, modifiers);
        state.KeyUp(Key.B, KeyModifiers.None);
        Assert.True(state.IsInvalid);
        Assert.Null(state.Hotkey);
        Assert.False(state.CanConfirm);
    }

    [Fact]
    public void ModifierOnlyGestureIsNotAHotkey()
    {
        var state = new HotkeyCaptureState();
        state.KeyDown(Key.LeftCtrl, KeyModifiers.Control);
        Assert.Equal("CTRL", state.DisplayText);
        Assert.Null(state.Hotkey);
        state.KeyUp(Key.LeftCtrl, KeyModifiers.None);
        Assert.False(state.CanConfirm);
        Assert.Empty(state.DisplayText);
    }

    [Fact]
    public void LeftAndRightModifiersAreCombinedOnceAndBothMustBeReleased()
    {
        var state = new HotkeyCaptureState();
        state.KeyDown(Key.LeftCtrl, KeyModifiers.Control);
        state.KeyDown(Key.RightCtrl, KeyModifiers.Control);
        state.KeyDown(Key.RightAlt, KeyModifiers.Alt | KeyModifiers.Control);
        state.KeyDown(Key.RWin, KeyModifiers.Alt | KeyModifiers.Control | KeyModifiers.Meta);
        state.KeyDown(Key.F, KeyModifiers.Alt | KeyModifiers.Control | KeyModifiers.Meta);
        Assert.Equal("ALT + CTRL + WIN + F", state.DisplayText);
        Assert.Equal("Ctrl+Alt+Win+F", state.Hotkey);
        state.KeyUp(Key.F, KeyModifiers.None);
        state.KeyUp(Key.LeftCtrl, KeyModifiers.None);
        state.KeyUp(Key.RightAlt, KeyModifiers.None);
        state.KeyUp(Key.RWin, KeyModifiers.None);
        Assert.False(state.CanConfirm);
        state.KeyUp(Key.RightCtrl, KeyModifiers.None);
        Assert.True(state.CanConfirm);
    }

    [Fact]
    public void RepeatedKeydownCannotDuplicateThePrimaryKeyOrQueueAnotherGesture()
    {
        var state = new HotkeyCaptureState();
        for (var i = 0; i < 10; i++) state.KeyDown(Key.B, KeyModifiers.Control);
        Assert.Equal("CTRL + B", state.DisplayText);
        Assert.False(state.IsInvalid);
        state.KeyUp(Key.B, KeyModifiers.Control);
        Assert.False(state.CanConfirm);
        state.KeyUp(Key.LeftCtrl, KeyModifiers.None);
        Assert.True(state.CanConfirm);
    }

    [Fact]
    public void MultiplePrimaryKeysRejectTheWholeGestureUntilRelease()
    {
        var state = new HotkeyCaptureState();
        state.KeyDown(Key.B, KeyModifiers.Control);
        state.KeyDown(Key.C, KeyModifiers.Control);
        Assert.True(state.IsInvalid);
        Assert.Null(state.Hotkey);
        state.KeyUp(Key.C, KeyModifiers.Control);
        state.KeyDown(Key.D, KeyModifiers.Control);
        Assert.True(state.IsInvalid);
        state.KeyUp(Key.B, KeyModifiers.Control);
        state.KeyUp(Key.D, KeyModifiers.None);
        state.KeyDown(Key.E, KeyModifiers.Alt);
        state.KeyUp(Key.E, KeyModifiers.None);
        Assert.Equal("Alt+E", state.Hotkey);
        Assert.True(state.CanConfirm);
    }

    [Fact]
    public void ANewGestureReplacesThePreviousCandidateAndFocusLossCanResetIt()
    {
        var state = new HotkeyCaptureState();
        state.KeyDown(Key.B, KeyModifiers.Control);
        state.KeyUp(Key.B, KeyModifiers.None);
        Assert.True(state.CanConfirm);
        state.KeyDown(Key.LeftAlt, KeyModifiers.Alt);
        Assert.Null(state.Hotkey);
        Assert.False(state.CanConfirm);
        state.KeyDown(Key.F, KeyModifiers.Alt);
        Assert.Equal("Alt+F", state.Hotkey);
        state.Reset();
        Assert.False(state.KeysHeld);
        Assert.False(state.CanConfirm);
        Assert.Empty(state.DisplayText);
        state.KeyUp(Key.F, KeyModifiers.None); // Late release from the lost focus cannot recreate a candidate.
        Assert.Null(state.Hotkey);
    }

    [Fact]
    public void ModifierChangeAfterPrimaryReleaseCannotConfirmAHiddenOldChord()
    {
        var state = new HotkeyCaptureState();
        state.KeyDown(Key.LeftCtrl, KeyModifiers.Control);
        state.KeyDown(Key.B, KeyModifiers.Control);
        state.KeyUp(Key.B, KeyModifiers.Control);
        state.KeyDown(Key.LeftAlt, KeyModifiers.Control | KeyModifiers.Alt);
        Assert.Equal("ALT + CTRL", state.DisplayText);
        Assert.Null(state.Hotkey);
        state.KeyUp(Key.LeftAlt, KeyModifiers.Control);
        state.KeyUp(Key.LeftCtrl, KeyModifiers.None);
        Assert.False(state.CanConfirm);
    }

    [Fact]
    public void AnotherPrimaryKeyCanCorrectAnUnsupportedOneWhileControlStaysHeld()
    {
        var state = new HotkeyCaptureState();
        state.KeyDown(Key.LeftCtrl, KeyModifiers.Control);
        state.KeyDown(Key.F12, KeyModifiers.Control);
        state.KeyUp(Key.F12, KeyModifiers.Control);
        state.KeyDown(Key.B, KeyModifiers.Control);
        state.KeyUp(Key.B, KeyModifiers.Control);
        state.KeyUp(Key.LeftCtrl, KeyModifiers.None);
        Assert.Equal("CTRL + B", state.DisplayText);
        Assert.Equal("Ctrl+B", state.Hotkey);
        Assert.True(state.CanConfirm);
    }

    [Fact]
    public void NavigationPreservesOnlyAReleasedCandidateAndFocusResetInvalidatesIt()
    {
        var state = new HotkeyCaptureState();
        state.KeyDown(Key.B, KeyModifiers.Control);
        state.KeyUp(Key.B, KeyModifiers.None);
        state.KeyDown(Key.LeftShift, KeyModifiers.Shift);
        state.Navigate();
        Assert.Equal("CTRL + B", state.DisplayText);
        Assert.False(state.CanConfirm);
        state.KeyUp(Key.LeftShift, KeyModifiers.None);
        Assert.True(state.CanConfirm);
        Assert.Equal("Ctrl+B", state.Hotkey);
        state.Reset();
        state.Navigate();
        Assert.Null(state.Hotkey);
    }

    [Fact]
    public void NavigationCannotResurrectAnOldChordAfterAnInvalidNewPrimary()
    {
        var state = new HotkeyCaptureState();
        state.KeyDown(Key.B, KeyModifiers.Control);
        state.KeyUp(Key.B, KeyModifiers.None);
        state.KeyDown(Key.F12, KeyModifiers.Control);
        state.KeyUp(Key.F12, KeyModifiers.None);
        state.Navigate();
        Assert.Null(state.Hotkey);
        Assert.True(state.IsInvalid);
        Assert.False(state.CanConfirm);
    }

    [Fact]
    public async Task NotificationsStaySuppressedUntilRestorationFinishes()
    {
        var coordinator = new HotkeyCaptureCoordinator();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = new List<string>();
        var task = coordinator.RunAsync(
            () => { Assert.True(coordinator.IsActive); calls.Add("pause"); return Task.CompletedTask; },
            async () => { Assert.True(coordinator.IsActive); calls.Add("restore"); await release.Task; },
            () => { Assert.True(coordinator.IsActive); calls.Add("dialog"); return Task.FromResult("Ctrl+Alt+B"); });
        Assert.True(coordinator.IsActive);
        Assert.Equal(new[] { "pause", "dialog", "restore" }, calls);
        release.SetResult();
        Assert.Equal("Ctrl+Alt+B", await task);
        Assert.False(coordinator.IsActive);
    }

    [Fact]
    public async Task CancellationReturnsNoChangeAndStillRestoresHotkeys()
    {
        var coordinator = new HotkeyCaptureCoordinator();
        var restored = false;
        var result = await coordinator.RunAsync(() => Task.CompletedTask,
            () => { restored = true; return Task.CompletedTask; }, () => Task.FromResult<string?>(null));
        Assert.Null(result);
        Assert.True(restored);
        Assert.False(coordinator.IsActive);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PauseOrDialogFailureStillRestoresHotkeys(bool pauseFails)
    {
        var coordinator = new HotkeyCaptureCoordinator();
        var restored = false;
        await Assert.ThrowsAsync<IOException>(() => coordinator.RunAsync<string?>(
            () => pauseFails ? Task.FromException(new IOException()) : Task.CompletedTask,
            () => { restored = true; return Task.CompletedTask; },
            () => Task.FromException<string?>(new IOException())));
        Assert.True(restored);
        Assert.False(coordinator.IsActive);
    }

    [Fact]
    public async Task RestorationFailureDoesNotLeaveCapturePermanentlyActive()
    {
        var coordinator = new HotkeyCaptureCoordinator();
        await Assert.ThrowsAsync<IOException>(() => coordinator.RunAsync(() => Task.CompletedTask,
            () => Task.FromException(new IOException()), () => Task.FromResult<string?>(null)));
        Assert.False(coordinator.IsActive);
    }

    [Fact]
    public async Task NestedCaptureIsRejectedWithoutPausingOrRestoringTwice()
    {
        var coordinator = new HotkeyCaptureCoordinator();
        var restores = 0;
        await coordinator.RunAsync(() => Task.CompletedTask,
            () => { restores++; return Task.CompletedTask; }, async () =>
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.RunAsync<string?>(
                    () => throw new Exception(), () => throw new Exception(), () => throw new Exception()));
                return (string?)null;
            });
        Assert.Equal(1, restores);
        Assert.False(coordinator.IsActive);
    }
}
