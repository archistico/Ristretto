using TextExpander.Core;
using TextExpander.Application.Features.Portability;

namespace TextExpander.Application.Abstractions;

public interface ILibraryStore
{
    Task InitializeAsync(CancellationToken ct = default);
    Task<LibrarySnapshot> ReadAsync(CancellationToken ct = default);
    Task<SnippetGroup> CreateGroupAsync(string name, CancellationToken ct = default);
    Task<SnippetGroup> UpdateGroupAsync(SnippetGroup previous, string newName, bool enabled, CancellationToken ct = default);
    Task DeleteGroupAsync(SnippetGroup previous, CancellationToken ct = default);
    Task<ImportSummary> ImportPortableAsync(PortableLibraryDocument document, CancellationToken ct = default,
        long? expectedLibraryRevision = null);
    Task<Snippet> SaveSnippetAsync(SnippetDraft draft, CancellationToken ct = default);
    Task DeleteSnippetAsync(Guid id, long expectedRevision, CancellationToken ct = default);
    Task<string?> ReadSettingAsync(string key, CancellationToken ct = default);
    Task WriteSettingAsync(string key, string value, CancellationToken ct = default);
    Task<string> CreateBackupAsync(string destinationPath, CancellationToken ct = default);
    Task RestoreBackupAsync(string sourcePath, CancellationToken ct = default);
}

public sealed class ConcurrencyException(string message) : Exception(message);
public sealed class DataIntegrityException(string message) : Exception(message);
