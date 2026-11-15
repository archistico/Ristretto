using TextExpander.Application.Features.Input;

namespace TextExpander.Infrastructure.Platform.Windows;

/// <summary>
/// Esito testuale privo di testo, identificativi di finestre, PID o posizioni del cursore.
/// Una modifica plausibile non prova la provenienza dell'input e NON autorizza scritture.
/// </summary>
public enum ExternalClassicEditLiveStatus
{
    BaselineEstablished,
    CandidateInsertion,
    NoChange,
    DeltaRejected,
    TargetChanged,
    ObservationUnavailable
}

public sealed record ExternalClassicEditLiveStep(
    ExternalClassicEditLiveStatus Status,
    ExternalClassicEditTextDeltaStatus? DeltaStatus = null)
{
    public bool CanAuthorizeExpansion => false;
    public override string ToString() => nameof(ExternalClassicEditLiveStep);
}

/// <summary>
/// R5: composizione SENZA effetti collaterali di snapshot di un Edit esterno.
/// Deve essere usata in sequenza su un solo thread: nessuna osservazione può
/// attraversare un cambio di controllo, un errore di lettura o una barriera di focus.
/// Non alimenta TriggerActivationSession: la provenienza umana non è dimostrata.
/// Il testo rimane esclusivamente in RAM e viene rilasciato da Reset().
/// </summary>
public sealed class ExternalClassicEditLiveSession
{
    private ExternalClassicEditReadResult? _previous;

    public void Reset() => _previous = null;

    public ExternalClassicEditLiveStep Observe(ExternalClassicEditReadResult? snapshot)
    {
        if (snapshot is null || !snapshot.IsObserved)
        {
            Reset();
            return new(ExternalClassicEditLiveStatus.ObservationUnavailable);
        }

        if (_previous is null)
        {
            _previous = snapshot;
            return new(ExternalClassicEditLiveStatus.BaselineEstablished);
        }

        var previous = _previous;
        _previous = snapshot; // una lettura vecchia non deve essere riutilizzata dopo un rifiuto.
        if (previous.Target != snapshot.Target)
            return new(ExternalClassicEditLiveStatus.TargetChanged);

        var delta = ExternalClassicEditTextDeltaPolicy.Evaluate(previous.Document, snapshot.Document, sameTarget: true);
        // Un snapshot intrinsecamente non valido non diventa mai la baseline
        // della prossima osservazione, anche quando il controllo è lo stesso.
        if (delta.Status is ExternalClassicEditTextDeltaStatus.InvalidUnicode or
            ExternalClassicEditTextDeltaStatus.TooLong or
            ExternalClassicEditTextDeltaStatus.InvalidSelection or
            ExternalClassicEditTextDeltaStatus.SelectionNotCollapsed or
            ExternalClassicEditTextDeltaStatus.GraphemeBoundary)
            Reset();
        return new(delta.Status switch
        {
            ExternalClassicEditTextDeltaStatus.CandidateInsertion => ExternalClassicEditLiveStatus.CandidateInsertion,
            ExternalClassicEditTextDeltaStatus.NoChange => ExternalClassicEditLiveStatus.NoChange,
            _ => ExternalClassicEditLiveStatus.DeltaRejected
        }, delta.Status);
    }
}
