using System.Text;
using TextExpander.Application.Abstractions;
using TextExpander.Application.Features.Portability;
using TextExpander.Core;
using TextExpander.Infrastructure.Storage;
using Xunit;

namespace TextExpander.Tests;

public sealed class ExternalImportTests
{
    [Fact]
    public async Task BeeftextV10_Previews_And_Imports_Disabled_Without_Losing_Unicode()
    {
        await UsingStore(async (root, store) =>
        {
            var path = Path.Combine(root, "beeftext.json");
            await File.WriteAllTextAsync(path, """
            {"fileFormatVersion":10,
             "groups":[{"uuid":"{11111111-1111-1111-1111-111111111111}","name":"Saluti 🌍","enabled":true}],
             "combos":[{"uuid":"{22222222-2222-2222-2222-222222222222}","group":"{11111111-1111-1111-1111-111111111111}",
              "name":"Unicode", "keyword":":ciao", "snippet":"Caffè 👨‍👩‍👧‍👦 é العربية", "description":"", "enabled":true,
              "matchingMode":1,"caseSensitivity":1}]}
            """, Encoding.UTF8);
            var importer = new ExternalLibraryImportService(store);
            var preview = await importer.PreviewAsync(path);
            Assert.Equal(ImportSource.BeeftextV10, preview.Source);
            Assert.Single(preview.Document.Snippets);
            Assert.False(preview.Document.Snippets[0].IsEnabled);
            Assert.Empty((await store.ReadAsync()).Groups); // preview di sola lettura
            await importer.ImportAsync(preview);
            var imported = Assert.Single((await store.ReadAsync()).Snippets);
            Assert.Equal("Caffè 👨‍👩‍👧‍👦 é العربية", imported.Content);
            Assert.False(imported.IsEnabled);
        });
    }

    [Fact]
    public async Task Beeftext_Unknown_Version_Is_Rejected_And_Db_Unchanged()
    {
        await UsingStore(async (root, store) =>
        {
            var path = Path.Combine(root, "future.json");
            await File.WriteAllTextAsync(path, """{"fileFormatVersion":11,"combos":[],"groups":[]}""");
            await Assert.ThrowsAsync<DataIntegrityException>(() =>
                new ExternalLibraryImportService(store).PreviewAsync(path));
            Assert.Empty((await store.ReadAsync()).Groups);
        });
    }

    [Fact]
    public async Task Beeftext_Export_Without_Groups_Uses_Default_Group()
    {
        await UsingStore(async (root, store) =>
        {
            var path = Path.Combine(root, "export.json");
            await File.WriteAllTextAsync(path, """
            {"fileFormatVersion":10,"groups":[],"combos":[
              {"name":"Firma","keyword":";firma","snippet":"Firma aziendale","enabled":true}]}
            """);
            var preview = await new ExternalLibraryImportService(store).PreviewAsync(path);
            Assert.Single(preview.Document.Groups);
            Assert.Single(preview.Document.Snippets);
            Assert.Equal(preview.Document.Groups[0].Id, preview.Document.Snippets[0].GroupId);
        });
    }

    [Fact]
    public async Task Espanso_Safe_And_Dynamic_Rules_Are_Previewed_But_Never_Executed()
    {
        await UsingStore(async (root, store) =>
        {
            var marker = Path.Combine(root, "executed.txt");
            var path = Path.Combine(root, "base.yml");
            var yaml = """
            matches:
              - trigger: ":hello"
                replace: "Caffè 😀"
              - triggers: [":one", ":two"]
                replace: "{{value}}"
                vars:
                  - name: value
                    type: shell
                    params:
                      cmd: "echo should-not-run"
              - trigger: ":form"
                form: |
                  Gentile [[name]],
                  saluti.
              - regex: ":re(.*)"
                replace: "not supported"
            """;
            await File.WriteAllTextAsync(path, yaml);
            var importer = new ExternalLibraryImportService(store);
            var preview = await importer.PreviewAsync(path);
            Assert.Equal(ImportSource.EspansoYaml, preview.Source);
            Assert.Equal(4, preview.Document.Snippets.Count);
            Assert.All(preview.Document.Snippets, s => Assert.False(s.IsEnabled));
            Assert.Contains(preview.Issues, i => i.Kind == ImportIssueKind.DynamicContent);
            Assert.Contains(preview.Issues, i => i.Kind == ImportIssueKind.SkippedItem);
            Assert.False(File.Exists(marker));
            Assert.Empty((await store.ReadAsync()).Groups);
            await importer.ImportAsync(preview);
            Assert.Equal(4, (await store.ReadAsync()).Snippets.Count);
            Assert.False(File.Exists(marker));
        });
    }

    [Fact]
    public async Task Preview_Finds_Collisions_Against_Existing_And_Within_Import()
    {
        await UsingStore(async (root, store) =>
        {
            var g = await store.CreateGroupAsync("Existing");
            await store.SaveSnippetAsync(new SnippetDraft(null, g.Id, "Old", ":test", "old", "", true, null));
            var path = Path.Combine(root, "same.yml");
            await File.WriteAllTextAsync(path, """
            matches:
              - trigger: ":test"
                replace: "first"
              - trigger: ":test"
                replace: "second"
            """);
            var preview = await new ExternalLibraryImportService(store).PreviewAsync(path);
            Assert.Contains(preview.Issues, i => i.Kind == ImportIssueKind.TriggerCollision);
            Assert.Single((await store.ReadAsync()).Snippets);
        });
    }

    [Fact]
    public async Task Import_Does_Not_Overwrite_Existing_Entries()
    {
        await UsingStore(async (root, store) =>
        {
            var g = await store.CreateGroupAsync("Existing");
            var old = await store.SaveSnippetAsync(new SnippetDraft(null, g.Id, "Original", ":x", "old", "", true, null));
            var path = Path.Combine(root, "base.yml");
            await File.WriteAllTextAsync(path, "matches:\n  - trigger: ':x'\n    replace: 'new'\n");
            var importer = new ExternalLibraryImportService(store);
            var preview = await importer.PreviewAsync(path);
            await importer.ImportAsync(preview);
            var current = await store.ReadAsync();
            Assert.Equal("old", current.Snippets.Single(s => s.Id == old.Id).Content);
            Assert.False(current.Snippets.Single(s => s.Id != old.Id).IsEnabled);
        });
    }

    [Fact]
    public async Task Invalid_Utf8_Is_Rejected_Instead_Of_Replacing_Characters()
    {
        await UsingStore(async (root, store) =>
        {
            var path = Path.Combine(root, "malformed.json");
            await File.WriteAllBytesAsync(path, [0x7B, 0x22, 0xFF, 0x22, 0x7D]);
            await Assert.ThrowsAsync<DataIntegrityException>(() =>
                new ExternalLibraryImportService(store).PreviewAsync(path));
            Assert.Empty((await store.ReadAsync()).Groups);
        });
    }

    [Fact]
    public async Task Utf8_Bom_Is_Accepted()
    {
        await UsingStore(async (root, store) =>
        {
            var path = Path.Combine(root, "with-bom.yml");
            var body = Encoding.UTF8.GetBytes("matches:\n  - trigger: ':hello'\n    replace: 'Salve 🌍'\n");
            await File.WriteAllBytesAsync(path, [0xEF, 0xBB, 0xBF, .. body]);
            var preview = await new ExternalLibraryImportService(store).PreviewAsync(path);
            Assert.Equal("Salve 🌍", Assert.Single(preview.Document.Snippets).Content);
        });
    }

    [Fact]
    public async Task Unsupported_Yaml_With_No_Convertible_Matches_Cannot_Commit()
    {
        await UsingStore(async (root, store) =>
        {
            var path = Path.Combine(root, "regex.yml");
            await File.WriteAllTextAsync(path, "matches:\n  - regex: ':name(.*)'\n    replace: 'text'\n");
            var importer = new ExternalLibraryImportService(store);
            var preview = await importer.PreviewAsync(path);
            Assert.False(preview.CanImport);
            await Assert.ThrowsAsync<DataIntegrityException>(() => importer.ImportAsync(preview));
            Assert.Empty((await store.ReadAsync()).Groups);
        });
    }

    private static async Task UsingStore(Func<string, SqliteLibraryStore, Task> body)
    {
        var root = Path.Combine(Path.GetTempPath(), "text-expander-legacy-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store = new SqliteLibraryStore(Path.Combine(root, "test.db"));
            await store.InitializeAsync();
            await body(root, store);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch (IOException) { }
        }
    }
}
