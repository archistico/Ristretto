namespace TextExpander.Application.Features.Output;

public enum ConditionalReplacementStatus { RejectedNoChange, Applied, PartialOrUnknown }

public sealed record ConditionalReplacementResult(
    ConditionalReplacementStatus Status, ReplacementTargetSnapshot? ObservedAfter = null);

/// <summary>
/// Qualified targets must compare the complete expected snapshot and perform the edit in
/// one indivisible operation, with text revision advanced by exactly one. A separate
/// focus check followed by SendInput does NOT satisfy this contract.
/// RejectedNoChange may be returned only when absence of effects is known.
/// Implemented by the simulated target and by the cooperating owned Win32 test editor.
/// No production adapter for arbitrary external editors currently satisfies this contract.
/// </summary>
public interface IConditionalReplacementTarget
{
    ConditionalReplacementResult CompareAndReplace(TriggerReplacementPlan plan, CancellationToken cancellationToken);
}

public enum ReplacementExecutionStatus { BlockedNoChange, VerifiedComplete, SubmittedUnverified, PartialOrUnknown }
public enum ReplacementExecutionReason
{
    None, AlreadyAttempted, CancelledBeforeEffect, LibraryChanged, AdmissionRejected, TargetRejected,
    MissingVerification, VerificationMismatch, BackendUnknown, BackendException
}

public sealed record ReplacementExecutionOutcome(ReplacementExecutionStatus Status, ReplacementExecutionReason Reason)
{
    public bool MayHaveModifiedTarget => Status != ReplacementExecutionStatus.BlockedNoChange;
    public bool RetryAllowedAutomatically => false;
}

public static class TriggerReplacementExecutor
{
    public static ReplacementExecutionOutcome Execute(TriggerReplacementPlan plan, IConditionalReplacementTarget target,
        long currentLibraryRevision, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(target);
        if (!plan.TryClaim()) return new(ReplacementExecutionStatus.BlockedNoChange, ReplacementExecutionReason.AlreadyAttempted);
        if (cancellationToken.IsCancellationRequested)
            return new(ReplacementExecutionStatus.BlockedNoChange, ReplacementExecutionReason.CancelledBeforeEffect);
        if (plan.Origin.LibraryRevision != currentLibraryRevision)
            return new(ReplacementExecutionStatus.BlockedNoChange, ReplacementExecutionReason.LibraryChanged);

        // Once the backend is entered, exceptions/cancellation may have happened after an effect.
        // Never retry or perform a compensating deletion, even when no receipt is available.
        ConditionalReplacementResult? result;
        try { result = target.CompareAndReplace(plan, cancellationToken); }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return new(ReplacementExecutionStatus.PartialOrUnknown, ReplacementExecutionReason.BackendException);
        }
        if (result?.Status == ConditionalReplacementStatus.RejectedNoChange)
            return new(ReplacementExecutionStatus.BlockedNoChange, ReplacementExecutionReason.TargetRejected);
        if (result?.Status != ConditionalReplacementStatus.Applied)
            return new(ReplacementExecutionStatus.PartialOrUnknown, ReplacementExecutionReason.BackendUnknown);
        if (result.ObservedAfter is null)
            return new(ReplacementExecutionStatus.SubmittedUnverified, ReplacementExecutionReason.MissingVerification);
        return result.ObservedAfter == plan.ExpectedResult
            ? new(ReplacementExecutionStatus.VerifiedComplete, ReplacementExecutionReason.None)
            : new(ReplacementExecutionStatus.PartialOrUnknown, ReplacementExecutionReason.VerificationMismatch);
    }
}
