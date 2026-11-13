using System.Text;
using System.Text.Json;
using TextExpander.Application.Abstractions;
using TextExpander.Core;
using YamlDotNet.RepresentationModel;

namespace TextExpander.Application.Features.Portability;

public enum ImportSource { StudioJson, BeeftextV10, EspansoYaml }
public enum ImportIssueKind { Notice, UnsupportedOptions, DynamicContent, SkippedItem, TriggerCollision, IgnoredMetadata }

[Flags]
public enum ImportItemRisk { None = 0, TriggerCollision = 1, DynamicContent = 2, UnsupportedOptions = 4 }

/// <summary>Una riga di revisione: la selezione iniziale esclude gli elementi da verificare.</summary>
public sealed record ImportCandidate(Guid Id, string GroupName, string Title, string Trigger,
    bool WillBeEnabled, ImportItemRisk Risk)
{
    public bool SuggestedForImport => Risk == ImportItemRisk.None;
}

/// <summary>Warning e decisioni esplicite: non contiene il testo dello snippet nei messaggi diagnostici.</summary>
public sealed record ImportIssue(ImportIssueKind Kind, string Reference);

/// <summary>Risultato immutabile di una lettura senza effetti sul database; il commit utilizza questo snapshot.</summary>
public sealed record ExternalImportPreview(
    ImportSource Source, PortableLibraryDocument Document, int SourceItemCount,
    int SkippedItems, IReadOnlyList<ImportIssue> Issues)
{
    public bool CanImport => Document.Snippets.Count > 0;
    /// <summary>Revisione della libreria letta durante l'anteprima; controllata atomicamente al commit.</summary>
    public long LibraryRevision { get; init; }
    public IReadOnlyList<ImportCandidate> Candidates { get; init; } = [];
    internal IReadOnlyDictionary<Guid, ImportItemRisk> SourceRisks { get; init; } =
        new Dictionary<Guid, ImportItemRisk>();
}

/// <summary>
/// Importatori NON esecutivi. Non segue include, non lancia shell, non espande macro e non legge file referenziati.
/// Ogni snippet di origine terza viene importato disattivato: le opzioni di matching non sono ancora modellate.
/// </summary>
public sealed class ExternalLibraryImportService(ILibraryStore store)
{
    private const long MaxExternalBytes = 8L * 1024 * 1024;
    private const int MaxItems = 10_000;
    private const int MaxWarnings = 2_000;

    public async Task<ExternalImportPreview> PreviewAsync(string sourcePath, CancellationToken ct = default)
    {
        var source = Path.GetFullPath(sourcePath);
        var file = new FileInfo(source);
        if (!file.Exists || file.Length == 0 || file.Length > MaxExternalBytes)
            throw new DataIntegrityException("File mancante, vuoto o oltre il limite di 8 MiB per l'importazione esterna.");
        var bytes = await File.ReadAllBytesAsync(source, ct).ConfigureAwait(false);
        if (bytes.Length == 0 || bytes.Length > MaxExternalBytes)
            throw new DataIntegrityException("File modificato durante la lettura o oltre il limite previsto.");
        // UTF-8 rigoroso: sequenze malformate non devono essere sostituite silenziosamente.
        var offset = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF
            ? 3 : 0;
        string contents;
        try { contents = new UTF8Encoding(false, true).GetString(bytes, offset, bytes.Length - offset); }
        catch (DecoderFallbackException)
        { throw new DataIntegrityException("Il file non è UTF-8 valido. Nessuna conversione silenziosa."); }
        var ext = Path.GetExtension(source).ToLowerInvariant();
        ExternalImportPreview result;
        if (ext is ".yaml" or ".yml")
        {
            result = ReadEspanso(contents);
        }
        else if (ext == ".json")
        {
            using var json = JsonDocument.Parse(contents, new JsonDocumentOptions { MaxDepth = 32 });
            if (json.RootElement.ValueKind != JsonValueKind.Object)
                throw new DataIntegrityException("La radice JSON deve essere un oggetto.");
            if (json.RootElement.TryGetProperty("fileFormatVersion", out _))
                result = ReadBeeftext(json.RootElement);
            else if (json.RootElement.TryGetProperty("format", out _))
            {
                var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web) { MaxDepth = 32 };
                var portable = JsonSerializer.Deserialize<PortableLibraryDocument>(contents, opts);
                PortableLibraryFormat.Validate(portable);
                result = new(ImportSource.StudioJson, portable!, portable!.Snippets.Count, 0, []);
            }
            else throw new DataIntegrityException("Formato JSON non riconosciuto. Nessuna modifica apportata.");
        }
        else throw new DataIntegrityException("Formato non riconosciuto: scegliere .json, .yml o .yaml.");

        PortableLibraryFormat.Validate(result.Document);
        var snapshot = await store.ReadAsync(ct).ConfigureAwait(false);
        var existing = snapshot.Snippets.Select(s => LibraryRules.CanonicalTrigger(s.Trigger))
            .ToHashSet(StringComparer.Ordinal);
        var duplicatedInFile = result.Document.Snippets
            .GroupBy(s => LibraryRules.CanonicalTrigger(s.Trigger), StringComparer.Ordinal)
            .Where(g => g.Count() > 1).Select(g => g.Key)
            .ToHashSet(StringComparer.Ordinal);
        var groupsById = result.Document.Groups.ToDictionary(g => g.Id, g => g.Name);
        var warnings = result.Issues.ToList();
        var candidates = new List<ImportCandidate>(result.Document.Snippets.Count);
        foreach (var item in result.Document.Snippets)
        {
            var key = LibraryRules.CanonicalTrigger(item.Trigger);
            var risk = result.SourceRisks.TryGetValue(item.Id, out var sourceRisk)
                ? sourceRisk : ImportItemRisk.None;
            if (existing.Contains(key) || duplicatedInFile.Contains(key))
            {
                risk |= ImportItemRisk.TriggerCollision;
                if (warnings.Count < MaxWarnings)
                    warnings.Add(new(ImportIssueKind.TriggerCollision, item.Trigger));
            }
            candidates.Add(new(item.Id, groupsById[item.GroupId], item.Title,
                item.Trigger, item.IsEnabled, risk));
        }
        return result with
        {
            LibraryRevision = snapshot.Revision,
            Candidates = candidates,
            Issues = warnings
        };
    }

    // Compatibilità con i chiamanti v3.2: importa tutti gli elementi della preview disattivati
    // per le sorgenti esterne. La GUI v3.3 usa sempre l'overload con selezione esplicita.
    public Task<ImportSummary> ImportAsync(ExternalImportPreview preview, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(preview);
        return ImportAsync(preview, preview.Candidates.Select(x => x.Id).ToArray(), ct);
    }

    public Task<ImportSummary> ImportAsync(ExternalImportPreview preview,
        IReadOnlyCollection<Guid> selectedSnippetIds, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(preview);
        ArgumentNullException.ThrowIfNull(selectedSnippetIds);
        PortableLibraryFormat.Validate(preview.Document);
        if (preview.Source != ImportSource.StudioJson && preview.Document.Snippets.Any(x => x.IsEnabled))
            throw new DataIntegrityException("Un'importazione esterna deve rimanere disattivata fino alla revisione.");
        var selected = selectedSnippetIds.ToHashSet();
        var available = preview.Candidates.Select(x => x.Id).ToHashSet();
        if (selected.Count == 0 || selected.Count != selectedSnippetIds.Count ||
            preview.Candidates.Count != preview.Document.Snippets.Count ||
            available.Count != preview.Candidates.Count || !selected.IsSubsetOf(available))
            throw new DataIntegrityException("Selezione di importazione vuota, duplicata o non valida.");
        var chosen = preview.Document.Snippets.Where(x => selected.Contains(x.Id)).ToArray();
        var groups = chosen.Select(x => x.GroupId).ToHashSet();
        // I gruppi non selezionati non devono essere creati come contenitori vuoti.
        var doc = preview.Document with
        {
            Groups = preview.Document.Groups.Where(x => groups.Contains(x.Id)).ToArray(),
            Snippets = chosen
        };
        PortableLibraryFormat.Validate(doc);
        // La revisione viene confrontata all'interno della stessa transazione SQLite,
        // evitando che una preview vecchia venga confermata contro una libreria modificata.
        return store.ImportPortableAsync(doc, ct, preview.LibraryRevision);
    }

    private static ExternalImportPreview ReadBeeftext(JsonElement root)
    {
        if (!root.TryGetProperty("fileFormatVersion", out var version) ||
            version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var formatVersion) ||
            formatVersion != 10)
            throw new DataIntegrityException("Supportato soltanto Beeftext JSON v10; file non convertito.");
        if (!root.TryGetProperty("combos", out var combos) || combos.ValueKind != JsonValueKind.Array)
            throw new DataIntegrityException("Il formato Beeftext non contiene la lista combos.");
        if (combos.GetArrayLength() > MaxItems)
            throw new DataIntegrityException("Troppi snippet nel file Beeftext.");

        var groups = new List<PortableGroup>();
        var items = new List<PortableSnippet>();
        var issues = new List<ImportIssue>();
        var sourceRisks = new Dictionary<Guid, ImportItemRisk>();
        var sourceGroupIds = new Dictionary<Guid, Guid>();
        if (root.TryGetProperty("groups", out var rawGroups))
        {
            if (rawGroups.ValueKind != JsonValueKind.Array || rawGroups.GetArrayLength() > MaxItems)
                throw new DataIntegrityException("Lista gruppi Beeftext non valida.");
            foreach (var group in rawGroups.EnumerateArray())
            {
                if (group.ValueKind != JsonValueKind.Object)
                    throw new DataIntegrityException("Gruppo Beeftext malformato.");
                var name = Property(group, "name");
                LibraryRules.ValidateGroupName(name);
                var key = Property(group, "uuid");
                if (!Guid.TryParse(key, out var sourceId) || sourceGroupIds.ContainsKey(sourceId))
                    throw new DataIntegrityException("UUID gruppo Beeftext mancante o duplicato.");
                var id = Guid.NewGuid();
                groups.Add(new(id, name!, Flag(group, "enabled", true)));
                sourceGroupIds.Add(sourceId, id);
                if (group.TryGetProperty("description", out var desc) && desc.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(desc.GetString()))
                    Add(issues, ImportIssueKind.IgnoredMetadata, name!);
            }
        }

        Guid fallbackId = Guid.Empty;
        for (int i = 0; i < combos.GetArrayLength(); i++)
        {
            var combo = combos[i];
            var reference = $"Beeftext #{i + 1}";
            if (combo.ValueKind != JsonValueKind.Object)
            {
                Add(issues, ImportIssueKind.SkippedItem, reference); continue;
            }
            var title = Property(combo, "name");
            var trigger = Property(combo, "keyword");
            var content = Property(combo, "snippet");
            var description = Property(combo, "description") ?? "";
            reference = $"#{i + 1} {Truncate(title ?? trigger ?? "", 48)}";
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrEmpty(trigger) || string.IsNullOrEmpty(content))
            {
                Add(issues, ImportIssueKind.SkippedItem, reference); continue;
            }
            Guid groupId;
            var externalGroup = Property(combo, "group");
            if (!Guid.TryParse(externalGroup, out var sourceGroupId) ||
                !sourceGroupIds.TryGetValue(sourceGroupId, out groupId))
            {
                if (fallbackId == Guid.Empty)
                {
                    fallbackId = Guid.NewGuid();
                    groups.Add(new(fallbackId, "Beeftext — senza gruppo", true));
                }
                groupId = fallbackId;
                if (externalGroup is not null) Add(issues, ImportIssueKind.IgnoredMetadata, reference);
            }
            var item = new PortableSnippet(Guid.NewGuid(), groupId, title!, trigger!, content!, description,
                false); // disattivazione di sicurezza, senza side effect
            if (!ValidItem(item)) { Add(issues, ImportIssueKind.SkippedItem, reference); continue; }
            items.Add(item);
            var risk = ImportItemRisk.None;
            // Le opzioni dell'originale non sono rappresentabili nell'attuale dominio.
            if (Integer(combo, "matchingMode", 0) != 1 || Integer(combo, "caseSensitivity", 0) != 1)
            {
                risk |= ImportItemRisk.UnsupportedOptions;
                Add(issues, ImportIssueKind.UnsupportedOptions, reference);
            }
            if (content!.Contains("#{", StringComparison.Ordinal) || Flag(combo, "useHtml", false))
            {
                risk |= ImportItemRisk.DynamicContent;
                Add(issues, ImportIssueKind.DynamicContent, reference);
            }
            sourceRisks.Add(item.Id, risk);
        }
        return new ExternalImportPreview(ImportSource.BeeftextV10, MakeDocument(groups, items),
            combos.GetArrayLength(), combos.GetArrayLength() - items.Count, issues)
        { SourceRisks = sourceRisks };
    }

    private static ExternalImportPreview ReadEspanso(string yaml)
    {
        var stream = new YamlStream();
        try { stream.Load(new StringReader(yaml)); }
        catch (Exception ex) when (ex is YamlDotNet.Core.YamlException or InvalidOperationException)
        { throw new DataIntegrityException("YAML Espanso non valido o non leggibile."); }
        if (stream.Documents.Count != 1 || stream.Documents[0].RootNode is not YamlMappingNode root)
            throw new DataIntegrityException("Espanso richiede un singolo documento YAML con matches.");
        ValidateYamlSize(root);
        if (Get(root, "matches") is not YamlSequenceNode matches)
            throw new DataIntegrityException("File Espanso senza lista matches. Gli include non vengono seguiti.");
        if (matches.Children.Count > MaxItems)
            throw new DataIntegrityException("Troppi match nel file Espanso.");
        var group = new PortableGroup(Guid.NewGuid(), "Espanso — importati", true);
        var items = new List<PortableSnippet>();
        var issues = new List<ImportIssue>();
        var sourceRisks = new Dictionary<Guid, ImportItemRisk>();
        var skippedRules = 0;
        foreach (var unsupported in new[] { "imports", "global_vars", "filter_class", "filter_exec", "filter_title" })
            if (Get(root, unsupported) is not null)
                Add(issues, ImportIssueKind.IgnoredMetadata, $"Espanso: {unsupported}");
        for (int i = 0; i < matches.Children.Count; i++)
        {
            var entry = matches.Children[i];
            var reference = $"Espanso #{i + 1}";
            if (entry is not YamlMappingNode match)
            { skippedRules++; Add(issues, ImportIssueKind.SkippedItem, reference); continue; }
            var trigger = Scalar(Get(match, "trigger"));
            var triggerList = Get(match, "triggers") as YamlSequenceNode;
            var triggers = new List<string>();
            if (trigger is not null) triggers.Add(trigger);
            if (triggerList is not null)
            {
                foreach (var t in triggerList.Children)
                {
                    var value = Scalar(t);
                    if (value is not null) triggers.Add(value);
                    else Add(issues, ImportIssueKind.SkippedItem, reference);
                }
            }
            if (triggers.Count == 0)
            { skippedRules++; Add(issues, ImportIssueKind.SkippedItem, reference); continue; }
            var content = Scalar(Get(match, "replace"));
            var form = Scalar(Get(match, "form"));
            content ??= form;
            if (string.IsNullOrEmpty(content))
            { skippedRules++; Add(issues, ImportIssueKind.SkippedItem, reference); continue; }
            bool dynamic = form is not null || Get(match, "vars") is not null ||
                content.Contains("{{", StringComparison.Ordinal) ||
                content.Contains("[[", StringComparison.Ordinal);
            var risk = ImportItemRisk.None;
            if (dynamic)
            {
                risk |= ImportItemRisk.DynamicContent;
                Add(issues, ImportIssueKind.DynamicContent, reference);
            }
            if (Get(match, "word") is not null || Get(match, "case_sensitive") is not null ||
                Get(match, "propagate_case") is not null || Get(match, "force_clipboard") is not null ||
                Get(match, "regex") is not null)
            {
                risk |= ImportItemRisk.UnsupportedOptions;
                Add(issues, ImportIssueKind.UnsupportedOptions, reference);
            }
            var importedFromRule = 0;
            foreach (var t in triggers)
            {
                var title = Truncate($"Espanso: {t}", 160);
                var item = new PortableSnippet(Guid.NewGuid(), group.Id, title, t, content!, "", false);
                if (ValidItem(item))
                {
                    items.Add(item);
                    sourceRisks.Add(item.Id, risk);
                    importedFromRule++;
                }
                else Add(issues, ImportIssueKind.SkippedItem, reference);
            }
            if (importedFromRule == 0) skippedRules++;
        }
        return new ExternalImportPreview(ImportSource.EspansoYaml, MakeDocument([group], items),
            matches.Children.Count, skippedRules, issues)
        { SourceRisks = sourceRisks };
    }

    private static PortableLibraryDocument MakeDocument(
        IReadOnlyList<PortableGroup> groups, IReadOnlyList<PortableSnippet> snippets) =>
        new(PortableLibraryFormat.Identifier, PortableLibraryFormat.Version, groups, snippets);

    private static bool ValidItem(PortableSnippet snippet)
    {
        try
        {
            LibraryRules.Validate(new SnippetDraft(null, snippet.GroupId, snippet.Title,
                snippet.Trigger, snippet.Content, snippet.Description, false, null));
            return true;
        }
        catch (ArgumentException) { return false; }
    }

    private static string? Property(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var value)) return null;
        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
    private static int Integer(JsonElement obj, string name, int defaultValue) =>
        obj.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number &&
        value.TryGetInt32(out var number) ? number : defaultValue;
    private static bool Flag(JsonElement obj, string name, bool defaultValue) =>
        obj.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean() : defaultValue;
    private static string Truncate(string value, int limit) => value.Length <= limit ? value : value[..limit];
    private static void Add(List<ImportIssue> target, ImportIssueKind kind, string reference)
    { if (target.Count < MaxWarnings) target.Add(new(kind, reference)); }

    private static YamlNode? Get(YamlMappingNode map, string name)
    {
        foreach (var (key, value) in map.Children)
            if (key is YamlScalarNode scalar && scalar.Value == name) return value;
        return null;
    }
    private static string? Scalar(YamlNode? node) => node is YamlScalarNode value ? value.Value : null;

    // Il documento YAML è dato non attendibile. Evita strutture gigantesche, cicli e nesting eccessivo.
    private static void ValidateYamlSize(YamlNode root)
    {
        var visiting = new HashSet<YamlNode>(ReferenceEqualityComparer.Instance);
        int count = 0;
        void Walk(YamlNode node, int depth)
        {
            if (++count > 50_000 || depth > 32 || !visiting.Add(node))
                throw new DataIntegrityException("YAML troppo complesso o contenente riferimenti ciclici.");
            if (node is YamlMappingNode mapping)
                foreach (var (key, value) in mapping.Children) { Walk(key, depth + 1); Walk(value, depth + 1); }
            else if (node is YamlSequenceNode seq)
                foreach (var child in seq.Children) Walk(child, depth + 1);
            visiting.Remove(node);
        }
        Walk(root, 0);
    }
}
