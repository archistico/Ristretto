using System.Globalization;
using TextExpander.Core.Matching;

namespace TextExpander.Application.Features.Input;

/// <summary>
/// Fotografia VOLATILE di un controllo Win32 Edit esterno, acquisita solo su richiesta.
/// Il contenuto non deve mai essere serializzato, tracciato o esposto nei rapporti.
/// Non include alcuna prova della provenienza della modifica o del permesso di scrivere.
/// </summary>
public sealed record ExternalClassicEditTextObservation(string Text, int SelectionStart, int SelectionEnd)
{
    public override string ToString() => nameof(ExternalClassicEditTextObservation);
}

public enum ExternalClassicEditTextDeltaStatus
{
    MissingObservation,
    TargetChanged,
    NoChange,
    InvalidSelection,
    SelectionNotCollapsed,
    NotInsertionAtCaret,
    TooLong,
    InvalidUnicode,
    GraphemeBoundary,
    CandidateInsertion
}

/// <summary>
/// Un candidato è soltanto una differenza testuale plausibile: può derivare anche da
/// incolla, IME o codice. Non diventa MAI CommittedInputObservation verificato.
/// </summary>
public sealed record ExternalClassicEditTextDeltaResult(ExternalClassicEditTextDeltaStatus Status)
{
    public bool CandidateObserved => Status == ExternalClassicEditTextDeltaStatus.CandidateInsertion;
    public bool CanAuthorizeExpansion => false;
    public override string ToString() => nameof(ExternalClassicEditTextDeltaResult);
}

public static class ExternalClassicEditTextDeltaPolicy
{
    public const int MaxDocumentUtf16Units = 4096;

    /// <summary>
    /// Confronto puramente funzionale, senza API native o effetti esterni.
    /// sameTarget deve essere stabilito dall'adapter Win32 confrontando HWND e PID;
    /// i due snapshot possono cambiare durante la lettura e NON sono atomici.
    /// </summary>
    public static ExternalClassicEditTextDeltaResult Evaluate(
        ExternalClassicEditTextObservation? before,
        ExternalClassicEditTextObservation? after,
        bool sameTarget)
    {
        if (before is null || after is null)
            return new(ExternalClassicEditTextDeltaStatus.MissingObservation);
        if (!sameTarget)
            return new(ExternalClassicEditTextDeltaStatus.TargetChanged);
        if (before.Text is null || after.Text is null ||
            before.Text.Length > MaxDocumentUtf16Units || after.Text.Length > MaxDocumentUtf16Units)
            return new(ExternalClassicEditTextDeltaStatus.TooLong);
        if (!WellFormedDocument(before.Text) || !WellFormedDocument(after.Text))
            return new(ExternalClassicEditTextDeltaStatus.InvalidUnicode);
        if (!ValidSelection(before) || !ValidSelection(after))
            return new(ExternalClassicEditTextDeltaStatus.InvalidSelection);
        if (before.SelectionStart != before.SelectionEnd || after.SelectionStart != after.SelectionEnd)
            return new(ExternalClassicEditTextDeltaStatus.SelectionNotCollapsed);
        if (before.Text == after.Text)
            return new(ExternalClassicEditTextDeltaStatus.NoChange);

        var start = before.SelectionEnd;
        var addedUnits = after.Text.Length - before.Text.Length;
        if (addedUnits <= 0 || addedUnits > CommittedInputSession.MaxObservationUtf16Units)
            return new(addedUnits > CommittedInputSession.MaxObservationUtf16Units
                ? ExternalClassicEditTextDeltaStatus.TooLong
                : ExternalClassicEditTextDeltaStatus.NotInsertionAtCaret);
        if (after.SelectionEnd != start + addedUnits ||
            !before.Text.AsSpan(0, start).SequenceEqual(after.Text.AsSpan(0, start)) ||
            !before.Text.AsSpan(start).SequenceEqual(after.Text.AsSpan(start + addedUnits)))
            return new(ExternalClassicEditTextDeltaStatus.NotInsertionAtCaret);
        if (!IsGraphemeBoundary(before.Text, start) ||
            !IsGraphemeBoundary(after.Text, after.SelectionEnd))
            return new(ExternalClassicEditTextDeltaStatus.GraphemeBoundary);

        var inserted = after.Text.Substring(start, addedUnits);
        if (!CommittedInputSession.IsValidText(inserted))
            return new(ExternalClassicEditTextDeltaStatus.InvalidUnicode);

        // Non copiare il testo nel risultato: nessuna prova di origine umana o
        // approvazione di espansione, nemmeno se la sequenza sembra un trigger.
        return new(ExternalClassicEditTextDeltaStatus.CandidateInsertion);
    }

    private static bool WellFormedDocument(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            var c = value[index];
            if (c == '\0') return false;
            if (char.IsHighSurrogate(c))
            {
                if (++index == value.Length || !char.IsLowSurrogate(value[index])) return false;
            }
            else if (char.IsLowSurrogate(c)) return false;
        }
        return true;
    }

    private static bool ValidSelection(ExternalClassicEditTextObservation value) =>
        value.SelectionStart >= 0 && value.SelectionEnd >= 0 &&
        value.SelectionStart <= value.Text.Length && value.SelectionEnd <= value.Text.Length;

    private static bool IsGraphemeBoundary(string value, int index) =>
        index == value.Length || Array.BinarySearch(StringInfo.ParseCombiningCharacters(value), index) >= 0;
}
