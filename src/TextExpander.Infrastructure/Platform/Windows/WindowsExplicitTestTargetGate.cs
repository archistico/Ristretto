namespace TextExpander.Infrastructure.Platform.Windows;

/// <summary>
/// F2a: barriera pura per il SOLO laboratorio Windows con controllo EDIT creato da Ristretto.
/// Non e' un'autorizzazione generale a iniettare input nelle applicazioni dell'utente.
/// L'identita' viene verificata nuovamente prima dell'invio, che mantiene i suoi preflight.
/// </summary>
public sealed record WindowsExplicitTestTargetSnapshot(
    nint ExpectedWindow,
    nint ObservedForegroundWindow,
    uint ExpectedProcessId,
    uint ObservedProcessId,
    nint ExpectedEditControl,
    nint ObservedFocusedControl,
    bool IsClassicEditControl,
    bool IsVisible,
    bool IsEnabled,
    bool IsPassword,
    bool IsReadOnly,
    bool IsComposingOrUnknown);

public static class WindowsExplicitTestTargetGate
{
    /// <summary>Null significa solo che questo snapshot passa il preflight; NON prova l'output.</summary>
    public static string? RejectionReason(WindowsExplicitTestTargetSnapshot? snapshot)
    {
        if (snapshot is null) return "snapshot_missing";
        if (snapshot.ExpectedWindow == 0 || snapshot.ExpectedEditControl == 0 ||
            snapshot.ExpectedProcessId == 0) return "target_invalid";
        if (snapshot.ObservedForegroundWindow != snapshot.ExpectedWindow ||
            snapshot.ObservedProcessId != snapshot.ExpectedProcessId) return "target_changed";
        if (snapshot.ObservedFocusedControl != snapshot.ExpectedEditControl) return "focus_changed";
        if (!snapshot.IsClassicEditControl) return "unsupported_control";
        if (!snapshot.IsVisible || !snapshot.IsEnabled) return "control_unavailable";
        if (snapshot.IsPassword) return "password_control";
        if (snapshot.IsReadOnly) return "read_only_control";
        if (snapshot.IsComposingOrUnknown) return "composition_unknown";
        return null;
    }
}
