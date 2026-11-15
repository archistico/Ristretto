using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using TextExpander.Application.Abstractions;
using TextExpander.Application.Features.Hotkeys;
using TextExpander.Application.Features.Portability;
using TextExpander.Core;
using TextExpander.Desktop.Localization;
using TextExpander.Desktop.ViewModels;
using TextExpander.Infrastructure.Storage;
using Xunit;

namespace TextExpander.Tests;

// All data lives in a new synthetic directory; no desktop backend or native registrations.
public sealed class SnippetHotkeyStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ristretto-hotkey-tests", Guid.NewGuid().ToString("N"));
    private string FileAt(string name) => Path.Combine(_root, name);
    private SqliteLibraryStore Store(string name = "library.db") => new(FileAt(name));
    private static SnippetDraft Draft(Guid group, string key = "Ctrl+Alt+F") =>
        new(null, group, "Firma", ";firma", "Caffè e\u0301 🧑🏽‍💻\nمرحبا", "Description", true, null, key);

    public void Dispose()
    {
        var parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "ristretto-hotkey-tests")) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(_root).StartsWith(parent, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException();
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task HotkeyIsCanonicalizedPersistedUpdatedAndRemoved()
    {
        var store = Store();
        await store.InitializeAsync();
        var group = await store.CreateGroupAsync("G");
        var saved = await store.SaveSnippetAsync(Draft(group.Id, "alt+ctrl+f"));
        Assert.Equal("Ctrl+Alt+F", saved.Hotkey);
        Assert.Equal(saved, Assert.Single((await Store().ReadAsync()).Snippets));
        var updated = await store.SaveSnippetAsync(Draft(group.Id, "Ctrl+Alt+G") with { Id = saved.Id, ExpectedRevision = saved.Revision });
        Assert.Equal(2, updated.Revision);
        var removed = await store.SaveSnippetAsync(Draft(group.Id, "None") with { Id = saved.Id, ExpectedRevision = updated.Revision });
        Assert.Empty(removed.Hotkey);
        Assert.Empty(Assert.Single((await store.ReadAsync()).Snippets).Hotkey);
    }

    [Fact]
    public async Task DuplicateAssignmentIsAtomicEvenForDisabledSnippetsAndDeleteReleasesIt()
    {
        var store = Store();
        await store.InitializeAsync();
        var group = await store.CreateGroupAsync("G");
        var first = await store.SaveSnippetAsync(Draft(group.Id) with { IsEnabled = false });
        var before = await store.ReadAsync();
        await Assert.ThrowsAsync<SnippetHotkeyConflictException>(() => store.SaveSnippetAsync(Draft(group.Id, "Alt+Ctrl+F")));
        Assert.Equal(before.Revision, (await store.ReadAsync()).Revision);
        Assert.Single((await store.ReadAsync()).Snippets);
        await store.DeleteSnippetAsync(first.Id, first.Revision);
        Assert.Equal("Ctrl+Alt+F", (await store.SaveSnippetAsync(Draft(group.Id))).Hotkey);
    }

    [Fact]
    public async Task PickerAndSnippetAssignmentsCannotCollideInEitherDirection()
    {
        var store = Store();
        await store.InitializeAsync();
        var group = await store.CreateGroupAsync("G");
        await Assert.ThrowsAsync<SnippetHotkeyConflictException>(() => store.SaveSnippetAsync(Draft(group.Id, PickerHotkey.Default)));
        await store.SaveSnippetAsync(Draft(group.Id));
        await Assert.ThrowsAsync<SnippetHotkeyConflictException>(() => store.WriteSettingAsync("picker.hotkey", "Alt+Ctrl+F"));
        Assert.Null(await store.ReadSettingAsync("picker.hotkey"));
        await store.WriteSettingAsync("picker.hotkey", "None");
        Assert.Equal(PickerHotkey.Default, (await store.SaveSnippetAsync(Draft(group.Id, PickerHotkey.Default))).Hotkey);
    }

    [Fact]
    public async Task InvalidHotkeyCannotChangeAnExistingSnippet()
    {
        var store = Store();
        await store.InitializeAsync();
        var group = await store.CreateGroupAsync("G");
        var saved = await store.SaveSnippetAsync(Draft(group.Id));
        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveSnippetAsync(Draft(group.Id, "Ctrl+Enter")
            with { Id = saved.Id, ExpectedRevision = saved.Revision }));
        Assert.Equal(saved, Assert.Single((await store.ReadAsync()).Snippets));
    }

    private void CreateLegacyDatabase(string file)
    {
        Directory.CreateDirectory(_root);
        using var db = new SqliteConnection("Data Source=" + file + ";Pooling=False");
        db.Open();
        using var cmd = db.CreateCommand();
        // Historical schema v1 fixture, intentionally without a hotkey column/index.
        cmd.CommandText = @"CREATE TABLE groups(id TEXT PRIMARY KEY,name TEXT,enabled INTEGER);
CREATE TABLE snippets(id TEXT PRIMARY KEY,group_id TEXT,title TEXT,trigger TEXT,content TEXT,
description TEXT,enabled INTEGER,revision INTEGER);
CREATE TABLE settings(key TEXT PRIMARY KEY,value TEXT);
CREATE TABLE app_meta(id INTEGER PRIMARY KEY,library_revision INTEGER);
INSERT INTO groups VALUES('00000000-0000-4000-8000-000000000001','Legacy',1);
INSERT INTO snippets VALUES('00000000-0000-4000-8000-000000000002','00000000-0000-4000-8000-000000000001',
'Firma',';firma','Caffè 🌍','Old description',1,3);
INSERT INTO settings VALUES('picker.hotkey','None');
INSERT INTO app_meta VALUES(1,7);
PRAGMA user_version=1;";
        cmd.ExecuteNonQuery();
    }

    [Fact]
    public async Task LegacyDatabaseMigratesIdempotentlyWithoutChangingTextSettingsOrRevisions()
    {
        CreateLegacyDatabase(FileAt("library.db"));
        var store = Store();
        await store.InitializeAsync();
        await store.InitializeAsync();
        var snapshot = await store.ReadAsync();
        var saved = Assert.Single(snapshot.Snippets);
        Assert.Equal(7, snapshot.Revision);
        Assert.Equal(3, saved.Revision);
        Assert.Equal("Caffè 🌍", saved.Content);
        Assert.Equal("Old description", saved.Description);
        Assert.Empty(saved.Hotkey);
        Assert.Equal("None", await store.ReadSettingAsync("picker.hotkey"));
        Assert.Equal("Ctrl+Alt+F", (await store.SaveSnippetAsync(Draft(saved.GroupId) with { Id = saved.Id, ExpectedRevision = 3 })).Hotkey);
    }

    [Fact]
    public async Task RestoreLegacyBackupMigratesOnlyTheStagedCopyAndPreservesRecovery()
    {
        var original = FileAt("legacy.db");
        CreateLegacyDatabase(original);
        var digest = SHA256.HashData(File.ReadAllBytes(original));
        var store = Store();
        await store.InitializeAsync();
        var group = await store.CreateGroupAsync("Current");
        await store.SaveSnippetAsync(Draft(group.Id));
        await store.RestoreBackupAsync(original);
        Assert.Equal(digest, SHA256.HashData(File.ReadAllBytes(original)));
        Assert.Empty(Assert.Single((await store.ReadAsync()).Snippets).Hotkey);
        var recovery = Assert.Single(Directory.GetFiles(_root, "*.recovery.db"));
        Assert.Equal("Ctrl+Alt+F", Assert.Single((await new SqliteLibraryStore(recovery).ReadAsync()).Snippets).Hotkey);
    }

    [Fact]
    public async Task BackupRoundtripPreservesHotkeyAndOriginalUnicode()
    {
        var store = Store();
        await store.InitializeAsync();
        var group = await store.CreateGroupAsync("G");
        var saved = await store.SaveSnippetAsync(Draft(group.Id));
        var backup = await store.CreateBackupAsync(FileAt("backup.db"));
        await store.SaveSnippetAsync(Draft(group.Id, "Ctrl+Alt+G") with { Id = saved.Id, ExpectedRevision = saved.Revision });
        await store.RestoreBackupAsync(backup);
        Assert.Equal(saved, Assert.Single((await store.ReadAsync()).Snippets));
    }

    [Fact]
    public async Task JsonRoundtripPreservesHotkeyAndLegacyJsonDefaultsToNoHotkey()
    {
        var store = Store();
        await store.InitializeAsync();
        var group = await store.CreateGroupAsync("G");
        var saved = await store.SaveSnippetAsync(Draft(group.Id));
        var file = FileAt("export.json");
        await new PortableLibraryService(store).ExportAsync(file);
        var destination = Store("destination.db");
        await destination.InitializeAsync();
        await new PortableLibraryService(destination).ImportAsync(file);
        var imported = Assert.Single((await destination.ReadAsync()).Snippets);
        Assert.Equal(saved.Hotkey, imported.Hotkey);
        Assert.Equal(saved.Content, imported.Content);
        Assert.NotEqual(saved.Id, imported.Id);
        // Remove the optional field, reproducing a JSON export from previous versions.
        var legacy = JsonSerializer.Serialize(new { format = PortableLibraryFormat.Identifier, version = 1,
            groups = new[] { new { id = group.Id, name = "Legacy", isEnabled = true } },
            snippets = new[] { new { id = saved.Id, groupId = group.Id, saved.Title, saved.Trigger, saved.Content,
                saved.Description, saved.IsEnabled } } }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        File.WriteAllText(file, legacy);
        await new PortableLibraryService(destination).ImportAsync(file);
        Assert.Single((await destination.ReadAsync()).Snippets, s => s.Hotkey.Length == 0);
    }

    [Fact]
    public async Task ImportConflictRollsBackGroupsSnippetsAndLibraryRevision()
    {
        var store = Store();
        await store.InitializeAsync();
        var group = await store.CreateGroupAsync("G");
        await store.SaveSnippetAsync(Draft(group.Id));
        var before = await store.ReadAsync();
        var document = PortableLibraryFormat.FromSnapshot(before);
        await Assert.ThrowsAsync<SnippetHotkeyConflictException>(() => store.ImportPortableAsync(document));
        var after = await store.ReadAsync();
        Assert.Equal(before.Revision, after.Revision);
        Assert.Equal(before.Groups, after.Groups);
        Assert.Equal(before.Snippets, after.Snippets);
    }

    [Fact]
    public async Task EditorTracksHotkeyChangesSavesCanonicalValueAndRefreshesRuntimeCatalog()
    {
        var store = Store();
        await store.InitializeAsync();
        var group = await store.CreateGroupAsync("G");
        await store.SaveSnippetAsync(Draft(group.Id, ""));
        var vm = new MainViewModel(store, _root);
        LibrarySnapshot? refreshed = null;
        vm.RefreshSnippetHotkeysAsync = snapshot => { refreshed = snapshot; return Task.CompletedTask; };
        await vm.InitializeAsync();
        Assert.False(vm.HasUnsavedChanges);
        vm.EditorHotkey = "Alt+Ctrl+F";
        Assert.True(vm.HasUnsavedChanges);
        await vm.SaveAsync();
        Assert.False(vm.HasUnsavedChanges);
        Assert.Equal("Ctrl+Alt+F", vm.EditorHotkey);
        Assert.Equal("Ctrl+Alt+F", Assert.Single(refreshed!.Snippets).Hotkey);
        vm.NewSnippet();
        Assert.Empty(vm.EditorHotkey);
        Assert.False(vm.HasUnsavedChanges);
    }

    [Theory]
    [InlineData("it")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("es")]
    public void NewEditorInstructionsAreLocalized(string code)
    {
        var texts = UiStrings.ForLanguage(code);
        Assert.NotEmpty(texts.SnippetHotkeyLabel);
        Assert.NotEqual("SnippetHotkeyLabel", texts.SnippetHotkeyLabel);
        Assert.Contains("F1", texts.SnippetHotkeyHint);
    }
}
