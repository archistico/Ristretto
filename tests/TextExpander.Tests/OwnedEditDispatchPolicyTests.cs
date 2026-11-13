using TextExpander.Application.Features.Output;
using Xunit;

namespace TextExpander.Tests;

public sealed class OwnedEditDispatchPolicyTests
{
    [Theory]
    [InlineData(0x0102u, 0x61)]
    [InlineData(0x0102u, 0x20)]
    [InlineData(0x0102u, 0x00e9)]
    [InlineData(0x0109u, 0x1f600)]
    public void PrintableCharacterMessagesRemainCandidatesButNotHumanProof(uint message, int value)
    {
        var result = OwnedEditDispatchPolicy.Classify(message, (nuint)value, false);
        Assert.Equal(OwnedEditMutationSource.OwnedCharacterMessage, result.MutationSource);
        Assert.False(result.BreaksContinuity);
    }

    [Theory]
    [InlineData(0x0102u, 0x08)] // Backspace character
    [InlineData(0x0102u, 0x16)] // Ctrl+V translated character
    [InlineData(0x0109u, 0xd800)] // WM_UNICHAR must carry a valid scalar
    [InlineData(0x0109u, 0xdc00)] // WM_UNICHAR cannot carry surrogate
    [InlineData(0x0109u, 0xffff)] // UNICODE_NOCHAR capability probe
    [InlineData(0x0109u, 0x110000)] // Invalid Unicode scalar
    public void ControlAndIncompleteCharacterMessagesInvalidate(uint message, int value)
    {
        var result = OwnedEditDispatchPolicy.Classify(message, (nuint)value, false);
        Assert.Equal(OwnedEditMutationSource.Unknown, result.MutationSource);
        Assert.True(result.BreaksContinuity);
    }

    [Theory]
    [InlineData(0xd800)]
    [InlineData(0xdc00)]
    public void SurrogateUnitsAreOnlyCandidatesUntilCommittedUnicodeIsVerified(int codeUnit)
    {
        var result = OwnedEditDispatchPolicy.Classify(0x0102, (nuint)codeUnit, false);
        Assert.Equal(OwnedEditMutationSource.OwnedCharacterMessage, result.MutationSource);
        Assert.False(result.BreaksContinuity);
        // A half-surrogate alone is NOT accepted by CommittedEditDeltaVerifier.
    }

    [Fact]
    public void ModifiersInvalidatePrintableCharacterCandidatesExceptShiftOnly()
    {
        Assert.True(OwnedEditDispatchPolicy.Classify(0x0102, (nuint)'A', true).BreaksContinuity);
        Assert.False(OwnedEditDispatchPolicy.Classify(0x0102, (nuint)'A', false).BreaksContinuity);
        Assert.True(OwnedEditDispatchPolicy.Classify(0x0100, (nuint)'V', true).BreaksContinuity);
        Assert.True(OwnedEditDispatchPolicy.Classify(0x0104, (nuint)'X', true).BreaksContinuity);
        Assert.True(OwnedEditDispatchPolicy.Classify(0x0104, (nuint)'X', false).BreaksContinuity);
        Assert.False(OwnedEditDispatchPolicy.Classify(0x0100, (nuint)'a', false).BreaksContinuity);
        Assert.False(OwnedEditDispatchPolicy.Classify(0x0100, (nuint)0x11, true).BreaksContinuity);
    }

    [Theory]
    [InlineData(0x08)] // Backspace
    [InlineData(0x09)] // Tab
    [InlineData(0x0d)] // Enter
    [InlineData(0x1b)] // Escape
    [InlineData(0x21)] // PageUp
    [InlineData(0x22)] // PageDown
    [InlineData(0x23)] // End
    [InlineData(0x24)] // Home
    [InlineData(0x25)] // Left
    [InlineData(0x26)] // Up
    [InlineData(0x27)] // Right
    [InlineData(0x28)] // Down
    [InlineData(0x2d)] // Insert
    [InlineData(0x2e)] // Delete
    [InlineData(0xe5)] // IME process key
    public void NavigationAndEditingKeysBreakContinuity(int key)
    {
        Assert.True(OwnedEditDispatchPolicy.Classify(0x0100, (nuint)key, false).BreaksContinuity);
    }

    [Theory]
    [InlineData(0x0302u, OwnedEditMutationSource.ClipboardOrPaste)] // WM_PASTE
    [InlineData(0x0300u, OwnedEditMutationSource.Unknown)] // WM_CUT
    [InlineData(0x0301u, OwnedEditMutationSource.Unknown)] // WM_COPY
    [InlineData(0x0303u, OwnedEditMutationSource.Unknown)] // WM_CLEAR
    [InlineData(0x0304u, OwnedEditMutationSource.Unknown)] // WM_UNDO
    [InlineData(0x000cu, OwnedEditMutationSource.Unknown)] // WM_SETTEXT
    [InlineData(0x00b1u, OwnedEditMutationSource.Unknown)] // EM_SETSEL
    [InlineData(0x00c2u, OwnedEditMutationSource.Unknown)] // EM_REPLACESEL
    [InlineData(0x0201u, OwnedEditMutationSource.Unknown)] // mouse down
    [InlineData(0x0203u, OwnedEditMutationSource.Unknown)] // double click
    [InlineData(0x0204u, OwnedEditMutationSource.Unknown)] // context menu
    [InlineData(0x020au, OwnedEditMutationSource.Unknown)] // mouse wheel
    [InlineData(0x0007u, OwnedEditMutationSource.Unknown)] // focus gained
    [InlineData(0x0008u, OwnedEditMutationSource.Unknown)] // focus lost
    [InlineData(0x0233u, OwnedEditMutationSource.Unknown)] // file drop
    [InlineData(0x0106u, OwnedEditMutationSource.Unknown)] // SYSCHAR
    public void EditCommandsAndPointerActionsInvalidate(uint message, OwnedEditMutationSource expected)
    {
        var result = OwnedEditDispatchPolicy.Classify(message, 0, false);
        Assert.True(result.BreaksContinuity);
        Assert.Equal(expected, result.MutationSource);
    }

    [Theory]
    [InlineData(0x010du)] // IME start
    [InlineData(0x010eu)] // IME end
    [InlineData(0x010fu)] // composition
    [InlineData(0x0286u)] // IME character
    [InlineData(0x0282u)] // IME notification
    [InlineData(0x0051u)] // input language change
    public void ImeSignalsInvalidateWithExplicitProvenance(uint message)
    {
        var result = OwnedEditDispatchPolicy.Classify(message, 0, false);
        Assert.True(result.BreaksContinuity);
        Assert.Equal(OwnedEditMutationSource.ImeOrComposition, result.MutationSource);
    }

    [Fact]
    public void IrrelevantMessagesDoNotResetContinuity()
    {
        Assert.False(OwnedEditDispatchPolicy.Classify(0x000f, 0, false).BreaksContinuity); // WM_PAINT
        Assert.False(OwnedEditDispatchPolicy.Classify(0x0101, 0, false).BreaksContinuity); // WM_KEYUP
    }

    [Fact]
    public void NoClassificationStoresUserTextOrWindowIdentity()
    {
        var decision = OwnedEditDispatchPolicy.Classify(0x0102, (nuint)'x', false);
        Assert.DoesNotContain("x", decision.ToString());
    }
}
