using TextExpander.Application.Features.Picker;
using TextExpander.Core;
using Xunit;

namespace TextExpander.Tests;

public sealed class PickerSearchTests
{
    private static LibrarySnapshot WithContent(string content)
    {
        var group = Guid.NewGuid();
        return new([new(group, "G", true)],
            [new(Guid.NewGuid(), group, "Alpha", ";p", content, "", true, 1)], 1);
    }

    [Fact]
    public void Content_limit_cannot_split_a_surrogate_pair_and_break_search()
    {
        var content = "needle" + new string('x', 4089) + "💫 tail";
        Assert.Equal(content, Assert.Single(PickerSearch.Find(WithContent(content), "needle")).Content);
    }

    [Fact]
    public void Content_limit_excludes_incomplete_combining_cluster()
    {
        var content = new string('x', 4095) + "e\u0301";
        Assert.Empty(PickerSearch.Find(WithContent(content), "e"));
    }

    [Theory]
    [InlineData(0xD83D)]
    [InlineData(0xDC4D)]
    public void Incomplete_query_returns_no_results_without_normalization_error(int codeUnit)
    {
        Assert.Empty(PickerSearch.Find(Sample(), new string((char)codeUnit, 1)));
    }

    private static LibrarySnapshot Sample()
    {
        var enabled = Guid.NewGuid();
        var disabled = Guid.NewGuid();
        var snippets = new List<Snippet>
        {
            new(Guid.NewGuid(), enabled, "Firma", ";firma", "Café 🧑🏽‍💻 مرحبا", "E-mail", true, 1),
            new(Guid.NewGuid(), enabled, "Appunti", ";app", "Ciao", "Testo breve", true, 1),
            new(Guid.NewGuid(), enabled, "Segreto", ";hidden", "Non mostrare", "", false, 1),
            new(Guid.NewGuid(), disabled, "Privato", ";private", "Non mostrare", "", true, 1),
        };
        return new([new(enabled, "Generale", true), new(disabled, "Disattivato", false)], snippets, 1);
    }

    [Fact] public void Empty_query_shows_only_enabled_snippets_in_enabled_groups()
    {
        var found = PickerSearch.Find(Sample(), "");
        Assert.Equal(2, found.Count);
        Assert.DoesNotContain(found, x => x.Title == "Privato" || x.Title == "Segreto");
    }

    [Fact] public void Canonically_equivalent_accents_match_without_changing_content()
    {
        var result = Assert.Single(PickerSearch.Find(Sample(), "Cafe\u0301"));
        Assert.Equal("Café 🧑🏽‍💻 مرحبا", result.Content);
    }

    [Theory]
    [InlineData(";firma")]
    [InlineData("firma")]
    [InlineData("E-mail")]
    [InlineData("مرحبا")]
    public void Queries_can_match_trigger_title_description_and_text(string query)
    {
        Assert.Equal("Firma", Assert.Single(PickerSearch.Find(Sample(), query)).Title);
    }

    [Fact] public void Exact_trigger_has_priority_over_title()
    {
        var groupId = Guid.NewGuid();
        var library = new LibrarySnapshot([new(groupId,"G",true)],
            [new(Guid.NewGuid(),groupId,";firma",";first","a","",true,1),
             new(Guid.NewGuid(),groupId,"Second",";firma","b","",true,1)],1);
        Assert.Equal(";firma", PickerSearch.Find(library, ";firma")[0].Trigger);
    }

    [Fact] public void Result_limit_is_enforced()
    {
        Assert.Single(PickerSearch.Find(Sample(), "", 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PickerSearch.Find(Sample(), "", 101));
    }
}
