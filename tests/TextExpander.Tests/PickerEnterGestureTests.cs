using TextExpander.Desktop;
using Xunit;

namespace TextExpander.Tests;

public sealed class PickerEnterGestureTests
{
    [Fact]
    public void SingleEnterReleaseSubmitsOneGesture()
    {
        var gesture = new PickerEnterGesture();
        gesture.Press(noModifiers: true);
        Assert.True(gesture.IsDown);
        Assert.True(gesture.Release(noModifiers: true));
        Assert.False(gesture.Release(noModifiers: true));
        Assert.False(gesture.IsDown);
    }

    [Fact]
    public void EnterHeldDownCannotCreateRepeatedInsertions()
    {
        var gesture = new PickerEnterGesture();
        gesture.Press(true);
        gesture.Press(true); // auto-repeat
        gesture.Press(true);
        Assert.True(gesture.Release(true));
        Assert.False(gesture.Release(true));
    }

    [Fact]
    public void EnterArmedBeforeResultsFinishLoadingMaySubmitOnRelease()
    {
        // Keyboard state must not depend on an asynchronous library reload.
        // The picker checks CanInsert at release, not at keydown.
        var gesture = new PickerEnterGesture();
        gesture.Press(true);
        Assert.True(gesture.Release(true));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void ModifiedEnterDoesNotSubmit(bool unmodifiedAtPress, bool unmodifiedAtRelease)
    {
        var gesture = new PickerEnterGesture();
        gesture.Press(unmodifiedAtPress);
        Assert.False(gesture.Release(unmodifiedAtRelease));
    }

    [Fact]
    public void LosingFocusOrClosingPickerDisarmsEnter()
    {
        var gesture = new PickerEnterGesture();
        gesture.Press(true);
        gesture.Reset();
        Assert.False(gesture.Release(true));
    }

    [Fact]
    public void EnterKeyUpWithoutKeyDownDoesNotInsert()
    {
        Assert.False(new PickerEnterGesture().Release(true));
    }

    [Fact]
    public void DeferredEnterIsAcceptedOnceAfterSameQueryBecomesReady()
    {
        var gesture = new PickerEnterGesture();
        gesture.Press(true);
        Assert.True(gesture.Release(true));
        gesture.DeferUntilLoaded(";firma");
        Assert.True(gesture.ConsumeDeferred(";firma", pickerActive: true, canInsert: true));
        Assert.False(gesture.ConsumeDeferred(";firma", pickerActive: true, canInsert: true));
    }

    [Theory]
    [InlineData("new query", true, true)]
    [InlineData(";firma", false, true)]
    [InlineData(";firma", true, false)]
    public void DeferredEnterMustNeverInsertWithChangedFocusQueryOrEligibility(
        string query, bool active, bool eligible)
    {
        var gesture = new PickerEnterGesture();
        gesture.DeferUntilLoaded(";firma");
        Assert.False(gesture.ConsumeDeferred(query, active, eligible));
    }

    [Fact]
    public void ClosingPickerDiscardsDeferredEnter()
    {
        var gesture = new PickerEnterGesture();
        gesture.DeferUntilLoaded(";firma");
        gesture.Reset();
        Assert.False(gesture.ConsumeDeferred(";firma", true, true));
    }
}
