using TextExpander.Core;

namespace TextExpander.Application.Features.Picker;

/// <summary>Ricerca pura per il picker locale. Non invia input e non accede alla clipboard.</summary>
public static class PickerSearch
{
    public sealed record Result(Guid SnippetId, string Title, string Trigger,
        string GroupName, string Content, string Description);

    public static IReadOnlyList<Result> Find(LibrarySnapshot library, string? query, int limit = 50)
    {
        ArgumentNullException.ThrowIfNull(library);
        if (limit is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(limit));
        return new PickerSearchIndex(library).Find(query, limit);
    }
}
