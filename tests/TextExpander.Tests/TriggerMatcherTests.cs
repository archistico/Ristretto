using TextExpander.Core;
using TextExpander.Core.Matching;
using Xunit;

namespace TextExpander.Tests;

public sealed class TriggerMatcherTests
{
    private static LibrarySnapshot Snapshot(params (string Trigger, string Content)[] values)
    {
        var group = new SnippetGroup(Guid.NewGuid(), "Test", true);
        return new LibrarySnapshot([group], values.Select(x =>
            new Snippet(Guid.NewGuid(), group.Id, x.Trigger, x.Trigger, x.Content, "", true, 1)).ToArray(), 1);
    }

    [Fact]
    public void Nfc_Equates_Precomposed_And_Decomposed_Accents()
    {
        var snapshot = Snapshot(("café", "OK"));
        Assert.Equal(MatchStatus.Matched, TriggerMatcher.MatchSuffix("un café", snapshot).Status);
        Assert.Equal(MatchStatus.None, TriggerMatcher.MatchSuffix("un cafe", snapshot).Status);
    }

    [Fact]
    public void Unicode_Emoji_Family_Is_Matched_As_Original_Sequence()
    {
        var snapshot = Snapshot(("👨‍👩‍👧‍👦", "Famiglia"));
        var result = TriggerMatcher.MatchSuffix("#👨‍👩‍👧‍👦", snapshot);
        Assert.Equal(MatchStatus.Matched, result.Status);
        Assert.Equal("Famiglia", result.Snippet?.Content);
    }

    [Fact]
    public void Duplicates_Are_Ambiguous_Not_Random()
    {
        var snapshot = Snapshot((";sig", "A"), (";sig", "B"));
        var result = TriggerMatcher.MatchSuffix(";sig", snapshot);
        Assert.Equal(MatchStatus.Ambiguous, result.Status);
        Assert.Null(result.Snippet);
        Assert.Equal(2, result.CandidateCount);
    }

    [Fact]
    public void Longest_Trigger_Wins_For_Suffix()
    {
        var snapshot = Snapshot(("ab", "short"), (";ab", "long"));
        Assert.Equal("long", TriggerMatcher.MatchSuffix("x;ab", snapshot).Snippet?.Content);
    }

    [Fact]
    public void Disabled_Group_Does_Not_Produce_Matches()
    {
        var group = new SnippetGroup(Guid.NewGuid(), "Off", false);
        var item = new Snippet(Guid.NewGuid(), group.Id, "Test", ";test", "Hi", "", true, 1);
        Assert.Equal(MatchStatus.None,
            TriggerMatcher.MatchSuffix(";test", new LibrarySnapshot([group], [item], 1)).Status);
    }

    [Fact]
    public void Different_Emoji_Modifiers_Are_Not_Equivalent()
    {
        var snapshot = Snapshot(("👍🏽", "OK"));
        Assert.Equal(MatchStatus.None, TriggerMatcher.MatchSuffix("👍", snapshot).Status);
    }
}
