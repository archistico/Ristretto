using TextExpander.Application.Features.Input;
using TextExpander.Core;
using TextExpander.Core.Matching;

namespace TextExpander.Infrastructure.Platform.Windows;

/// <summary>
/// Esito di una simulazione su testo ESTERNO non attribuito. Non contiene snippet,
/// testo, HWND, PID, posizione del cursore o alcun permesso di scrittura.
/// </summary>
public enum ExternalClassicEditTriggerPreviewStatus
{
    BaselineEstablished,
    InputCandidate,
    PotentialTrigger,
    AmbiguousTrigger,
    NoChange,
    DeltaRejected,
    TargetChanged,
    ObservationUnavailable
}

public sealed record ExternalClassicEditTriggerPreviewStep(ExternalClassicEditTriggerPreviewStatus Status)
{
    public bool CanAuthorizeExpansion => false;
    public override string ToString() => nameof(ExternalClassicEditTriggerPreviewStep);
}

/// <summary>
/// R5 - SIMULAZIONE NON AFFIDABILE: applica la politica dei trigger ai delta
/// osservati in un Edit Win32 esterno, esclusivamente per una prova volontaria.
/// WinEvent non dimostra digitazione: il delta potrebbe provenire da appunti,
/// IME o codice. Il matcher è privato e il risultato non restituisce Snippet.
/// NON usare mai questa classe come sorgente di permessi per R6 o SendInput.
/// Tutte le chiamate devono essere serializzate sul thread dell'osservatore.
/// </summary>
public sealed class ExternalClassicEditTriggerPreviewSession
{
    private readonly ExternalClassicEditLiveSession _live = new();
    private readonly TriggerActivationSession _simulation;
    private ExternalClassicEditReadResult? _previous;

    public ExternalClassicEditTriggerPreviewSession(LibrarySnapshot syntheticCatalog)
    {
        ArgumentNullException.ThrowIfNull(syntheticCatalog);
        _simulation = new TriggerActivationSession(syntheticCatalog);
    }

    public void Reset()
    {
        _live.Reset();
        _simulation.Reset();
        _previous = null;
    }

    public ExternalClassicEditTriggerPreviewStep Observe(ExternalClassicEditReadResult? snapshot,
        long monotonicMilliseconds)
    {
        var before = _previous;
        var observed = _live.Observe(snapshot);
        if (observed.Status == ExternalClassicEditLiveStatus.ObservationUnavailable)
        {
            Reset();
            return new(ExternalClassicEditTriggerPreviewStatus.ObservationUnavailable);
        }

        _previous = snapshot;
        switch (observed.Status)
        {
            case ExternalClassicEditLiveStatus.BaselineEstablished:
                _simulation.Reset();
                return new(ExternalClassicEditTriggerPreviewStatus.BaselineEstablished);
            case ExternalClassicEditLiveStatus.TargetChanged:
                _simulation.Reset();
                return new(ExternalClassicEditTriggerPreviewStatus.TargetChanged);
            case ExternalClassicEditLiveStatus.NoChange:
                return new(ExternalClassicEditTriggerPreviewStatus.NoChange);
            case ExternalClassicEditLiveStatus.DeltaRejected:
                _simulation.Reset();
                return new(ExternalClassicEditTriggerPreviewStatus.DeltaRejected);
            case ExternalClassicEditLiveStatus.CandidateInsertion:
                break;
            default:
                Reset();
                return new(ExternalClassicEditTriggerPreviewStatus.ObservationUnavailable);
        }

        if (before?.Document is null || snapshot?.Document is null ||
            before.Target is null || snapshot.Target is null || before.Target != snapshot.Target)
        {
            Reset();
            return new(ExternalClassicEditTriggerPreviewStatus.ObservationUnavailable);
        }

        // La politica del delta ha già controllato identità, caret, UTF-16,
        // assenza di selezione, suffisso immutato e dimensioni limitate.
        var offset = before.Document.SelectionEnd;
        var insertedLength = snapshot.Document.Text.Length - before.Document.Text.Length;
        if (offset < 0 || insertedLength <= 0 ||
            offset > snapshot.Document.Text.Length - insertedLength)
        {
            Reset();
            return new(ExternalClassicEditTriggerPreviewStatus.DeltaRejected);
        }

        var inserted = snapshot.Document.Text.Substring(offset, insertedLength);
        var boundary = offset == 0 || char.IsWhiteSpace(before.Document.Text[offset - 1])
            ? InitialWordBoundaryEvidence.Verified
            : InitialWordBoundaryEvidence.Unknown;

        // Si usa un'osservazione SIMULATA soltanto all'interno di questo oggetto.
        // Non è una prova di provenienza e NON può raggiungere il motore di scrittura.
        var decision = _simulation.Observe(
            new CommittedInputObservation(1, inserted,
                TextObservationKind.VerifiedCommitted,
                ObservedFieldSafety.VerifiedNormalEditable),
            monotonicMilliseconds, boundary);

        return new(decision.Status switch
        {
            TriggerActivationStatus.Ready => ExternalClassicEditTriggerPreviewStatus.PotentialTrigger,
            TriggerActivationStatus.Ambiguous => ExternalClassicEditTriggerPreviewStatus.AmbiguousTrigger,
            TriggerActivationStatus.Suppressed => ExternalClassicEditTriggerPreviewStatus.DeltaRejected,
            _ => ExternalClassicEditTriggerPreviewStatus.InputCandidate
        });
    }
}
