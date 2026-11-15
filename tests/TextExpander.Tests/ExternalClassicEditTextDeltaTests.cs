using TextExpander.Application.Features.Input;
using Xunit;

namespace TextExpander.Tests;

public sealed class ExternalClassicEditTextDeltaTests
{
    private static ExternalClassicEditTextObservation At(string text, int? caret = null) =>
        new(text, caret ?? text.Length, caret ?? text.Length);

    [Theory]
    [InlineData("", ";firma ")]
    [InlineData("", "caffè")]
    [InlineData("", "cafe\u0301")]
    [InlineData("", "漢字 مرحبا")]
    [InlineData("", "🧑🏽‍💻")]
    [InlineData("testo ", "testo ☕")]
    public void ValidUnicodeInsertionIsOnlyACandidate(string before, string after)
    {
        var result = ExternalClassicEditTextDeltaPolicy.Evaluate(At(before), At(after), true);
        Assert.Equal(ExternalClassicEditTextDeltaStatus.CandidateInsertion, result.Status);
        Assert.True(result.CandidateObserved);
        Assert.False(result.CanAuthorizeExpansion);
    }

    [Fact]
    public void InsertionInMiddleAtCaretIsOnlyCandidate()
    {
        var before = At("ab", 1);
        var after = At("a☕b", 2); // a (1 UTF-16) + ☕ (1 UTF-16): caret immediatamente dopo l’inserimento.
        Assert.Equal(ExternalClassicEditTextDeltaStatus.CandidateInsertion,
            ExternalClassicEditTextDeltaPolicy.Evaluate(before, after, true).Status);
    }

    [Fact]
    public void InsertionInMiddleWithCaretMovedPastTheInsertedTextIsRejected()
    {
        // Non accettare uno snapshot dove il cursore si è spostato sul carattere seguente.
        Assert.Equal(ExternalClassicEditTextDeltaStatus.NotInsertionAtCaret,
            ExternalClassicEditTextDeltaPolicy.Evaluate(At("ab", 1), At("a☕b", 3), true).Status);
    }

    [Fact]
    public void InsertionInMiddleWithSurrogatePairUsesUtf16CaretOffsets()
    {
        // L'emoji occupa due unità UTF-16, quindi il caret passa da 1 a 3.
        Assert.Equal(ExternalClassicEditTextDeltaStatus.CandidateInsertion,
            ExternalClassicEditTextDeltaPolicy.Evaluate(At("ab", 1), At("a🧑b", 3), true).Status);
    }

    [Fact]
    public void CannotCompleteTriggerAcrossDifferentControls()
    {
        Assert.Equal(ExternalClassicEditTextDeltaStatus.TargetChanged,
            ExternalClassicEditTextDeltaPolicy.Evaluate(At(";fir"), At(";firma "), false).Status);
    }

    [Theory]
    [InlineData("ciao", "ciao")]
    [InlineData("ciao", "cia")]
    [InlineData("ciao", "saluto")]
    public void UnchangedDeletionAndReplacementAreNotInsertions(string before, string after)
    {
        var result = ExternalClassicEditTextDeltaPolicy.Evaluate(At(before), At(after), true);
        Assert.False(result.CandidateObserved);
        Assert.False(result.CanAuthorizeExpansion);
    }

    [Fact]
    public void ChangeElsewhereIsNotAccepted()
    {
        Assert.Equal(ExternalClassicEditTextDeltaStatus.NotInsertionAtCaret,
            ExternalClassicEditTextDeltaPolicy.Evaluate(At("ciao", 1), At("ci☕ao", 3), true).Status);
    }

    [Fact]
    public void SelectionOrMovedCaretIsRejected()
    {
        Assert.Equal(ExternalClassicEditTextDeltaStatus.SelectionNotCollapsed,
            ExternalClassicEditTextDeltaPolicy.Evaluate(new("ab", 0, 1), At("abb", 2), true).Status);
        Assert.Equal(ExternalClassicEditTextDeltaStatus.NotInsertionAtCaret,
            ExternalClassicEditTextDeltaPolicy.Evaluate(At("ab"), At("abc", 1), true).Status);
    }

    [Fact]
    public void NullObservationIsRejected()
    {
        Assert.Equal(ExternalClassicEditTextDeltaStatus.MissingObservation,
            ExternalClassicEditTextDeltaPolicy.Evaluate(null, At("x"), true).Status);
    }

    [Fact]
    public void HalfSurrogateIsRejected()
    {
        Assert.Equal(ExternalClassicEditTextDeltaStatus.InvalidUnicode,
            ExternalClassicEditTextDeltaPolicy.Evaluate(At(""), At("\uD83D"), true).Status);
    }

    [Fact]
    public void GraphemeSplitAtCaretIsRejected()
    {
        Assert.Equal(ExternalClassicEditTextDeltaStatus.GraphemeBoundary,
            ExternalClassicEditTextDeltaPolicy.Evaluate(At("e\u0301", 1), At("eX\u0301", 2), true).Status);
    }

    [Fact]
    public void ControlCharacterIsNotCandidate()
    {
        Assert.Equal(ExternalClassicEditTextDeltaStatus.InvalidUnicode,
            ExternalClassicEditTextDeltaPolicy.Evaluate(At(""), At("a\t"), true).Status);
    }

    [Fact]
    public void OversizedDocumentIsRejected()
    {
        var text = new string('x', ExternalClassicEditTextDeltaPolicy.MaxDocumentUtf16Units + 1);
        Assert.Equal(ExternalClassicEditTextDeltaStatus.TooLong,
            ExternalClassicEditTextDeltaPolicy.Evaluate(At(text), At(text + "a"), true).Status);
    }

    [Fact]
    public void InvalidExistingDocumentIsRejected()
    {
        Assert.Equal(ExternalClassicEditTextDeltaStatus.InvalidUnicode,
            ExternalClassicEditTextDeltaPolicy.Evaluate(At("\uD83D"), At("\uD83Da"), true).Status);
    }

    [Fact]
    public void DiagnosticReceiptsDoNotRevealText()
    {
        var observation = At("documento privato 👀");
        var result = ExternalClassicEditTextDeltaPolicy.Evaluate(At(""), observation, true);
        Assert.Equal(nameof(ExternalClassicEditTextObservation), observation.ToString());
        Assert.Equal(nameof(ExternalClassicEditTextDeltaResult), result.ToString());
        Assert.DoesNotContain("privato", result.ToString());
    }
}
