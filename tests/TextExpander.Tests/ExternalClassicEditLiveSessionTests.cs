using TextExpander.Application.Features.Input;
using TextExpander.Infrastructure.Platform.Windows;
using Xunit;

namespace TextExpander.Tests;

public sealed class ExternalClassicEditLiveSessionTests
{
    private static readonly ExternalClassicEditFocusToken First = new((nint)100, (nint)101, 200);
    private static readonly ExternalClassicEditFocusToken Second = new((nint)100, (nint)102, 200);

    private static ExternalClassicEditReadResult At(string text, int? caret = null,
        ExternalClassicEditFocusToken? target = null) => new(
        ExternalClassicEditReadStatus.Observed, target ?? First,
        new ExternalClassicEditTextObservation(text, caret ?? text.Length, caret ?? text.Length));

    [Fact]
    public void FirstSnapshotOnlyEstablishesBaseline()
    {
        var session = new ExternalClassicEditLiveSession();
        var result = session.Observe(At(";firma "));
        Assert.Equal(ExternalClassicEditLiveStatus.BaselineEstablished, result.Status);
        Assert.False(result.CanAuthorizeExpansion);
    }

    [Theory]
    [InlineData("", ";firma ")]
    [InlineData("a", "a☕")]
    [InlineData("", "🧑🏽‍💻")]
    [InlineData("", "cafe\u0301")]
    [InlineData("", "مرحبا 漢字")]
    public void UnicodeInsertionsStayCandidatesNotAuthorized(string before, string after)
    {
        var session = new ExternalClassicEditLiveSession();
        session.Observe(At(before));
        var result = session.Observe(At(after));
        Assert.Equal(ExternalClassicEditLiveStatus.CandidateInsertion, result.Status);
        Assert.Equal(ExternalClassicEditTextDeltaStatus.CandidateInsertion, result.DeltaStatus);
        Assert.False(result.CanAuthorizeExpansion);
    }

    [Fact]
    public void NoChangeDoesNotCreateAnotherCandidate()
    {
        var session = new ExternalClassicEditLiveSession();
        session.Observe(At("abc"));
        Assert.Equal(ExternalClassicEditLiveStatus.NoChange, session.Observe(At("abc")).Status);
    }

    [Fact]
    public void ChangingTargetNeverCompletesPreviousSequence()
    {
        var session = new ExternalClassicEditLiveSession();
        session.Observe(At(";fir"));
        Assert.Equal(ExternalClassicEditLiveStatus.TargetChanged,
            session.Observe(At(";firma ", target: Second)).Status);
        Assert.Equal(ExternalClassicEditLiveStatus.CandidateInsertion,
            session.Observe(At(";firma x", target: Second)).Status);
    }

    [Fact]
    public void FailedReadRemovesPreviousBaseline()
    {
        var session = new ExternalClassicEditLiveSession();
        session.Observe(At(";fir"));
        Assert.Equal(ExternalClassicEditLiveStatus.ObservationUnavailable,
            session.Observe(new ExternalClassicEditReadResult(ExternalClassicEditReadStatus.FocusChanged)).Status);
        Assert.Equal(ExternalClassicEditLiveStatus.BaselineEstablished,
            session.Observe(At(";firma ")).Status);
    }

    [Fact]
    public void ExplicitFocusBarrierRemovesBaseline()
    {
        var session = new ExternalClassicEditLiveSession();
        session.Observe(At(";fir"));
        session.Reset();
        Assert.Equal(ExternalClassicEditLiveStatus.BaselineEstablished,
            session.Observe(At(";firma ")).Status);
    }

    [Fact]
    public void DeletesAndReplacementsAreRejected()
    {
        var session = new ExternalClassicEditLiveSession();
        session.Observe(At("abc"));
        Assert.Equal(ExternalClassicEditLiveStatus.DeltaRejected,
            session.Observe(At("ab")).Status);
        Assert.Equal(ExternalClassicEditLiveStatus.DeltaRejected,
            session.Observe(At("xy")).Status);
    }

    [Fact]
    public void RejectedObservationBecomesNewBaselineWithoutReplaying()
    {
        var session = new ExternalClassicEditLiveSession();
        session.Observe(At("abc"));
        Assert.Equal(ExternalClassicEditLiveStatus.DeltaRejected,
            session.Observe(At("ab")).Status);
        Assert.Equal(ExternalClassicEditLiveStatus.CandidateInsertion,
            session.Observe(At("abx")).Status);
    }

    [Fact]
    public void SelectionDisablesCandidateEvenForValidInsertedText()
    {
        var session = new ExternalClassicEditLiveSession();
        session.Observe(At("ab"));
        var next = new ExternalClassicEditReadResult(ExternalClassicEditReadStatus.Observed, First,
            new ExternalClassicEditTextObservation("abx", 1, 3));
        Assert.Equal(ExternalClassicEditLiveStatus.DeltaRejected, session.Observe(next).Status);
    }

    [Fact]
    public void DuplicateReadAndInvalidUnicodeNeverAuthorizeAnything()
    {
        var session = new ExternalClassicEditLiveSession();
        session.Observe(At(""));
        Assert.Equal(ExternalClassicEditLiveStatus.DeltaRejected,
            session.Observe(At("\uD83D")).Status);
        var accepted = session.Observe(At("🧑"));
        Assert.False(accepted.CanAuthorizeExpansion);
        Assert.Equal(nameof(ExternalClassicEditLiveStep), accepted.ToString());
    }

    [Fact]
    public void InvalidUnicodeForcesRebaseBeforeAnyFurtherCandidate()
    {
        var session = new ExternalClassicEditLiveSession();
        session.Observe(At(""));
        Assert.Equal(ExternalClassicEditLiveStatus.DeltaRejected,
            session.Observe(At("\uD83D")).Status);
        Assert.Equal(ExternalClassicEditLiveStatus.BaselineEstablished,
            session.Observe(At("ok")).Status);
    }

    [Fact]
    public void DiagnosticToStringDoesNotContainObservedText()
    {
        var session = new ExternalClassicEditLiveSession();
        session.Observe(At("privato"));
        var result = session.Observe(At("privato☕"));
        Assert.DoesNotContain("privato", result.ToString());
        Assert.False(result.CanAuthorizeExpansion);
    }
}
