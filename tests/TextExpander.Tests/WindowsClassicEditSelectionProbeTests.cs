using TextExpander.Infrastructure.Platform.Windows;
using Xunit;

namespace TextExpander.Tests;

public sealed class WindowsClassicEditSelectionProbeTests
{
    private static ExternalEditorProbeAssessment Candidate() =>
        new(ExternalEditorProbeStatus.ClassicEditCandidateNotQualified);

    private static ClassicEditSelectionEvidence Valid(ClassicEditSelectionSignal signal) =>
        new(Candidate(), FocusConsistent: true, MessageCompleted: true, Signal: signal);

    [Theory]
    [InlineData(0u, ClassicEditSelectionSignal.Collapsed)]
    [InlineData(0x00010001u, ClassicEditSelectionSignal.Collapsed)]
    [InlineData(0x00640064u, ClassicEditSelectionSignal.Collapsed)]
    [InlineData(0x00050002u, ClassicEditSelectionSignal.SelectedRange)]
    [InlineData(0x00020005u, ClassicEditSelectionSignal.InvalidRange)]
    [InlineData(uint.MaxValue, ClassicEditSelectionSignal.Unrepresentable)]
    public void PackedWin32SelectionIsClassifiedWithoutRetainingOffsets(
        uint packed, ClassicEditSelectionSignal expected) =>
        Assert.Equal(expected, WindowsClassicEditSelectionPolicy.DecodePackedRange(packed));

    [Fact]
    public void MissingEvidenceIsRejected() =>
        Assert.Equal(ClassicEditSelectionProbeStatus.NotResearchCandidate,
            WindowsClassicEditSelectionPolicy.Evaluate(null).Status);

    [Theory]
    [InlineData(ExternalEditorProbeStatus.PasswordControl)]
    [InlineData(ExternalEditorProbeStatus.ReadOnlyControl)]
    [InlineData(ExternalEditorProbeStatus.ControlStateUnknown)]
    [InlineData(ExternalEditorProbeStatus.TechnologyNotQualified)]
    [InlineData(ExternalEditorProbeStatus.FocusOutsideForeground)]
    [InlineData(ExternalEditorProbeStatus.OwnProcess)]
    public void ExistingExternalProbeRejectionsRemainBlocking(ExternalEditorProbeStatus prior)
    {
        var evidence = Valid(ClassicEditSelectionSignal.Collapsed) with
        { Preflight = new ExternalEditorProbeAssessment(prior) };
        Assert.Equal(ClassicEditSelectionProbeStatus.NotResearchCandidate,
            WindowsClassicEditSelectionPolicy.Evaluate(evidence).Status);
    }

    [Fact]
    public void ChangingFocusAlwaysDiscardsSelectionResult()
    {
        var evidence = Valid(ClassicEditSelectionSignal.SelectedRange) with { FocusConsistent = false };
        Assert.Equal(ClassicEditSelectionProbeStatus.FocusChanged,
            WindowsClassicEditSelectionPolicy.Evaluate(evidence).Status);
    }

    [Fact]
    public void TimeoutOrUipiFailureDoesNotInferSelection()
    {
        var evidence = Valid(ClassicEditSelectionSignal.Collapsed) with { MessageCompleted = false };
        Assert.Equal(ClassicEditSelectionProbeStatus.QueryUnavailable,
            WindowsClassicEditSelectionPolicy.Evaluate(evidence).Status);
    }

    [Theory]
    [InlineData(ClassicEditSelectionSignal.Unknown, ClassicEditSelectionProbeStatus.QueryUnavailable)]
    [InlineData(ClassicEditSelectionSignal.Unrepresentable, ClassicEditSelectionProbeStatus.RangeUnrepresentable)]
    [InlineData(ClassicEditSelectionSignal.InvalidRange, ClassicEditSelectionProbeStatus.RangeInvalid)]
    [InlineData(ClassicEditSelectionSignal.Collapsed, ClassicEditSelectionProbeStatus.CaretObservedNotQualified)]
    [InlineData(ClassicEditSelectionSignal.SelectedRange, ClassicEditSelectionProbeStatus.SelectionObservedNotQualified)]
    public void AssessmentIsNeverAnOutputGrant(
        ClassicEditSelectionSignal signal, ClassicEditSelectionProbeStatus expected)
    {
        var assessment = WindowsClassicEditSelectionPolicy.Evaluate(Valid(signal));
        Assert.Equal(expected, assessment.Status);
        Assert.False(assessment.CanReadCommittedText);
        Assert.False(assessment.CanReplaceText);
    }

    [Fact]
    public void AllStatusValuesDenyExternalWrites()
    {
        foreach (var status in Enum.GetValues<ClassicEditSelectionProbeStatus>())
        {
            var assessment = new ClassicEditSelectionAssessment(status);
            Assert.False(assessment.CanReadCommittedText);
            Assert.False(assessment.CanReplaceText);
        }
    }

    [Fact]
    public void ObservationToStringCannotAccidentallyLeakPrivateEvidence() =>
        Assert.Equal(nameof(ClassicEditSelectionEvidence),
            Valid(ClassicEditSelectionSignal.SelectedRange).ToString());
}
