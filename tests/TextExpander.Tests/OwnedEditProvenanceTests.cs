using TextExpander.Application.Features.Output;
using Xunit;

namespace TextExpander.Tests;

public sealed class OwnedEditProvenanceTests
{
    private static readonly Guid Target = new("f51d2dbd-6c66-4cb6-bb80-b3213c0364a9");

    private static ReplacementTargetSnapshot Current => new(
        Target, 3, 12, "before x", 8, 8,
        ReplacementFieldState.VerifiedNormalEditable,
        ReplacementCapability.VerifiedConditionalReplace);

    private static OwnedEditMutationEvidence Evidence(OwnedEditMutationSource source,
        bool observed = false) => new(3, 12, source, observed);

    [Fact]
    public void ExactOwnedCharacterMessageCorrelationIsAdmittedOnlyInLaboratory()
    {
        var result = OwnedEditProvenancePolicy.Evaluate(Current,
            Evidence(OwnedEditMutationSource.OwnedCharacterMessage, observed: true), false);
        Assert.True(result.Accepted);
        Assert.Equal(OwnedEditProvenanceRejection.None, result.Rejection);
    }

    [Fact]
    public void CharacterMessageWithoutMatchingOwnedDispatchIsDenied()
    {
        Assert.Equal(OwnedEditProvenanceRejection.MissingMessageCorrelation,
            OwnedEditProvenancePolicy.Evaluate(Current,
                Evidence(OwnedEditMutationSource.OwnedCharacterMessage), false).Rejection);
    }

    [Theory]
    [InlineData(OwnedEditMutationSource.Unknown)]
    [InlineData(OwnedEditMutationSource.ClipboardOrPaste)]
    [InlineData(OwnedEditMutationSource.ImeOrComposition)]
    [InlineData(OwnedEditMutationSource.ProgrammaticChange)]
    public void UnknownPasteImeAndProgrammaticAreRejectedEvenWhenDeltaMatches(
        OwnedEditMutationSource source)
    {
        Assert.Equal(OwnedEditProvenanceRejection.UntrustedSource,
            OwnedEditProvenancePolicy.Evaluate(Current, Evidence(source, observed: true), true).Rejection);
    }

    [Fact]
    public void SyntheticFixtureIsAllowedOnlyByExplicitSyntheticLabPath()
    {
        var evidence = Evidence(OwnedEditMutationSource.OwnedFixture);
        Assert.True(OwnedEditProvenancePolicy.Evaluate(Current, evidence, true).Accepted);
        Assert.Equal(OwnedEditProvenanceRejection.FixtureNotAuthorized,
            OwnedEditProvenancePolicy.Evaluate(Current, evidence, false).Rejection);
        Assert.Equal(OwnedEditProvenanceRejection.FixtureNotAuthorized,
            OwnedEditProvenancePolicy.Evaluate(Current,
                Evidence(OwnedEditMutationSource.OwnedFixture, observed: true), true).Rejection);
    }

    [Theory]
    [InlineData(OwnedEditMutationSource.ClipboardOrPaste)]
    [InlineData(OwnedEditMutationSource.ProgrammaticChange)]
    [InlineData(OwnedEditMutationSource.ImeOrComposition)]
    public void AnOtherwiseValidTextDeltaIsInsufficientToProveSource(OwnedEditMutationSource origin)
    {
        var before = Current with { Text = "before ", TextRevision = 11,
            SelectionStart = 7, SelectionEnd = 7 };
        var verified = CommittedEditDeltaVerifier.Verify(before, Current,
            new OwnedEditObservationContext(OwnsControl: true, InputFocusVerified: true,
                CompositionStateKnown: true, IsComposing: false, ModifiersPressed: false));
        Assert.True(verified.IsVerified);
        Assert.False(OwnedEditProvenancePolicy.Evaluate(Current, Evidence(origin), true).Accepted);
    }

    [Fact]
    public void MissingOrStaleEvidenceIsRejected()
    {
        Assert.Equal(OwnedEditProvenanceRejection.MissingEvidence,
            OwnedEditProvenancePolicy.Evaluate(Current, null, true).Rejection);
        Assert.Equal(OwnedEditProvenanceRejection.RevisionMismatch,
            OwnedEditProvenancePolicy.Evaluate(Current,
                Evidence(OwnedEditMutationSource.OwnedCharacterMessage, true) with { TextRevision = 11 }, false).Rejection);
        Assert.Equal(OwnedEditProvenanceRejection.RevisionMismatch,
            OwnedEditProvenancePolicy.Evaluate(Current,
                Evidence(OwnedEditMutationSource.OwnedCharacterMessage, true) with { TextRevision = 13 }, false).Rejection);
        Assert.Equal(OwnedEditProvenanceRejection.TargetChanged,
            OwnedEditProvenancePolicy.Evaluate(Current,
                Evidence(OwnedEditMutationSource.OwnedCharacterMessage, true) with { TargetEpoch = 4 }, false).Rejection);
    }

    [Fact]
    public void UnknownTargetEpochIsNeverTrusted()
    {
        Assert.Equal(OwnedEditProvenanceRejection.TargetChanged,
            OwnedEditProvenancePolicy.Evaluate(Current with { TargetEpoch = 0 },
                Evidence(OwnedEditMutationSource.OwnedCharacterMessage, true) with { TargetEpoch = 0 }, false).Rejection);
    }

    [Fact]
    public void DiagnosticsCannotPrintEditorTextOrCharacters()
    {
        var evidence = Evidence(OwnedEditMutationSource.OwnedCharacterMessage, observed: true);
        var outcome = OwnedEditProvenancePolicy.Evaluate(Current, evidence, false);
        Assert.DoesNotContain("before", evidence.ToString());
        Assert.DoesNotContain("before", outcome.ToString());
        Assert.DoesNotContain("before", evidence.Source.ToString());
    }
}
