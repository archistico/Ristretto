using TextExpander.Core;
using TextExpander.Core.Matching;
using Xunit;

namespace TextExpander.Tests;

public sealed class CommittedInputSessionTests
{
    private static LibrarySnapshot Snapshot(long revision,
        params (string Trigger, string Content, bool Enabled)[] entries)
    {
        var group = new SnippetGroup(Guid.NewGuid(), "Demo", true);
        return new LibrarySnapshot([group], entries.Select((entry, index) =>
            new Snippet(Guid.NewGuid(), group.Id, "Snippet " + index,
                entry.Trigger, entry.Content, "", entry.Enabled, 1)).ToArray(), revision);
    }

    private static CommittedInputObservation Good(string value, long epoch = 100) =>
        new(epoch, value, TextObservationKind.VerifiedCommitted,
            ObservedFieldSafety.VerifiedNormalEditable);

    [Fact]
    public void SingleCharacterCommitsCompleteAnNfcUnicodeTrigger()
    {
        var session = new CommittedInputSession(Snapshot(1, ("café", "OK", true)));
        session.Observe(Good("c"));
        session.Observe(Good("a"));
        session.Observe(Good("f"));
        session.Observe(Good("e"));
        var result = session.Observe(Good("\u0301"));
        Assert.Equal(TriggerRecognitionStatus.Matched, result.Status);
        Assert.Equal("OK", result.Snippet?.Content);
    }

    [Fact]
    public void EmojiZwjAndNonLatinScriptsRemainExact()
    {
        var session = new CommittedInputSession(Snapshot(1,
            ("👩🏽‍💻", "Emoji", true), ("こんにちは", "Japanese", true),
            ("مرحبا", "Arabic", true)));
        Assert.Equal("Emoji", session.Observe(Good("x👩🏽‍💻")).Snippet?.Content);
        session.Reset();
        Assert.Equal("Japanese", session.Observe(Good("こんにちは")).Snippet?.Content);
        session.Reset();
        Assert.Equal("Arabic", session.Observe(Good("مرحبا")).Snippet?.Content);
    }

    [Fact]
    public void LongestCompletedTriggerWinsOverShorterSuffix()
    {
        var session = new CommittedInputSession(Snapshot(1,
            ("b", "Short", true), ("ab", "Long", true)));
        Assert.Equal("Long", session.Observe(Good("ab")).Snippet?.Content);
    }

    [Fact]
    public void MatchingIsCaseSensitiveAndDoesNotConflateUnicodeCharacters()
    {
        var session = new CommittedInputSession(Snapshot(1,
            ("İ", "Dotted uppercase I", true), ("i", "Lowercase i", true)));
        Assert.Equal("Lowercase i", session.Observe(Good("i")).Snippet?.Content);
        session.Reset();
        Assert.Equal("Dotted uppercase I", session.Observe(Good("İ")).Snippet?.Content);
        session.Reset();
        Assert.Equal(TriggerRecognitionStatus.None, session.Observe(Good("I")).Status);
    }

    [Fact]
    public void ShortTriggerIsDeferredWhileLongerTriggerCanStillComplete()
    {
        var session = new CommittedInputSession(Snapshot(1,
            (";sig", "Short", true), (";signature", "Long", true)));
        Assert.Equal(TriggerRecognitionStatus.WaitingForLongerTrigger,
            session.Observe(Good(";sig")).Status);
        Assert.Equal("Long", session.Observe(Good("nature")).Snippet?.Content);
    }

    [Fact]
    public void LongerPotentialPrefixPreventsPrematureShortSuffix()
    {
        var session = new CommittedInputSession(Snapshot(1,
            ("b", "Short", true), ("abc", "Long", true)));
        Assert.Equal(TriggerRecognitionStatus.WaitingForLongerTrigger,
            session.Observe(Good("ab")).Status);
        Assert.Equal("Long", session.Observe(Good("c")).Snippet?.Content);
    }

    [Fact]
    public void BrokenDeferredPrefixNeverRetroactivelyDeletesText()
    {
        var session = new CommittedInputSession(Snapshot(1,
            (";sig", "Short", true), (";signature", "Long", true)));
        Assert.Equal(TriggerRecognitionStatus.WaitingForLongerTrigger,
            session.Observe(Good(";sig")).Status);
        Assert.Equal(TriggerRecognitionStatus.None, session.Observe(Good("x")).Status);
    }

    [Fact]
    public void DuplicateCanonicalTriggersAreAmbiguousEvenIfInsertedInDifferentOrder()
    {
        var entries = new[] { ("café", "A", true), ("cafe\u0301", "B", true) };
        var forward = new CommittedInputSession(Snapshot(1, entries));
        var backward = new CommittedInputSession(Snapshot(1, entries.Reverse().ToArray()));
        foreach (var session in new[] { forward, backward })
        {
            var found = session.Observe(Good("café"));
            Assert.Equal(TriggerRecognitionStatus.Ambiguous, found.Status);
            Assert.Null(found.Snippet);
            Assert.Equal(2, found.CandidateCount);
        }
    }

    [Fact]
    public void DisabledSnippetAndGroupNeverMatch()
    {
        var snapshot = Snapshot(1, (";on", "OK", true), (";off", "NOT", false));
        var session = new CommittedInputSession(snapshot);
        Assert.Equal(TriggerRecognitionStatus.None, session.Observe(Good(";off")).Status);
        Assert.Equal(TriggerRecognitionStatus.Matched, session.Observe(Good(";on")).Status);

        var disabledGroup = new LibrarySnapshot(
            snapshot.Groups.Select(g => g with { IsEnabled = false }).ToArray(), snapshot.Snippets, 2);
        session.PublishSnapshot(disabledGroup);
        Assert.Equal(TriggerRecognitionStatus.None, session.Observe(Good(";on")).Status);
    }

    [Fact]
    public void TargetEpochChangeForbidsCrossWindowMatch()
    {
        var session = new CommittedInputSession(Snapshot(1, (";firma", "OK", true)));
        session.Observe(Good(";fir", epoch: 1));
        Assert.Equal(TriggerRecognitionStatus.None, session.Observe(Good("ma", epoch: 2)).Status);
        Assert.Equal("OK", session.Observe(Good(";firma", epoch: 2)).Snippet?.Content);
    }

    [Theory]
    [InlineData(TextObservationKind.Composing)]
    [InlineData(TextObservationKind.Unknown)]
    [InlineData(TextObservationKind.Synthetic)]
    [InlineData(TextObservationKind.NavigationOrSelectionChange)]
    public void UntrustedObservationClearsPreviousSuffix(TextObservationKind kind)
    {
        var session = new CommittedInputSession(Snapshot(1, (";firma", "OK", true)));
        session.Observe(Good(";fir"));
        var blocked = session.Observe(new CommittedInputObservation(100, "ma", kind,
            ObservedFieldSafety.VerifiedNormalEditable));
        Assert.Equal(TriggerRecognitionStatus.Suppressed, blocked.Status);
        Assert.Equal(TriggerRecognitionStatus.None, session.Observe(Good("ma")).Status);
    }

    [Theory]
    [InlineData(ObservedFieldSafety.Protected)]
    [InlineData(ObservedFieldSafety.Unknown)]
    public void UnsafeFieldClearsPreviousSuffix(ObservedFieldSafety safety)
    {
        var session = new CommittedInputSession(Snapshot(1, (";firma", "OK", true)));
        session.Observe(Good(";fir"));
        Assert.Equal(TriggerRecognitionStatus.Suppressed,
            session.Observe(new CommittedInputObservation(100, "ma",
                TextObservationKind.VerifiedCommitted, safety)).Status);
        Assert.Equal(TriggerRecognitionStatus.None, session.Observe(Good("ma")).Status);
    }

    [Fact]
    public void UnknownEpochNeverMatches()
    {
        var session = new CommittedInputSession(Snapshot(1, ("a", "OK", true)));
        Assert.Equal(TriggerRecognitionStatus.Suppressed, session.Observe(Good("a", 0)).Status);
        Assert.Equal(TriggerRecognitionStatus.Suppressed, session.Observe(Good("a", -3)).Status);
    }

    [Fact]
    public void PausingAndResumingDiscardTypedContext()
    {
        var session = new CommittedInputSession(Snapshot(1, (";firma", "OK", true)));
        session.Observe(Good(";fir"));
        session.SetPaused(true);
        Assert.Equal(TriggerRecognitionStatus.Suppressed, session.Observe(Good("ma")).Status);
        session.SetPaused(false);
        Assert.Equal(TriggerRecognitionStatus.None, session.Observe(Good("ma")).Status);
        Assert.Equal(TriggerRecognitionStatus.Matched, session.Observe(Good(";firma")).Status);
    }

    [Fact]
    public void PublishingNewRevisionClearsSuffixAndReplacesCatalog()
    {
        var session = new CommittedInputSession(Snapshot(1, (";firma", "Old", true)));
        session.Observe(Good(";fir"));
        session.PublishSnapshot(Snapshot(2, (";firma", "New", true)));
        Assert.Equal(2, session.LibraryRevision);
        Assert.Equal(TriggerRecognitionStatus.None, session.Observe(Good("ma")).Status);
        Assert.Equal("New", session.Observe(Good(";firma")).Snippet?.Content);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\t")]
    [InlineData("\u2028")]
    public void InvalidOrControlObservationsFailClosed(string text)
    {
        var session = new CommittedInputSession(Snapshot(1, (";ok", "OK", true)));
        session.Observe(Good(";o"));
        Assert.Equal(TriggerRecognitionStatus.Suppressed, session.Observe(Good(text)).Status);
        Assert.Equal(TriggerRecognitionStatus.None, session.Observe(Good("k")).Status);
    }

    [Fact]
    public void UnpairedSurrogatesCannotEnterTheTypingBuffer()
    {
        var session = new CommittedInputSession(Snapshot(1, (";ok", "OK", true)));
        foreach (var surrogate in new[] { (char)0xD83D, (char)0xDC4D })
        {
            session.Observe(Good(";o"));
            Assert.Equal(TriggerRecognitionStatus.Suppressed,
                session.Observe(Good(new string(surrogate, 1))).Status);
            Assert.Equal(TriggerRecognitionStatus.None, session.Observe(Good("k")).Status);
        }
    }

    [Fact]
    public void LargeObservationIsRejectedButLongTypingHistoryRemainsBounded()
    {
        var session = new CommittedInputSession(Snapshot(1, (";ok", "OK", true)));
        Assert.Equal(TriggerRecognitionStatus.Suppressed,
            session.Observe(Good(new string('x', CommittedInputSession.MaxObservationUtf16Units + 1))).Status);
        for (var i = 0; i < 200; i++)
            Assert.Equal(TriggerRecognitionStatus.None, session.Observe(Good("word")).Status);
        Assert.Equal(TriggerRecognitionStatus.Matched, session.Observe(Good(";ok")).Status);
    }

    [Fact]
    public void MaximumLengthUnicodePrefixResolvesAfterLongUnmatchedHistory()
    {
        var prefix = ";" + new string('x', 125);
        var session = new CommittedInputSession(Snapshot(1,
            (prefix + "😀", "First", true), (prefix + "😁", "Second", true)));
        session.Observe(Good(new string('q', CommittedInputSession.MaxBufferedUtf16Units)));
        Assert.Equal(TriggerRecognitionStatus.WaitingForLongerTrigger,
            session.Observe(Good(prefix)).Status);
        var result = session.Observe(Good("😁"));
        Assert.Equal(TriggerRecognitionStatus.Matched, result.Status);
        Assert.Equal("Second", result.Snippet?.Content);
        session.Reset();
        Assert.Equal(TriggerRecognitionStatus.None, session.Observe(Good(prefix + "😂")).Status);
    }

    [Fact]
    public void IndexFindsOneTriggerAmongTenThousandWithoutScanningTheirContent()
    {
        var entries = Enumerable.Range(0, 10000)
            .Select(i => ($";t{i:D5}", $"payload-{i}", true)).ToArray();
        var session = new CommittedInputSession(Snapshot(51, entries));
        Assert.Equal("payload-9999", session.Observe(Good(";t09999")).Snippet?.Content);
        Assert.Equal(51, session.LibraryRevision);
    }
}
