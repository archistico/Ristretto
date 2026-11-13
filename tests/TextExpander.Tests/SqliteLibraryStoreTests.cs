using TextExpander.Application.Abstractions;
using TextExpander.Infrastructure.Storage;
using TextExpander.Core;
using Xunit;

namespace TextExpander.Tests;

public sealed class SqliteLibraryStoreTests
{
    private sealed class TempStore : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "text-expander-tests", Guid.NewGuid().ToString("N"));
        public SqliteLibraryStore Store { get; }
        public TempStore()
        {
            Directory.CreateDirectory(Root);
            Store = new SqliteLibraryStore(Path.Combine(Root, "test.db"));
        }
        public void Dispose() { try { Directory.Delete(Root, true); } catch (IOException) { } }
    }

    private static SnippetDraft Draft(Guid group, string title = "Email", string trigger = ";mail",
        string content = "Ciao 🌍 مرحبا") => new(null, group, title, trigger, content, "", true, null);

    [Fact]
    public async Task Create_Read_Update_Delete_Roundtrip_Unicode()
    {
        using var t = new TempStore();
        await t.Store.InitializeAsync();
        var group = await t.Store.CreateGroupAsync("Messaggi 🌍");
        var first = await t.Store.SaveSnippetAsync(Draft(group.Id));
        Assert.Contains("مرحبا", (await t.Store.ReadAsync()).Snippets.Single().Content);
        var changed = await t.Store.SaveSnippetAsync(new SnippetDraft(first.Id, group.Id,
            "Firma", ";firma", "café 😀", "", true, first.Revision));
        Assert.Equal(2, changed.Revision);
        Assert.Equal("café 😀", (await t.Store.ReadAsync()).Snippets.Single().Content.Normalize(System.Text.NormalizationForm.FormC));
        await t.Store.DeleteSnippetAsync(changed.Id, changed.Revision);
        Assert.Empty((await t.Store.ReadAsync()).Snippets);
    }

    [Fact]
    public async Task Stale_Revision_Is_Rejected_Without_Modification()
    {
        using var t = new TempStore();
        await t.Store.InitializeAsync();
        var group = await t.Store.CreateGroupAsync("Personal");
        var first = await t.Store.SaveSnippetAsync(Draft(group.Id));
        await t.Store.SaveSnippetAsync(new SnippetDraft(first.Id, group.Id, "New", ";mail", "B", "", true, 1));
        await Assert.ThrowsAsync<ConcurrencyException>(() => t.Store.SaveSnippetAsync(
            new SnippetDraft(first.Id, group.Id, "Stale", ";mail", "C", "", true, 1)));
        Assert.Equal("B", (await t.Store.ReadAsync()).Snippets.Single().Content);
    }

    [Fact]
    public async Task Sqlite_Online_Backup_And_Restore_Preserves_Old_Library()
    {
        using var t = new TempStore();
        await t.Store.InitializeAsync();
        var group = await t.Store.CreateGroupAsync("Backups");
        await t.Store.SaveSnippetAsync(Draft(group.Id));
        var backupPath = Path.Combine(t.Root, "backup.db");
        await t.Store.CreateBackupAsync(backupPath);
        await t.Store.SaveSnippetAsync(Draft(group.Id, "Second", ";second", "2"));
        Assert.Equal(2, (await t.Store.ReadAsync()).Snippets.Count);
        await t.Store.RestoreBackupAsync(backupPath);
        Assert.Single((await t.Store.ReadAsync()).Snippets);
        Assert.True(File.Exists(backupPath));
        Assert.Single(Directory.GetFiles(t.Root, "*.recovery.db"));
    }

    [Fact]
    public async Task Invalid_Backup_Is_Rejected_Without_Touching_Current_Data()
    {
        using var t = new TempStore();
        await t.Store.InitializeAsync();
        var group = await t.Store.CreateGroupAsync("Safe");
        await t.Store.SaveSnippetAsync(Draft(group.Id));
        var bad = Path.Combine(t.Root, "bad.db");
        await File.WriteAllTextAsync(bad, "not a sqlite file");
        await Assert.ThrowsAnyAsync<Exception>(() => t.Store.RestoreBackupAsync(bad));
        Assert.Single((await t.Store.ReadAsync()).Snippets);
    }

    [Fact]
    public async Task Theme_And_Language_Stored_In_Sqlite()
    {
        using var t = new TempStore();
        await t.Store.InitializeAsync();
        await t.Store.WriteSettingAsync("language", "it");
        await t.Store.WriteSettingAsync("theme", "Dark");
        Assert.Equal("it", await t.Store.ReadSettingAsync("language"));
        Assert.Equal("Dark", await t.Store.ReadSettingAsync("theme"));
    }
}
