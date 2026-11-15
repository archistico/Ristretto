using TextExpander.Application.Features.Output;
using TextExpander.Core;
using TextExpander.Core.Matching;

namespace TextExpander.Application.Features.Input;

/// <summary>
/// Risultato effimero dell'osservazione nel SOLO editor di laboratorio posseduto.
/// La decisione Ready e' una decisione del matcher, NON il permesso di modificare
/// un editor esterno. Il ToString non deve esporre testo o contenuti degli snippet.
/// </summary>
public sealed record OwnedCommittedInputResult(
    CommittedEditDeltaRejection DeltaRejection,
    OwnedEditProvenanceRejection? ProvenanceRejection,
    TriggerActivationDecision? Activation)
{
    public bool HasTrustedObservation => DeltaRejection == CommittedEditDeltaRejection.None &&
        ProvenanceRejection == OwnedEditProvenanceRejection.None && Activation is not null;

    public override string ToString() => nameof(OwnedCommittedInputResult);
}

/// <summary>
/// R5: raccordo applicativo fra snapshot Unicode, prova dell'origine della modifica
/// e motore dei trigger. Nessun hook, SendInput, accesso agli appunti o logging.
/// Le prove di proprieta'/focus devono arrivare dal controllo Win32 POSSEDUTO:
/// non sono autorizzazioni per editor di terze parti.
/// Tutti i metodi devono essere eseguiti in sequenza sul thread proprietario.
/// </summary>
public sealed class OwnedCommittedInputBridge
{
    private readonly TriggerActivationSession _activation;
    private ReplacementTargetSnapshot? _previous;

    public OwnedCommittedInputBridge(LibrarySnapshot library, ReplacementTargetSnapshot initial)
    {
        ArgumentNullException.ThrowIfNull(library);
        ArgumentNullException.ThrowIfNull(initial);
        _activation = new TriggerActivationSession(library);
        _previous = initial;
    }

    public long LibraryRevision => _activation.LibraryRevision;

    /// <summary>
    /// Una barriera di focus, navigazione o contesto non osservabile svuota il
    /// riconoscimento e invalida la baseline. Il primo snapshot successivo
    /// stabilisce solo la nuova baseline: NON e' interpretato come digitazione.
    /// </summary>
    public void Invalidate()
    {
        _activation.Reset();
        _previous = null;
    }

    /// <summary>
    /// Il controllo proprietario puo' ancorare una nuova baseline. Nessun testo
    /// contenuto in quello snapshot viene riconosciuto retroattivamente.
    /// </summary>
    public void Rebase(ReplacementTargetSnapshot baseline)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        _activation.Reset();
        _previous = baseline;
    }

    public OwnedCommittedInputResult Observe(
        ReplacementTargetSnapshot current,
        OwnedEditObservationContext context,
        OwnedEditMutationEvidence? mutation,
        bool allowOwnedFixture,
        long monotonicMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(context);
        if (_previous is null)
        {
            // Nessuna interpretazione del salto dalla finestra precedente.
            _previous = current;
            _activation.Reset();
            return new(CommittedEditDeltaRejection.MissingSnapshot, null, null);
        }

        var before = _previous;
        _previous = current; // Non riutilizzare uno snapshot vecchio dopo un rifiuto.
        var delta = CommittedEditDeltaVerifier.Verify(before, current, context);
        if (delta.Rejection == CommittedEditDeltaRejection.NoChange)
            return new(delta.Rejection, null, null);
        if (!delta.IsVerified)
        {
            _activation.Reset();
            return new(delta.Rejection, null, null);
        }

        var provenance = OwnedEditProvenancePolicy.Evaluate(current, mutation, allowOwnedFixture);
        if (!provenance.Accepted)
        {
            _activation.Reset();
            return new(delta.Rejection, provenance.Rejection, null);
        }

        var insertion = delta.Insertion!;
        var activation = _activation.Observe(insertion.Observation,
            monotonicMilliseconds, insertion.InitialBoundary);
        return new(delta.Rejection, provenance.Rejection, activation);
    }
}
