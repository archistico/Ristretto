namespace TextExpander.Infrastructure.Platform.Windows;

/// <summary>
/// One explicit Insert click for one captured picker session. A request cannot
/// be reused, even after rejection or an unknown effect. Cancel on dismissal or
/// a newer picker session; cancellation is checked at every async boundary.
/// Clipboard contents already written by the caller are not rolled back.
/// </summary>
public sealed class WindowsPickerInsertRequest
{
    private readonly WindowsPickerPasteService _service;
    private readonly WindowsPickerPasteTarget _target;
    private int _started;
    private int _cancelled;
    // Atomic handoff between cancellation and the first native paste attempt.
    // 0 = still cancellable; 1 = cancelled before dispatch; 2 = dispatch reserved.
    // The reservation is one-way: even an unknown native result must never retry.
    private int _dispatchDecision;

    public WindowsPickerInsertRequest(WindowsPickerPasteService service, WindowsPickerPasteTarget target)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public void Cancel()
    {
        // Linearize cancellation FIRST, against the exact same atomic value
        // used by the dispatch reservation. A separate cancellation flag is
        // published only afterwards for the earlier asynchronous boundaries.
        Interlocked.CompareExchange(ref _dispatchDecision, 1, 0);
        Interlocked.Exchange(ref _cancelled, 1);
        // If dispatch already won, the native effect cannot be undone. The
        // picker's generation still prevents an obsolete UI continuation.
    }

    private bool TryReserveDispatch() =>
        Interlocked.CompareExchange(ref _dispatchDecision, 2, 0) == 0;
    private bool IsCancelled => Volatile.Read(ref _cancelled) != 0;

    /// <param name="prepareClipboard">Write the selected snippet only after the focus and
    /// clipboard preflight, then return its verified clipboard sequence.</param>
    /// <param name="settleFocus">Await the UI's focus transition before writing to the clipboard.</param>
    public async Task<PickerPasteResult> ExecuteAsync(Func<Task<uint>> prepareClipboard, Func<Task> settleFocus)
    {
        ArgumentNullException.ThrowIfNull(prepareClipboard);
        ArgumentNullException.ThrowIfNull(settleFocus);
        if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
            return new(PickerPasteStatus.Rejected, "request_already_used");
        var dispatchAttempted = false;
        try
        {
            if (IsCancelled) return Cancelled();
            if (!_service.TryActivate(_target)) return new(PickerPasteStatus.Rejected, "activation_failed");
            if (IsCancelled) return Cancelled();
            // Capture the clipboard version before any asynchronous focus wait.
            // Do not overwrite a concurrent third-party clipboard change.
            var beforeFocus = _service.ClipboardSequence();
            if (beforeFocus == 0)
                return new(PickerPasteStatus.Rejected, "clipboard_unverified");
            await settleFocus();
            if (IsCancelled) return Cancelled();
            // SetForegroundWindow can return success without producing focus.
            // This gate runs BEFORE SetTextAsync, so a lost target does not
            // needlessly overwrite the user's clipboard.
            var preflight = _service.Preflight(_target);
            if (preflight is not null) return preflight;
            if (!_service.ClipboardStillAt(beforeFocus))
                return new(PickerPasteStatus.Rejected, "clipboard_changed_before_preparation");
            if (IsCancelled) return Cancelled();
            var sequence = await prepareClipboard();
            if (IsCancelled) return Cancelled();
            if (sequence == 0) return new(PickerPasteStatus.Rejected, "clipboard_unverified");
            dispatchAttempted = true;
            // The service checks the reservation after its LAST native preflight,
            // immediately before SendInput. Cancellation that wins first means
            // no input; cancellation after reservation cannot retract input.
            return _service.Paste(_target, sequence, TryReserveDispatch);
        }
        catch (Exception)
        {
            // No logging of clipboard text or native error messages. Never retry:
            // an exception after entering Paste may have followed a native effect.
            return dispatchAttempted
                ? new(PickerPasteStatus.PartialOrUnknown, "native_error_unknown_effect")
                : new(PickerPasteStatus.Rejected, IsCancelled ? "request_cancelled" : "preparation_failed");
        }
    }

    private static PickerPasteResult Cancelled() => new(PickerPasteStatus.Rejected, "request_cancelled");
}
