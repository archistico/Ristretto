using System.Globalization;
using System.Text;
using TextExpander.Core;

namespace TextExpander.Application.Features.Picker;

/// <summary>
/// Immutable search projection of a library snapshot. Normalize searchable fields
/// once per reload; retain original strings for display/copy. No I/O or clipboard.
/// </summary>
public sealed class PickerSearchIndex
{
    private const int ContentPrefixLimit = 4096;
    private sealed record Entry(PickerSearch.Result Result, string Title, string Trigger,
        string Description, string ContentPrefix)
    {
        public int Rank(string query)
        {
            if (Trigger.Equals(query, StringComparison.OrdinalIgnoreCase)) return 0;
            if (Trigger.StartsWith(query, StringComparison.OrdinalIgnoreCase)) return 1;
            if (Title.StartsWith(query, StringComparison.OrdinalIgnoreCase)) return 2;
            if (Title.Contains(query, StringComparison.OrdinalIgnoreCase)) return 3;
            if (Trigger.Contains(query, StringComparison.OrdinalIgnoreCase)) return 4;
            if (Description.Contains(query, StringComparison.OrdinalIgnoreCase)) return 5;
            return ContentPrefix.Contains(query, StringComparison.OrdinalIgnoreCase) ? 6 : -1;
        }
    }

    private readonly Entry[] _entries;
    public long LibraryRevision { get; }

    public PickerSearchIndex(LibrarySnapshot library, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(library);
        cancellationToken.ThrowIfCancellationRequested();
        LibraryRevision = library.Revision;
        var groups = library.Groups.Where(g => g.IsEnabled).ToDictionary(g => g.Id, g => g.Name);
        var entries = new List<Entry>();
        foreach (var snippet in library.Snippets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!snippet.IsEnabled || !groups.TryGetValue(snippet.GroupId, out var group)) continue;
            try
            {
                var result = new PickerSearch.Result(snippet.Id, snippet.Title, snippet.Trigger,
                    group, snippet.Content, snippet.Description);
                entries.Add(new(result, Normalize(snippet.Title), Normalize(snippet.Trigger),
                    Normalize(snippet.Description), Normalize(ContentPrefix(snippet.Content))));
            }
            catch (ArgumentException)
            {
                // A malformed indexed field must not break every other result.
                // Do not repair or log contents, or modify the stored library.
            }
        }
        _entries = entries.OrderBy(e => e.Result.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.Result.Trigger, StringComparer.Ordinal)
            .ThenBy(e => e.Result.SnippetId).ToArray();
        cancellationToken.ThrowIfCancellationRequested();
    }

    public IReadOnlyList<PickerSearch.Result> Find(string? query, int limit = 50)
    {
        if (limit is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(limit));
        string text;
        try { text = Normalize((query ?? string.Empty).Trim()); }
        catch (ArgumentException) { return []; } // e.g. a transient lone surrogate in the search box
        if (text.Length == 0) return _entries.Take(limit).Select(e => e.Result).ToArray();

        // Entries already have the stable tie-break order. Retain at most limit
        // results per rank rather than sorting/allocating every match per keypress.
        var buckets = new List<PickerSearch.Result>?[7];
        foreach (var entry in _entries)
        {
            var rank = entry.Rank(text);
            if (rank < 0) continue;
            var bucket = buckets[rank] ??= [];
            if (bucket.Count < limit) bucket.Add(entry.Result);
        }
        var results = new List<PickerSearch.Result>(limit);
        foreach (var bucket in buckets)
        {
            if (bucket is null) continue;
            foreach (var result in bucket)
            {
                results.Add(result);
                if (results.Count == limit) return results;
            }
        }
        return results;
    }

    private static string Normalize(string text) => text.Normalize(NormalizationForm.FormC);

    private static string ContentPrefix(string content)
    {
        if (content.Length <= ContentPrefixLimit) return content;
        var end = 0;
        while (end < ContentPrefixLimit)
        {
            var length = StringInfo.GetNextTextElementLength(content.AsSpan(end));
            if (length > ContentPrefixLimit - end) break;
            end += length;
        }
        return content[..end];
    }
}
