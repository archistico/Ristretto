using TextExpander.Core;
using TextExpander.Core.Matching;
using Xunit;

namespace TextExpander.Tests;

public sealed class TriggerActivationSessionTests
{
    private static LibrarySnapshot Snapshot(long revision,
        params (string Trigger, string Content, bool Enabled)[] entries)
    {
        var group = new SnippetGroup(Guid.NewGuid(), "F1b", true);
        return new LibrarySnapshot([group], entries.Select((entry, i) =>
            new Snippet(Guid.NewGuid(), group.Id, $"Snippet {i}", entry.Trigger,
                entry.Content, "", entry.Enabled, 1)).ToArray(), revision);
    }

    private static CommittedInputObservation Good(string text, long epoch = 1) =>
        new(epoch, text, TextObservationKind.VerifiedCommitted,
            ObservedFieldSafety.VerifiedNormalEditable);

    [Fact]
    public void DefaultPolicyWaitsForWhitespaceBeforeActivating()
    {
        var session = new TriggerActivationSession(Snapshot(1, (";sig", "Short", true)));
        Assert.Equal(TriggerActivationStatus.WaitingForDelimiter,
            session.Observe(Good(";sig"), 100).Status);
        var result = session.Observe(Good(" "), 120);
        Assert.Equal(TriggerActivationStatus.Ready, result.Status);
        Assert.Equal("Short", result.Snippet?.Content);
    }

    [Fact]
    public void OverlappingTriggersAreSelectedByWhatWasActuallyCommitted()
    {
        var library = Snapshot(1, (";sig", "Short", true), (";signature", "Long", true));
        var shortSession = new TriggerActivationSession(library);
        Assert.Equal(TriggerActivationStatus.WaitingForDelimiter,
            shortSession.Observe(Good(";sig"), 10).Status);
        Assert.Equal("Short", shortSession.Observe(Good(" "), 11).Snippet?.Content);

        var longSession = new TriggerActivationSession(library);
        Assert.Equal(TriggerActivationStatus.WaitingForDelimiter,
            longSession.Observe(Good(";signature"), 20).Status);
        Assert.Equal("Long", longSession.Observe(Good(" "), 21).Snippet?.Content);
    }

    [Fact]
    public void ImmediateModeDefersShorterPrefixAndDoesNotRetroactivelyFire()
    {
        var policy = new TriggerActivationPolicy(Mode: TriggerActivationMode.Immediate);
        var library = Snapshot(1, (";sig", "Short", true), (";signature", "Long", true));
        var session = new TriggerActivationSession(library, policy);
        Assert.Equal(TriggerActivationStatus.WaitingForMoreInput,
            session.Observe(Good(";sig"), 10).Status);
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good("x"), 11).Status);
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good(" "), 12).Status);

        var completed = new TriggerActivationSession(library, policy);
        Assert.Equal(TriggerActivationStatus.WaitingForMoreInput,
            completed.Observe(Good(";sig"), 10).Status);
        Assert.Equal("Long", completed.Observe(Good("nature"), 11).Snippet?.Content);
    }

    [Fact]
    public void ImmediateDeferredPrefixCanResolveAtWhitespaceWithoutRewritingHistory()
    {
        var session = new TriggerActivationSession(
            Snapshot(1, (";sig", "Short", true), (";signature", "Long", true)),
            new TriggerActivationPolicy(Mode: TriggerActivationMode.Immediate));
        session.Observe(Good(";sig"), 10);
        Assert.Equal("Short", session.Observe(Good(" "), 12).Snippet?.Content);
    }

    [Fact]
    public void DefaultDelimiterSetDoesNotTreatPunctuationAsTermination()
    {
        var session = new TriggerActivationSession(Snapshot(1, (";sig", "Short", true)));
        session.Observe(Good(";sig"), 1);
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good("."), 2).Status);
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good(" "), 3).Status);
    }

    [Fact]
    public void OptInUnicodePunctuationDelimitersAreSupported()
    {
        var session = new TriggerActivationSession(Snapshot(1, (";sig", "Short", true)),
            new TriggerActivationPolicy(Delimiters: TriggerDelimiterSet.UnicodeWhitespaceOrPunctuation));
        session.Observe(Good(";sig"), 1);
        Assert.Equal("Short", session.Observe(Good("。"), 2).Snippet?.Content);
    }

    [Fact]
    public void ConnectorPunctuationIsNotADelimiterInsideAnIdentifier()
    {
        var session = new TriggerActivationSession(Snapshot(1, ("brb", "NO", true)),
            new TriggerActivationPolicy(Delimiters: TriggerDelimiterSet.UnicodeWhitespaceOrPunctuation));
        session.Observe(Good("brb"), 1, InitialWordBoundaryEvidence.Verified);
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good("_"), 2).Status);
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good(" "), 3).Status);
    }

    [Fact]
    public void UnicodeWhitespaceSupportsNonBreakingSpace()
    {
        var session = new TriggerActivationSession(Snapshot(1, (";sig", "OK", true)));
        session.Observe(Good(";sig"), 1);
        Assert.Equal("OK", session.Observe(Good("\u00A0"), 2).Snippet?.Content);
    }

    [Fact]
    public void WordStartRuleRejectsUnanchoredAlphabeticTrigger()
    {
        var session = new TriggerActivationSession(Snapshot(1, ("brb", "OK", true)));
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good("brb"), 1).Status);
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good(" "), 2).Status);
    }

    [Fact]
    public void VerifiedInitialWordBoundaryAllowsAlphabeticTrigger()
    {
        var session = new TriggerActivationSession(Snapshot(1, ("brb", "OK", true)));
        Assert.Equal(TriggerActivationStatus.WaitingForDelimiter,
            session.Observe(Good("brb"), 1, InitialWordBoundaryEvidence.Verified).Status);
        Assert.Equal("OK", session.Observe(Good(" "), 2).Snippet?.Content);
    }

    [Fact]
    public void LeadingVerifiedSpaceProvidesBoundaryButPreviousWordDoesNot()
    {
        var session = new TriggerActivationSession(Snapshot(1, ("brb", "OK", true)));
        Assert.Equal("OK", session.Observe(Good("hello brb "), 1).Snippet?.Content);

        var midWord = new TriggerActivationSession(Snapshot(1, ("brb", "NO", true)));
        Assert.Equal(TriggerActivationStatus.None,
            midWord.Observe(Good("hellobrb "), 1).Status);
    }

    [Fact]
    public void UnicodeLettersAndCombiningMarksDoNotBecomeFalseWordBoundaries()
    {
        var session = new TriggerActivationSession(Snapshot(1, ("brb", "NO", true)));
        Assert.Equal(TriggerActivationStatus.None,
            session.Observe(Good("漢brb "), 1).Status);

        var combining = new TriggerActivationSession(Snapshot(1, ("brb", "NO", true)));
        Assert.Equal(TriggerActivationStatus.None,
            combining.Observe(Good("e\u0301brb "), 1).Status);
    }

    [Fact]
    public void ExactCanonicalDuplicatesStayAmbiguousEvenWithDelimiter()
    {
        var session = new TriggerActivationSession(Snapshot(1,
            ("café", "A", true), ("cafe\u0301", "B", true)));
        Assert.Equal(TriggerActivationStatus.WaitingForDelimiter,
            session.Observe(Good("cafe\u0301"), 1, InitialWordBoundaryEvidence.Verified).Status);
        var result = session.Observe(Good(" "), 2);
        Assert.Equal(TriggerActivationStatus.Ambiguous, result.Status);
        Assert.Equal(2, result.CandidateCount);
        Assert.Null(result.Snippet);
    }

    [Fact]
    public void CanonicalDecompositionAcrossSeparateEventsMatchesAtDelimiter()
    {
        var session = new TriggerActivationSession(Snapshot(1, ("café", "OK", true)));
        session.Observe(Good("caf"), 1, InitialWordBoundaryEvidence.Verified);
        session.Observe(Good("e"), 2);
        session.Observe(Good("\u0301"), 3);
        Assert.Equal("OK", session.Observe(Good(" "), 4).Snippet?.Content);
    }

    [Fact]
    public void EmojiZwjTriggerWorksWithDelimiter()
    {
        var session = new TriggerActivationSession(Snapshot(1, ("👩🏽‍💻", "Emoji", true)));
        session.Observe(Good("👩🏽‍💻"), 1);
        Assert.Equal("Emoji", session.Observe(Good(" "), 2).Snippet?.Content);
    }

    [Fact]
    public void LongestEligibleSuffixIsChosenOverShorterOne()
    {
        var session = new TriggerActivationSession(Snapshot(1,
            ("sig", "Short", true), (";sig", "Long", true)));
        session.Observe(Good("x;sig"), 1);
        Assert.Equal("Long", session.Observe(Good(" "), 2).Snippet?.Content);
    }

    [Fact]
    public void IneligibleLongWordTriggerDoesNotMaskEligiblePunctuationTrigger()
    {
        var session = new TriggerActivationSession(Snapshot(1,
            ("x;sig", "Wrong", true), (";sig", "Right", true)));
        session.Observe(Good("ax;sig"), 1);
        Assert.Equal("Right", session.Observe(Good(" "), 2).Snippet?.Content);
    }

    [Fact]
    public void ImmediateAnywhereModeCanMatchInsideWordWhenExplicitlySelected()
    {
        var session = new TriggerActivationSession(Snapshot(1, ("brb", "OK", true)),
            new TriggerActivationPolicy(Mode: TriggerActivationMode.Immediate,
                StartBoundary: TriggerStartBoundary.Anywhere));
        Assert.Equal("OK", session.Observe(Good("hello-brb"), 1).Snippet?.Content);
    }

    [Fact]
    public void ImmediateWordStartModeDoesNotDeferItsOwnCompletedTrigger()
    {
        var session = new TriggerActivationSession(Snapshot(1, ("brb", "OK", true)),
            new TriggerActivationPolicy(Mode: TriggerActivationMode.Immediate));
        var result = session.Observe(Good("brb"), 1, InitialWordBoundaryEvidence.Verified);
        Assert.Equal(TriggerActivationStatus.Ready, result.Status);
        Assert.Equal("OK", result.Snippet?.Content);
    }

    [Fact]
    public void ImmediateCompletedTriggerIsNotMaskedByUnrelatedShorterPrefix()
    {
        var session = new TriggerActivationSession(Snapshot(1,
                ("brb", "OK", true), ("because", "Another", true)),
            new TriggerActivationPolicy(Mode: TriggerActivationMode.Immediate,
                StartBoundary: TriggerStartBoundary.Anywhere));
        Assert.Equal("OK", session.Observe(Good("hello-brb"), 1).Snippet?.Content);
    }

    [Fact]
    public void ImmediateEqualLengthPrefixOfLongerTriggerStillDefers()
    {
        var session = new TriggerActivationSession(Snapshot(1,
                ("brb", "Short", true), ("brbLong", "Long", true)),
            new TriggerActivationPolicy(Mode: TriggerActivationMode.Immediate,
                StartBoundary: TriggerStartBoundary.Anywhere));
        Assert.Equal(TriggerActivationStatus.WaitingForMoreInput,
            session.Observe(Good("brb"), 1).Status);
        Assert.Equal("Long", session.Observe(Good("Long"), 2).Snippet?.Content);
    }

    [Fact]
    public void IdlingClearsOldPrefixEvenWhenNextEventIsVerified()
    {
        var session = new TriggerActivationSession(Snapshot(1, (";firma", "OK", true)),
            new TriggerActivationPolicy(MaxIdleMilliseconds: 100));
        session.Observe(Good(";fir"), 0);
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good("ma"), 101).Status);
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good(" "), 102).Status);
    }

    [Fact]
    public void SameMonotonicTimestampIsValidButReversalFailsClosed()
    {
        var session = new TriggerActivationSession(Snapshot(1, (";sig", "OK", true)));
        session.Observe(Good(";si"), 100);
        Assert.Equal(TriggerActivationStatus.Suppressed, session.Observe(Good("g"), 99).Status);
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good(" "), 100).Status);

        session.Observe(Good(";sig"), 101);
        Assert.Equal("OK", session.Observe(Good(" "), 101).Snippet?.Content);
    }

    [Fact]
    public void FocusEpochChangeCannotCompletePreviousTrigger()
    {
        var session = new TriggerActivationSession(Snapshot(1, (";sig", "OK", true)));
        session.Observe(Good(";si", 1), 1);
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good("g", 2), 2).Status);
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good(" ", 2), 3).Status);
    }

    [Theory]
    [InlineData(TextObservationKind.Composing)]
    [InlineData(TextObservationKind.Unknown)]
    [InlineData(TextObservationKind.Synthetic)]
    [InlineData(TextObservationKind.NavigationOrSelectionChange)]
    public void UnverifiedOrNonTypingObservationsInvalidatePrefix(TextObservationKind kind)
    {
        var session = new TriggerActivationSession(Snapshot(1, (";sig", "OK", true)));
        session.Observe(Good(";si"), 1);
        Assert.Equal(TriggerActivationStatus.Suppressed,
            session.Observe(new CommittedInputObservation(1, "g", kind,
                ObservedFieldSafety.VerifiedNormalEditable), 2).Status);
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good("g "), 3).Status);
    }

    [Theory]
    [InlineData(ObservedFieldSafety.Protected)]
    [InlineData(ObservedFieldSafety.Unknown)]
    public void ProtectedOrUnknownFieldNeverActivates(ObservedFieldSafety safety)
    {
        var session = new TriggerActivationSession(Snapshot(1, (";sig", "OK", true)));
        session.Observe(Good(";si"), 1);
        Assert.Equal(TriggerActivationStatus.Suppressed,
            session.Observe(new CommittedInputObservation(1, "g", TextObservationKind.VerifiedCommitted,
                safety), 2).Status);
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good("g "), 3).Status);
    }

    [Fact]
    public void PauseResumeAndSnapshotPublicationDiscardTypingContext()
    {
        var session = new TriggerActivationSession(Snapshot(1, (";sig", "Old", true)));
        session.Observe(Good(";si"), 1);
        session.SetPaused(true);
        Assert.Equal(TriggerActivationStatus.Suppressed, session.Observe(Good("g"), 2).Status);
        session.SetPaused(false);
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good("g "), 3).Status);

        session.Observe(Good(";si"), 4);
        session.PublishSnapshot(Snapshot(2, (";sig", "New", true)));
        Assert.Equal(2, session.LibraryRevision);
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good("g "), 5).Status);
        Assert.Equal("New", session.Observe(Good(";sig "), 6).Snippet?.Content);
    }

    [Fact]
    public void DisabledSnippetsAndGroupsAreNotActivated()
    {
        var snapshot = Snapshot(1, (";no", "Disabled", false), (";yes", "Enabled", true));
        var session = new TriggerActivationSession(snapshot);
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good(";no "), 1).Status);
        Assert.Equal("Enabled", session.Observe(Good(";yes "), 2).Snippet?.Content);

        session.PublishSnapshot(new LibrarySnapshot(
            snapshot.Groups.Select(g => g with { IsEnabled = false }).ToArray(), snapshot.Snippets, 2));
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good(";yes "), 3).Status);
    }

    [Fact]
    public void MultiCharacterChunkCannotActivateMatchBeforeTheLastGrapheme()
    {
        var session = new TriggerActivationSession(Snapshot(1, (";sig", "OK", true)));
        Assert.Equal(TriggerActivationStatus.Suppressed,
            session.Observe(Good(";sig followed"), 1).Status);
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good(" "), 2).Status);
    }

    [Fact]
    public void InvalidUnicodeAndControlChunksCannotActivateOrRetainContext()
    {
        var session = new TriggerActivationSession(Snapshot(1, (";sig", "OK", true)));
        session.Observe(Good(";si"), 1);
        Assert.Equal(TriggerActivationStatus.Suppressed,
            session.Observe(Good("\uD800"), 2).Status);
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good("g "), 3).Status);
        session.Observe(Good(";si"), 4);
        Assert.Equal(TriggerActivationStatus.Suppressed, session.Observe(Good("\t"), 5).Status);
    }

    [Fact]
    public void PendingTriggerIsNotCompletedAcrossAnUnsafeBulkObservation()
    {
        var session = new TriggerActivationSession(Snapshot(1, (";sig", "OK", true)));
        session.Observe(Good(";si"), 1);
        Assert.Equal(TriggerActivationStatus.Suppressed,
            session.Observe(Good(new string('x', CommittedInputSession.MaxObservationUtf16Units + 1)), 2).Status);
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good("g "), 3).Status);
    }

    [Fact]
    public void HistoryTrimmingCannotInventAnInitialWordBoundary()
    {
        var session = new TriggerActivationSession(Snapshot(1, ("brb", "OK", true)));
        session.Observe(Good(new string('a', 510)), 1, InitialWordBoundaryEvidence.Verified);
        Assert.Equal(TriggerActivationStatus.None,
            session.Observe(Good("brb "), 2).Status);
    }

    [Fact]
    public void InvalidPolicyIsRejectedBeforeAnyEvents()
    {
        var lib = Snapshot(1, (";sig", "OK", true));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TriggerActivationSession(lib, new TriggerActivationPolicy(MaxIdleMilliseconds: -1)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new TriggerActivationSession(lib,
                new TriggerActivationPolicy(Mode: (TriggerActivationMode)300)));
    }

    [Fact]
    public void LongUnicodePrefixDoesNotMaskCompletedShortTrigger()
    {
        var prefix = ";" + new string('x', 125);
        var session = new TriggerActivationSession(Snapshot(1,
            (prefix + "😀", "Long", true), (";ok", "Short", true)),
            new TriggerActivationPolicy(Mode: TriggerActivationMode.Immediate));
        session.Observe(Good(new string('q', 512)), 1);
        Assert.Equal(TriggerActivationStatus.WaitingForMoreInput,
            session.Observe(Good(prefix), 2).Status);
        Assert.Equal(TriggerActivationStatus.None, session.Observe(Good("😁"), 3).Status);
        var result = session.Observe(Good(";ok"), 4);
        Assert.Equal(TriggerActivationStatus.Ready, result.Status);
        Assert.Equal("Short", result.Snippet?.Content);
    }

    [Fact]
    public void LargeCatalogUsesSameCompiledIndexAsFirstRecognitionStage()
    {
        var entries = Enumerable.Range(0, 10_000)
            .Select(i => ($";k{i:D5}", $"text{i}", true)).ToArray();
        var session = new TriggerActivationSession(Snapshot(100, entries));
        Assert.Equal(TriggerActivationStatus.WaitingForDelimiter,
            session.Observe(Good(";k09999"), 1).Status);
        Assert.Equal("text9999", session.Observe(Good(" "), 2).Snippet?.Content);
    }
}
