using System.Text;
using TextExpander.Application.Abstractions;
using TextExpander.Application.Features.Portability;
using TextExpander.Core;
using TextExpander.Infrastructure.Storage;
using Xunit;

namespace TextExpander.Tests;

public sealed class PortableLibraryTests
{
    [Fact]
    public async Task Export_Import_Unicode_Preserves_Data_And_Creates_New_Identifiers()
    {
        var root = CreateRoot();
        try
        {
            var store = new SqliteLibraryStore(Path.Combine(root, "library.db"));
            await store.InitializeAsync();
            var group = await store.CreateGroupAsync("Работа 🌍");
            var source = await store.SaveSnippetAsync(new SnippetDraft(null, group.Id,
                "العربية", ";café", "👨‍👩‍👧‍👦 مرحبا e\u0301", "descrizione", true, null));
            var service = new PortableLibraryService(store);
            var exportPath = Path.Combine(root, "export.json");
            await service.ExportAsync(exportPath);
            var json = await File.ReadAllTextAsync(exportPath, Encoding.UTF8);
            using (var parsed = System.Text.Json.JsonDocument.Parse(json))
            {
                var item = parsed.RootElement.GetProperty("snippets")[0];
                Assert.Equal("👨‍👩‍👧‍👦 مرحبا e\u0301", item.GetProperty("content").GetString());
            }
            Assert.DoesNotContain("theme", json); // impostazioni escluse: non è un backup
            var imported = await service.ImportAsync(exportPath);
            Assert.Equal(1, imported.GroupsImported);
            Assert.Equal(1, imported.SnippetsImported);
            var loaded = await store.ReadAsync();
            Assert.Equal(2, loaded.Groups.Count);
            Assert.Equal(2, loaded.Snippets.Count);
            Assert.NotEqual(source.Id, loaded.Snippets.Single(s => s.Id != source.Id).Id);
            Assert.All(loaded.Snippets, s => Assert.Equal("👨‍👩‍👧‍👦 مرحبا e\u0301", s.Content));
            Assert.Equal(2, loaded.Groups.Select(g => g.Name).Distinct().Count());
        }
        finally { Cleanup(root); }
    }

    [Fact]
    public async Task Bad_Json_Is_Rejected_Without_Changing_The_Library()
    {
        var root = CreateRoot();
        try
        {
            var store = new SqliteLibraryStore(Path.Combine(root, "library.db"));
            await store.InitializeAsync();
            await store.CreateGroupAsync("Intatto");
            var input = Path.Combine(root, "bad.json");
            await File.WriteAllTextAsync(input, "{\"format\":\"text-expander-studio/snippets\",\"version\":99,\"groups\":[],\"snippets\":[]}");
            await Assert.ThrowsAsync<DataIntegrityException>(() =>
                new PortableLibraryService(store).ImportAsync(input));
            Assert.Single((await store.ReadAsync()).Groups);
        }
        finally { Cleanup(root); }
    }

    [Fact]
    public async Task Invalid_Reference_Is_Rejected_Before_Transaction()
    {
        var root = CreateRoot();
        try
        {
            var store = new SqliteLibraryStore(Path.Combine(root, "library.db"));
            await store.InitializeAsync();
            var invalid = new PortableLibraryDocument(PortableLibraryFormat.Identifier, 1,
                [new PortableGroup(Guid.NewGuid(), "A", true)],
                [new PortableSnippet(Guid.NewGuid(), Guid.NewGuid(), "B", ";b", "Hi", "", true)]);
            await Assert.ThrowsAsync<DataIntegrityException>(() => store.ImportPortableAsync(invalid));
            Assert.Empty((await store.ReadAsync()).Groups);
        }
        finally { Cleanup(root); }
    }

    [Fact]
    public async Task Export_Does_Not_Overwrite_Existing_File()
    {
        var root = CreateRoot();
        try
        {
            var store = new SqliteLibraryStore(Path.Combine(root, "library.db"));
            await store.InitializeAsync();
            var export = Path.Combine(root, "existing.json");
            await File.WriteAllTextAsync(export, "untouched");
            await Assert.ThrowsAsync<IOException>(() =>
                new PortableLibraryService(store).ExportAsync(export));
            Assert.Equal("untouched", await File.ReadAllTextAsync(export));
        }
        finally { Cleanup(root); }
    }

    private static string CreateRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "text-expander-portable-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
    private static void Cleanup(string path)
    {
        try { Directory.Delete(path, true); } catch (IOException) { }
    }
}
