using System.Globalization;

namespace Ristretto.WindowsOutputLab;

/// <summary>
/// One-shot, text-free receipt protocol for the disposable picker EDIT laboratory.
/// The token correlates a response to the child launched for this single run;
/// it is not an authentication mechanism for unrelated or untrusted processes.
/// Only the child reads its own EDIT contents, and only a categorical result
/// crosses the process boundary. This is not a production editor adapter.
/// </summary>
internal static class PickerOwnedEditReceipt
{
    internal enum Outcome { Match, Mismatch, Unavailable }

    internal static string Ready(string token, nint window, nint edit) =>
        $"PICKER_READY|{token}|{window.ToInt64().ToString(CultureInfo.InvariantCulture)}|{edit.ToInt64().ToString(CultureInfo.InvariantCulture)}";

    internal static bool TryReadReady(string? line, string expectedToken, out nint window, out nint edit)
    {
        window = 0;
        edit = 0;
        if (!IsToken(expectedToken) || line is null) return false;
        var fields = line.Split('|');
        if (fields.Length != 4 || fields[0] != "PICKER_READY" || fields[1] != expectedToken ||
            !long.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out var w) ||
            !long.TryParse(fields[3], NumberStyles.None, CultureInfo.InvariantCulture, out var e) ||
            w <= 0 || e <= 0 || w == e) return false;
        window = (nint)w;
        edit = (nint)e;
        return true;
    }

    internal static string Result(string token, Outcome outcome)
    {
        var code = outcome switch
        {
            Outcome.Match => "MATCH",
            Outcome.Mismatch => "MISMATCH",
            _ => "UNAVAILABLE"
        };
        return $"PICKER_RESULT|{token}|{code}";
    }

    internal static bool TryReadResult(string? line, string expectedToken, out Outcome outcome)
    {
        outcome = Outcome.Unavailable;
        if (!IsToken(expectedToken) || line is null) return false;
        var fields = line.Split('|');
        if (fields.Length != 3 || fields[0] != "PICKER_RESULT" || fields[1] != expectedToken)
            return false;
        switch (fields[2])
        {
            case "MATCH": outcome = Outcome.Match; return true;
            case "MISMATCH": outcome = Outcome.Mismatch; return true;
            case "UNAVAILABLE": outcome = Outcome.Unavailable; return true;
            default: return false;
        }
    }

    /// <summary>Exact UTF-16 code-unit comparison, without NFC/NFD or line-ending normalization.</summary>
    internal static Outcome Compare(string? received, string expected, bool controlAvailable) =>
        !controlAvailable || received is null ? Outcome.Unavailable :
        received.AsSpan().SequenceEqual(expected.AsSpan()) ? Outcome.Match : Outcome.Mismatch;

    private static bool IsToken(string token) =>
        token.Length == 32 && token.All(Uri.IsHexDigit);
}
