using TextExpander.Application.Features.Input;
using TextExpander.Core;
using TextExpander.Infrastructure.Platform.Windows;
using Xunit;

namespace TextExpander.Tests;

public sealed class ExternalClassicEditTriggerPreviewSessionTests
{
    private static readonly ExternalClassicEditFocusToken First = new((nint)100, (nint)101, 200);
    private static readonly ExternalClassicEditFocusToken Second = new((nint)100, (nint)102, 200);

    private static LibrarySnapshot Catalog(params string[] triggers)
    {
        var group = new SnippetGroup(Guid.NewGuid(), "Sintetico", true);
        return new LibrarySnapshot([group], triggers.Select((trigger, index) =>
            new Snippet(Guid.NewGuid(), group.Id, $"Prova {index}", trigger,
                "CONTENUTO DI PROVA", "", true, 1)).ToArray(), 1);
    }

    private static ExternalClassicEditReadResult At(string text, int? caret = null,
        ExternalClassicEditFocusToken? target = null) => new(
        ExternalClassicEditReadStatus.Observed, target ?? First,
        new ExternalClassicEditTextObservation(text, caret ?? text.Length, caret ?? text.Length));

    [Fact]
    public void FirstSnapshotDoesNotRecognizeExistingTrigger()
    {
        var preview = new ExternalClassicEditTriggerPreviewSession(Catalog(";firma"));
        var result = preview.Observe(At(";firma "), 1);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.BaselineEstablished, result.Status);
        Assert.False(result.CanAuthorizeExpansion);
    }

    [Fact]
    public void SplitTriggerAndDelimiterProduceOnlyPotentialMatch()
    {
        var preview = new ExternalClassicEditTriggerPreviewSession(Catalog(";firma"));
        preview.Observe(At(""), 0);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.InputCandidate,
            preview.Observe(At(";fir"), 1).Status);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.InputCandidate,
            preview.Observe(At(";firma"), 2).Status);
        var result = preview.Observe(At(";firma "), 3);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.PotentialTrigger, result.Status);
        Assert.False(result.CanAuthorizeExpansion);
        Assert.Equal(nameof(ExternalClassicEditTriggerPreviewStep), result.ToString());
    }

    [Fact]
    public void BulkPasteLooksLikeTriggerButIsNeverAuthorized()
    {
        var preview = new ExternalClassicEditTriggerPreviewSession(Catalog(";firma"));
        preview.Observe(At(""), 0);
        var result = preview.Observe(At(";firma "), 1);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.PotentialTrigger, result.Status);
        Assert.False(result.CanAuthorizeExpansion);
    }

    [Fact]
    public void ExistingTriggerBeforeObservationIsNotReplayed()
    {
        var preview = new ExternalClassicEditTriggerPreviewSession(Catalog(";firma"));
        preview.Observe(At(";firma"), 0);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.InputCandidate,
            preview.Observe(At(";firma "), 1).Status);
    }

    [Fact]
    public void SwitchOfControlPreventsCrossTargetMatch()
    {
        var preview = new ExternalClassicEditTriggerPreviewSession(Catalog(";firma"));
        preview.Observe(At(""), 0);
        preview.Observe(At(";fir"), 1);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.TargetChanged,
            preview.Observe(At(";fir", target: Second), 2).Status);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.InputCandidate,
            preview.Observe(At(";firma ", target: Second), 3).Status);
        Assert.False(preview.Observe(At(";firma ", target: Second), 4).CanAuthorizeExpansion);
    }

    [Fact]
    public void ReadFailureClearsPendingMatch()
    {
        var preview = new ExternalClassicEditTriggerPreviewSession(Catalog(";firma"));
        preview.Observe(At(""), 0);
        preview.Observe(At(";fir"), 1);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.ObservationUnavailable,
            preview.Observe(new ExternalClassicEditReadResult(ExternalClassicEditReadStatus.FocusChanged), 2).Status);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.BaselineEstablished,
            preview.Observe(At(";firma"), 3).Status);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.InputCandidate,
            preview.Observe(At(";firma "), 4).Status);
    }

    [Fact]
    public void ExplicitResetInvalidatesAllPreviouslyObservedCharacters()
    {
        var preview = new ExternalClassicEditTriggerPreviewSession(Catalog(";firma"));
        preview.Observe(At(""), 0);
        preview.Observe(At(";fir"), 1);
        preview.Reset();
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.BaselineEstablished,
            preview.Observe(At(";firma"), 2).Status);
    }

    [Fact]
    public void UnicodeNfdCanMatchNfcTriggerInPreview()
    {
        var preview = new ExternalClassicEditTriggerPreviewSession(Catalog(";café"));
        preview.Observe(At(""), 0);
        preview.Observe(At(";cafe\u0301"), 1);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.PotentialTrigger,
            preview.Observe(At(";cafe\u0301 "), 2).Status);
    }

    [Fact]
    public void EmojiTriggerCanMatchButNeverGrantWritePermission()
    {
        var preview = new ExternalClassicEditTriggerPreviewSession(Catalog(";👩🏽‍💻"));
        preview.Observe(At(""), 0);
        var result = preview.Observe(At(";👩🏽‍💻 "), 1);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.PotentialTrigger, result.Status);
        Assert.False(result.CanAuthorizeExpansion);
    }

    [Fact]
    public void InterruptedUnicodeRebasesRatherThanResumingTrigger()
    {
        var preview = new ExternalClassicEditTriggerPreviewSession(Catalog(";firma"));
        preview.Observe(At(""), 0);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.DeltaRejected,
            preview.Observe(At("\uD83D"), 1).Status);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.BaselineEstablished,
            preview.Observe(At(";firma"), 2).Status);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.InputCandidate,
            preview.Observe(At(";firma "), 3).Status);
    }

    [Fact]
    public void AlphabeticTriggerRequiresBeginningOfWord()
    {
        var preview = new ExternalClassicEditTriggerPreviewSession(Catalog("brb"));
        preview.Observe(At("foobar"), 0);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.InputCandidate,
            preview.Observe(At("foobarbrb "), 1).Status);

        preview.Reset();
        preview.Observe(At("foo "), 2);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.PotentialTrigger,
            preview.Observe(At("foo brb "), 3).Status);
    }

    [Fact]
    public void SelectionAndDeletionInvalidatePreview()
    {
        var preview = new ExternalClassicEditTriggerPreviewSession(Catalog(";firma"));
        preview.Observe(At("abc"), 0);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.DeltaRejected,
            preview.Observe(At("ab"), 1).Status);
        preview.Reset();
        preview.Observe(At("abc"), 2);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.DeltaRejected,
            preview.Observe(new ExternalClassicEditReadResult(ExternalClassicEditReadStatus.Observed, First,
                new ExternalClassicEditTextObservation("abcd", 1, 3)), 3).Status);
    }

    [Fact]
    public void DuplicateSyntheticTriggersAreReportedOnlyAsAmbiguous()
    {
        var preview = new ExternalClassicEditTriggerPreviewSession(Catalog(";firma", ";firma"));
        preview.Observe(At(""), 0);
        var result = preview.Observe(At(";firma "), 1);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.AmbiguousTrigger, result.Status);
        Assert.False(result.CanAuthorizeExpansion);
    }

    [Fact]
    public void IdleTimeoutCannotCompleteStalePrefix()
    {
        var preview = new ExternalClassicEditTriggerPreviewSession(Catalog(";firma"));
        preview.Observe(At(""), 0);
        preview.Observe(At(";fir"), 1);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.InputCandidate,
            preview.Observe(At(";firma"), 20_000).Status);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.InputCandidate,
            preview.Observe(At(";firma "), 20_001).Status);
    }

    [Fact]
    public void NoChangeRemainsNoChange()
    {
        var preview = new ExternalClassicEditTriggerPreviewSession(Catalog(";firma"));
        preview.Observe(At(""), 0);
        Assert.Equal(ExternalClassicEditTriggerPreviewStatus.NoChange, preview.Observe(At(""), 1).Status);
    }
}
