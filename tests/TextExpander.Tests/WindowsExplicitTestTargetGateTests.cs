using TextExpander.Infrastructure.Platform.Windows;
using Xunit;

namespace TextExpander.Tests;

public sealed class WindowsExplicitTestTargetGateTests
{
    private static WindowsExplicitTestTargetSnapshot Valid() => new(
        ExpectedWindow: (nint)101,
        ObservedForegroundWindow: (nint)101,
        ExpectedProcessId: 42,
        ObservedProcessId: 42,
        ExpectedEditControl: (nint)202,
        ObservedFocusedControl: (nint)202,
        IsClassicEditControl: true,
        IsVisible: true,
        IsEnabled: true,
        IsPassword: false,
        IsReadOnly: false,
        IsComposingOrUnknown: false);

    [Fact]
    public void ExplicitKnownPlainEditCanPassPreflight() =>
        Assert.Null(WindowsExplicitTestTargetGate.RejectionReason(Valid()));

    [Fact]
    public void MissingEvidenceFailsClosed() =>
        Assert.Equal("snapshot_missing", WindowsExplicitTestTargetGate.RejectionReason(null));

    [Fact]
    public void MissingHandleFailsClosed() =>
        Assert.Equal("target_invalid", WindowsExplicitTestTargetGate.RejectionReason(
            Valid() with { ExpectedEditControl = 0 }));

    [Fact]
    public void ChangedForegroundIsRejected() =>
        Assert.Equal("target_changed", WindowsExplicitTestTargetGate.RejectionReason(
            Valid() with { ObservedForegroundWindow = (nint)302 }));

    [Fact]
    public void ChangedProcessIsRejected() =>
        Assert.Equal("target_changed", WindowsExplicitTestTargetGate.RejectionReason(
            Valid() with { ObservedProcessId = 99 }));

    [Fact]
    public void FocusOnDifferentControlIsRejected() =>
        Assert.Equal("focus_changed", WindowsExplicitTestTargetGate.RejectionReason(
            Valid() with { ObservedFocusedControl = (nint)303 }));

    [Fact]
    public void UnknownEditorClassIsRejected() =>
        Assert.Equal("unsupported_control", WindowsExplicitTestTargetGate.RejectionReason(
            Valid() with { IsClassicEditControl = false }));

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void InvisibleOrDisabledControlIsRejected(bool invisible, bool disabled) =>
        Assert.Equal("control_unavailable", WindowsExplicitTestTargetGate.RejectionReason(
            Valid() with { IsVisible = !invisible, IsEnabled = !disabled }));

    [Fact]
    public void PasswordControlIsRejected() =>
        Assert.Equal("password_control", WindowsExplicitTestTargetGate.RejectionReason(
            Valid() with { IsPassword = true }));

    [Fact]
    public void ReadOnlyControlIsRejected() =>
        Assert.Equal("read_only_control", WindowsExplicitTestTargetGate.RejectionReason(
            Valid() with { IsReadOnly = true }));

    [Fact]
    public void CompositionUnknownIsRejected() =>
        Assert.Equal("composition_unknown", WindowsExplicitTestTargetGate.RejectionReason(
            Valid() with { IsComposingOrUnknown = true }));
}
