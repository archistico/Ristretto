using System.Globalization;
using System.Text;

namespace TextExpander.Core.Matching;

/// <summary>
/// F1b policy is separate from F1a recognition. No keyboard hooks, edits, output,
/// or assumptions about the number of Backspace presses are part of this API.
/// </summary>
public enum TriggerActivationMode
{
    Immediate,
    OnDelimiter
}

public enum TriggerStartBoundary
{
    Anywhere,
    WordStart
}

public enum TriggerDelimiterSet
{
    UnicodeWhitespace,
    UnicodeWhitespaceOrPunctuation
}

/// <summary>
/// An adapter may assert this only when it can prove the caret was at a word boundary
/// before the *first* observed insertion. Unknown is the fail-closed default.
/// </summary>
public enum InitialWordBoundaryEvidence
{
    Unknown,
    Verified
}

public sealed record TriggerActivationPolicy(
    TriggerActivationMode Mode = TriggerActivationMode.OnDelimiter,
    TriggerStartBoundary StartBoundary = TriggerStartBoundary.WordStart,
    TriggerDelimiterSet Delimiters = TriggerDelimiterSet.UnicodeWhitespace,
    int MaxIdleMilliseconds = 15_000)
{
    public void Validate()
    {
        if (!Enum.IsDefined(Mode) || !Enum.IsDefined(StartBoundary) || !Enum.IsDefined(Delimiters))
            throw new ArgumentOutOfRangeException(nameof(Mode), "Unknown trigger activation policy option.");
        if (MaxIdleMilliseconds < 0 || MaxIdleMilliseconds > 600_000)
            throw new ArgumentOutOfRangeException(nameof(MaxIdleMilliseconds));
    }
}

public enum TriggerActivationStatus
{
    None,
    WaitingForMoreInput,
    WaitingForDelimiter,
    Ready,
    Ambiguous,
    Suppressed
}

/// <summary>
/// A decision for a *pure* stream of verified text. Ready means the recognition policy
/// is satisfied, NOT that the text may be removed or replaced in another application.
/// In OnDelimiter mode the delimiter has already been committed to the target.
/// </summary>
public sealed record TriggerActivationDecision(
    TriggerActivationStatus Status,
    Snippet? Snippet = null,
    int CandidateCount = 0);

/// <summary>
/// A single-consumer, bounded, non-persistent recognizer. The caller must serialize
/// native events, focus transitions, pause and catalog publication. The timestamp is
/// supplied in monotonic milliseconds by that caller (never a wall-clock DateTime).
/// </summary>
public sealed class TriggerActivationSession
{
    private CompiledTriggerCatalog _catalog;
    private readonly TriggerActivationPolicy _policy;
    private string _suffix = string.Empty;
    private long? _targetEpoch;
    private long? _lastTimestamp;
    private bool _verifiedStartBoundary;

    public TriggerActivationSession(LibrarySnapshot snapshot, TriggerActivationPolicy? policy = null)
    {
        _policy = policy ?? new TriggerActivationPolicy();
        _policy.Validate();
        _catalog = CompiledTriggerCatalog.Compile(snapshot);
    }

    public long LibraryRevision => _catalog.LibraryRevision;
    public bool IsPaused { get; private set; }

    public void Reset() => Clear();

    public void SetPaused(bool paused)
    {
        if (paused == IsPaused) return;
        Clear();
        IsPaused = paused;
    }

    public void PublishSnapshot(LibrarySnapshot snapshot)
    {
        var replacement = CompiledTriggerCatalog.Compile(snapshot);
        Clear();
        _catalog = replacement;
    }

    public TriggerActivationDecision Observe(
        CommittedInputObservation observation,
        long monotonicMilliseconds,
        InitialWordBoundaryEvidence initialBoundary = InitialWordBoundaryEvidence.Unknown)
    {
        ArgumentNullException.ThrowIfNull(observation);
        if (IsPaused || observation.TargetEpoch <= 0 || monotonicMilliseconds < 0 ||
            !Enum.IsDefined(initialBoundary) ||
            observation.Kind != TextObservationKind.VerifiedCommitted ||
            observation.FieldSafety != ObservedFieldSafety.VerifiedNormalEditable ||
            string.IsNullOrEmpty(observation.Text) ||
            observation.Text.Length > CommittedInputSession.MaxObservationUtf16Units ||
            !CommittedInputSession.IsValidText(observation.Text))
        {
            Clear();
            return new(TriggerActivationStatus.Suppressed);
        }

        if (_targetEpoch != observation.TargetEpoch)
            Clear(); // A new target cannot inherit even one code point of typed history.

        if (_lastTimestamp is long prior)
        {
            if (monotonicMilliseconds < prior)
            {
                Clear(); // A time source reset or unsorted event stream is not reliable.
                return new(TriggerActivationStatus.Suppressed);
            }
            if (_policy.MaxIdleMilliseconds > 0 &&
                (ulong)(monotonicMilliseconds - prior) > (ulong)_policy.MaxIdleMilliseconds)
                Clear(); // Never activate a stale prefix after idle time.
        }

        if (_targetEpoch is null)
        {
            _targetEpoch = observation.TargetEpoch;
            // This assertion is consulted only at the beginning of a fresh run.
            // Untrusted or repeated assertions cannot re-anchor existing text.
            _verifiedStartBoundary = initialBoundary == InitialWordBoundaryEvidence.Verified;
        }
        _lastTimestamp = monotonicMilliseconds;

        var elements = StringInfo.GetTextElementEnumerator(observation.Text);
        var decision = new TriggerActivationDecision(TriggerActivationStatus.None);
        while (elements.MoveNext())
        {
            var grapheme = elements.GetTextElement();
            if (IsDelimiter(grapheme))
            {
                // Inspect only the text BEFORE the newly committed delimiter. A
                // delimiter is never silently removed from or added to a trigger.
                var delimited = ResolveExact(_suffix);
                if (delimited.Status is TriggerActivationStatus.Ready or TriggerActivationStatus.Ambiguous)
                {
                    // Only a match at the end of this observation can become a
                    // decision. Matching inside a bulk/paste chunk must not
                    // authorize an edit at a different caret position.
                    if (elements.MoveNext())
                    {
                        Clear();
                        return new(TriggerActivationStatus.Suppressed);
                    }
                    Clear();
                    return delimited;
                }
            }

            if (!Append(grapheme))
            {
                Clear();
                return new(TriggerActivationStatus.Suppressed);
            }

            decision = EvaluateAfterAppend();
            if (decision.Status is TriggerActivationStatus.Ready or TriggerActivationStatus.Ambiguous)
            {
                if (elements.MoveNext())
                {
                    // If a whole committed chunk contains additional text after the
                    // immediate match, it is not an actionable end-of-chunk match.
                    Clear();
                    return new(TriggerActivationStatus.Suppressed);
                }
                Clear();
                return decision;
            }
        }
        return decision;
    }

    private TriggerActivationDecision EvaluateAfterAppend()
    {
        var exact = ResolveExact(_suffix, out var exactLength);
        if (_policy.Mode == TriggerActivationMode.OnDelimiter)
        {
            if (exact.Status is TriggerActivationStatus.Ready or TriggerActivationStatus.Ambiguous)
                return new(TriggerActivationStatus.WaitingForDelimiter);
            return HasEligiblePrefix(_suffix)
                ? new(TriggerActivationStatus.WaitingForMoreInput)
                : new(TriggerActivationStatus.None);
        }

        // Only a pending prefix at least as long as the best exact match
        // can defer it. For "brb", the final "b" is a proper prefix of
        // "brb", but it is NOT a possible longer alternative to "brb".
        if (HasEligiblePrefix(_suffix, exactLength))
            return new(TriggerActivationStatus.WaitingForMoreInput);
        return exact;
    }

    private TriggerActivationDecision ResolveExact(string text) => ResolveExact(text, out _);

    private TriggerActivationDecision ResolveExact(string text, out int matchedLength)
    {
        matchedLength = 0;
        if (text.Length == 0) return new(TriggerActivationStatus.None);
        foreach (var (length, candidates) in _catalog.ExactSuffixesDescending(text))
        {
            if (!AllowedStart(text, length)) continue;
            matchedLength = length;
            return candidates.Length == 1
                ? new(TriggerActivationStatus.Ready, candidates[0], 1)
                : new(TriggerActivationStatus.Ambiguous, null, candidates.Length);
        }
        return new(TriggerActivationStatus.None);
    }

    private bool HasEligiblePrefix(string text, int minimumLength = 1) =>
        _catalog.HasEligibleProperPrefixSuffix(text,
            length => length >= minimumLength && AllowedStart(text, length));

    private bool AllowedStart(string text, int triggerLength)
    {
        if (_policy.StartBoundary == TriggerStartBoundary.Anywhere)
            return true;
        var start = text.Length - triggerLength;
        if (start < 0) return false;
        // A punctuation-prefixed trigger (e.g. ';firma') is an explicit prefix:
        // it is not constrained by the word-start rule for alphabetic shortcuts.
        if (!Rune.TryGetRuneAt(text, start, out var first)) return false;
        if (!IsWordRune(first)) return true;

        if (start == 0) return _verifiedStartBoundary;
        // This checks a committed preceding grapheme, never the adjacent text in
        // an external editor (which this class cannot inspect).
        if (Rune.DecodeLastFromUtf16(text.AsSpan(0, start), out var previous, out _) !=
            System.Buffers.OperationStatus.Done)
            return false;
        return !IsWordRune(previous);
    }

    private bool IsDelimiter(string grapheme)
    {
        if (!Rune.TryGetRuneAt(grapheme, 0, out var first)) return false;
        if (Rune.IsWhiteSpace(first)) return true;
        if (_policy.Delimiters == TriggerDelimiterSet.UnicodeWhitespace) return false;
        return Rune.GetUnicodeCategory(first) is
            UnicodeCategory.DashPunctuation or UnicodeCategory.OpenPunctuation or
            UnicodeCategory.ClosePunctuation or
            UnicodeCategory.InitialQuotePunctuation or UnicodeCategory.FinalQuotePunctuation or
            UnicodeCategory.OtherPunctuation;
    }

    private static bool IsWordRune(Rune rune) => Rune.GetUnicodeCategory(rune) is
        UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or
        UnicodeCategory.TitlecaseLetter or UnicodeCategory.ModifierLetter or
        UnicodeCategory.OtherLetter or UnicodeCategory.DecimalDigitNumber or
        UnicodeCategory.LetterNumber or UnicodeCategory.OtherNumber or
        UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or
        UnicodeCategory.EnclosingMark or UnicodeCategory.ConnectorPunctuation;

    private bool Append(string grapheme)
    {
        var next = (_suffix + grapheme).Normalize(NormalizationForm.FormC);
        if (next.Length > CommittedInputSession.MaxBufferedUtf16Units)
        {
            var threshold = next.Length - CommittedInputSession.MaxBufferedUtf16Units;
            var boundaries = StringInfo.ParseCombiningCharacters(next);
            var boundary = Array.Find(boundaries, index => index >= threshold);
            if (boundary < threshold) return false;
            next = next[boundary..];
            _verifiedStartBoundary = false; // Original left context was trimmed.
        }
        _suffix = next;
        return true;
    }

    private void Clear()
    {
        _suffix = string.Empty;
        _targetEpoch = null;
        _lastTimestamp = null;
        _verifiedStartBoundary = false;
    }
}
