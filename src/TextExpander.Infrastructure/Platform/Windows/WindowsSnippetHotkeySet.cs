using TextExpander.Application.Features.Hotkeys;

namespace TextExpander.Infrastructure.Platform.Windows;

/// <summary>Registration lifecycle separated from the native message pump for deterministic tests.</summary>
public sealed class WindowsSnippetHotkeySet(Func<int, PickerHotkey, bool> register, Func<int, bool> unregister)
{
    private readonly Dictionary<int, SnippetHotkeyBinding> _active = [];
    private readonly HashSet<int> _owned = [];

    public IReadOnlyDictionary<Guid, bool> Replace(IReadOnlyList<SnippetHotkeyBinding> bindings, Action drainOldMessages)
    {
        Clear();
        var states = bindings.ToDictionary(b => b.SnippetId, _ => false);
        // Failed unregistration must not allow an old OS ID to be rebound to a different snippet.
        if (_owned.Count != 0) return states;
        drainOldMessages();
        var id = 0;
        foreach (var binding in bindings)
        {
            id++;
            if (id > 0xbfff || binding.Gesture.IsDisabled) continue;
            bool ok;
            try { ok = register(id, binding.Gesture); }
            catch { ok = false; }
            if (!ok) continue;
            _active[id] = binding;
            _owned.Add(id);
            states[binding.SnippetId] = true;
        }
        return states;
    }

    public SnippetHotkeyBinding? Resolve(int id, uint modifiers, uint virtualKey) =>
        _active.TryGetValue(id, out var binding) && binding.Gesture.Modifiers == modifiers &&
        binding.Gesture.VirtualKey == virtualKey ? binding : null;

    public void Clear()
    {
        _active.Clear();
        foreach (var id in _owned.ToArray())
        {
            try { if (unregister(id)) _owned.Remove(id); }
            catch { /* Keep ownership recorded; retry cleanup, and never reuse an ambiguous ID. */ }
        }
    }
}
