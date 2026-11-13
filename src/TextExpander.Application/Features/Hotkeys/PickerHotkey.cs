namespace TextExpander.Application.Features.Hotkeys;

/// <summary>
/// Portable, deliberately limited keyboard-chord contract. No keylogging or keyboard hooks.
/// A value of None means that no hotkey is registered.
/// </summary>
public readonly record struct PickerHotkey(uint Modifiers, uint VirtualKey, string Canonical)
{
    public const string Default = "Ctrl+Shift+Space";
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;

    public bool IsDisabled => VirtualKey == 0;

    public static bool TryParse(string? input, out PickerHotkey hotkey)
    {
        hotkey = default;
        if (input is null) return false;
        input = input.Trim();
        if (input.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            hotkey = new PickerHotkey(0, 0, "None");
            return true;
        }
        if (input.Length is < 3 or > 64) return false;
        var tokens = input.Split('+', StringSplitOptions.TrimEntries);
        if (tokens.Length is < 2 or > 5 || tokens.Any(string.IsNullOrWhiteSpace)) return false;

        uint modifiers = 0;
        uint key = 0;
        string? keyName = null;
        foreach (var part in tokens)
        {
            uint bit = part.ToUpperInvariant() switch
            {
                "CTRL" or "CONTROL" => ModControl,
                "ALT" => ModAlt,
                "SHIFT" => ModShift,
                "WIN" or "WINDOWS" => ModWin,
                _ => 0
            };
            if (bit != 0)
            {
                if ((modifiers & bit) != 0) return false;
                modifiers |= bit;
                continue;
            }
            if (key != 0 || !TryParseKey(part, out key, out keyName)) return false;
        }
        // Shift, Win or a bare function key must not silently swallow ordinary input.
        if (key == 0 || (modifiers & (ModControl | ModAlt)) == 0) return false;
        var names = new List<string>(5);
        if ((modifiers & ModControl) != 0) names.Add("Ctrl");
        if ((modifiers & ModAlt) != 0) names.Add("Alt");
        if ((modifiers & ModShift) != 0) names.Add("Shift");
        if ((modifiers & ModWin) != 0) names.Add("Win");
        names.Add(keyName!);
        hotkey = new PickerHotkey(modifiers, key, string.Join('+', names));
        return true;
    }

    private static bool TryParseKey(string raw, out uint vk, out string? name)
    {
        vk = 0;
        name = null;
        var upper = raw.Trim().ToUpperInvariant();
        if (upper is "SPACE" or "SPACEBAR")
        {
            vk = 0x20;
            name = "Space";
        }
        else if (upper.Length == 1 && ((upper[0] is >= 'A' and <= 'Z') || (upper[0] is >= '0' and <= '9')))
        {
            vk = upper[0];
            name = upper;
        }
        else if (upper.Length >= 2 && upper[0] == 'F' &&
                 int.TryParse(upper.AsSpan(1), out var number) && number is >= 1 and <= 11)
        {
            vk = (uint)(0x70 + number - 1);
            name = $"F{number}";
        }
        return vk != 0;
    }
}
