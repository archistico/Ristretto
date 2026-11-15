using System.Diagnostics;
using System.Globalization;
using System.Text;
using TextExpander.Application.Features.Input;
using TextExpander.Application.Features.Output;
using TextExpander.Core;
using TextExpander.Core.Matching;

namespace Ristretto.WindowsOutputLab;

/// <summary>
/// Osservatore SOLO del controllo EDIT posseduto dal laboratorio. Il raccordo
/// applicativo verifica delta Unicode e provenienza della revisione prima di
/// consegnare il testo al matcher. Nessun input globale viene intercettato.
/// </summary>
internal sealed class OwnedEditRecognitionSession
{
    private readonly OwnedEditTarget _target;
    private readonly OwnedCommittedInputBridge _input;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly bool _requireHumanFocus;

    public OwnedEditRecognitionSession(OwnedEditTarget target, LibrarySnapshot library, bool requireHumanFocus = false)
    {
        _target = target;
        _requireHumanFocus = requireHumanFocus;
        _input = new OwnedCommittedInputBridge(library, target.ReadSnapshot());
    }

    public TriggerActivationStatus LastRecognition { get; private set; }
    public ReplacementRejection? LastPreparation { get; private set; }
    public CommittedEditDeltaRejection? LastInputRejection { get; private set; }
    public OwnedEditProvenanceRejection? LastProvenanceRejection { get; private set; }

    public ReplacementExecutionOutcome? ObserveCurrent()
    {
        var current = _target.ReadSnapshot();
        var focusVerified = !_requireHumanFocus || _target.HasHumanInputFocus;
        var result = _input.Observe(current,
            new OwnedEditObservationContext(
                OwnsControl: true, InputFocusVerified: focusVerified,
                CompositionStateKnown: true, IsComposing: false,
                ModifiersPressed: _requireHumanFocus && _target.HasHumanInputModifiers),
            _target.LastMutation,
            allowOwnedFixture: !_requireHumanFocus,
            monotonicMilliseconds: _clock.ElapsedMilliseconds);
        LastInputRejection = result.DeltaRejection;
        LastProvenanceRejection = result.ProvenanceRejection;
        if (result.DeltaRejection == CommittedEditDeltaRejection.NoChange) return null;
        if (!result.HasTrustedObservation)
        {
            LastRecognition = TriggerActivationStatus.Suppressed;
            return null;
        }

        var decision = result.Activation!;
        LastRecognition = decision.Status;
        if (decision is not { Status: TriggerActivationStatus.Ready, Snippet: not null }) return null;
        var raw = ResolveRawSuffix(current, decision.Snippet.Trigger, out var delimiter);
        if (raw is null)
        {
            LastPreparation = ReplacementRejection.InvalidRawMatch;
            _input.Rebase(current);
            return null;
        }
        var origin = new ReplacementOrigin(current.TargetToken, current.TargetEpoch, current.TextRevision, _input.LibraryRevision);
        var prepared = TriggerReplacementPlanner.Prepare(new(decision, origin, current, raw, delimiter), _input.LibraryRevision);
        LastPreparation = prepared.Rejection;
        if (!prepared.IsReady)
        {
            _input.Rebase(current);
            return null;
        }
        var evidence = new ExpansionAdmissionEvidence(
            ExpansionTargetScope.OwnedCooperatingLaboratory,
            ExpansionInputProvenance.VerifiedCommittedTextDelta,
            current, ExplicitOptIn: true, Paused: false, ProcessExcluded: false,
            FocusVerified: !_requireHumanFocus || _target.HasHumanInputFocus,
            CompositionStateKnown: true, IsComposing: false,
            ModifiersPressed: _requireHumanFocus && _target.HasHumanInputModifiers);
        var outcome = QualifiedReplacementDispatcher.Execute(prepared.Plan!, _target, evidence,
            _input.LibraryRevision).Execution;
        // Il risultato generato dall'espansione non deve diventare nuovo input.
        _input.Rebase(_target.ReadSnapshot());
        return outcome;
    }

    internal static string? ResolveRawSuffix(ReplacementTargetSnapshot snapshot, string trigger, out string delimiter)
    {
        delimiter = "";
        if (snapshot.Text is not { } text || snapshot.SelectionEnd <= 0 || snapshot.SelectionEnd > text.Length) return null;
        var caret = snapshot.SelectionEnd;
        var boundaries = StringInfo.ParseCombiningCharacters(text[..caret]);
        if (boundaries.Length == 0) return null;
        var delimiterStart = boundaries[^1];
        delimiter = text[delimiterStart..caret];
        if (!Rune.TryGetRuneAt(delimiter, 0, out var rune) || !Rune.IsWhiteSpace(rune)) return null;
        var canonical = trigger.Normalize(NormalizationForm.FormC);
        string? found = null;
        foreach (var boundary in boundaries)
        {
            if (boundary >= delimiterStart || delimiterStart - boundary > TriggerReplacementPlanner.MaxRawMatchUtf16Units) continue;
            var candidate = text[boundary..delimiterStart];
            if (!string.Equals(candidate.Normalize(NormalizationForm.FormC), canonical, StringComparison.Ordinal)) continue;
            if (found is not null) return null;
            found = candidate;
        }
        return found;
    }
}
