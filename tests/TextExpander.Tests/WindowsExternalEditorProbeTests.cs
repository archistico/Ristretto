using TextExpander.Application.Features.Output;
using TextExpander.Infrastructure.Platform.Windows;
using Xunit;

namespace TextExpander.Tests;

/// <summary>Pure, cross-platform tests. They do not query or modify any desktop window.</summary>
public sealed class WindowsExternalEditorProbeTests
{
    private static ExternalEditorProbeObservation PlainEdit() => new(
        ForegroundAvailable: true,
        FocusAvailable: true,
        ExternalProcess: true,
        FocusProcessMatchesForeground: true,
        FocusInsideForeground: true,
        Visible: true,
        Enabled: true,
        Family: ExternalControlFamily.ClassicEdit,
        IsPassword: false,
        IsReadOnly: false);

    [Theory]
    [InlineData("Edit", ExternalControlFamily.ClassicEdit)]
    [InlineData("EDIT", ExternalControlFamily.ClassicEdit)]
    [InlineData("RichEdit20W", ExternalControlFamily.RichEdit)]
    [InlineData("RichEdit50W", ExternalControlFamily.RichEdit)]
    [InlineData("Chrome_RenderWidgetHostHWND", ExternalControlFamily.Other)]
    [InlineData("WindowsForms10.EDIT.app.0.123", ExternalControlFamily.Other)]
    [InlineData("", ExternalControlFamily.Unknown)]
    [InlineData(null, ExternalControlFamily.Unknown)]
    public void ControlFamilyUsesConservativeExactMatching(string? raw, ExternalControlFamily expected) =>
        Assert.Equal(expected, WindowsExternalEditorProbeReader.ClassifyControl(raw));

    [Fact]
    public void MissingObservationFailsClosed() =>
        Assert.Equal(ExternalEditorProbeStatus.MissingObservation,
            WindowsExternalEditorProbePolicy.Evaluate(null).Status);

    [Theory]
    [InlineData(false, true, true, ExternalEditorProbeStatus.ForegroundUnavailable)]
    [InlineData(true, false, true, ExternalEditorProbeStatus.FocusUnavailable)]
    [InlineData(true, true, false, ExternalEditorProbeStatus.OwnProcess)]
    public void AbsentForegroundFocusOrExternalityCannotBeCandidate(
        bool foreground, bool focus, bool external, ExternalEditorProbeStatus expected) =>
        Assert.Equal(expected, WindowsExternalEditorProbePolicy.Evaluate(PlainEdit() with
        {
            ForegroundAvailable = foreground, FocusAvailable = focus, ExternalProcess = external
        }).Status);

    [Fact]
    public void FocusFromDifferentProcessIsRejected() =>
        Assert.Equal(ExternalEditorProbeStatus.FocusProcessMismatch,
            WindowsExternalEditorProbePolicy.Evaluate(PlainEdit() with
            { FocusProcessMatchesForeground = false }).Status);

    [Fact]
    public void FocusOutsideForegroundRootIsRejected() =>
        Assert.Equal(ExternalEditorProbeStatus.FocusOutsideForeground,
            WindowsExternalEditorProbePolicy.Evaluate(PlainEdit() with
            { FocusInsideForeground = false }).Status);

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void InvisibleOrDisabledIsRejected(bool visible, bool enabled) =>
        Assert.Equal(ExternalEditorProbeStatus.ControlUnavailable,
            WindowsExternalEditorProbePolicy.Evaluate(PlainEdit() with
            { Visible = visible, Enabled = enabled }).Status);

    [Theory]
    [InlineData(ExternalControlFamily.Unknown)]
    [InlineData(ExternalControlFamily.Other)]
    [InlineData(ExternalControlFamily.RichEdit)]
    public void OtherTechnologiesNeverInheritClassicEditCapability(ExternalControlFamily family) =>
        Assert.Equal(ExternalEditorProbeStatus.TechnologyNotQualified,
            WindowsExternalEditorProbePolicy.Evaluate(PlainEdit() with
            { Family = family }).Status);

    [Theory]
    [InlineData(null, false)]
    [InlineData(false, null)]
    [InlineData(null, null)]
    public void MissingStyleEvidenceFailsClosed(bool? password, bool? readOnly) =>
        Assert.Equal(ExternalEditorProbeStatus.ControlStateUnknown,
            WindowsExternalEditorProbePolicy.Evaluate(PlainEdit() with
            { IsPassword = password, IsReadOnly = readOnly }).Status);

    [Fact]
    public void ProtectedEditIsRejected() =>
        Assert.Equal(ExternalEditorProbeStatus.PasswordControl,
            WindowsExternalEditorProbePolicy.Evaluate(PlainEdit() with
            { IsPassword = true }).Status);

    [Fact]
    public void ReadOnlyEditIsRejected() =>
        Assert.Equal(ExternalEditorProbeStatus.ReadOnlyControl,
            WindowsExternalEditorProbePolicy.Evaluate(PlainEdit() with
            { IsReadOnly = true }).Status);

    [Fact]
    public void ClassicEditIsOnlyAResearchCandidateNotAuthorizedForOutput()
    {
        var result = WindowsExternalEditorProbePolicy.Evaluate(PlainEdit());
        Assert.Equal(ExternalEditorProbeStatus.ClassicEditCandidateNotQualified, result.Status);
        Assert.True(result.CandidateForFurtherResearch);
        Assert.Equal(ReplacementCapability.Unsupported, result.OutputCapability);
        Assert.False(result.CanReadCommittedText);
        Assert.False(result.CanReplaceText);
    }

    [Fact]
    public void AllOutcomesPermanentlyDenyReplacement()
    {
        foreach (var status in Enum.GetValues<ExternalEditorProbeStatus>())
        {
            var assessment = new ExternalEditorProbeAssessment(status);
            Assert.Equal(ReplacementCapability.Unsupported, assessment.OutputCapability);
            Assert.False(assessment.CanReplaceText);
            Assert.False(assessment.CanReadCommittedText);
        }
    }

    [Fact]
    public void ObservationToStringNeverIncludesSourceMetadata()
    {
        Assert.Equal(nameof(ExternalEditorProbeObservation), PlainEdit().ToString());
    }
}
