using TextExpander.Application.Features.Output;
using TextExpander.Core;
using TextExpander.Core.Matching;
using TextExpander.Infrastructure.Simulation;
using Xunit;

namespace TextExpander.Tests;

public sealed class ExpansionAdmissionTests
{
    private static TriggerReplacementPlan Plan()
    {
        var group = new SnippetGroup(Guid.NewGuid(), "Test", true);
        var snippet = new Snippet(Guid.NewGuid(), group.Id, "Firma", ";firma", "Caffè ☕ مرحبا", "", true, 1);
        var text = "prima ;firma dopo";
        var caret = "prima ;firma ".Length;
        var token = Guid.NewGuid();
        var snapshot = new ReplacementTargetSnapshot(token, 1, 5, text, caret, caret,
            ReplacementFieldState.VerifiedNormalEditable, ReplacementCapability.VerifiedConditionalReplace);
        var request = new TriggerReplacementRequest(
            new TriggerActivationDecision(TriggerActivationStatus.Ready, snippet, 1),
            new ReplacementOrigin(token, 1, 5, 2), snapshot, ";firma", " ");
        var preparation = TriggerReplacementPlanner.Prepare(request, 2);
        Assert.True(preparation.IsReady, preparation.Rejection.ToString());
        return Assert.IsType<TriggerReplacementPlan>(preparation.Plan);
    }

    private static ExpansionAdmissionEvidence Valid(TriggerReplacementPlan plan) => new(
        ExpansionTargetScope.OwnedCooperatingLaboratory,
        ExpansionInputProvenance.VerifiedCommittedTextDelta,
        plan.ExpectedTarget,
        ExplicitOptIn: true,
        Paused: false,
        ProcessExcluded: false,
        FocusVerified: true,
        CompositionStateKnown: true,
        IsComposing: false,
        ModifiersPressed: false);

    [Fact]
    public void MissingPlanOrEvidenceFailsClosed()
    {
        var plan = Plan();
        Assert.Equal(ExpansionAdmissionRejection.MissingPlan,
            ExpansionAdmissionPolicy.Evaluate(null, Valid(plan)).Rejection);
        Assert.Equal(ExpansionAdmissionRejection.MissingEvidence,
            ExpansionAdmissionPolicy.Evaluate(plan, null).Rejection);
    }

    [Fact]
    public void ManualOptInIsMandatory() => Denied(
        e => e with { ExplicitOptIn = false }, ExpansionAdmissionRejection.NotOptedIn);

    [Fact]
    public void PausedSessionCannotReplace() => Denied(
        e => e with { Paused = true }, ExpansionAdmissionRejection.Paused);

    [Fact]
    public void ExcludedProcessCannotReplace() => Denied(
        e => e with { ProcessExcluded = true }, ExpansionAdmissionRejection.ProcessExcluded);

    [Theory]
    [InlineData(ExpansionTargetScope.Unknown)]
    [InlineData(ExpansionTargetScope.ExternalApplication)]
    public void NoExternalEditorIsQualifiedYet(ExpansionTargetScope scope) => Denied(
        e => e with { Scope = scope }, ExpansionAdmissionRejection.ExternalTargetNotQualified);

    [Theory]
    [InlineData(ExpansionInputProvenance.Unknown)]
    [InlineData(ExpansionInputProvenance.RawKeyboardEvents)]
    public void PhysicalKeyEventsAreNotCommittedText(ExpansionInputProvenance provenance) => Denied(
        e => e with { Provenance = provenance }, ExpansionAdmissionRejection.UntrustedInput);

    [Fact]
    public void LostFocusIsDenied() => Denied(
        e => e with { FocusVerified = false }, ExpansionAdmissionRejection.FocusNotVerified);

    [Fact]
    public void UnknownImeStateIsDenied() => Denied(
        e => e with { CompositionStateKnown = false }, ExpansionAdmissionRejection.CompositionUnknown);

    [Fact]
    public void ActiveCompositionIsDenied() => Denied(
        e => e with { IsComposing = true }, ExpansionAdmissionRejection.CompositionActive);

    [Fact]
    public void HeldModifiersAreDenied() => Denied(
        e => e with { ModifiersPressed = true }, ExpansionAdmissionRejection.ModifiersActive);

    [Fact]
    public void MissingSnapshotIsDenied() => Denied(
        e => e with { Snapshot = null }, ExpansionAdmissionRejection.SnapshotMissing);

    [Fact]
    public void ChangedCaretIsDenied() => Denied(
        e => e with { Snapshot = e.Snapshot! with { SelectionStart = 0 } },
        ExpansionAdmissionRejection.TargetChanged);

    [Fact]
    public void ChangedRevisionIsDenied() => Denied(
        e => e with { Snapshot = e.Snapshot! with { TextRevision = 6 } },
        ExpansionAdmissionRejection.TargetChanged);

    [Fact]
    public void ChangedTargetTokenIsDenied() => Denied(
        e => e with { Snapshot = e.Snapshot! with { TargetToken = Guid.NewGuid() } },
        ExpansionAdmissionRejection.TargetChanged);

    [Fact]
    public void ChangedUnicodeSourceIsDenied() => Denied(
        e => e with { Snapshot = e.Snapshot! with { Text = "prima ;firma 🎉dopo" } },
        ExpansionAdmissionRejection.TargetChanged);

    [Theory]
    [InlineData(ReplacementFieldState.Unknown)]
    [InlineData(ReplacementFieldState.Protected)]
    [InlineData(ReplacementFieldState.ReadOnly)]
    [InlineData(ReplacementFieldState.Composing)]
    [InlineData(ReplacementFieldState.Disabled)]
    public void UnsafeFieldNeverGetsAdmission(ReplacementFieldState state) => Denied(
        e => e with { Snapshot = e.Snapshot! with { FieldState = state, Text = null } },
        ExpansionAdmissionRejection.UnsafeField);

    [Fact]
    public void UnsupportedBackendCapabilityIsDenied() => Denied(
        e => e with { Snapshot = e.Snapshot! with { Capability = ReplacementCapability.Unknown } },
        ExpansionAdmissionRejection.CapabilityUnavailable);

    [Fact]
    public void SnapshotPendingModifiersAreDenied() => Denied(
        e => e with { Snapshot = e.Snapshot! with { HasPendingModifiers = true } },
        ExpansionAdmissionRejection.UnsafeField);

    [Fact]
    public void ValidCooperatingLaboratoryExecutesAndVerifiesUnicode()
    {
        var plan = Plan();
        var target = new SimulatedReplacementTarget(plan.ExpectedTarget);
        var outcome = QualifiedReplacementDispatcher.Execute(plan, target, Valid(plan), 2);
        Assert.True(outcome.Admission.Allowed);
        Assert.Equal(ReplacementExecutionStatus.VerifiedComplete, outcome.Execution.Status);
        Assert.Equal(plan.ExpectedResult, target.ReadSnapshot());
        Assert.False(outcome.Execution.RetryAllowedAutomatically);
    }

    [Fact]
    public void RejectedAdmissionNeverTouchesBackendAndDoesNotConsumePlan()
    {
        var plan = Plan();
        var target = new CountingTarget(plan.ExpectedTarget);
        var rejected = QualifiedReplacementDispatcher.Execute(plan, target,
            Valid(plan) with { Scope = ExpansionTargetScope.ExternalApplication }, 2);
        Assert.Equal(ReplacementExecutionReason.AdmissionRejected, rejected.Execution.Reason);
        Assert.Equal(ReplacementExecutionStatus.BlockedNoChange, rejected.Execution.Status);
        Assert.Equal(0, target.Calls);
        Assert.Equal(plan.ExpectedTarget, target.ReadSnapshot());
        // In-memory policy rejection has no effect, and has not claimed the single-use plan.
        var permitted = QualifiedReplacementDispatcher.Execute(plan, target, Valid(plan), 2);
        Assert.Equal(ReplacementExecutionStatus.VerifiedComplete, permitted.Execution.Status);
        Assert.Equal(1, target.Calls);
    }

    [Fact]
    public void StaleLibraryStillRejectedByOriginalExecutor()
    {
        var plan = Plan();
        var target = new CountingTarget(plan.ExpectedTarget);
        var outcome = QualifiedReplacementDispatcher.Execute(plan, target, Valid(plan), 3);
        Assert.True(outcome.Admission.Allowed);
        Assert.Equal(ReplacementExecutionReason.LibraryChanged, outcome.Execution.Reason);
        Assert.Equal(0, target.Calls);
        Assert.Equal(plan.ExpectedTarget, target.ReadSnapshot());
    }

    [Fact]
    public void EvidenceCannotLeakInputThroughToString()
    {
        var plan = Plan();
        var text = Valid(plan).ToString();
        Assert.DoesNotContain("firma", text.ToLowerInvariant());
        Assert.DoesNotContain("caffè", text.ToLowerInvariant());
    }

    private static void Denied(Func<ExpansionAdmissionEvidence, ExpansionAdmissionEvidence> change,
        ExpansionAdmissionRejection expected)
    {
        var plan = Plan();
        var changed = change(Valid(plan));
        Assert.Equal(expected, ExpansionAdmissionPolicy.Evaluate(plan, changed).Rejection);
        var target = new CountingTarget(plan.ExpectedTarget);
        var dispatch = QualifiedReplacementDispatcher.Execute(plan, target, changed, 2);
        Assert.Equal(ReplacementExecutionStatus.BlockedNoChange, dispatch.Execution.Status);
        Assert.Equal(0, target.Calls);
        Assert.Equal(plan.ExpectedTarget, target.ReadSnapshot());
    }

    private sealed class CountingTarget : IConditionalReplacementTarget
    {
        private readonly SimulatedReplacementTarget _inner;
        public int Calls { get; private set; }
        public CountingTarget(ReplacementTargetSnapshot initial) => _inner = new(initial);
        public ReplacementTargetSnapshot ReadSnapshot() => _inner.ReadSnapshot();
        public ConditionalReplacementResult CompareAndReplace(TriggerReplacementPlan plan, CancellationToken cancellationToken)
        {
            Calls++;
            return _inner.CompareAndReplace(plan, cancellationToken);
        }
    }
}
