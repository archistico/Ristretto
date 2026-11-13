using TextExpander.Application.Abstractions;

namespace TextExpander.Application.Features.Backups;

public sealed class BackupService(ILibraryStore store)
{
    public Task<string> CreateAsync(string destinationPath, CancellationToken ct = default)
        => store.CreateBackupAsync(destinationPath, ct);
    public Task RestoreAsync(string sourcePath, CancellationToken ct = default)
        => store.RestoreBackupAsync(sourcePath, ct);
}
