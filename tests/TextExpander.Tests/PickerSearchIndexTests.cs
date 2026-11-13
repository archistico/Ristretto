using TextExpander.Application.Features.Picker;
using TextExpander.Core;
using Xunit;

namespace TextExpander.Tests;

public sealed class PickerSearchIndexTests
{
    private static readonly Guid Group = Guid.Parse("00000000-0000-4000-8000-000000000001");
    private static Snippet Item(int id, string title = "Alpha", string trigger = ";p",
        string content = "Payload", string description = "") =>
        new(new Guid(id, 0, 0, new byte[8]), Group, title, trigger, content, description, true, 1);
    private static LibrarySnapshot Library(params Snippet[] snippets) =>
        new([new(Group, "General", true)], snippets, 17);

    [Fact]
    public void CancelledIndexConstructionDoesNotPublishAPartialIndex()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => new PickerSearchIndex(Library(Item(1)), cancellation.Token));
        Assert.Single(new PickerSearchIndex(Library(Item(1))).Find("Payload"));
    }

    [Fact]
    public void CancellationDuringEnumerationStopsIndexConstruction()
    {
        using var cancellation = new CancellationTokenSource();
        var snippets = new CancellingSnippets(cancellation, Item(1), Item(2));
        var library = new LibrarySnapshot([new(Group, "G", true)], snippets, 1);
        Assert.Throws<OperationCanceledException>(() => new PickerSearchIndex(library, cancellation.Token));
    }

    private sealed class CancellingSnippets(CancellationTokenSource cancellation, Snippet first, Snippet second)
        : IReadOnlyList<Snippet>
    {
        public int Count => 2;
        public Snippet this[int index] => index switch { 0 => first, 1 => second, _ => throw new ArgumentOutOfRangeException(nameof(index)) };
        public IEnumerator<Snippet> GetEnumerator()
        {
            yield return first;
            cancellation.Cancel();
            yield return second;
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Fact]
    public void EveryRankKeepsItsPriorityAfterIndexingAndLimiting()
    {
        var byRank = new[]
        {
            Item(1, title: "Z", trigger: "needle"),
            Item(2, title: "Y", trigger: "needleSuffix"),
            Item(3, title: "Needle starts"),
            Item(4, title: "Some needle"),
            Item(5, title: "W", trigger: ";a-needle"),
            Item(6, title: "V", description: "needle"),
            Item(7, title: "U", content: "needle")
        };
        var index = new PickerSearchIndex(Library(byRank.Reverse().ToArray()));
        Assert.Equal(byRank.Select(s => s.Id), index.Find("needle").Select(s => s.SnippetId));
        Assert.Equal(byRank.Take(3).Select(s => s.Id), index.Find("needle", 3).Select(s => s.SnippetId));
    }

    [Fact]
    public void StableTiesUseRawTitleThenTriggerThenId()
    {
        var low = Item(1, trigger: ";a", content: "needle");
        var high = Item(2, trigger: ";a", content: "needle");
        var laterTrigger = Item(3, trigger: ";b", content: "needle");
        var laterTitle = Item(4, title: "Zulu", trigger: ";0", content: "needle");
        var index = new PickerSearchIndex(Library(laterTitle, high, laterTrigger, low));
        var expected = new[] { low.Id, high.Id, laterTrigger.Id, laterTitle.Id };
        Assert.Equal(expected, index.Find("needle").Select(s => s.SnippetId));
        Assert.Equal(expected, index.Find("").Select(s => s.SnippetId));
    }

    [Fact]
    public void IndexIsAStableProjectionUntilTheNextSnapshotIsCompiled()
    {
        var original = Item(1);
        var snippets = new List<Snippet> { original };
        var groups = new List<SnippetGroup> { new(Group, "General", true) };
        var snapshot = new LibrarySnapshot(groups, snippets, 17);
        var index = new PickerSearchIndex(snapshot);
        snippets.Clear();
        groups.Clear();
        Assert.Equal(17, index.LibraryRevision);
        Assert.Equal(original.Id, Assert.Single(index.Find("Payload")).SnippetId);
        Assert.Empty(new PickerSearchIndex(snapshot).Find("Payload"));
    }

    [Fact]
    public void SearchNormalizationDoesNotRewriteDisplayedOrCopiedStrings()
    {
        var snippet = Item(1, "Cafe\u0301", ";e\u0301", "Cafe\u0301 👩🏽‍💻", "De\u0301tail");
        var index = new PickerSearchIndex(Library(snippet));
        foreach (var query in new[] { "CAFÉ", ";é", "détail", "👩🏽‍💻" })
        {
            var result = Assert.Single(index.Find(query));
            Assert.Equal(snippet.Title, result.Title);
            Assert.Equal(snippet.Trigger, result.Trigger);
            Assert.Equal(snippet.Content, result.Content);
            Assert.Equal(snippet.Description, result.Description);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void InvalidLimitsAreRejected(int limit)
    {
        var index = new PickerSearchIndex(Library(Item(1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => index.Find("", limit));
    }

    [Fact]
    public void LimitsApplyToEveryQueryIncludingNullAndWhitespace()
    {
        var index = new PickerSearchIndex(Library(Enumerable.Range(1, 150)
            .Select(i => Item(i, title: $"Item {i:D3}")).ToArray()));
        foreach (var query in new[] { null, "", "   ", "Payload" })
        {
            Assert.Equal(50, index.Find(query).Count);
            Assert.Equal(100, index.Find(query, 100).Count);
            Assert.Single(index.Find(query, 1));
        }
    }

    [Theory]
    [InlineData("title")]
    [InlineData("trigger")]
    [InlineData("description")]
    [InlineData("content")]
    public void MalformedIndexedFieldsCannotHideOtherValidResults(string field)
    {
        var invalid = new string((char)0xD83D, 1);
        var malformed = Item(1);
        malformed = field switch
        {
            "title" => malformed with { Title = invalid },
            "trigger" => malformed with { Trigger = invalid },
            "description" => malformed with { Description = invalid },
            _ => malformed with { Content = invalid }
        };
        var valid = Item(2);
        var index = new PickerSearchIndex(Library(malformed, valid));
        Assert.Equal(valid.Id, Assert.Single(index.Find("")).SnippetId);
        Assert.Equal(valid.Id, Assert.Single(index.Find("Payload")).SnippetId);
        Assert.Equal(field == "content" ? invalid : "Payload", malformed.Content);
    }

    [Theory]
    [InlineData("👩🏽‍💻")]
    [InlineData("🇮🇹")]
    public void PrefixCannotExposeHalfOfAnEmojiCluster(string cluster)
    {
        var content = new string('x', 4094) + cluster;
        var index = new PickerSearchIndex(Library(Item(1, content: content)));
        Assert.Empty(index.Find(cluster[..2]));
        Assert.Equal(content, Assert.Single(index.Find("xxx")).Content);
    }

    [Fact]
    public void CompleteCombiningClusterAtExactBoundaryIsSearchable()
    {
        var content = new string('x', 4094) + "e\u0301" + "beyond";
        var index = new PickerSearchIndex(Library(Item(1, content: content)));
        Assert.Equal(content, Assert.Single(index.Find("é")).Content);
        Assert.Empty(index.Find("beyond"));
    }

    [Fact]
    public void OversizedFirstGraphemeIsExcludedWithoutDroppingOtherFields()
    {
        var content = "e" + new string('\u0301', 5000);
        var index = new PickerSearchIndex(Library(Item(1, title: "Findable", content: content)));
        Assert.Empty(index.Find("é"));
        Assert.Equal(content, Assert.Single(index.Find("Findable")).Content);
    }

    [Fact]
    public void DisabledAndOrphanSnippetsRemainExcluded()
    {
        var valid = Item(1);
        var disabled = Item(2) with { IsEnabled = false };
        var orphan = Item(3) with { GroupId = Guid.NewGuid() };
        var disabledGroup = Guid.NewGuid();
        var hidden = Item(4) with { GroupId = disabledGroup };
        var snapshot = new LibrarySnapshot([new(Group, "General", true), new(disabledGroup, "Off", false)],
            [valid, disabled, orphan, hidden], 1);
        Assert.Equal(valid.Id, Assert.Single(new PickerSearchIndex(snapshot).Find("")).SnippetId);
    }

    [Fact]
    public void UnicodeReplacementCharacterIsValidContent()
    {
        Assert.Single(new PickerSearchIndex(Library(Item(1, content: "Text �"))).Find("�"));
    }
}
