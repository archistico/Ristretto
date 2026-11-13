using System.Text;

namespace TextExpander.Core.Matching;

/// <summary>
/// An immutable index built outside the keyboard callback. Resolves exact, NFC-normalized,
/// case-sensitive suffixes without scanning every snippet on each observation.
/// It never decides how many Backspace presses are safe in an external editor.
/// </summary>
public sealed class CompiledTriggerCatalog
{
    private readonly Dictionary<int, Dictionary<string, Snippet[]>> _triggers;
    private readonly HashSet<string> _properPrefixes;
    private readonly int[] _lengthsDescending;
    private readonly int _maxTriggerLength;

    private CompiledTriggerCatalog(
        long revision,
        Dictionary<int, Dictionary<string, Snippet[]>> triggers,
        HashSet<string> properPrefixes,
        int[] lengthsDescending,
        int maxTriggerLength)
    {
        LibraryRevision = revision;
        _triggers = triggers;
        _properPrefixes = properPrefixes;
        _lengthsDescending = lengthsDescending;
        _maxTriggerLength = maxTriggerLength;
    }

    public long LibraryRevision { get; }

    public static CompiledTriggerCatalog Compile(LibrarySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var enabledGroups = snapshot.Groups.Where(g => g.IsEnabled).Select(g => g.Id).ToHashSet();
        var mutable = new Dictionary<int, Dictionary<string, List<Snippet>>>();
        var prefixes = new HashSet<string>(StringComparer.Ordinal);
        var longest = 0;

        foreach (var snippet in snapshot.Snippets)
        {
            if (!snippet.IsEnabled || !enabledGroups.Contains(snippet.GroupId) ||
                string.IsNullOrEmpty(snippet.Trigger) || snippet.Trigger.Length > 128 ||
                !CommittedInputSession.IsValidText(snippet.Trigger))
                continue;

            var canonical = snippet.Trigger.Normalize(NormalizationForm.FormC);
            if (canonical.Length == 0 || canonical.Length > 128) continue;
            if (!mutable.TryGetValue(canonical.Length, out var atLength))
                mutable[canonical.Length] = atLength = new(StringComparer.Ordinal);
            if (!atLength.TryGetValue(canonical, out var candidates))
                atLength[canonical] = candidates = [];
            candidates.Add(snippet);
            longest = Math.Max(longest, canonical.Length);

            // A shorter match must not fire while the user may still be typing a longer one.
            // Prefixes are recorded only once when compiling the snapshot.
            for (var length = 1; length < canonical.Length; length++)
                prefixes.Add(canonical[..length]);
        }

        var frozen = mutable.ToDictionary(
            x => x.Key,
            x => x.Value.ToDictionary(y => y.Key, y => y.Value.ToArray(), StringComparer.Ordinal));
        return new CompiledTriggerCatalog(snapshot.Revision, frozen, prefixes,
            frozen.Keys.OrderDescending().ToArray(), longest);
    }

    // Shared by F1b. Only F1b decides which completed suffix is eligible for activation.
    // The longest *eligible* match wins; an ineligible word-internal match must not hide
    // a shorter, eligible suffix. These methods are internal to keep the public Core API
    // independent from implementation details of the compiled index.
    internal IEnumerable<(int Length, Snippet[] Candidates)> ExactSuffixesDescending(string suffix)
    {
        foreach (var length in _lengthsDescending)
        {
            if (length > suffix.Length) continue;
            if (_triggers[length].GetAlternateLookup<ReadOnlySpan<char>>()
                .TryGetValue(suffix.AsSpan(suffix.Length - length), out var candidates))
                yield return (length, candidates);
        }
    }

    internal bool HasEligibleProperPrefixSuffix(string suffix, Func<int, bool> eligible)
    {
        // Ordinal alternate lookup keeps suffix slices on the stack. A miss with a
        // long shared prefix must not allocate a new string for every prefix length.
        var prefixes = _properPrefixes.GetAlternateLookup<ReadOnlySpan<char>>();
        for (var length = Math.Min(_maxTriggerLength - 1, suffix.Length); length >= 1; length--)
            if (prefixes.Contains(suffix.AsSpan(suffix.Length - length)) && eligible(length))
                return true;
        return false;
    }

    /// <summary>
    /// Match an already composed NFC suffix. A pending longer prefix takes precedence over
    /// a shorter exact match; an ambiguous exact match is never resolved by insertion order.
    /// </summary>
    public TriggerRecognition Recognize(string composedSuffix)
    {
        ArgumentNullException.ThrowIfNull(composedSuffix);
        if (composedSuffix.Length == 0) return new(TriggerRecognitionStatus.None);

        Snippet[]? best = null;
        var matchedLength = 0;
        foreach (var length in _lengthsDescending)
        {
            if (length > composedSuffix.Length) continue;
            if (!_triggers[length].GetAlternateLookup<ReadOnlySpan<char>>()
                .TryGetValue(composedSuffix.AsSpan(composedSuffix.Length - length), out var matches)) continue;
            best = matches;
            matchedLength = length;
            break;
        }

        // A longer trigger may have a partially observed prefix. Prefer waiting over
        // deleting text prematurely; a broken prefix never triggers a retroactive write.
        var maxPrefixLength = Math.Min(_maxTriggerLength - 1, composedSuffix.Length);
        var prefixes = _properPrefixes.GetAlternateLookup<ReadOnlySpan<char>>();
        for (var length = maxPrefixLength; length >= Math.Max(1, matchedLength); length--)
        {
            if (prefixes.Contains(composedSuffix.AsSpan(composedSuffix.Length - length)))
                return new(TriggerRecognitionStatus.WaitingForLongerTrigger);
        }

        if (best is null) return new(TriggerRecognitionStatus.None);
        return best.Length == 1
            ? new(TriggerRecognitionStatus.Matched, best[0], 1)
            : new(TriggerRecognitionStatus.Ambiguous, null, best.Length);
    }
}

public enum TriggerRecognitionStatus
{
    None,
    WaitingForLongerTrigger,
    Matched,
    Ambiguous,
    Suppressed
}

/// <summary>
/// Information only. A match is never a permission to delete or insert text on another app.
/// </summary>
public sealed record TriggerRecognition(
    TriggerRecognitionStatus Status,
    Snippet? Snippet = null,
    int CandidateCount = 0);
