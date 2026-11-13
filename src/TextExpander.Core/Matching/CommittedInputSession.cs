using System.Globalization;
using System.Text;

namespace TextExpander.Core.Matching;

/// <summary>
/// Only the native adapter may truthfully assert that an observation is committed text
/// in a normal editable target. Unknown/protected/composing/synthetic observations are
/// barriers: never guess text from raw physical key presses or from an unfinished IME.
/// </summary>
public enum TextObservationKind
{
    VerifiedCommitted,
    Composing,
    Unknown,
    Synthetic,
    NavigationOrSelectionChange
}

public enum ObservedFieldSafety
{
    VerifiedNormalEditable,
    Protected,
    Unknown
}

public sealed record CommittedInputObservation(
    long TargetEpoch,
    string? Text,
    TextObservationKind Kind,
    ObservedFieldSafety FieldSafety)
{
    // Observation contents are transient text, never safe for diagnostic record printing.
    public override string ToString() => nameof(CommittedInputObservation);
}

/// <summary>
/// An in-memory, strictly bounded suffix for one focused text target. No native events,
/// clipboard, filesystem, logs, background hook or external side effects.
/// Must be used from a single serialized consumer; OS callbacks must not execute it
/// concurrently. A different target epoch or snapshot revision clears prior context.
/// </summary>
public sealed class CommittedInputSession
{
    public const int MaxBufferedUtf16Units = 512;
    public const int MaxObservationUtf16Units = 1024;

    private CompiledTriggerCatalog _catalog;
    private string _suffix = string.Empty;
    private long? _targetEpoch;

    public CommittedInputSession(LibrarySnapshot snapshot) =>
        _catalog = CompiledTriggerCatalog.Compile(snapshot);

    public bool IsPaused { get; private set; }
    public long LibraryRevision => _catalog.LibraryRevision;

    public void PublishSnapshot(LibrarySnapshot snapshot)
    {
        // Compile first: on an invalid update, do not publish a half-built catalog.
        var replacement = CompiledTriggerCatalog.Compile(snapshot);
        Clear();
        _catalog = replacement;
    }

    public void SetPaused(bool paused)
    {
        // On either transition discard the old typing context, including previous matches.
        if (paused == IsPaused) return;
        Clear();
        IsPaused = paused;
    }

    public void Reset() => Clear();

    public TriggerRecognition Observe(CommittedInputObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        if (IsPaused || observation.TargetEpoch <= 0 ||
            observation.Kind != TextObservationKind.VerifiedCommitted ||
            observation.FieldSafety != ObservedFieldSafety.VerifiedNormalEditable ||
            string.IsNullOrEmpty(observation.Text) ||
            observation.Text.Length > MaxObservationUtf16Units ||
            !IsValidText(observation.Text))
        {
            Clear();
            return new(TriggerRecognitionStatus.Suppressed);
        }

        if (_targetEpoch != observation.TargetEpoch)
        {
            // The first verified text event of a new target can start a fresh session,
            // but it can never complete a trigger from the previous target.
            Clear();
            _targetEpoch = observation.TargetEpoch;
        }

        var updated = (_suffix + observation.Text).Normalize(NormalizationForm.FormC);
        if (updated.Length > MaxBufferedUtf16Units)
        {
            // Trim only at grapheme boundaries, not at a UTF-16 offset that could
            // split an emoji ZWJ sequence, a combining mark, or a surrogate pair.
            var boundaries = StringInfo.ParseCombiningCharacters(updated);
            var threshold = updated.Length - MaxBufferedUtf16Units;
            var boundary = Array.Find(boundaries, index => index >= threshold);
            if (boundary < threshold)
            {
                Clear();
                return new(TriggerRecognitionStatus.Suppressed);
            }
            updated = updated[boundary..];
        }

        _suffix = updated;
        return _catalog.Recognize(_suffix);
    }

    private void Clear()
    {
        _suffix = string.Empty;
        _targetEpoch = null;
    }

    /// <summary>
    /// Reject invalid surrogate sequences and control/line separators. Emoji joiners,
    /// bidi characters, combining marks and variation selectors are valid Unicode input.
    /// Public shared contract used by Core matching and Application's owned-EDIT delta verifier.
    /// Callers must supply non-null text; this does not authorize editing external controls.
    /// </summary>
    public static bool IsValidText(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var current = text[i];
            if (char.IsHighSurrogate(current))
            {
                if (++i >= text.Length || !char.IsLowSurrogate(text[i])) return false;
                continue;
            }
            if (char.IsLowSurrogate(current) || char.IsControl(current) ||
                char.GetUnicodeCategory(current) is UnicodeCategory.LineSeparator or
                    UnicodeCategory.ParagraphSeparator)
                return false;
        }
        return true;
    }
}
