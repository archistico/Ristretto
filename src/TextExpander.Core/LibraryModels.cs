using System.Text;

namespace TextExpander.Core;

public sealed record SnippetGroup(Guid Id, string Name, bool IsEnabled);

public sealed record Snippet(
    Guid Id,
    Guid GroupId,
    string Title,
    string Trigger,
    string Content,
    string Description,
    bool IsEnabled,
    long Revision);

public sealed record LibrarySnapshot(
    IReadOnlyList<SnippetGroup> Groups,
    IReadOnlyList<Snippet> Snippets,
    long Revision);

public sealed record SnippetDraft(
    Guid? Id,
    Guid GroupId,
    string Title,
    string Trigger,
    string Content,
    string Description,
    bool IsEnabled,
    long? ExpectedRevision);

public static class LibraryRules
{
    public static string CanonicalTrigger(string value) => value.Normalize(NormalizationForm.FormC);

    public static void ValidateGroupName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 120)
            throw new ArgumentException("Il nome del gruppo deve contenere 1–120 caratteri.");
    }

    public static void Validate(SnippetDraft draft)
    {
        if (draft.GroupId == Guid.Empty) throw new ArgumentException("Gruppo obbligatorio.");
        if (string.IsNullOrWhiteSpace(draft.Title) || draft.Title.Length > 160)
            throw new ArgumentException("Il titolo deve contenere 1–160 caratteri.");
        if (string.IsNullOrEmpty(draft.Trigger) || draft.Trigger.Length > 128 ||
            draft.Trigger.Any(char.IsControl))
            throw new ArgumentException("Trigger obbligatorio: massimo 128 unità UTF-16, senza controlli.");
        if (string.IsNullOrEmpty(draft.Content) || draft.Content.Length > 1_000_000)
            throw new ArgumentException("Il testo deve contenere 1–1.000.000 caratteri UTF-16.");
        if (draft.Description is null || draft.Description.Length > 8_000)
            throw new ArgumentException("Descrizione troppo lunga.");
    }
}
