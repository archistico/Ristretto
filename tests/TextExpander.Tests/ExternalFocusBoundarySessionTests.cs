using TextExpander.Application.Features.Input;
using TextExpander.Core;
using TextExpander.Core.Matching;
using Xunit;

namespace TextExpander.Tests;

public sealed class ExternalFocusBoundarySessionTests
{
    private static LibrarySnapshot Library()
    {
        var group = new SnippetGroup(Guid.NewGuid(), "Confini", true);
        var snippet = new Snippet(Guid.NewGuid(), group.Id, "Firma", ";firma", "TEST", "", true, 1);
        return new LibrarySnapshot([group], [snippet], 1);
    }

    private static CommittedInputObservation Verified(string text) =>
        new(1, text, TextObservationKind.VerifiedCommitted,
            ObservedFieldSafety.VerifiedNormalEditable);

    [Theory]
    [InlineData(ExternalFocusBoundaryKind.ForegroundChanged)]
    [InlineData(ExternalFocusBoundaryKind.ControlFocusChanged)]
    [InlineData(ExternalFocusBoundaryKind.ObservationUnavailable)]
    [InlineData(ExternalFocusBoundaryKind.ObserverStopped)]
    public void EachBoundaryPreventsCompletionOfAPreviouslyTypedTrigger(ExternalFocusBoundaryKind kind)
    {
        var snapshot = Library();
        var activation = new TriggerActivationSession(snapshot);
        var recognition = new CommittedInputSession(snapshot);
        var session = new ExternalFocusBoundarySession(activation, recognition);
        Assert.Equal(TriggerActivationStatus.WaitingForDelimiter,
            activation.Observe(Verified(";firma"), 10).Status);
        Assert.Equal(TriggerRecognitionStatus.Matched,
            recognition.Observe(Verified(";firma")).Status);

        var receipt = session.Observe(kind);

        Assert.True(receipt.RecognitionCleared);
        Assert.False(receipt.CanAuthorizeExternalExpansion);
        Assert.Equal(1, receipt.BoundaryCount);
        Assert.Equal(TriggerActivationStatus.None,
            activation.Observe(Verified(" "), 11).Status);
        Assert.NotEqual(TriggerRecognitionStatus.Matched,
            recognition.Observe(Verified(" ")).Status);
    }

    [Fact]
    public void NewVerifiedTextAfterBoundaryCanStartOnlyAFreshRecognition()
    {
        var snapshot = Library();
        var activation = new TriggerActivationSession(snapshot);
        var session = new ExternalFocusBoundarySession(activation, new CommittedInputSession(snapshot));
        activation.Observe(Verified(";firma"), 1);
        session.Observe(ExternalFocusBoundaryKind.ForegroundChanged);
        Assert.Equal(TriggerActivationStatus.WaitingForDelimiter,
            activation.Observe(Verified(";firma"), 2).Status);
        Assert.Equal(TriggerActivationStatus.Ready,
            activation.Observe(Verified(" "), 3).Status);
    }

    [Fact]
    public void RepeatedSignalsRemainFailClosedAndNeverBecomeAnExpansionPermission()
    {
        var snapshot = Library();
        var session = new ExternalFocusBoundarySession(
            new TriggerActivationSession(snapshot), new CommittedInputSession(snapshot));
        for (var i = 1; i <= 20; ++i)
        {
            var receipt = session.Observe(ExternalFocusBoundaryKind.ControlFocusChanged);
            Assert.Equal((long)i, receipt.BoundaryCount);
            Assert.False(receipt.CanAuthorizeExternalExpansion);
            Assert.Equal(nameof(ExternalFocusBoundaryReceipt), receipt.ToString());
        }
    }

    [Fact]
    public void InvalidBoundaryCannotChangeState()
    {
        var snapshot = Library();
        var session = new ExternalFocusBoundarySession(
            new TriggerActivationSession(snapshot), new CommittedInputSession(snapshot));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Observe((ExternalFocusBoundaryKind)999));
        Assert.Equal(0, session.BoundaryCount);
    }

    [Fact]
    public void UnknownKeyboardEventsStillCannotProveCommittedText()
    {
        var snapshot = Library();
        var activation = new TriggerActivationSession(snapshot);
        var session = new ExternalFocusBoundarySession(activation, new CommittedInputSession(snapshot));
        session.Observe(ExternalFocusBoundaryKind.ForegroundChanged);
        var unknown = new CommittedInputObservation(1, ";firma ",
            TextObservationKind.Unknown, ObservedFieldSafety.Unknown);
        Assert.Equal(TriggerActivationStatus.Suppressed,
            activation.Observe(unknown, 10).Status);
    }
}
