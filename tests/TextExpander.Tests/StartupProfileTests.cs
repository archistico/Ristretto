using System.Text.Json;
using TextExpander.Application.Abstractions;
using TextExpander.Core;
using TextExpander.Desktop.SingleInstance;
using TextExpander.Desktop.Startup;
using TextExpander.Desktop.ViewModels;
using TextExpander.Infrastructure.Storage;
using Xunit;

namespace TextExpander.Tests;

// No Avalonia app/backend, native input, global hotkey or personal directory is used.
public sealed class StartupProfileTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ristretto-profile-tests", Guid.NewGuid().ToString("N"));
    private string DirectoryFor(string name) => Path.Combine(_root, name);
    private string LocalData() => DirectoryFor("synthetic-local");

    public void Dispose()
    {
        var root = Path.GetFullPath(_root);
        var parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "ristretto-profile-tests")) + Path.DirectorySeparatorChar;
        if (!root.StartsWith(parent, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException();
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void DefaultKeepsLegacyLocationAndResolutionDoesNotCreateIt()
    {
        var profile = ApplicationProfile.Resolve(null, LocalData);
        Assert.Equal(Path.Combine(LocalData(), "TextExpanderStudio"), profile.DataDirectory);
        Assert.Equal(Path.Combine(profile.DataDirectory, "library.db"), profile.DatabasePath);
        Assert.Equal(Path.Combine(profile.DataDirectory, "Backups"), profile.BackupDirectory);
        Assert.False(Directory.Exists(_root));
    }

    [Fact]
    public void ExplicitRelativeUnicodeDirectoryUsesGivenWorkingDirectoryAndNeverResolvesDefault()
    {
        var profile = ApplicationProfile.Resolve("prove con spazi/日本語 🌍/../Caffè", () => throw new Exception(), _root);
        Assert.Equal(Path.Combine(_root, "prove con spazi", "Caffè"), profile.DataDirectory);
        profile.EnsureWritable();
        Assert.True(Directory.Exists(profile.DataDirectory));
        Assert.Empty(Directory.EnumerateFileSystemEntries(profile.DataDirectory));
        Assert.False(Directory.Exists(LocalData()));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("relative-user-folder")]
    public void MissingOrRelativeLocalDataNeverFallsBackToWorkingDirectory(string local)
    {
        Assert.Throws<ArgumentException>(() => ApplicationProfile.Resolve(null, () => local, _root));
        Assert.False(Directory.Exists(_root));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid\0path")]
    public void InvalidExplicitDirectoryDoesNotFallBack(string directory)
    {
        Assert.Throws<ArgumentException>(() => ApplicationProfile.Resolve(directory, LocalData, _root));
        Assert.False(Directory.Exists(_root));
    }

    [Fact]
    public void ExistingProfileIsNotOpenedOrModifiedByPreflight()
    {
        var profile = ApplicationProfile.Resolve(DirectoryFor("existing"));
        Directory.CreateDirectory(profile.DataDirectory);
        File.WriteAllText(profile.DatabasePath, "sentinel, not SQLite");
        using var locked = new FileStream(profile.DatabasePath, FileMode.Open, FileAccess.Read, FileShare.None);
        profile.EnsureWritable();
        Assert.Equal(new[] { profile.DatabasePath }, Directory.GetFiles(profile.DataDirectory));
        Assert.Equal(20, locked.Length);
    }

    [Theory]
    [InlineData("--unknown")]
    [InlineData("--data-dir")]
    [InlineData("--data-dir=")]
    [InlineData("--data-dir= ")]
    public void InvalidOptionsNeverLaunchDesktopOrResolveDefault(string argument)
    {
        var errors = new StringWriter();
        var result = StartupCommand.Run([argument], new StringWriter(), errors,
            (_, _) => throw new Exception("Desktop must not start"), () => throw new Exception("Default must not resolve"));
        Assert.Equal(2, result);
        Assert.NotEmpty(errors.ToString());
        Assert.False(Directory.Exists(_root));
    }

    [Fact]
    public void DuplicateDirectoryMissingValueAndAmbiguousActionsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => StartupOptions.Parse(["--data-dir=a", "--data-dir", "b"]));
        Assert.Throws<ArgumentException>(() => StartupOptions.Parse(["--data-dir", "--check-profile"]));
        Assert.Throws<ArgumentException>(() => StartupOptions.Parse(["--help", "--check-profile"]));
        Assert.Throws<ArgumentException>(() => StartupOptions.Parse(["--version", "--help"]));
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("--version")]
    public void InformationCommandsNeverTouchFilesOrLaunchDesktop(string argument)
    {
        var output = new StringWriter();
        var errors = new StringWriter();
        Assert.Equal(0, StartupCommand.Run([argument], output, errors,
            (_, _) => throw new Exception(), () => throw new Exception()));
        Assert.Contains("Ristretto", output.ToString());
        Assert.Empty(errors.ToString());
        Assert.False(Directory.Exists(_root));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CheckProfileReturnsResolvedJsonAndCreatesNoDatabaseOrLease(bool equalsForm)
    {
        var path = DirectoryFor("Dati di prova 日本語");
        string[] args = equalsForm ? ["--data-dir=" + path, "--check-profile"] : ["--check-profile", "--data-dir", path];
        var output = new StringWriter();
        Assert.Equal(0, StartupCommand.Run(args, output, new StringWriter(), (_, _) => throw new Exception(), LocalData));
        using var json = JsonDocument.Parse(output.ToString());
        Assert.Equal("profile_writable", json.RootElement.GetProperty("status").GetString());
        Assert.Equal(path, json.RootElement.GetProperty("data_directory").GetString());
        Assert.False(json.RootElement.GetProperty("database_opened").GetBoolean());
        Assert.Empty(Directory.EnumerateFileSystemEntries(path));
        Assert.False(Directory.Exists(LocalData()));
    }

    [Fact]
    public void FileAsDirectoryIsAnActionableStartupErrorWithoutFallback()
    {
        Directory.CreateDirectory(_root);
        var path = DirectoryFor("occupied");
        File.WriteAllText(path, "untouched");
        var errors = new StringWriter();
        Assert.Equal(2, StartupCommand.Run(["--check-profile", "--data-dir", path], new StringWriter(), errors,
            (_, _) => throw new Exception(), LocalData));
        Assert.Contains("--data-dir", errors.ToString());
        Assert.Equal("untouched", File.ReadAllText(path));
        Assert.False(Directory.Exists(LocalData()));
    }

    [Fact]
    public void BusyProfileLeaseStopsStartupAndCanBeReacquiredAfterRelease()
    {
        var path = DirectoryFor("leased");
        Directory.CreateDirectory(path);
        using (var lease = new FileStream(Path.Combine(path, ".ristretto.profile.lock"), FileMode.OpenOrCreate,
                   FileAccess.ReadWrite, FileShare.None))
        {
            Assert.Equal(2, StartupCommand.Run(["--data-dir", path], new StringWriter(), new StringWriter(),
                (_, _) => throw new Exception(), LocalData));
        }
        var called = false;
        Assert.Equal(0, StartupCommand.Run(["--data-dir", path], new StringWriter(), new StringWriter(), (profile, host) =>
        {
            called = true;
            Assert.True(host.IsPrimary);
            Assert.Equal(path, profile.DataDirectory);
        }, LocalData));
        Assert.True(called);
        Assert.False(Directory.Exists(LocalData()));
    }

    [Fact]
    public void EquivalentDirectoriesShareInstanceKeyButDistinctProfilesDoNot()
    {
        var path = DirectoryFor("profile");
        var key = SingleInstanceHost.InstanceKey(path);
        Assert.Equal(key, SingleInstanceHost.InstanceKey(Path.Combine(path, "child", "..") + Path.DirectorySeparatorChar));
        if (OperatingSystem.IsWindows())
            Assert.Equal(key, SingleInstanceHost.InstanceKey(path.ToUpperInvariant().Replace('\\', '/')));
        Assert.NotEqual(key, SingleInstanceHost.InstanceKey(DirectoryFor("other")));
    }

    [Fact]
    public void OnlyOneOwnerPerProfileAndIndependentProfileCanRunConcurrently()
    {
        var path = DirectoryFor("first");
        using (var first = new SingleInstanceHost(path))
        using (var second = new SingleInstanceHost(path))
        using (var independent = new SingleInstanceHost(DirectoryFor("second")))
        {
            Assert.True(first.IsPrimary);
            Assert.False(second.IsPrimary);
            Assert.True(independent.IsPrimary);
        }
        using var restarted = new SingleInstanceHost(path);
        Assert.True(restarted.IsPrimary);
    }

    [Fact]
    public void DesktopStartupFailureReleasesProfileOwnership()
    {
        var path = DirectoryFor("startup-failure");
        Assert.Throws<InvalidOperationException>(() => StartupCommand.Run(["--data-dir", path],
            new StringWriter(), new StringWriter(), (_, _) => throw new InvalidOperationException("Synthetic failure"), LocalData));
        using var restarted = new SingleInstanceHost(path);
        Assert.True(restarted.IsPrimary);
        Assert.False(Directory.Exists(LocalData()));
    }

    [Fact]
    public void CheckProfileDoesNotAcquireOrReleaseRunningProfileOwnership()
    {
        var path = DirectoryFor("running");
        using var primary = new SingleInstanceHost(path);
        Assert.Equal(0, StartupCommand.Run(["--data-dir", path, "--check-profile"],
            new StringWriter(), new StringWriter(), (_, _) => throw new Exception(), LocalData));
        using var secondary = new SingleInstanceHost(path);
        Assert.False(secondary.IsPrimary);
        Assert.True(primary.IsPrimary);
    }

    [Fact]
    public async Task ActivationPipeAddressesOnlyTheRequestedProfile()
    {
        var first = DirectoryFor("pipe-one");
        var second = DirectoryFor("pipe-two");
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var activated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var wrongActivations = 0;
        using var release = new ManualResetEventSlim();
        // Keep mutex ownership and disposal on the same worker thread while the async test uses the pipe.
        var owners = Task.Run(() =>
        {
            using var a = new SingleInstanceHost(first);
            using var b = new SingleInstanceHost(second);
            a.StartListening(() => activated.TrySetResult());
            b.StartListening(() => Interlocked.Increment(ref wrongActivations));
            ready.TrySetResult();
            if (!release.Wait(TimeSpan.FromSeconds(15))) throw new TimeoutException();
        });
        try
        {
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));
            // Exercise the same secondary-startup branch used by Program, including argument resolution.
            Assert.Equal(0, StartupCommand.Run(["--data-dir", first], new StringWriter(), new StringWriter(),
                (_, _) => throw new Exception("A second desktop must not start"), LocalData));
            await activated.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally { release.Set(); await owners.WaitAsync(TimeSpan.FromSeconds(5)); }
        Assert.Equal(0, Volatile.Read(ref wrongActivations));
    }

    [Fact]
    public async Task SeparateProfilesHaveIndependentSnippetsSettingsAndViewModelBackups()
    {
        var a = ApplicationProfile.Resolve(DirectoryFor("library-one"));
        var b = ApplicationProfile.Resolve(DirectoryFor("library-two"));
        var storeA = new SqliteLibraryStore(a.DatabasePath);
        var storeB = new SqliteLibraryStore(b.DatabasePath);
        await storeA.InitializeAsync();
        await storeB.InitializeAsync();
        var group = await storeA.CreateGroupAsync("Synthetic group");
        await storeA.SaveSnippetAsync(new SnippetDraft(null, group.Id, "Sample", ";test", "Caffè 🌍", "", true, null));
        await storeA.WriteSettingAsync("picker.hotkey", "None");
        await storeB.WriteSettingAsync("picker.hotkey", "Ctrl+Alt+K");
        Assert.Empty((await storeB.ReadAsync()).Snippets);
        Assert.Single((await storeA.ReadAsync()).Snippets);
        Assert.Equal("None", await storeA.ReadSettingAsync("picker.hotkey"));
        Assert.Equal("Ctrl+Alt+K", await storeB.ReadSettingAsync("picker.hotkey"));
        var vm = new MainViewModel(storeA, a.DataDirectory);
        await vm.CreateBackupAsync();
        var backup = Assert.Single(Directory.GetFiles(a.BackupDirectory, "*.db"));
        Assert.Single((await new SqliteLibraryStore(backup).ReadAsync()).Snippets);
        Assert.False(Directory.Exists(b.BackupDirectory));
        Assert.False(Directory.Exists(LocalData()));
    }

    [Theory]
    [InlineData("None")]
    [InlineData("Ctrl+Alt+K")]
    public async Task PickerHotkeySavedThroughViewModelSurvivesDatabaseReopen(string hotkey)
    {
        var profile = ApplicationProfile.Resolve(DirectoryFor("persisted-hotkey"));
        var store = new SqliteLibraryStore(profile.DatabasePath);
        await store.InitializeAsync();
        var vm = new MainViewModel(store, profile.DataDirectory);
        await vm.SavePickerHotkeyAsync(hotkey);
        var reopened = new MainViewModel(new SqliteLibraryStore(profile.DatabasePath), profile.DataDirectory);
        await reopened.InitializeAsync();
        Assert.Equal(hotkey, reopened.PickerHotkeyDraft);
    }

    [Fact]
    public async Task UnknownSettingsRemainRejected()
    {
        var profile = ApplicationProfile.Resolve(DirectoryFor("settings-allowlist"));
        var store = new SqliteLibraryStore(profile.DatabasePath);
        await store.InitializeAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => store.WriteSettingAsync("unknown", "synthetic"));
        Assert.Null(await store.ReadSettingAsync("unknown"));
    }
}
