using TextExpander.Application.Features.Input;
using TextExpander.Application.Features.Output;
using TextExpander.Core;
using TextExpander.Core.Matching;
using Xunit;

namespace TextExpander.Tests;

public sealed class OwnedCommittedInputBridgeTests
{
    private static readonly Guid Target = new("40b505b0-3e49-42b5-bbdd-29e3d8f2c6b9");
    private static readonly OwnedEditObservationContext Trusted = new(true, true, true, false, false);

    private static LibrarySnapshot Library(string trigger = ";firma", string content = "Caffè 🌍")
    {
        var group = new SnippetGroup(Guid.NewGuid(), "Prove", true);
        var snippet = new Snippet(Guid.NewGuid(), group.Id, "Firma", trigger, content, "", true, 1);
        return new LibrarySnapshot([group], [snippet], 1);
    }

    private static ReplacementTargetSnapshot Start(string text = "") => new(
        Target, 1, 0, text, text.Length, text.Length,
        ReplacementFieldState.VerifiedNormalEditable, ReplacementCapability.VerifiedConditionalReplace);

    private static ReplacementTargetSnapshot Insert(ReplacementTargetSnapshot previous, string text)
    {
        var position = previous.SelectionEnd;
        var next = previous.Text!.Insert(position, text);
        return previous with { Text = next, TextRevision = previous.TextRevision + 1,
            SelectionStart = position + text.Length, SelectionEnd = position + text.Length };
    }

    private static OwnedEditMutationEvidence Character(ReplacementTargetSnapshot current) =>
        new(current.TargetEpoch, current.TextRevision, OwnedEditMutationSource.OwnedCharacterMessage, true);

    [Fact]
    public void VerifiedDeltaAndRevisionBoundProvenanceReachDelimiterRecognition()
    {
        var baseline = Start();
        var bridge = new OwnedCommittedInputBridge(Library(), baseline);
        var first = Insert(baseline, ";firma");
        var waiting = bridge.Observe(first, Trusted, Character(first), false, 10);
        Assert.True(waiting.HasTrustedObservation);
        Assert.Equal(TriggerActivationStatus.WaitingForDelimiter, waiting.Activation!.Status);
        var second = Insert(first, " ");
        var ready = bridge.Observe(second, Trusted, Character(second), false, 11);
        Assert.True(ready.HasTrustedObservation);
        Assert.Equal(TriggerActivationStatus.Ready, ready.Activation!.Status);
        Assert.Equal("Caffè 🌍", ready.Activation.Snippet!.Content);
    }

    [Fact]
    public void AnUnchangedSnapshotIsNotASecondInputEvent()
    {
        var initial = Start();
        var bridge = new OwnedCommittedInputBridge(Library(), initial);
        var typed = Insert(initial, ";firma");
        bridge.Observe(typed, Trusted, Character(typed), false, 1);
        var noChange = bridge.Observe(typed, Trusted, null, false, 2);
        Assert.Equal(CommittedEditDeltaRejection.NoChange, noChange.DeltaRejection);
        Assert.Null(noChange.Activation);
        var delimiter = Insert(typed, " ");
        Assert.Equal(TriggerActivationStatus.Ready,
            bridge.Observe(delimiter, Trusted, Character(delimiter), false, 3).Activation!.Status);
    }

    [Theory]
    [InlineData(OwnedEditMutationSource.ClipboardOrPaste)]
    [InlineData(OwnedEditMutationSource.ProgrammaticChange)]
    [InlineData(OwnedEditMutationSource.ImeOrComposition)]
    [InlineData(OwnedEditMutationSource.Unknown)]
    public void UntrustedOriginsClearPreviousTrigger(OwnedEditMutationSource origin)
    {
        var initial = Start();
        var bridge = new OwnedCommittedInputBridge(Library(), initial);
        var typed = Insert(initial, ";firma");
        var declined = bridge.Observe(typed, Trusted,
            new(typed.TargetEpoch, typed.TextRevision, origin, true), false, 1);
        Assert.Equal(OwnedEditProvenanceRejection.UntrustedSource, declined.ProvenanceRejection);
        Assert.False(declined.HasTrustedObservation);
        var delimiter = Insert(typed, " ");
        var result = bridge.Observe(delimiter, Trusted, Character(delimiter), false, 2);
        Assert.NotEqual(TriggerActivationStatus.Ready, result.Activation!.Status);
    }

    [Fact]
    public void MissingOrStaleProvenanceCannotFinishAStoredTrigger()
    {
        var initial = Start();
        var bridge = new OwnedCommittedInputBridge(Library(), initial);
        var typed = Insert(initial, ";firma");
        bridge.Observe(typed, Trusted, Character(typed), false, 1);
        var delimiter = Insert(typed, " ");
        Assert.Equal(OwnedEditProvenanceRejection.MissingEvidence,
            bridge.Observe(delimiter, Trusted, null, false, 2).ProvenanceRejection);
        var next = Insert(delimiter, " ");
        Assert.NotEqual(TriggerActivationStatus.Ready,
            bridge.Observe(next, Trusted, Character(next), false, 3).Activation!.Status);
    }

    [Fact]
    public void RevisionGapOrDifferentTargetCannotContinueOldRecognition()
    {
        var initial = Start();
        var bridge = new OwnedCommittedInputBridge(Library(), initial);
        var typed = Insert(initial, ";firma");
        bridge.Observe(typed, Trusted, Character(typed), false, 1);
        var gap = Insert(typed, " ") with { TextRevision = 9 };
        Assert.Equal(CommittedEditDeltaRejection.RevisionNotConsecutive,
            bridge.Observe(gap, Trusted, Character(gap), false, 2).DeltaRejection);
        var anotherTarget = Insert(gap, ";firma") with { TargetToken = Guid.NewGuid() };
        Assert.Equal(CommittedEditDeltaRejection.TargetChanged,
            bridge.Observe(anotherTarget, Trusted, Character(anotherTarget), false, 3).DeltaRejection);
    }

    [Theory]
    [InlineData(false, true, false, false, CommittedEditDeltaRejection.FocusNotVerified)]
    [InlineData(true, false, false, false, CommittedEditDeltaRejection.CompositionUnknown)]
    [InlineData(true, true, true, false, CommittedEditDeltaRejection.CompositionActive)]
    [InlineData(true, true, false, true, CommittedEditDeltaRejection.ModifiersActive)]
    public void UntrustedFocusImeAndModifiersFailClosed(bool focus, bool known, bool composing,
        bool modifiers, CommittedEditDeltaRejection expected)
    {
        var initial = Start();
        var bridge = new OwnedCommittedInputBridge(Library(), initial);
        var typed = Insert(initial, ";firma");
        Assert.Equal(expected,
            bridge.Observe(typed, new(true, focus, known, composing, modifiers), Character(typed), false, 1)
                .DeltaRejection);
    }

    [Fact]
    public void InvalidateRequiresFreshBaselineAndNeverInterpretsOldText()
    {
        var initial = Start();
        var bridge = new OwnedCommittedInputBridge(Library(), initial);
        var typed = Insert(initial, ";firma");
        bridge.Observe(typed, Trusted, Character(typed), false, 1);
        bridge.Invalidate();
        var interrupted = Insert(typed, " ");
        Assert.Equal(CommittedEditDeltaRejection.MissingSnapshot,
            bridge.Observe(interrupted, Trusted, Character(interrupted), false, 2).DeltaRejection);
        var next = Insert(interrupted, ";firma");
        Assert.Equal(TriggerActivationStatus.WaitingForDelimiter,
            bridge.Observe(next, Trusted, Character(next), false, 3).Activation!.Status);
        var delimiter = Insert(next, " ");
        Assert.Equal(TriggerActivationStatus.Ready,
            bridge.Observe(delimiter, Trusted, Character(delimiter), false, 4).Activation!.Status);
    }

    [Fact]
    public void RebaseDoesNotReinterpretAlreadyCommittedContent()
    {
        var initial = Start(";firma");
        var bridge = new OwnedCommittedInputBridge(Library(), initial);
        bridge.Rebase(initial);
        var delimiter = Insert(initial, " ");
        Assert.NotEqual(TriggerActivationStatus.Ready,
            bridge.Observe(delimiter, Trusted, Character(delimiter), false, 1).Activation!.Status);
    }

    [Fact]
    public void SyntheticFixtureIsNeverAcceptedWithoutExplicitLaboratoryOptIn()
    {
        var initial = Start();
        var bridge = new OwnedCommittedInputBridge(Library(), initial);
        var typed = Insert(initial, ";firma");
        var fixture = new OwnedEditMutationEvidence(typed.TargetEpoch, typed.TextRevision,
            OwnedEditMutationSource.OwnedFixture, false);
        Assert.Equal(OwnedEditProvenanceRejection.FixtureNotAuthorized,
            bridge.Observe(typed, Trusted, fixture, false, 1).ProvenanceRejection);
        bridge.Rebase(initial);
        Assert.Equal(TriggerActivationStatus.WaitingForDelimiter,
            bridge.Observe(typed, Trusted, fixture, true, 2).Activation!.Status);
    }

    [Fact]
    public void UnicodeTextIsMatchedOnlyAfterVerifiedCommittedChange()
    {
        var initial = Start();
        var bridge = new OwnedCommittedInputBridge(Library(";café", "☕🧑🏽‍💻"), initial);
        var typed = Insert(initial, ";cafe\u0301");
        Assert.Equal(TriggerActivationStatus.WaitingForDelimiter,
            bridge.Observe(typed, Trusted, Character(typed), false, 1).Activation!.Status);
        var delimiter = Insert(typed, " ");
        Assert.Equal("☕🧑🏽‍💻",
            bridge.Observe(delimiter, Trusted, Character(delimiter), false, 2).Activation!.Snippet!.Content);
    }

    [Fact]
    public void DiagnosticResultNeverPrintsCapturedSnippetOrDocument()
    {
        var initial = Start();
        var bridge = new OwnedCommittedInputBridge(Library(), initial);
        var typed = Insert(initial, ";firma");
        var result = bridge.Observe(typed, Trusted, Character(typed), false, 1);
        Assert.DoesNotContain(";firma", result.ToString());
        Assert.DoesNotContain("Caffè", result.ToString());
        Assert.Equal(nameof(OwnedCommittedInputResult), result.ToString());
    }
}
