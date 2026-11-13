using TextExpander.Application.Features.Output;

namespace TextExpander.Infrastructure.Simulation;

/// <summary>
/// Managed test editor only: never represents or modifies an OS window.
/// The lock makes comparison, editing and the verification receipt atomic in this simulation.
/// </summary>
public sealed class SimulatedReplacementTarget : IConditionalReplacementTarget
{
    private readonly object _sync = new();
    private ReplacementTargetSnapshot _snapshot;

    public SimulatedReplacementTarget(ReplacementTargetSnapshot initial) =>
        _snapshot = initial ?? throw new ArgumentNullException(nameof(initial));

    public ReplacementTargetSnapshot ReadSnapshot() { lock (_sync) return _snapshot; }

    /// <summary>Simulates user edits, selection, protection or focus changes; no native I/O.</summary>
    public void SetSnapshot(ReplacementTargetSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (_sync) _snapshot = snapshot;
    }

    public ConditionalReplacementResult CompareAndReplace(TriggerReplacementPlan plan, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(plan);
        lock (_sync)
        {
            if (cancellationToken.IsCancellationRequested || !plan.MatchesExpectedTarget(_snapshot))
                return new(ConditionalReplacementStatus.RejectedNoChange);
            var original = _snapshot.Text!;
            var changed = original.Remove(plan.RangeStart, plan.RangeLength).Insert(plan.RangeStart, plan.ReplacementText);
            var caret = _snapshot.SelectionEnd - plan.RangeLength + plan.ReplacementText.Length;
            _snapshot = _snapshot with
            {
                Text = changed, TextRevision = checked(_snapshot.TextRevision + 1),
                SelectionStart = caret, SelectionEnd = caret
            };
            return new(ConditionalReplacementStatus.Applied, _snapshot);
        }
    }
}
