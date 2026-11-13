namespace TextExpander.Desktop.Startup;

/// <summary>The one resolved data directory shared by storage, backups and instance ownership.</summary>
public sealed class ApplicationProfile
{
    public string DataDirectory { get; }
    public string DatabasePath => Path.Combine(DataDirectory, "library.db");
    public string BackupDirectory => Path.Combine(DataDirectory, "Backups");

    private ApplicationProfile(string directory) => DataDirectory = directory;

    public static ApplicationProfile Resolve(string? requestedDirectory, Func<string>? localDataDirectory = null,
        string? workingDirectory = null)
    {
        var directory = requestedDirectory;
        if (directory is null)
        {
            var local = (localDataDirectory ?? (() => Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData)))();
            if (string.IsNullOrWhiteSpace(local) || !Path.IsPathFullyQualified(local))
                throw new ArgumentException("Directory locale dell'utente non disponibile. Usare --data-dir.");
            directory = Path.Combine(local, "TextExpanderStudio");
        }
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("La directory dati non può essere vuota.");
        // Resolve exactly once: later changes to the process working directory cannot redirect storage.
        var fullPath = Path.GetFullPath(directory, workingDirectory ?? Environment.CurrentDirectory);
        return new(Path.TrimEndingDirectorySeparator(fullPath));
    }

    public void EnsureWritable()
    {
        Directory.CreateDirectory(DataDirectory);
        // A new, disposable probe never truncates or opens an existing library, setting or backup.
        var probe = Path.Combine(DataDirectory, ".ristretto-write-check-" + Guid.NewGuid().ToString("N"));
        using var stream = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            bufferSize: 1, FileOptions.DeleteOnClose);
        stream.WriteByte(0);
        stream.Flush(flushToDisk: true);
    }
}
