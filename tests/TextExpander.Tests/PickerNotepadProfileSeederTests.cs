using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ristretto.WindowsOutputLab;
using TextExpander.Infrastructure.Storage;
using Xunit;

namespace TextExpander.Tests;

/// <summary>
/// Filesystem + production SQLite integration tests. The isolated fixture is
/// created under the test's temporary folder, not the user's profile.
/// </summary>
public sealed class PickerNotepadProfileSeederTests : IDisposable
{
    private const string Token = "0123456789abcdef0123456789abcdef";
    private const string Content = "Caffè cafe\u0301 👩🏽‍💻 العربية 日本語\nseconda riga";
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ristretto-notepad-seed-tests", Guid.NewGuid().ToString("N"));
    private readonly string _session;
    private readonly string _document;
    private readonly string _fixture;

    public PickerNotepadProfileSeederTests()
    {
        _session = Path.Combine(_root, ".local-cache", "notepad-qualification", Token);
        _document = Path.Combine(_session, "notepad-synthetic.txt");
        _fixture = Path.Combine(_root, "data", "PICKER_INSERT_SYNTHETIC_FIXTURE.txt");
        Directory.CreateDirectory(_session);
        Directory.CreateDirectory(Path.GetDirectoryName(_fixture)!);
        File.WriteAllText(Path.Combine(_root, "Ristretto.sln"), "TEST");
        File.WriteAllText(Path.Combine(_root, "VERSION.txt"), "Ristretto test");
        File.WriteAllText(_fixture, Content, new UTF8Encoding(false));
        var baseline = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes("RISTRETTO NOTEPAD TEST\n")).ToArray();
        File.WriteAllBytes(_document, baseline);
        File.WriteAllText(Path.Combine(_session, "session.json"), JsonSerializer.Serialize(new
        {
            schema = "ristretto/notepad-qualification-session/1", session = Token,
            version = "Ristretto test", baseline_sha256 = Hex(baseline),
            fixture_sha256 = Hex(Encoding.UTF8.GetBytes(Content)), document_name = "notepad-synthetic.txt"
        }));
    }

    private static string Hex(byte[] value) => Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();

    [Fact]
    public async Task PreparesExactlyOneRealSqliteSnippetWithOriginalUnicode()
    {
        var profile = await PickerNotepadProfileSeeder.SeedAsync(_root, Token);
        Assert.Equal(Path.Combine(_session, "profile"), profile);
        Assert.True(File.Exists(Path.Combine(profile, "library.db")));
        var store = new SqliteLibraryStore(Path.Combine(profile, "library.db"));
        var snapshot = await store.ReadAsync();
        var item = Assert.Single(snapshot.Snippets);
        Assert.Equal(PickerNotepadProfileSeeder.Trigger, item.Trigger);
        Assert.Equal(Content, item.Content);
        Assert.True(item.IsEnabled);
        Assert.Single(snapshot.Groups);
    }

    [Fact]
    public async Task NeverOverwritesExistingProfileEvenOnSecondRun()
    {
        await PickerNotepadProfileSeeder.SeedAsync(_root, Token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => PickerNotepadProfileSeeder.SeedAsync(_root, Token));
        var store = new SqliteLibraryStore(Path.Combine(_session, "profile", "library.db"));
        Assert.Single((await store.ReadAsync()).Snippets);
    }

    [Fact]
    public async Task RejectsInvalidTokenWithoutCreatingProfile()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => PickerNotepadProfileSeeder.SeedAsync(_root, "../personal"));
        Assert.False(Directory.Exists(Path.Combine(_session, "profile")));
    }

    [Fact]
    public async Task RejectsChangedFixtureBeforeCreatingProfile()
    {
        File.AppendAllText(_fixture, "!");
        await Assert.ThrowsAsync<InvalidOperationException>(() => PickerNotepadProfileSeeder.SeedAsync(_root, Token));
        Assert.False(Directory.Exists(Path.Combine(_session, "profile")));
    }

    [Fact]
    public async Task RejectsModifiedNotepadBaselineBeforeCreatingProfile()
    {
        File.AppendAllText(_document, "EDITED");
        await Assert.ThrowsAsync<InvalidOperationException>(() => PickerNotepadProfileSeeder.SeedAsync(_root, Token));
        Assert.False(Directory.Exists(Path.Combine(_session, "profile")));
    }

    [Fact]
    public async Task RejectsUnknownSession()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            PickerNotepadProfileSeeder.SeedAsync(_root, "ffffffffffffffffffffffffffffffff"));
    }

    public void Dispose()
    {
        var path = Path.GetFullPath(_root);
        var requiredParent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "ristretto-notepad-seed-tests"))
            + Path.DirectorySeparatorChar;
        if (!path.StartsWith(requiredParent, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Unsafe test cleanup path.");
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
    }
}
