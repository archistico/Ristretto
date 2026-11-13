using TextExpander.Application.Features.Output;
using TextExpander.Core;
using TextExpander.Core.Matching;
using Xunit;

namespace TextExpander.Tests;

public sealed class CommittedEditDeltaVerifierTests
{
    private static readonly Guid Token = new("99098a9e-a3ea-4a49-8d9a-ad0d88c7352e");
    private static readonly OwnedEditObservationContext Trusted = new(
        OwnsControl: true, InputFocusVerified: true, CompositionStateKnown: true,
        IsComposing: false, ModifiersPressed: false);

    private static ReplacementTargetSnapshot Before(string text = "prefix ", int? caret = null) => new(
        Token, 11, 20, text, caret ?? text.Length, caret ?? text.Length,
        ReplacementFieldState.VerifiedNormalEditable, ReplacementCapability.VerifiedConditionalReplace);

    private static ReplacementTargetSnapshot Insert(ReplacementTargetSnapshot before, string value)
    {
        var text = before.Text!;
        var caret = before.SelectionEnd + value.Length;
        return before with
        {
            Text = text.Insert(before.SelectionEnd, value), TextRevision = before.TextRevision + 1,
            SelectionStart = caret, SelectionEnd = caret
        };
    }

    private static CommittedEditDeltaResult Verify(ReplacementTargetSnapshot before,
        ReplacementTargetSnapshot after, OwnedEditObservationContext? context = null) =>
        CommittedEditDeltaVerifier.Verify(before, after, context ?? Trusted);

    [Theory]
    [InlineData(";firma")]
    [InlineData("café")]
    [InlineData("cafe\u0301")]
    [InlineData("👩🏽‍💻")]
    [InlineData("こんにちは")]
    [InlineData("مرحبا")]
    [InlineData("नमस्ते")]
    public void ExactUnicodeInsertionsAreVerifiedWithoutNormalizingTheirRawText(string value)
    {
        var before = Before();
        var result = Verify(before, Insert(before, value));
        Assert.True(result.IsVerified);
        Assert.Equal(CommittedEditDeltaRejection.None, result.Rejection);
        Assert.Equal(value, result.Insertion!.Observation.Text);
        Assert.Equal(11, result.Insertion.Observation.TargetEpoch);
        Assert.Equal(TextObservationKind.VerifiedCommitted, result.Insertion.Observation.Kind);
        Assert.Equal(ObservedFieldSafety.VerifiedNormalEditable, result.Insertion.Observation.FieldSafety);
        Assert.Equal(InitialWordBoundaryEvidence.Verified, result.Insertion.InitialBoundary);
    }

    [Fact]
    public void SharedUnicodeInputValidatorIsPublicAndRejectsUnsafeObservations()
    {
        // This API is used by Core and by the Application assembly: a regression to
        // 'internal' would break the production build (CS0117) rather than these rules.
        Assert.True(CommittedInputSession.IsValidText("cafe\u0301"));
        Assert.True(CommittedInputSession.IsValidText("👩🏽‍💻"));
        Assert.True(CommittedInputSession.IsValidText("مرحبا"));
        Assert.False(CommittedInputSession.IsValidText("\uD800"));
        Assert.False(CommittedInputSession.IsValidText("\uDC00"));
        Assert.False(CommittedInputSession.IsValidText("\r"));
        Assert.False(CommittedInputSession.IsValidText("\u2028"));
    }

    [Fact]
    public void CombiningMarkCanBeAppendedToPriorBaseWithoutDiscardingItsRawSequence()
    {
        var before = Before("cafe");
        var result = Verify(before, Insert(before, "\u0301"));
        Assert.True(result.IsVerified);
        Assert.Equal("\u0301", result.Insertion!.Observation.Text);
        Assert.Equal(InitialWordBoundaryEvidence.Unknown, result.Insertion.InitialBoundary);
    }

    [Fact]
    public void InsertionInsideDocumentMustPreserveBothSidesExactly()
    {
        var before = Before("Prima dopo", 6);
        var result = Verify(before, Insert(before, "☕ "));
        Assert.Equal("☕ ", result.Insertion!.Observation.Text);
    }

    [Fact]
    public void EqualSnapshotsAreNoChangeNotUntrustedText()
    {
        var before = Before();
        var result = Verify(before, before);
        Assert.Equal(CommittedEditDeltaRejection.NoChange, result.Rejection);
        Assert.Null(result.Insertion);
    }

    [Fact]
    public void MissingSnapshotsAreRejected()
    {
        var before = Before();
        Assert.Equal(CommittedEditDeltaRejection.MissingSnapshot,
            CommittedEditDeltaVerifier.Verify(null, before, Trusted).Rejection);
        Assert.Equal(CommittedEditDeltaRejection.MissingSnapshot,
            CommittedEditDeltaVerifier.Verify(before, null, Trusted).Rejection);
    }

    [Fact]
    public void AnUnownedControlCannotProduceVerifiedInputEvenWithMatchingText()
    {
        var before = Before();
        Assert.Equal(CommittedEditDeltaRejection.UntrustedControl,
            Verify(before, Insert(before, "x"), Trusted with { OwnsControl = false }).Rejection);
    }

    [Theory]
    [InlineData(false, true, false, false, CommittedEditDeltaRejection.FocusNotVerified)]
    [InlineData(true, false, false, false, CommittedEditDeltaRejection.CompositionUnknown)]
    [InlineData(true, true, true, false, CommittedEditDeltaRejection.CompositionActive)]
    [InlineData(true, true, false, true, CommittedEditDeltaRejection.ModifiersActive)]
    public void InputContextBarriersFailClosed(bool focus, bool known, bool composing, bool modifiers,
        CommittedEditDeltaRejection reason)
    {
        var before = Before();
        Assert.Equal(reason, Verify(before, Insert(before, "x"),
            Trusted with { InputFocusVerified = focus, CompositionStateKnown = known,
                IsComposing = composing, ModifiersPressed = modifiers }).Rejection);
    }

    [Fact]
    public void PendingModifiersOnEitherSnapshotAreRejected()
    {
        var before = Before();
        var after = Insert(before, "x");
        Assert.Equal(CommittedEditDeltaRejection.ModifiersActive,
            Verify(before with { HasPendingModifiers = true }, after).Rejection);
        Assert.Equal(CommittedEditDeltaRejection.ModifiersActive,
            Verify(before, after with { HasPendingModifiers = true }).Rejection);
    }

    [Fact]
    public void ChangedEpochTokenAndInvalidIdentitiesAreRejected()
    {
        var before = Before();
        var after = Insert(before, "x");
        foreach (var changed in new[]
        {
            after with { TargetEpoch = after.TargetEpoch + 1 },
            after with { TargetToken = Guid.NewGuid() },
            after with { TargetToken = Guid.Empty },
            after with { TargetEpoch = 0 }
        })
            Assert.Equal(CommittedEditDeltaRejection.TargetChanged, Verify(before, changed).Rejection);
        Assert.Equal(CommittedEditDeltaRejection.TargetChanged,
            Verify(before with { TargetToken = Guid.Empty }, after).Rejection);
    }

    [Fact]
    public void ReplayedSkippedOrOverflownRevisionsAreRejected()
    {
        var before = Before();
        var after = Insert(before, "x");
        foreach (var revision in new[] { 20L, 22L, 0L })
            Assert.Equal(CommittedEditDeltaRejection.RevisionNotConsecutive,
                Verify(before, after with { TextRevision = revision }).Rejection);
        Assert.Equal(CommittedEditDeltaRejection.RevisionNotConsecutive,
            Verify(before with { TextRevision = long.MaxValue }, after).Rejection);
    }

    [Theory]
    [InlineData(ReplacementFieldState.Unknown)]
    [InlineData(ReplacementFieldState.Protected)]
    [InlineData(ReplacementFieldState.ReadOnly)]
    [InlineData(ReplacementFieldState.Disabled)]
    [InlineData(ReplacementFieldState.Composing)]
    public void UnsafeFieldBeforeOrAfterChangeIsRejected(ReplacementFieldState state)
    {
        var before = Before();
        var after = Insert(before, "x");
        Assert.Equal(CommittedEditDeltaRejection.UnsafeField,
            Verify(before with { FieldState = state }, after).Rejection);
        Assert.Equal(CommittedEditDeltaRejection.UnsafeField,
            Verify(before, after with { FieldState = state }).Rejection);
    }

    [Theory]
    [InlineData(ReplacementCapability.Unknown)]
    [InlineData(ReplacementCapability.Unsupported)]
    public void UnknownReplacementCapabilityDoesNotClaimOwnedEditQualification(ReplacementCapability capability)
    {
        var before = Before();
        Assert.Equal(CommittedEditDeltaRejection.CapabilityUnavailable,
            Verify(before, Insert(before, "x") with { Capability = capability }).Rejection);
    }

    [Fact]
    public void NullCorruptOrOversizedDocumentIsRejected()
    {
        var before = Before();
        var after = Insert(before, "x");
        foreach (var text in new[] { (string?)null, "a\0b", "x\uD800", "x\uDC00", new string('a', TriggerReplacementPlanner.MaxTextUtf16Units + 1) })
        {
            Assert.Equal(CommittedEditDeltaRejection.MissingOrInvalidDocument,
                Verify(before with { Text = text }, after).Rejection);
            Assert.Equal(CommittedEditDeltaRejection.MissingOrInvalidDocument,
                Verify(before, after with { Text = text }).Rejection);
        }
    }

    [Fact]
    public void InvalidOrActiveSelectionsCannotBeTrusted()
    {
        var before = Before();
        var after = Insert(before, "x");
        Assert.Equal(CommittedEditDeltaRejection.InvalidSelection,
            Verify(before with { SelectionStart = -1 }, after).Rejection);
        Assert.Equal(CommittedEditDeltaRejection.InvalidSelection,
            Verify(before, after with { SelectionEnd = after.Text!.Length + 1 }).Rejection);
        Assert.Equal(CommittedEditDeltaRejection.SelectionNotCollapsed,
            Verify(before with { SelectionStart = 0 }, after).Rejection);
        Assert.Equal(CommittedEditDeltaRejection.SelectionNotCollapsed,
            Verify(before, after with { SelectionStart = 0 }).Rejection);
    }

    [Fact]
    public void DeletionReplacementAndUnrelatedChangeAreNotInsertions()
    {
        var before = Before("ab", 1);
        Assert.Equal(CommittedEditDeltaRejection.NotAnInsertionAtCaret,
            Verify(before, before with { Text = "b", TextRevision = 21, SelectionStart = 0, SelectionEnd = 0 }).Rejection);
        Assert.Equal(CommittedEditDeltaRejection.NotAnInsertionAtCaret,
            Verify(before, before with { Text = "axb", TextRevision = 21, SelectionStart = 1, SelectionEnd = 1 }).Rejection);
        Assert.Equal(CommittedEditDeltaRejection.NotAnInsertionAtCaret,
            Verify(before, before with { Text = "azb", TextRevision = 21, SelectionStart = 3, SelectionEnd = 3 }).Rejection);
        Assert.Equal(CommittedEditDeltaRejection.NotAnInsertionAtCaret,
            Verify(before, before with { Text = "aXc", TextRevision = 21, SelectionStart = 2, SelectionEnd = 2 }).Rejection);
    }

    [Fact]
    public void VeryLargeSingleCommitIsNotObservation()
    {
        var before = Before("");
        var after = Insert(before, new string('x', CommittedInputSession.MaxObservationUtf16Units + 1));
        Assert.Equal(CommittedEditDeltaRejection.ObservationTooLong, Verify(before, after).Rejection);
    }

    [Theory]
    [InlineData("\t")]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\u2028")]
    public void ControlOrLineSeparatorInsertionResetsTriggerContext(string text)
    {
        var before = Before();
        Assert.Equal(CommittedEditDeltaRejection.UnsafeCommittedText,
            Verify(before, Insert(before, text)).Rejection);
    }

    [Fact]
    public void ExistingMultilineDocumentCanStillReceiveNormalText()
    {
        var before = Before("First\r\nSecond ");
        Assert.Equal("x", Verify(before, Insert(before, "x")).Insertion!.Observation.Text);
    }

    [Fact]
    public void InsertingInsideSurrogateOrGraphemeIsRejected()
    {
        var before = Before("😀end", 1); // UTF-16 surrogate pair split.
        var after = before with { Text = "\uD83DX\uDE00end", TextRevision = 21, SelectionStart = 2, SelectionEnd = 2 };
        Assert.Equal(CommittedEditDeltaRejection.MissingOrInvalidDocument, Verify(before, after).Rejection);

        var grapheme = Before("cafe\u0301", 4);
        var committed = Insert(grapheme, "x");
        Assert.Equal(CommittedEditDeltaRejection.GraphemeBoundary, Verify(grapheme, committed).Rejection);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("hello ", true)]
    [InlineData("hello;", true)]
    [InlineData("😀", true)]
    [InlineData("hello", false)]
    [InlineData("hello_", false)]
    [InlineData("مرحبا", false)]
    [InlineData("e\u0301", false)]
    public void InitialBoundaryRequiresVerifiedPrecedingWordDelimiter(string preceding, bool expected)
    {
        var before = Before(preceding);
        var result = Verify(before, Insert(before, "brb"));
        Assert.True(result.IsVerified);
        Assert.Equal(expected ? InitialWordBoundaryEvidence.Verified : InitialWordBoundaryEvidence.Unknown,
            result.Insertion!.InitialBoundary);
    }

    [Fact]
    public void VerifiedBoundaryAllowsAlphabeticTriggerAfterWhitespace()
    {
        var group = new SnippetGroup(Guid.NewGuid(), "Test", true);
        var snippet = new Snippet(Guid.NewGuid(), group.Id, "Away", "brb", "Be right back", "", true, 1);
        var session = new TriggerActivationSession(new LibrarySnapshot([group], [snippet], 1));
        var before = Before("Some text ");
        var word = Insert(before, "brb");
        var verified = Verify(before, word);
        Assert.True(verified.IsVerified);
        Assert.Equal(TriggerActivationStatus.WaitingForDelimiter,
            session.Observe(verified.Insertion!.Observation, 100, verified.Insertion.InitialBoundary).Status);
        var space = Verify(word, Insert(word, " "));
        Assert.Equal(TriggerActivationStatus.Ready,
            session.Observe(space.Insertion!.Observation, 101, space.Insertion.InitialBoundary).Status);
    }

    [Fact]
    public void InsideWordNeverInheritsFalseWordStartEvidence()
    {
        var group = new SnippetGroup(Guid.NewGuid(), "Test", true);
        var snippet = new Snippet(Guid.NewGuid(), group.Id, "Away", "brb", "Be right back", "", true, 1);
        var session = new TriggerActivationSession(new LibrarySnapshot([group], [snippet], 1));
        var before = Before("someword");
        var first = Verify(before, Insert(before, "brb"));
        Assert.Equal(InitialWordBoundaryEvidence.Unknown, first.Insertion!.InitialBoundary);
        Assert.NotEqual(TriggerActivationStatus.Ready,
            session.Observe(first.Insertion.Observation, 100, first.Insertion.InitialBoundary).Status);
        var withWord = Insert(before, "brb");
        var space = Verify(withWord, Insert(withWord, " "));
        Assert.NotEqual(TriggerActivationStatus.Ready,
            session.Observe(space.Insertion!.Observation, 101, space.Insertion.InitialBoundary).Status);
    }

    [Fact]
    public void DiagnosticsNeverPrintTransientEditorOrObservationText()
    {
        var before = Before("private prefix ");
        var result = Verify(before, Insert(before, "confidential user text"));
        Assert.True(result.IsVerified);
        Assert.DoesNotContain("confidential", result.ToString());
        Assert.DoesNotContain("confidential", result.Insertion!.ToString());
        Assert.DoesNotContain("confidential", result.Insertion.Observation.ToString());
    }
}
