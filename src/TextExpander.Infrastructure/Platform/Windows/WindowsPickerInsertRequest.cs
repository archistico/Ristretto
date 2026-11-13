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

    public WindowsPickerInsertRequest(WindowsPickerPasteService service, WindowsPickerPasteTarget target)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public void Cancel() => Interlocked.Exchange(ref _cancelled, 1);
    private bool IsCancelled => Volatile.Read(ref _cancelled) != 0;

    /// <param name="prepareClipboard">Write the selected snippet, then return its clipboard sequence.</param>
    /// <param name="settleFocus">Await the UI's focus transition without performing input.</param>
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
            var sequence = await prepareClipboard();
            if (IsCancelled) return Cancelled();
            if (sequence == 0) return new(PickerPasteStatus.Rejected, "clipboard_unverified");
            await settleFocus();
            if (IsCancelled) return Cancelled();
            dispatchAttempted = true;
            return _service.Paste(_target, sequence);
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
