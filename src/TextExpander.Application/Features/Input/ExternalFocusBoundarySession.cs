using TextExpander.Core.Matching;

namespace TextExpander.Application.Features.Input;

/// <summary>
/// Segnali di CONTINUITÀ DEL CONTESTO, non eventi di testo. Nessuno di questi
/// prova cosa sia stato digitato né autorizza una sostituzione in un editor.
/// </summary>
public enum ExternalFocusBoundaryKind
{
    ForegroundChanged,
    ControlFocusChanged,
    ObservationUnavailable,
    ObserverStopped
}

/// <summary>
/// Risposta senza dati del destinatario. Non conserva HWND, PID, titoli, offset,
/// tasti, testo o contenuti degli appunti.
/// </summary>
public sealed record ExternalFocusBoundaryReceipt(
    ExternalFocusBoundaryKind Reason,
    long BoundaryCount,
    bool RecognitionCleared,
    bool CanAuthorizeExternalExpansion)
{
    public override string ToString() => nameof(ExternalFocusBoundaryReceipt);
}

/// <summary>
/// R5: un cambio di finestra/focus è una barriera irrevocabile per il suffisso
/// temporaneo. L'osservatore Windows deve serializzare i callback prima di
/// chiamare questo oggetto. È intenzionalmente indipendente da Win32/Avalonia.
/// Non produce CommittedInputObservation: i soli eventi WinEvent non sono testo.
/// </summary>
public sealed class ExternalFocusBoundarySession
{
    private readonly TriggerActivationSession _activation;
    private readonly CommittedInputSession _recognition;

    public ExternalFocusBoundarySession(
        TriggerActivationSession activation,
        CommittedInputSession recognition)
    {
        _activation = activation ?? throw new ArgumentNullException(nameof(activation));
        _recognition = recognition ?? throw new ArgumentNullException(nameof(recognition));
    }

    public long BoundaryCount { get; private set; }

    public ExternalFocusBoundaryReceipt Observe(ExternalFocusBoundaryKind reason)
    {
        if (!Enum.IsDefined(reason))
            throw new ArgumentOutOfRangeException(nameof(reason));

        // Anche un segnale duplicato o non attribuibile richiede reset.
        // Non riutilizzare mai input parziale tra due contesti differenti.
        _activation.Reset();
        _recognition.Reset();
        if (BoundaryCount < long.MaxValue) BoundaryCount++;
        return new ExternalFocusBoundaryReceipt(reason, BoundaryCount, true, false);
    }
}
