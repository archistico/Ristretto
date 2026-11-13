using TextExpander.Application.Abstractions;
using TextExpander.Core;
using TextExpander.Core.Matching;

namespace TextExpander.Application.Features.Library;

/// <summary>Vertical slice Library: non conosce SQLite o Avalonia.</summary>
public sealed class LibraryService(ILibraryStore store)
{
    public Task InitializeAsync(CancellationToken ct = default) => store.InitializeAsync(ct);
    public Task<LibrarySnapshot> ReadAsync(CancellationToken ct = default) => store.ReadAsync(ct);
    public Task<SnippetGroup> CreateGroupAsync(string name, CancellationToken ct = default)
    {
        LibraryRules.ValidateGroupName(name);
        return store.CreateGroupAsync(name.Trim(), ct);
    }
    public Task<SnippetGroup> UpdateGroupAsync(SnippetGroup previous, string newName, bool enabled,
        CancellationToken ct = default)
    {
        LibraryRules.ValidateGroupName(newName);
        return store.UpdateGroupAsync(previous, newName.Trim(), enabled, ct);
    }
    public Task DeleteGroupAsync(SnippetGroup previous, CancellationToken ct = default)
        => store.DeleteGroupAsync(previous, ct);
    public Task<Snippet> SaveAsync(SnippetDraft draft, CancellationToken ct = default)
    {
        LibraryRules.Validate(draft);
        return store.SaveSnippetAsync(draft with { Title = draft.Title.Trim(), Trigger = LibraryRules.CanonicalTrigger(draft.Trigger) }, ct);
    }
    public Task DeleteAsync(Snippet snippet, CancellationToken ct = default) => store.DeleteSnippetAsync(snippet.Id, snippet.Revision, ct);
    public TriggerMatch Preview(string committedText, LibrarySnapshot snapshot) => TriggerMatcher.MatchSuffix(committedText, snapshot);
}
