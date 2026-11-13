namespace TextExpander.Application.Features.Output;

/// <summary>
/// Claims about how input was observed. Raw key events are NOT committed Unicode text.
/// The producer is responsible for providing trustworthy evidence, not merely setting a flag.
/// </summary>
public enum ExpansionInputProvenance { Unknown, RawKeyboardEvents, VerifiedCommittedTextDelta }

/// <summary>
/// Only the cooperating, locally owned Win32 EDIT laboratory is currently qualified.
/// External applications are intentionally unavailable until a separate native adapter
/// can establish an equivalent contract and receive its own explicit qualification.
/// </summary>
public enum ExpansionTargetScope { Unknown, OwnedCooperatingLaboratory, ExternalApplication }

public enum ExpansionAdmissionRejection
{
    None, MissingPlan, MissingEvidence, NotOptedIn, Paused, ProcessExcluded,
    ExternalTargetNotQualified, UntrustedInput, FocusNotVerified, CompositionUnknown,
    CompositionActive, ModifiersActive, SnapshotMissing, TargetChanged,
    UnsafeField, CapabilityUnavailable
}

/// <summary>
/// Ephemeral, in-memory evidence: never persist or log the snapshot's text.
/// Opt-in and the focus claim must be obtained from the owned laboratory itself.
/// This DTO is a policy input, not an unforgeable OS security capability.
/// </summary>
public sealed record ExpansionAdmissionEvidence(
    ExpansionTargetScope Scope,
    ExpansionInputProvenance Provenance,
    ReplacementTargetSnapshot? Snapshot,
    bool ExplicitOptIn,
    bool Paused,
    bool ProcessExcluded,
    bool FocusVerified,
    bool CompositionStateKnown,
    bool IsComposing,
    bool ModifiersPressed)
{
    public override string ToString() => nameof(ExpansionAdmissionEvidence);
}

public sealed record ExpansionAdmissionDecision(ExpansionAdmissionRejection Rejection)
{
    public bool Allowed => Rejection == ExpansionAdmissionRejection.None;
}

/// <summary>
/// Fail-closed application boundary. The native backend MUST still compare its complete
/// snapshot and perform the edit indivisibly. Admission is never a replacement for that.
/// </summary>
public static class ExpansionAdmissionPolicy
{
    public static ExpansionAdmissionDecision Evaluate(TriggerReplacementPlan? plan, ExpansionAdmissionEvidence? evidence)
    {
        if (plan is null) return new(ExpansionAdmissionRejection.MissingPlan);
        if (evidence is null) return new(ExpansionAdmissionRejection.MissingEvidence);
        if (!evidence.ExplicitOptIn) return new(ExpansionAdmissionRejection.NotOptedIn);
        if (evidence.Paused) return new(ExpansionAdmissionRejection.Paused);
        if (evidence.ProcessExcluded) return new(ExpansionAdmissionRejection.ProcessExcluded);
        if (evidence.Scope != ExpansionTargetScope.OwnedCooperatingLaboratory)
            return new(ExpansionAdmissionRejection.ExternalTargetNotQualified);
        if (evidence.Provenance != ExpansionInputProvenance.VerifiedCommittedTextDelta)
            return new(ExpansionAdmissionRejection.UntrustedInput);
        if (!evidence.FocusVerified) return new(ExpansionAdmissionRejection.FocusNotVerified);
        if (!evidence.CompositionStateKnown) return new(ExpansionAdmissionRejection.CompositionUnknown);
        if (evidence.IsComposing) return new(ExpansionAdmissionRejection.CompositionActive);
        if (evidence.ModifiersPressed) return new(ExpansionAdmissionRejection.ModifiersActive);
        var snapshot = evidence.Snapshot;
        if (snapshot is null) return new(ExpansionAdmissionRejection.SnapshotMissing);
        if (snapshot.FieldState != ReplacementFieldState.VerifiedNormalEditable || snapshot.HasPendingModifiers)
            return new(ExpansionAdmissionRejection.UnsafeField);
        if (snapshot.Capability != ReplacementCapability.VerifiedConditionalReplace)
            return new(ExpansionAdmissionRejection.CapabilityUnavailable);
        if (!plan.MatchesExpectedTarget(snapshot)) return new(ExpansionAdmissionRejection.TargetChanged);
        return new(ExpansionAdmissionRejection.None);
    }
}

public sealed record QualifiedReplacementResult(
    ExpansionAdmissionDecision Admission, ReplacementExecutionOutcome Execution);

/// <summary>
/// Enforces admission before the single-use executor. Denial has no target side effects,
/// and does not consume the plan. A permitted backend attempt still has no automatic retry.
/// </summary>
public static class QualifiedReplacementDispatcher
{
    public static QualifiedReplacementResult Execute(
        TriggerReplacementPlan plan,
        IConditionalReplacementTarget target,
        ExpansionAdmissionEvidence? evidence,
        long currentLibraryRevision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(target);
        var admission = ExpansionAdmissionPolicy.Evaluate(plan, evidence);
        if (!admission.Allowed)
            return new(admission, new(ReplacementExecutionStatus.BlockedNoChange,
                ReplacementExecutionReason.AdmissionRejected));
        return new(admission, TriggerReplacementExecutor.Execute(plan, target,
            currentLibraryRevision, cancellationToken));
    }
}
