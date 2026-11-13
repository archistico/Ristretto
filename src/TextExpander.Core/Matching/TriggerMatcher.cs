using System.Text;

namespace TextExpander.Core.Matching;

public enum MatchStatus { None, Matched, Ambiguous }

public sealed record TriggerMatch(MatchStatus Status, Snippet? Snippet = null, int CandidateCount = 0);

/// <summary>
/// Riconoscimento puro dimostrativo su testo già composto. Non simula la cancellazione.
/// Non esegue match durante la composizione IME: spetta al futuro adapter segnalarla.
/// </summary>
public static class TriggerMatcher
{
    public static TriggerMatch MatchSuffix(string committedText, LibrarySnapshot library)
    {
        ArgumentNullException.ThrowIfNull(committedText);
        ArgumentNullException.ThrowIfNull(library);
        var composed = committedText.Normalize(NormalizationForm.FormC);
        var enabledGroups = library.Groups.Where(x => x.IsEnabled).Select(x => x.Id).ToHashSet();
        var matches = library.Snippets
            .Where(x => x.IsEnabled && enabledGroups.Contains(x.GroupId) &&
                        composed.EndsWith(LibraryRules.CanonicalTrigger(x.Trigger), StringComparison.Ordinal))
            .OrderByDescending(x => LibraryRules.CanonicalTrigger(x.Trigger).Length)
            .ToList();
        if (matches.Count == 0) return new(MatchStatus.None);
        var longest = LibraryRules.CanonicalTrigger(matches[0].Trigger).Length;
        var best = matches.Where(x => LibraryRules.CanonicalTrigger(x.Trigger).Length == longest).ToList();
        return best.Count == 1
            ? new(MatchStatus.Matched, best[0], 1)
            : new(MatchStatus.Ambiguous, null, best.Count);
    }
}
