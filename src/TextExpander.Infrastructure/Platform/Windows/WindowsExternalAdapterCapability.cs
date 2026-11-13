using TextExpander.Application.Features.Output;

namespace TextExpander.Infrastructure.Platform.Windows;

/// <summary>
/// Observational evidence only, never an authorization to read user text or write to an
/// external editor. Flags represent the *category* of evidence, not a retained HWND,
/// PID, selection offset, window title or document content.
/// </summary>
[Flags]
public enum ExternalAdapterObservedEvidence
{
    None = 0,
    ClassicEditMetadataCandidate = 1,
    SelectionCategoryObserved = 2
}

/// <summary>
/// Guarantees that a future independently qualified native adapter must prove on its
/// own target. These are not granted by EM_GETSEL or by foreground metadata.
/// </summary>
[Flags]
public enum ExternalAdapterMissingProof
{
    None = 0,
    TrustedCommittedUnicodeSource = 1,
    StableTargetAndDocumentRevision = 2,
    VerifiedFocusAndProtectedFieldState = 4,
    ImeAndModifierState = 8,
    GraphemeSafeTriggerRange = 16,
    AtomicConditionalReplacement = 32,
    VerifiedPostEditResult = 64,
    ExplicitUserConsentAndExclusions = 128
}

public enum ExternalAdapterResearchStatus
{
    MissingEvidence,
    PreflightRejected,
    FocusNotStable,
    SelectionQueryUnavailable,
    SelectionUnrepresentable,
    SelectionInvalid,
    SelectionObservedReadOnly
}

/// <summary>
/// Research result, not a capability token. It deliberately cannot be upgraded by
/// its caller into ReplacementCapability.VerifiedConditionalReplace.
/// </summary>
public sealed record ExternalAdapterCapabilityAssessment(
    ExternalAdapterResearchStatus Status,
    ExternalAdapterObservedEvidence Observed,
    ExternalAdapterMissingProof Missing)
{
    public ReplacementCapability OutputCapability => ReplacementCapability.Unsupported;
    public bool CanReadCommittedText => false;
    public bool CanReplaceText => false;
    public bool CandidateForFurtherResearch =>
        Status == ExternalAdapterResearchStatus.SelectionObservedReadOnly;

    public override string ToString() => nameof(ExternalAdapterCapabilityAssessment);
}

/// <summary>
/// Fail-closed synthesis of the already-existing one-shot Win32 probes. A selection
/// query cannot establish which text was committed, whether the document changed,
/// or whether the same HWND will still have focus at the time of an edit.
/// </summary>
public static class WindowsExternalAdapterCapabilityPolicy
{
    public const ExternalAdapterMissingProof EveryWriteProof =
        ExternalAdapterMissingProof.TrustedCommittedUnicodeSource |
        ExternalAdapterMissingProof.StableTargetAndDocumentRevision |
        ExternalAdapterMissingProof.VerifiedFocusAndProtectedFieldState |
        ExternalAdapterMissingProof.ImeAndModifierState |
        ExternalAdapterMissingProof.GraphemeSafeTriggerRange |
        ExternalAdapterMissingProof.AtomicConditionalReplacement |
        ExternalAdapterMissingProof.VerifiedPostEditResult |
        ExternalAdapterMissingProof.ExplicitUserConsentAndExclusions;

    public static ExternalAdapterCapabilityAssessment Evaluate(ClassicEditSelectionEvidence? observation)
    {
        if (observation is null)
            return new(ExternalAdapterResearchStatus.MissingEvidence,
                ExternalAdapterObservedEvidence.None, EveryWriteProof);

        // Never accept a self-described selection category when its preceding
        // metadata probe did not identify a qualified research candidate.
        if (observation.Preflight?.CandidateForFurtherResearch != true)
            return new(ExternalAdapterResearchStatus.PreflightRejected,
                ExternalAdapterObservedEvidence.None, EveryWriteProof);

        const ExternalAdapterObservedEvidence metadata =
            ExternalAdapterObservedEvidence.ClassicEditMetadataCandidate;

        if (!observation.FocusConsistent)
            return new(ExternalAdapterResearchStatus.FocusNotStable, metadata, EveryWriteProof);
        if (!observation.MessageCompleted)
            return new(ExternalAdapterResearchStatus.SelectionQueryUnavailable, metadata, EveryWriteProof);

        return observation.Signal switch
        {
            ClassicEditSelectionSignal.Collapsed or ClassicEditSelectionSignal.SelectedRange =>
                new(ExternalAdapterResearchStatus.SelectionObservedReadOnly,
                    metadata | ExternalAdapterObservedEvidence.SelectionCategoryObserved, EveryWriteProof),
            ClassicEditSelectionSignal.Unrepresentable =>
                new(ExternalAdapterResearchStatus.SelectionUnrepresentable, metadata, EveryWriteProof),
            ClassicEditSelectionSignal.InvalidRange =>
                new(ExternalAdapterResearchStatus.SelectionInvalid, metadata, EveryWriteProof),
            _ => new(ExternalAdapterResearchStatus.SelectionQueryUnavailable, metadata, EveryWriteProof)
        };
    }
}
