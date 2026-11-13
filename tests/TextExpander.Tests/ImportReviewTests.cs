using TextExpander.Application.Abstractions;
using TextExpander.Application.Features.Portability;
using TextExpander.Core;
using TextExpander.Infrastructure.Storage;
using Xunit;

namespace TextExpander.Tests;

public sealed class ImportReviewTests
{
    [Fact]
    public async Task Preview_Identifies_All_Duplicates_And_Collisions_With_Existing_Triggers()
    {
        await InStore(async (root, store) =>
        {
            var group = await store.CreateGroupAsync("Esistenti");
            await store.SaveSnippetAsync(new SnippetDraft(null, group.Id,
                "Prima", "café", "già presente", "", true, null));
            var path = Path.Combine(root, "duplicates.yml");
            await File.WriteAllTextAsync(path, """
            matches:
              - trigger: "café"
                replace: "espresso"
              - trigger: ":double"
                replace: "primo"
              - trigger: ":double"
                replace: "secondo"
              - trigger: ":free"
                replace: "Привет 🌍"
            """);
            var preview = await new ExternalLibraryImportService(store).PreviewAsync(path);
            Assert.Equal(4, preview.Candidates.Count);
            Assert.All(preview.Candidates.Where(x => x.Trigger is ":double"),
                x => Assert.True(x.Risk.HasFlag(ImportItemRisk.TriggerCollision)));
            Assert.True(preview.Candidates[0].Risk.HasFlag(ImportItemRisk.TriggerCollision));
            Assert.True(preview.Candidates[3].SuggestedForImport);
            Assert.False(preview.Candidates[0].SuggestedForImport);
            Assert.Single((await store.ReadAsync()).Snippets);
        });
    }

    [Fact]
    public async Task Selected_Import_Only_Adds_Chosen_Snippets_And_Their_Groups()
    {
        await InStore(async (root, store) =>
        {
            var path = Path.Combine(root, "selection.json");
            await File.WriteAllTextAsync(path, """
            {"fileFormatVersion":10,"groups":[
              {"uuid":"11111111-1111-1111-1111-111111111111","name":"Gruppo A"},
              {"uuid":"22222222-2222-2222-2222-222222222222","name":"Gruppo B"}],
             "combos":[
              {"name":"Semplice","keyword":";ok","snippet":"Caffè 😀",
               "group":"11111111-1111-1111-1111-111111111111","matchingMode":1,"caseSensitivity":1},
              {"name":"Dinamico","keyword":";dyn","snippet":"#{date}",
               "group":"22222222-2222-2222-2222-222222222222","matchingMode":1,"caseSensitivity":1}]}
            """);
            var importer = new ExternalLibraryImportService(store);
            var preview = await importer.PreviewAsync(path);
            var safe = Assert.Single(preview.Candidates, x => x.SuggestedForImport);
            Assert.True(preview.Candidates.Single(x => x.Trigger == ";dyn").Risk.HasFlag(ImportItemRisk.DynamicContent));
            Assert.Empty((await store.ReadAsync()).Groups);
            var summary = await importer.ImportAsync(preview, [safe.Id]);
            Assert.Equal(1, summary.SnippetsImported);
            Assert.Equal(1, summary.GroupsImported);
            var current = await store.ReadAsync();
            Assert.Equal("Gruppo A", Assert.Single(current.Groups).Name);
            Assert.Equal("Caffè 😀", Assert.Single(current.Snippets).Content);
            Assert.False(current.Snippets[0].IsEnabled);
        });
    }

    [Fact]
    public async Task Invalid_Or_Empty_Selections_Do_Not_Modify_The_Database()
    {
        await InStore(async (root, store) =>
        {
            var path = Path.Combine(root, "one.yml");
            await File.WriteAllTextAsync(path, "matches:\n  - trigger: ':one'\n    replace: 'uno'\n");
            var importer = new ExternalLibraryImportService(store);
            var preview = await importer.PreviewAsync(path);
            var id = Assert.Single(preview.Candidates).Id;
            await Assert.ThrowsAsync<DataIntegrityException>(() => importer.ImportAsync(preview, Array.Empty<Guid>()));
            await Assert.ThrowsAsync<DataIntegrityException>(() => importer.ImportAsync(preview, [Guid.NewGuid()]));
            await Assert.ThrowsAsync<DataIntegrityException>(() => importer.ImportAsync(preview, [id, id]));
            Assert.Empty((await store.ReadAsync()).Snippets);
        });
    }

    [Fact]
    public async Task Library_Changes_Between_Preview_And_Commit_Reject_Stale_Selection()
    {
        await InStore(async (root, store) =>
        {
            var path = Path.Combine(root, "one.yml");
            await File.WriteAllTextAsync(path, "matches:\n  - trigger: ':one'\n    replace: 'uno'\n");
            var importer = new ExternalLibraryImportService(store);
            var preview = await importer.PreviewAsync(path);
            await store.CreateGroupAsync("Nuovo gruppo dopo la preview");
            var selected = new[] { Assert.Single(preview.Candidates).Id };
            await Assert.ThrowsAsync<ConcurrencyException>(() => importer.ImportAsync(preview, selected));
            var after = await store.ReadAsync();
            Assert.Single(after.Groups);
            Assert.Empty(after.Snippets);
        });
    }

    [Fact]
    public async Task Commit_Of_The_Same_Preview_Twice_Is_Not_Allowed()
    {
        await InStore(async (root, store) =>
        {
            var path = Path.Combine(root, "one.yml");
            await File.WriteAllTextAsync(path, "matches:\n  - trigger: ':one'\n    replace: 'uno'\n");
            var importer = new ExternalLibraryImportService(store);
            var preview = await importer.PreviewAsync(path);
            var selected = new[] { Assert.Single(preview.Candidates).Id };
            await importer.ImportAsync(preview, selected);
            await Assert.ThrowsAsync<ConcurrencyException>(() => importer.ImportAsync(preview, selected));
            Assert.Single((await store.ReadAsync()).Snippets);
        });
    }

    [Fact]
    public async Task Preview_Risks_Of_Espanso_Dynamic_And_Unsupported_Are_Visible()
    {
        await InStore(async (root, store) =>
        {
            var path = Path.Combine(root, "dynamic.yml");
            await File.WriteAllTextAsync(path, """
            matches:
              - trigger: ':dynamic'
                replace: '{{shell_output}}'
                vars:
                  - name: shell_output
                    type: shell
                    params: { cmd: 'echo never-run' }
                word: true
              - trigger: ':safe'
                replace: 'مرحبا 👋🏽'
            """);
            var preview = await new ExternalLibraryImportService(store).PreviewAsync(path);
            var dynamicItem = preview.Candidates.Single(x => x.Trigger == ":dynamic");
            Assert.True(dynamicItem.Risk.HasFlag(ImportItemRisk.DynamicContent));
            Assert.True(dynamicItem.Risk.HasFlag(ImportItemRisk.UnsupportedOptions));
            Assert.False(dynamicItem.SuggestedForImport);
            Assert.True(preview.Candidates.Single(x => x.Trigger == ":safe").SuggestedForImport);
            Assert.Empty((await store.ReadAsync()).Groups);
        });
    }

    private static async Task InStore(Func<string, SqliteLibraryStore, Task> action)
    {
        var directory = Path.Combine(Path.GetTempPath(), "text-expander-review-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var store = new SqliteLibraryStore(Path.Combine(directory, "data.db"));
            await store.InitializeAsync();
            await action(directory, store);
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch (IOException) { }
        }
    }
}
