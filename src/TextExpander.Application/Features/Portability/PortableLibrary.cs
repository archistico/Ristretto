using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using TextExpander.Application.Abstractions;
using TextExpander.Core;
using TextExpander.Application.Features.Hotkeys;

namespace TextExpander.Application.Features.Portability;

// Interoperabilità fra installazioni. Non è il formato di backup: il backup è SQLite.
public sealed record PortableGroup(Guid Id, string Name, bool IsEnabled);
public sealed record PortableSnippet(Guid Id, Guid GroupId, string Title, string Trigger,
    string Content, string Description, bool IsEnabled, string Hotkey = "");
public sealed record PortableLibraryDocument(string Format, int Version,
    IReadOnlyList<PortableGroup> Groups, IReadOnlyList<PortableSnippet> Snippets);
public sealed record ImportSummary(int GroupsImported, int SnippetsImported);

public static class PortableLibraryFormat
{
    public const string Identifier = "text-expander-studio/snippets";
    public const int Version = 1;
    public const long MaxImportBytes = 32L * 1024 * 1024;
    public const int MaxGroups = 100_000;
    public const int MaxSnippets = 100_000;

    public static PortableLibraryDocument FromSnapshot(LibrarySnapshot snapshot) => new(
        Identifier, Version,
        snapshot.Groups.Select(g => new PortableGroup(g.Id, g.Name, g.IsEnabled)).ToArray(),
        snapshot.Snippets.Select(s => new PortableSnippet(s.Id, s.GroupId, s.Title, s.Trigger,
            s.Content, s.Description, s.IsEnabled, s.Hotkey)).ToArray());

    public static void Validate(PortableLibraryDocument? doc)
    {
        if (doc is null || doc.Format != Identifier || doc.Version != Version ||
            doc.Groups is null || doc.Snippets is null)
            throw new DataIntegrityException("Formato JSON non supportato o incompleto.");
        if (doc.Groups.Count > MaxGroups || doc.Snippets.Count > MaxSnippets)
            throw new DataIntegrityException("Archivio JSON oltre il limite di elementi.");
        var groupIds = new HashSet<Guid>();
        foreach (var group in doc.Groups)
        {
            if (group is null || group.Id == Guid.Empty || !groupIds.Add(group.Id))
                throw new DataIntegrityException("ID gruppo duplicato o non valido.");
            LibraryRules.ValidateGroupName(group.Name);
        }
        var snippetIds = new HashSet<Guid>();
        var hotkeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var snippet in doc.Snippets)
        {
            if (snippet is null || snippet.Id == Guid.Empty || !snippetIds.Add(snippet.Id) ||
                !groupIds.Contains(snippet.GroupId))
                throw new DataIntegrityException("ID snippet duplicato o riferimento a un gruppo inesistente.");
            LibraryRules.Validate(new SnippetDraft(null, snippet.GroupId, snippet.Title,
                snippet.Trigger, snippet.Content, snippet.Description, snippet.IsEnabled, null));
            var hotkey = SnippetHotkeyRules.Normalize(snippet.Hotkey);
            if (hotkey.Length > 0 && !hotkeys.Add(hotkey)) throw new SnippetHotkeyConflictException();
        }
    }
}

public sealed class PortableLibraryService(ILibraryStore store)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        MaxDepth = 32
    };

    public async Task<string> ExportAsync(string targetPath, CancellationToken ct = default)
    {
        var doc = PortableLibraryFormat.FromSnapshot(await store.ReadAsync(ct));
        PortableLibraryFormat.Validate(doc);
        var destination = Path.GetFullPath(targetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        if (File.Exists(destination)) throw new IOException("Il file di esportazione esiste già.");
        var temp = destination + "." + Guid.NewGuid().ToString("N") + ".partial";
        try
        {
            var json = JsonSerializer.Serialize(doc, JsonOptions);
            await File.WriteAllTextAsync(temp, json, new UTF8Encoding(false), ct);
            // Il file finale appare solo dopo il completamento della scrittura.
            File.Move(temp, destination, overwrite: false);
            return destination;
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }

    public async Task<ImportSummary> ImportAsync(string sourcePath, CancellationToken ct = default)
    {
        var path = Path.GetFullPath(sourcePath);
        var length = new FileInfo(path).Length;
        if (length > PortableLibraryFormat.MaxImportBytes)
            throw new DataIntegrityException("File JSON troppo grande.");
        var json = await File.ReadAllTextAsync(path, ct);
        var doc = JsonSerializer.Deserialize<PortableLibraryDocument>(json, JsonOptions);
        PortableLibraryFormat.Validate(doc);
        // Importazione additiva: genera nuovi ID, non modifica i dati esistenti.
        return await store.ImportPortableAsync(doc!, ct);
    }
}
