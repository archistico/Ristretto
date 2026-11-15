using Avalonia.Input;
using TextExpander.Application.Features.Hotkeys;

namespace TextExpander.Desktop;

/// <summary>Local dialog keystrokes only. Keeps the last complete chord visible after release;
/// repeated keydown, partial modifiers and multi-key gestures never silently select a shortcut.</summary>
public sealed class HotkeyCaptureState
{
    private readonly HashSet<Key> _held = [];
    private KeyModifiers _modifiers;
    private bool _invalid;
    private bool _hasPrimary;
    public string DisplayText { get; private set; } = "";
    public string? Hotkey { get; private set; }
    public bool IsInvalid => _invalid;
    public bool KeysHeld => _held.Count > 0 || _modifiers != KeyModifiers.None;
    public bool CanConfirm => Hotkey is not null && !_invalid && !KeysHeld;

    public void Reset()
    {
        _held.Clear();
        _modifiers = KeyModifiers.None;
        _invalid = _hasPrimary = false;
        Hotkey = null;
        DisplayText = "";
    }

    public void KeyDown(Key key, KeyModifiers modifiers)
    {
        if (!KeysHeld) Reset();
        var primaryWasHeld = _held.Any(k => Modifier(k) == KeyModifiers.None);
        if (!_held.Add(key)) return;
        _modifiers = Relevant(modifiers) | HeldModifiers();
        var primary = _held.Where(k => Modifier(k) == KeyModifiers.None).ToArray();
        var names = ModifierNames(_modifiers);
        names.AddRange(primary.Select(KeyName));
        DisplayText = string.Join(" + ", names);
        if (primary.Length == 0)
        {
            Hotkey = null;
            _hasPrimary = _invalid = false;
            return;
        }
        if (Modifier(key) == KeyModifiers.None && !primaryWasHeld) _invalid = false;
        _hasPrimary = true;
        if (_invalid || primary.Length != 1 || !PickerHotkey.TryParse(DisplayText, out var parsed) ||
            !IsSupportedPrimary(primary[0]))
        {
            _invalid = true;
            Hotkey = null;
            return;
        }
        Hotkey = parsed.Canonical;
    }

    public void KeyUp(Key key, KeyModifiers modifiers)
    {
        _held.Remove(key);
        _modifiers = Relevant(modifiers) | HeldModifiers();
        if (!_hasPrimary) DisplayText = string.Join(" + ", ModifierNames(_modifiers));
    }

    private static KeyModifiers Relevant(KeyModifiers value) =>
        value & (KeyModifiers.Alt | KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Meta);

    private KeyModifiers HeldModifiers() => _held.Aggregate(KeyModifiers.None, (value, key) => value | Modifier(key));

    private static KeyModifiers Modifier(Key key) => key switch
    {
        Key.LeftAlt or Key.RightAlt => KeyModifiers.Alt,
        Key.LeftCtrl or Key.RightCtrl => KeyModifiers.Control,
        Key.LeftShift or Key.RightShift => KeyModifiers.Shift,
        Key.LWin or Key.RWin => KeyModifiers.Meta,
        _ => KeyModifiers.None
    };

    private static List<string> ModifierNames(KeyModifiers modifiers)
    {
        var names = new List<string>();
        if (modifiers.HasFlag(KeyModifiers.Alt)) names.Add("ALT");
        if (modifiers.HasFlag(KeyModifiers.Control)) names.Add("CTRL");
        if (modifiers.HasFlag(KeyModifiers.Shift)) names.Add("SHIFT");
        if (modifiers.HasFlag(KeyModifiers.Meta)) names.Add("WIN");
        return names;
    }

    private static bool IsSupportedPrimary(Key key) => key is >= Key.A and <= Key.Z or
        >= Key.D0 and <= Key.D9 or >= Key.F1 and <= Key.F11 or Key.Space;

    private static string KeyName(Key key) => key switch
    {
        >= Key.D0 and <= Key.D9 => ((int)key - (int)Key.D0).ToString(),
        Key.Space => "SPACE",
        _ => key.ToString().ToUpperInvariant()
    };
}
