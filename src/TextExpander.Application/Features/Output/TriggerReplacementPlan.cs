using System.Globalization;
using System.Text;
using TextExpander.Core.Matching;

namespace TextExpander.Application.Features.Output;

public enum ReplacementFieldState { Unknown, VerifiedNormalEditable, Protected, ReadOnly, Disabled, Composing }
public enum ReplacementCapability { Unknown, Unsupported, VerifiedConditionalReplace }

/// <summary>
/// Evidence captured by a qualified adapter, never inferred from a foreground window.
/// UTF-16 offsets describe this exact text snapshot, not keyboard deletion counts.
/// Text is transient and must never be logged. No production adapter currently provides this evidence.
/// </summary>
public sealed record ReplacementTargetSnapshot(
    Guid TargetToken, long TargetEpoch, long TextRevision, string? Text,
    int SelectionStart, int SelectionEnd, ReplacementFieldState FieldState,
    ReplacementCapability Capability, bool HasPendingModifiers = false)
{
    public override string ToString() => nameof(ReplacementTargetSnapshot);
}

/// <summary>Identity recorded with the original committed match, before any focus transition.</summary>
public sealed record ReplacementOrigin(Guid TargetToken, long TargetEpoch, long TextRevision, long LibraryRevision);

public sealed record TriggerReplacementRequest(
    TriggerActivationDecision? Decision, ReplacementOrigin? Origin,
    ReplacementTargetSnapshot? Target, string? RawMatchedText, string? CommittedDelimiter = "")
{
    public override string ToString() => nameof(TriggerReplacementRequest);
}

public enum ReplacementRejection
{
    None, MissingRequest, ActivationNotReady, InvalidOrigin, LibraryChanged,
    TargetChanged, TextRevisionChanged, FieldNotEditable, CapabilityUnavailable,
    ModifiersPressed, InvalidText, TextTooLong, InvalidSelection, SelectionNotCollapsed,
    InvalidSnippet, InvalidRawMatch, InvalidDelimiter, RawTextChanged, GraphemeBoundary
}

public sealed record ReplacementPreparation(ReplacementRejection Rejection, TriggerReplacementPlan? Plan = null)
{
    public bool IsReady => Rejection == ReplacementRejection.None && Plan is not null;
}

/// <summary>
/// Immutable, single-use compare-and-replace proposal. It is not a SendInput or Backspace plan.
/// Source and result remain in memory for this attempt only; ToString does not expose text.
/// </summary>
public sealed class TriggerReplacementPlan
{
    private int _claimed;

    internal TriggerReplacementPlan(ReplacementOrigin origin, ReplacementTargetSnapshot expectedTarget,
        ReplacementTargetSnapshot expectedResult, int rangeStart, int rangeLength, string replacementText)
    {
        Origin = origin;
        ExpectedTarget = expectedTarget;
        ExpectedResult = expectedResult;
        RangeStart = rangeStart;
        RangeLength = rangeLength;
        ReplacementText = replacementText;
    }

    public ReplacementOrigin Origin { get; }
    public ReplacementTargetSnapshot ExpectedTarget { get; }
    public ReplacementTargetSnapshot ExpectedResult { get; }
    public int RangeStart { get; }
    public int RangeLength { get; }
    public string ReplacementText { get; }

    public bool MatchesExpectedTarget(ReplacementTargetSnapshot? observed) => ExpectedTarget == observed;
    internal bool TryClaim() => Interlocked.CompareExchange(ref _claimed, 1, 0) == 0;
}

public static class TriggerReplacementPlanner
{
    public const int MaxTextUtf16Units = 65_536;
    public const int MaxRawMatchUtf16Units = 512;

    public static ReplacementPreparation Prepare(TriggerReplacementRequest? request, long currentLibraryRevision)
    {
        if (request is null) return new(ReplacementRejection.MissingRequest);
        var decision = request.Decision;
        if (decision is not { Status: TriggerActivationStatus.Ready, CandidateCount: 1, Snippet: not null })
            return new(ReplacementRejection.ActivationNotReady);
        var origin = request.Origin;
        if (origin is null || origin.TargetToken == Guid.Empty || origin.TargetEpoch <= 0 ||
            origin.TextRevision < 0 || origin.TextRevision == long.MaxValue || origin.LibraryRevision <= 0)
            return new(ReplacementRejection.InvalidOrigin);
        if (currentLibraryRevision != origin.LibraryRevision)
            return new(ReplacementRejection.LibraryChanged);
        var target = request.Target;
        if (target is null || target.TargetToken != origin.TargetToken || target.TargetEpoch != origin.TargetEpoch)
            return new(ReplacementRejection.TargetChanged);
        if (target.TextRevision != origin.TextRevision)
            return new(ReplacementRejection.TextRevisionChanged);
        if (target.FieldState != ReplacementFieldState.VerifiedNormalEditable)
            return new(ReplacementRejection.FieldNotEditable);
        if (target.Capability != ReplacementCapability.VerifiedConditionalReplace)
            return new(ReplacementRejection.CapabilityUnavailable);
        if (target.HasPendingModifiers) return new(ReplacementRejection.ModifiersPressed);

        var text = target.Text;
        if (text is null) return new(ReplacementRejection.InvalidText);
        if (text.Length > MaxTextUtf16Units) return new(ReplacementRejection.TextTooLong);
        if (!WellFormed(text)) return new(ReplacementRejection.InvalidText);
        if (target.SelectionStart < 0 || target.SelectionEnd < 0 ||
            target.SelectionStart > text.Length || target.SelectionEnd > text.Length)
            return new(ReplacementRejection.InvalidSelection);
        if (target.SelectionStart != target.SelectionEnd)
            return new(ReplacementRejection.SelectionNotCollapsed);

        var snippet = decision.Snippet;
        if (snippet.Id == Guid.Empty || snippet.GroupId == Guid.Empty || !snippet.IsEnabled || snippet.Revision <= 0 ||
            string.IsNullOrEmpty(snippet.Trigger) || snippet.Trigger.Length > 128 ||
            !WellFormed(snippet.Trigger) || snippet.Trigger.Any(UnsafeTriggerUnit) ||
            string.IsNullOrEmpty(snippet.Content) || !WellFormed(snippet.Content))
            return new(ReplacementRejection.InvalidSnippet);
        if (snippet.Content.Length > MaxTextUtf16Units) return new(ReplacementRejection.TextTooLong);
        var raw = request.RawMatchedText;
        if (string.IsNullOrEmpty(raw) || raw.Length > MaxRawMatchUtf16Units || !WellFormed(raw) ||
            !string.Equals(raw.Normalize(NormalizationForm.FormC),
                snippet.Trigger.Normalize(NormalizationForm.FormC), StringComparison.Ordinal))
            return new(ReplacementRejection.InvalidRawMatch);
        var delimiter = request.CommittedDelimiter;
        if (delimiter is null || !ValidDelimiter(delimiter)) return new(ReplacementRejection.InvalidDelimiter);

        var caret = target.SelectionEnd;
        var end = caret - delimiter.Length;
        var start = end - raw.Length;
        if (start < 0 || !text.AsSpan(start, raw.Length).SequenceEqual(raw.AsSpan()) ||
            !text.AsSpan(end, delimiter.Length).SequenceEqual(delimiter.AsSpan()))
            return new(ReplacementRejection.RawTextChanged);
        var boundaries = StringInfo.ParseCombiningCharacters(text);
        if (!Boundary(boundaries, start, text.Length) || !Boundary(boundaries, end, text.Length) ||
            !Boundary(boundaries, caret, text.Length))
            return new(ReplacementRejection.GraphemeBoundary);
        if (text.Length - raw.Length + snippet.Content.Length > MaxTextUtf16Units)
            return new(ReplacementRejection.TextTooLong);

        // The delimiter and all text following the matched trigger are copied verbatim.
        // NFC is only a matching key. Offsets are never converted into key presses.
        var result = string.Concat(text.AsSpan(0, start), snippet.Content.AsSpan(), text.AsSpan(end));
        var resultCaret = start + snippet.Content.Length + delimiter.Length;
        if (!Boundary(StringInfo.ParseCombiningCharacters(result), resultCaret, result.Length))
            return new(ReplacementRejection.GraphemeBoundary);
        var expectedResult = target with
        {
            Text = result, TextRevision = target.TextRevision + 1,
            SelectionStart = resultCaret, SelectionEnd = resultCaret
        };
        return new(ReplacementRejection.None,
            new TriggerReplacementPlan(origin, target, expectedResult, start, raw.Length, snippet.Content));
    }

    private static bool Boundary(int[] boundaries, int offset, int textLength) =>
        offset == textLength || Array.BinarySearch(boundaries, offset) >= 0;

    private static bool ValidDelimiter(string text)
    {
        if (text.Length == 0) return true;
        if (text.Length > CommittedInputSession.MaxObservationUtf16Units || !WellFormed(text) ||
            text.Any(UnsafeTriggerUnit) || StringInfo.ParseCombiningCharacters(text).Length != 1)
            return false;
        if (!Rune.TryGetRuneAt(text, 0, out var first)) return false;
        return Rune.IsWhiteSpace(first) || Rune.GetUnicodeCategory(first) is
            UnicodeCategory.DashPunctuation or UnicodeCategory.OpenPunctuation or UnicodeCategory.ClosePunctuation or
            UnicodeCategory.InitialQuotePunctuation or UnicodeCategory.FinalQuotePunctuation or UnicodeCategory.OtherPunctuation;
    }

    private static bool UnsafeTriggerUnit(char value) => char.IsControl(value) ||
        char.GetUnicodeCategory(value) is UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator;

    private static bool WellFormed(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var unit = text[i];
            if (unit == '\0') return false;
            if (char.IsHighSurrogate(unit))
            {
                if (++i >= text.Length || !char.IsLowSurrogate(text[i])) return false;
            }
            else if (char.IsLowSurrogate(unit)) return false;
        }
        return true;
    }
}
