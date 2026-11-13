using TextExpander.Application.Features.Output;
using TextExpander.Infrastructure.Platform.Windows;
using Xunit;

namespace TextExpander.Tests;

public sealed class WindowsExternalAdapterCapabilityTests
{
    private static ClassicEditSelectionEvidence Evidence(ClassicEditSelectionSignal signal) =>
        new(new ExternalEditorProbeAssessment(
                ExternalEditorProbeStatus.ClassicEditCandidateNotQualified),
            FocusConsistent: true, MessageCompleted: true, Signal: signal);

    [Fact]
    public void MissingEvidenceRemainsEntirelyUnsupported()
    {
        var actual = WindowsExternalAdapterCapabilityPolicy.Evaluate(null);
        Assert.Equal(ExternalAdapterResearchStatus.MissingEvidence, actual.Status);
        Assert.Equal(ExternalAdapterObservedEvidence.None, actual.Observed);
        Assert.Equal(WindowsExternalAdapterCapabilityPolicy.EveryWriteProof, actual.Missing);
        Assert.False(actual.CanReplaceText);
    }

    [Theory]
    [InlineData(ExternalEditorProbeStatus.PasswordControl)]
    [InlineData(ExternalEditorProbeStatus.ReadOnlyControl)]
    [InlineData(ExternalEditorProbeStatus.TechnologyNotQualified)]
    [InlineData(ExternalEditorProbeStatus.ControlStateUnknown)]
    [InlineData(ExternalEditorProbeStatus.FocusProcessMismatch)]
    [InlineData(ExternalEditorProbeStatus.FocusOutsideForeground)]
    [InlineData(ExternalEditorProbeStatus.OwnProcess)]
    [InlineData(ExternalEditorProbeStatus.ForegroundUnavailable)]
    public void RejectedMetadataCannotBeOverriddenByASelectionSignal(ExternalEditorProbeStatus status)
    {
        var result = WindowsExternalAdapterCapabilityPolicy.Evaluate(Evidence(ClassicEditSelectionSignal.Collapsed) with
        { Preflight = new ExternalEditorProbeAssessment(status) });
        Assert.Equal(ExternalAdapterResearchStatus.PreflightRejected, result.Status);
        Assert.Equal(ExternalAdapterObservedEvidence.None, result.Observed);
        Assert.False(result.CandidateForFurtherResearch);
    }

    [Fact]
    public void UnknownPreflightCannotBecomeCandidate()
    {
        var result = WindowsExternalAdapterCapabilityPolicy.Evaluate(Evidence(ClassicEditSelectionSignal.Collapsed) with
        { Preflight = null });
        Assert.Equal(ExternalAdapterResearchStatus.PreflightRejected, result.Status);
        Assert.False(result.CanReadCommittedText);
    }

    [Fact]
    public void FocusChangeInvalidatesSelectionEvidence()
    {
        var result = WindowsExternalAdapterCapabilityPolicy.Evaluate(Evidence(ClassicEditSelectionSignal.SelectedRange) with
        { FocusConsistent = false });
        Assert.Equal(ExternalAdapterResearchStatus.FocusNotStable, result.Status);
        Assert.Equal(ExternalAdapterObservedEvidence.ClassicEditMetadataCandidate, result.Observed);
    }

    [Fact]
    public void TimeoutNeverProducesCaretClaim()
    {
        var result = WindowsExternalAdapterCapabilityPolicy.Evaluate(Evidence(ClassicEditSelectionSignal.Collapsed) with
        { MessageCompleted = false });
        Assert.Equal(ExternalAdapterResearchStatus.SelectionQueryUnavailable, result.Status);
        Assert.Equal(ExternalAdapterObservedEvidence.ClassicEditMetadataCandidate, result.Observed);
    }

    [Theory]
    [InlineData(ClassicEditSelectionSignal.Unknown, ExternalAdapterResearchStatus.SelectionQueryUnavailable)]
    [InlineData(ClassicEditSelectionSignal.Unrepresentable, ExternalAdapterResearchStatus.SelectionUnrepresentable)]
    [InlineData(ClassicEditSelectionSignal.InvalidRange, ExternalAdapterResearchStatus.SelectionInvalid)]
    public void UnusableSelectionCannotBeResearchReady(
        ClassicEditSelectionSignal signal, ExternalAdapterResearchStatus status)
    {
        var result = WindowsExternalAdapterCapabilityPolicy.Evaluate(Evidence(signal));
        Assert.Equal(status, result.Status);
        Assert.False(result.CandidateForFurtherResearch);
    }

    [Theory]
    [InlineData(ClassicEditSelectionSignal.Collapsed)]
    [InlineData(ClassicEditSelectionSignal.SelectedRange)]
    public void ObservedCaretOrRangeStillLeavesEveryWriteProofMissing(ClassicEditSelectionSignal signal)
    {
        var result = WindowsExternalAdapterCapabilityPolicy.Evaluate(Evidence(signal));
        Assert.Equal(ExternalAdapterResearchStatus.SelectionObservedReadOnly, result.Status);
        Assert.True(result.CandidateForFurtherResearch);
        Assert.Equal(ExternalAdapterObservedEvidence.ClassicEditMetadataCandidate |
            ExternalAdapterObservedEvidence.SelectionCategoryObserved, result.Observed);
        Assert.Equal(WindowsExternalAdapterCapabilityPolicy.EveryWriteProof, result.Missing);
        Assert.Equal(ReplacementCapability.Unsupported, result.OutputCapability);
        Assert.False(result.CanReadCommittedText);
        Assert.False(result.CanReplaceText);
    }

    [Fact]
    public void EveryResearchStatusIsDeniedOutput()
    {
        foreach (var status in Enum.GetValues<ExternalAdapterResearchStatus>())
        {
            var result = new ExternalAdapterCapabilityAssessment(status,
                ExternalAdapterObservedEvidence.ClassicEditMetadataCandidate |
                ExternalAdapterObservedEvidence.SelectionCategoryObserved,
                WindowsExternalAdapterCapabilityPolicy.EveryWriteProof);
            Assert.Equal(ReplacementCapability.Unsupported, result.OutputCapability);
            Assert.False(result.CanReadCommittedText);
            Assert.False(result.CanReplaceText);
        }
    }

    [Fact]
    public void EveryRequiredWriteProofIsExplicitlyDeclared()
    {
        var requirements = Enum.GetValues<ExternalAdapterMissingProof>()
            .Where(value => value != ExternalAdapterMissingProof.None)
            .Aggregate(ExternalAdapterMissingProof.None, (a, b) => a | b);
        Assert.Equal(requirements, WindowsExternalAdapterCapabilityPolicy.EveryWriteProof);
        Assert.Equal(8, Enum.GetValues<ExternalAdapterMissingProof>().Length - 1);
    }

    [Fact]
    public void SelectionObservationCannotSatisfyRevisionOrAtomicEditRequirements()
    {
        var result = WindowsExternalAdapterCapabilityPolicy.Evaluate(
            Evidence(ClassicEditSelectionSignal.SelectedRange));
        Assert.True(result.Missing.HasFlag(ExternalAdapterMissingProof.StableTargetAndDocumentRevision));
        Assert.True(result.Missing.HasFlag(ExternalAdapterMissingProof.AtomicConditionalReplacement));
        Assert.True(result.Missing.HasFlag(ExternalAdapterMissingProof.TrustedCommittedUnicodeSource));
        Assert.True(result.Missing.HasFlag(ExternalAdapterMissingProof.GraphemeSafeTriggerRange));
        Assert.False(result.CanReplaceText);
    }

    [Fact]
    public void DiagnosticStringDoesNotPrintRawEvidence()
    {
        var result = WindowsExternalAdapterCapabilityPolicy.Evaluate(Evidence(ClassicEditSelectionSignal.Collapsed));
        Assert.Equal(nameof(ExternalAdapterCapabilityAssessment), result.ToString());
    }
}
