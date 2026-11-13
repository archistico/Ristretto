using System.Buffers;
using System.Globalization;
using System.Text;
using TextExpander.Core.Matching;

namespace TextExpander.Application.Features.Output;

/// <summary>
/// Evidence obtained by the adapter owning a local EDIT. These booleans are not OS
/// capabilities and must never be asserted merely because a foreign window has focus.
/// A synthetic fixture can explicitly establish its own controlled editing context.
/// </summary>
public sealed record OwnedEditObservationContext(
    bool OwnsControl,
    bool InputFocusVerified,
    bool CompositionStateKnown,
    bool IsComposing,
    bool ModifiersPressed);

public enum CommittedEditDeltaRejection
{
    None,
    NoChange,
    MissingSnapshot,
    UntrustedControl,
    FocusNotVerified,
    CompositionUnknown,
    CompositionActive,
    ModifiersActive,
    TargetChanged,
    RevisionNotConsecutive,
    UnsafeField,
    CapabilityUnavailable,
    MissingOrInvalidDocument,
    InvalidSelection,
    SelectionNotCollapsed,
    NotAnInsertionAtCaret,
    ObservationTooLong,
    UnsafeCommittedText,
    GraphemeBoundary
}

/// <summary>
/// A verified insertion into the owned EDIT's actual document. Not proof that a human
/// typed it: paste or programmatic changes can produce exactly the same text delta.
/// This evidence alone never grants permission to replace a snippet in another app.
/// </summary>
public sealed record VerifiedCommittedEditInsertion(
    CommittedInputObservation Observation,
    InitialWordBoundaryEvidence InitialBoundary)
{
    public override string ToString() => nameof(VerifiedCommittedEditInsertion);
}

public sealed record CommittedEditDeltaResult(
    CommittedEditDeltaRejection Rejection,
    VerifiedCommittedEditInsertion? Insertion = null)
{
    public bool IsVerified => Rejection == CommittedEditDeltaRejection.None && Insertion is not null;
    public override string ToString() => nameof(CommittedEditDeltaResult);
}

/// <summary>
/// F3 / Alpha 13: pure, bounded, fail-closed validator of two snapshots from the SAME
/// locally owned control. Does not read global keys, inspect external editors, or edit
/// a target. The caller must serialize observations on the owner's UI thread and reset
/// recognition on every rejected transition (except NoChange).
/// </summary>
public static class CommittedEditDeltaVerifier
{
    public static CommittedEditDeltaResult Verify(
        ReplacementTargetSnapshot? before,
        ReplacementTargetSnapshot? after,
        OwnedEditObservationContext evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (before is null || after is null)
            return new(CommittedEditDeltaRejection.MissingSnapshot);
        if (!evidence.OwnsControl)
            return new(CommittedEditDeltaRejection.UntrustedControl);
        if (!evidence.InputFocusVerified)
            return new(CommittedEditDeltaRejection.FocusNotVerified);
        if (!evidence.CompositionStateKnown)
            return new(CommittedEditDeltaRejection.CompositionUnknown);
        if (evidence.IsComposing)
            return new(CommittedEditDeltaRejection.CompositionActive);
        if (evidence.ModifiersPressed || before.HasPendingModifiers || after.HasPendingModifiers)
            return new(CommittedEditDeltaRejection.ModifiersActive);
        if (before.TargetToken == Guid.Empty || before.TargetEpoch <= 0 ||
            before.TargetToken != after.TargetToken || before.TargetEpoch != after.TargetEpoch)
            return new(CommittedEditDeltaRejection.TargetChanged);
        if (before == after)
            return new(CommittedEditDeltaRejection.NoChange);
        if (before.TextRevision < 0 || before.TextRevision == long.MaxValue ||
            after.TextRevision != before.TextRevision + 1)
            return new(CommittedEditDeltaRejection.RevisionNotConsecutive);
        if (before.FieldState != ReplacementFieldState.VerifiedNormalEditable ||
            after.FieldState != ReplacementFieldState.VerifiedNormalEditable)
            return new(CommittedEditDeltaRejection.UnsafeField);
        if (before.Capability != ReplacementCapability.VerifiedConditionalReplace ||
            after.Capability != ReplacementCapability.VerifiedConditionalReplace)
            return new(CommittedEditDeltaRejection.CapabilityUnavailable);

        var oldText = before.Text;
        var newText = after.Text;
        if (oldText is null || newText is null ||
            oldText.Length > TriggerReplacementPlanner.MaxTextUtf16Units ||
            newText.Length > TriggerReplacementPlanner.MaxTextUtf16Units ||
            !WellFormedDocument(oldText) || !WellFormedDocument(newText))
            return new(CommittedEditDeltaRejection.MissingOrInvalidDocument);
        if (!ValidSelection(before, oldText.Length) || !ValidSelection(after, newText.Length))
            return new(CommittedEditDeltaRejection.InvalidSelection);
        if (before.SelectionStart != before.SelectionEnd || after.SelectionStart != after.SelectionEnd)
            return new(CommittedEditDeltaRejection.SelectionNotCollapsed);

        var start = before.SelectionEnd;
        var insertedLength = newText.Length - oldText.Length;
        if (insertedLength <= 0 || start > newText.Length ||
            after.SelectionEnd != (long)start + insertedLength ||
            !oldText.AsSpan(0, start).SequenceEqual(newText.AsSpan(0, start)) ||
            !oldText.AsSpan(start).SequenceEqual(newText.AsSpan(start + insertedLength)))
            return new(CommittedEditDeltaRejection.NotAnInsertionAtCaret);
        if (insertedLength > CommittedInputSession.MaxObservationUtf16Units)
            return new(CommittedEditDeltaRejection.ObservationTooLong);
        if (!IsBoundary(oldText, start) || !IsBoundary(newText, after.SelectionEnd))
            return new(CommittedEditDeltaRejection.GraphemeBoundary);

        var inserted = newText.Substring(start, insertedLength);
        if (!CommittedInputSession.IsValidText(inserted))
            return new(CommittedEditDeltaRejection.UnsafeCommittedText);
        var boundary = VerifiedWordStart(oldText, start)
            ? InitialWordBoundaryEvidence.Verified
            : InitialWordBoundaryEvidence.Unknown;
        return new(CommittedEditDeltaRejection.None,
            new VerifiedCommittedEditInsertion(
                new CommittedInputObservation(after.TargetEpoch, inserted,
                    TextObservationKind.VerifiedCommitted, ObservedFieldSafety.VerifiedNormalEditable),
                boundary));
    }

    private static bool ValidSelection(ReplacementTargetSnapshot snapshot, int textLength) =>
        snapshot.SelectionStart >= 0 && snapshot.SelectionEnd >= 0 &&
        snapshot.SelectionStart <= textLength && snapshot.SelectionEnd <= textLength;

    private static bool IsBoundary(string text, int index) => index == text.Length ||
        Array.BinarySearch(StringInfo.ParseCombiningCharacters(text), index) >= 0;

    private static bool VerifiedWordStart(string text, int insertionStart)
    {
        if (insertionStart == 0) return true;
        if (Rune.DecodeLastFromUtf16(text.AsSpan(0, insertionStart), out var last, out _) != OperationStatus.Done)
            return false;
        return Rune.GetUnicodeCategory(last) is not (
            UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or
            UnicodeCategory.TitlecaseLetter or UnicodeCategory.ModifierLetter or
            UnicodeCategory.OtherLetter or UnicodeCategory.DecimalDigitNumber or
            UnicodeCategory.LetterNumber or UnicodeCategory.OtherNumber or
            UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or
            UnicodeCategory.EnclosingMark or UnicodeCategory.ConnectorPunctuation);
    }

    // Documents may contain line breaks; the newly inserted observation may not.
    private static bool WellFormedDocument(string text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '\0') return false;
            if (char.IsHighSurrogate(text[index]))
            {
                if (++index == text.Length || !char.IsLowSurrogate(text[index])) return false;
            }
            else if (char.IsLowSurrogate(text[index])) return false;
        }
        return true;
    }
}
