namespace TextExpander.Application.Features.Output;

/// <summary>
/// Prima tranche del backend Unicode Windows: solo testo semplice, mai comandi, clipboard,
/// trigger, IME o rimozione. I controlli non sono rifiuti del dato: potranno usare un backend
/// differente quando tale backend sarà stato validato.
/// </summary>
public sealed class UnicodeOutputPlan
{
    public const int MaxUtf16Units = 1024;

    private UnicodeOutputPlan(string text) => Text = text;

    public string Text { get; }
    public int RequiredInputEvents => checked(Text.Length * 2);

    public static bool TryCreate(string? text, out UnicodeOutputPlan? plan, out string reason)
    {
        plan = null;
        if (string.IsNullOrEmpty(text))
        {
            reason = "empty";
            return false;
        }
        if (text.Length > MaxUtf16Units)
        {
            reason = "too_long";
            return false;
        }

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsHighSurrogate(c))
            {
                if (i + 1 >= text.Length || !char.IsLowSurrogate(text[i + 1]))
                {
                    reason = "invalid_utf16";
                    return false;
                }
                i++; // La coppia viene trasmessa come due unità UTF-16, senza alterazioni.
                continue;
            }
            if (char.IsLowSurrogate(c))
            {
                reason = "invalid_utf16";
                return false;
            }
            if (char.IsControl(c) || char.GetUnicodeCategory(c) is
                System.Globalization.UnicodeCategory.LineSeparator or
                System.Globalization.UnicodeCategory.ParagraphSeparator)
            {
                // Tab/newline sono legittimi negli snippet, ma non sono ancora stati
                // qualificati per l'inserimento SendInput: niente perdita silenziosa.
                reason = "controls_require_other_backend";
                return false;
            }
        }

        plan = new UnicodeOutputPlan(text);
        reason = "ok";
        return true;
    }
}

/// <summary>Risultato della sottomissione all'OS: mai una conferma di testo scritto nell'editor.</summary>
public enum TextSubmissionStatus
{
    Rejected,
    SubmittedUnverified,
    PartialOrUnknown
}

public sealed record TextSubmissionResult(
    TextSubmissionStatus Status,
    string Reason,
    int RequiredInputEvents = 0,
    int AcceptedInputEvents = 0);
