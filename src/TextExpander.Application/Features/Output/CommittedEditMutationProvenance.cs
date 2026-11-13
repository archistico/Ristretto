namespace TextExpander.Application.Features.Output;

/// <summary>
/// An attribution of a change in the locally owned Win32 EDIT. A character-message
/// observation is NOT proof of a physical/human key press: messages can be synthesized.
/// These categories must not be used to grant access to third-party editors.
/// </summary>
public enum OwnedEditMutationSource
{
    Unknown,
    OwnedCharacterMessage,
    OwnedFixture,
    ClipboardOrPaste,
    ImeOrComposition,
    ProgrammaticChange
}

/// <summary>
/// Ephemeral metadata recorded by the owning adapter at the SAME revision as EN_CHANGE
/// or its own fixture edit. No characters, handles, key codes or document offsets.
/// The caller's ownership claims are not operating-system security tokens.
/// </summary>
public sealed record OwnedEditMutationEvidence(
    long TargetEpoch,
    long TextRevision,
    OwnedEditMutationSource Source,
    bool ObservedDuringOwnedMessageDispatch)
{
    public override string ToString() => nameof(OwnedEditMutationEvidence);
}

public enum OwnedEditProvenanceRejection
{
    None,
    MissingEvidence,
    TargetChanged,
    RevisionMismatch,
    UntrustedSource,
    MissingMessageCorrelation,
    FixtureNotAuthorized
}

public sealed record OwnedEditProvenanceResult(OwnedEditProvenanceRejection Rejection)
{
    public bool Accepted => Rejection == OwnedEditProvenanceRejection.None;
}

/// <summary>
/// Second gate AFTER the exact committed-delta check. A text difference by itself
/// never identifies paste, IME, keyboard messages or programmatic edits. Only an EDIT
/// owned by the local laboratory can supply this revision-correlated metadata.
/// </summary>
public static class OwnedEditProvenancePolicy
{
    public static OwnedEditProvenanceResult Evaluate(
        ReplacementTargetSnapshot current,
        OwnedEditMutationEvidence? evidence,
        bool allowOwnedFixture)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (evidence is null) return new(OwnedEditProvenanceRejection.MissingEvidence);
        if (current.TargetEpoch <= 0 || evidence.TargetEpoch != current.TargetEpoch)
            return new(OwnedEditProvenanceRejection.TargetChanged);
        if (current.TextRevision < 0 || evidence.TextRevision != current.TextRevision)
            return new(OwnedEditProvenanceRejection.RevisionMismatch);
        return evidence.Source switch
        {
            OwnedEditMutationSource.OwnedCharacterMessage when evidence.ObservedDuringOwnedMessageDispatch =>
                new(OwnedEditProvenanceRejection.None),
            OwnedEditMutationSource.OwnedCharacterMessage =>
                new(OwnedEditProvenanceRejection.MissingMessageCorrelation),
            OwnedEditMutationSource.OwnedFixture when allowOwnedFixture &&
                !evidence.ObservedDuringOwnedMessageDispatch => new(OwnedEditProvenanceRejection.None),
            OwnedEditMutationSource.OwnedFixture => new(OwnedEditProvenanceRejection.FixtureNotAuthorized),
            _ => new(OwnedEditProvenanceRejection.UntrustedSource)
        };
    }
}
