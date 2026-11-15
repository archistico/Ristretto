using TextExpander.Core;

namespace TextExpander.Application.Features.Hotkeys;

public sealed class SnippetHotkeyConflictException() : InvalidOperationException(
    "La combinazione è già assegnata a un altro snippet o al picker.");

public static class SnippetHotkeyRules
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        if (!PickerHotkey.TryParse(value, out var gesture))
            throw new ArgumentException("Hotkey non valida: usare Ctrl/Alt e una lettera, cifra, Space o F1–F11.");
        return gesture.IsDisabled ? "" : gesture.Canonical;
    }
}

// No content crosses the native notification boundary. Generation/revision reject queued stale events.
public sealed record SnippetHotkeyBinding(Guid SnippetId, long SnippetRevision, PickerHotkey Gesture, long Generation);

public sealed class SnippetHotkeyCatalog
{
    private readonly Dictionary<Guid, (Snippet Snippet, SnippetHotkeyBinding Binding)> _entries = [];
    public IReadOnlyList<SnippetHotkeyBinding> Bindings { get; }

    public SnippetHotkeyCatalog(LibrarySnapshot snapshot, string pickerHotkey, long generation)
    {
        var groups = snapshot.Groups.Where(g => g.IsEnabled).Select(g => g.Id).ToHashSet();
        var candidates = snapshot.Snippets.Where(s => s.IsEnabled && groups.Contains(s.GroupId))
            .Select(s => (Snippet: s, Valid: PickerHotkey.TryParse(s.Hotkey, out var key), Key: key))
            .Where(x => x.Valid && !x.Key.IsDisabled).ToArray();
        var reserved = PickerHotkey.TryParse(pickerHotkey, out var picker) && !picker.IsDisabled
            ? picker.Canonical : "";
        foreach (var group in candidates.GroupBy(x => x.Key.Canonical))
        {
            // Malformed/external catalogs must never select an arbitrary winner for duplicates.
            if (group.Count() != 1 || group.Key == reserved) continue;
            var item = group.Single();
            var binding = new SnippetHotkeyBinding(item.Snippet.Id, item.Snippet.Revision, item.Key, generation);
            _entries[item.Snippet.Id] = (item.Snippet, binding);
        }
        Bindings = _entries.Values.Select(e => e.Binding).ToArray();
    }

    public Snippet? Resolve(SnippetHotkeyBinding binding) =>
        _entries.TryGetValue(binding.SnippetId, out var entry) && entry.Binding == binding ? entry.Snippet : null;
}
