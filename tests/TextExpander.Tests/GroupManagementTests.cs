using TextExpander.Application.Abstractions;
using TextExpander.Infrastructure.Storage;
using TextExpander.Core;
using Xunit;

namespace TextExpander.Tests;

public sealed class GroupManagementTests
{
    [Fact]
    public async Task Rename_And_Toggle_Are_Persistent_And_Do_Not_Modify_Snippets()
    {
        var path = TestDatabasePath();
        try
        {
            var store = new SqliteLibraryStore(path);
            await store.InitializeAsync();
            var group = await store.CreateGroupAsync("Lavoro");
            var snippet = await store.SaveSnippetAsync(new SnippetDraft(null, group.Id,
                "Saluto", ";hello", "Ciao 🌎", "", true, null));
            var renamed = await store.UpdateGroupAsync(group, "Übung 🌍", false);
            Assert.False(renamed.IsEnabled);
            Assert.Equal("Übung 🌍", (await store.ReadAsync()).Groups.Single().Name);
            Assert.Equal(snippet.Id, (await store.ReadAsync()).Snippets.Single().Id);
            Assert.Equal("Ciao 🌎", (await store.ReadAsync()).Snippets.Single().Content);
            await Assert.ThrowsAsync<ConcurrencyException>(() =>
                store.UpdateGroupAsync(group, "Old state", true));
        }
        finally { Cleanup(path); }
    }

    [Fact]
    public async Task Delete_Only_Empty_Group()
    {
        var path = TestDatabasePath();
        try
        {
            var store = new SqliteLibraryStore(path);
            await store.InitializeAsync();
            var first = await store.CreateGroupAsync("Protezione");
            var snippet = await store.SaveSnippetAsync(new SnippetDraft(null, first.Id,
                "Firma", ";firma", "Testo", "", true, null));
            await Assert.ThrowsAsync<InvalidOperationException>(() => store.DeleteGroupAsync(first));
            Assert.Single((await store.ReadAsync()).Groups);
            await store.DeleteSnippetAsync(snippet.Id, snippet.Revision);
            await store.DeleteGroupAsync(first);
            Assert.Empty((await store.ReadAsync()).Groups);
        }
        finally { Cleanup(path); }
    }

    private static string TestDatabasePath() => Path.Combine(Path.GetTempPath(),
        "text-expander-group-tests", Guid.NewGuid().ToString("N"), "test.db");
    private static void Cleanup(string path)
    {
        try { Directory.Delete(Path.GetDirectoryName(path)!, true); } catch (IOException) { }
    }
}
