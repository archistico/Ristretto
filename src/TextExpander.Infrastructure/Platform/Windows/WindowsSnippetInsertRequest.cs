namespace TextExpander.Infrastructure.Platform.Windows;

/// <summary>A direct hotkey never activates another window. Wait for release on the captured target,
/// preserve concurrent clipboard changes, and use the same one-shot paste checks as the picker.</summary>
public sealed class WindowsSnippetInsertRequest
{
    private readonly WindowsPickerPasteService _service;
    private readonly WindowsPickerPasteTarget _target;
    private readonly WindowsPickerInsertRequest _insert;
    private readonly uint _clipboardAtHotkey;
    private readonly Func<long> _milliseconds;
    private int _started;
    private int _cancelled;

    public WindowsSnippetInsertRequest(WindowsPickerPasteService service, WindowsPickerPasteTarget target,
        uint clipboardAtHotkey, Func<long>? milliseconds = null)
    {
        _service = service;
        _target = target;
        _clipboardAtHotkey = clipboardAtHotkey;
        _milliseconds = milliseconds ?? (() => Environment.TickCount64);
        _insert = new(service, target, restoreFocus: false);
    }

    public void Cancel() { Interlocked.Exchange(ref _cancelled, 1); _insert.Cancel(); }

    public async Task<PickerPasteResult> ExecuteAsync(Func<Task<uint>> prepareClipboard,
        Func<bool> isTriggerKeyDown, Func<Task> waitForRelease)
    {
        if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
            return new(PickerPasteStatus.Rejected, "request_already_used");
        var deadline = _milliseconds() + 2000;
        for (var attempt = 0; attempt < 100 && _milliseconds() <= deadline; attempt++)
        {
            if (Volatile.Read(ref _cancelled) != 0) return new(PickerPasteStatus.Rejected, "request_cancelled");
            var preflight = _service.Preflight(_target);
            if (preflight is not null && preflight.Reason != "modifier_pressed") return preflight;
            if (!_service.ClipboardStillAt(_clipboardAtHotkey))
                return new(PickerPasteStatus.Rejected, "clipboard_changed_before_preparation");
            if (preflight is null && !isTriggerKeyDown())
            {
                return await _insert.ExecuteAsync(() => _service.ClipboardStillAt(_clipboardAtHotkey)
                    ? prepareClipboard() : Task.FromResult(0u), () => Task.CompletedTask);
            }
            await waitForRelease();
        }
        return new(PickerPasteStatus.Rejected, "hotkey_release_timeout");
    }
}
