namespace TextExpander.Application.Features.Hotkeys;

/// <summary>Call on the UI context. Notifications remain suppressed through pause, dialog and
/// restoration, including cancellation or a failed pause. No settings are persisted here.</summary>
public sealed class HotkeyCaptureCoordinator
{
    private volatile bool _active;
    public bool IsActive => _active;

    public async Task<T> RunAsync<T>(Func<Task> pause, Func<Task> restore, Func<Task<T>> capture)
    {
        if (IsActive) throw new InvalidOperationException("Acquisizione hotkey già in corso.");
        _active = true;
        try
        {
            await pause();
            return await capture();
        }
        finally
        {
            try { await restore(); }
            finally { _active = false; }
        }
    }
}
