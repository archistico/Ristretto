using TextExpander.Infrastructure.Platform.Windows;

namespace TextExpander.Desktop;

/// <summary>
/// Coordinates the lifetime of one explicit picker-to-editor insert request.
/// A repeated invocation while the picker itself is active must NOT erase its
/// original external target. A new invocation after switching to another
/// application starts a new session and cancels every pending old operation.
/// No window handles or clipboard contents are retained beyond the session.
/// </summary>
public sealed class PickerSessionController
{
    private WindowsPickerInsertRequest? _request;
    private long _generation;
    private int _insertionInProgress;

    public WindowsPickerInsertRequest? Request => _request;
    public long Generation => _generation;
    public bool IsInsertionInProgress => Volatile.Read(ref _insertionInProgress) != 0;

    /// <summary>Prevents a late picker hotkey from invalidating a paste already
    /// requested by Enter or Insert. Explicit cancellation still takes priority.</summary>
    public bool TryBeginInsertion(long generation, WindowsPickerInsertRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!IsCurrent(generation, request) ||
            Interlocked.CompareExchange(ref _insertionInProgress, 1, 0) != 0)
            return false;
        if (IsCurrent(generation, request)) return true;
        Interlocked.Exchange(ref _insertionInProgress, 0);
        return false;
    }

    public void CompleteInsertion(long generation, WindowsPickerInsertRequest request)
    {
        if (IsCurrent(generation, request))
            Interlocked.Exchange(ref _insertionInProgress, 0);
    }

    public PickerSessionOpenResult Open(bool pickerVisible, bool pickerActive,
        Func<WindowsPickerInsertRequest?> capture)
    {
        ArgumentNullException.ThrowIfNull(capture);
        if (IsInsertionInProgress)
            return PickerSessionOpenResult.InsertionInProgress;
        if (pickerVisible && pickerActive)
            return PickerSessionOpenResult.Reused;

        Cancel();
        // A missing / unqualified external target is a valid new session:
        // Copy works, Insert remains unavailable.
        try { _request = capture(); }
        catch (Exception)
        {
            // Native focus/foreground inspection is best-effort: never crash
            // the UI or retain a stale target when it fails.
            _request = null;
        }
        return PickerSessionOpenResult.Started;
    }

    public bool IsCurrent(long generation) => generation == _generation;

    public bool IsCurrent(long generation, WindowsPickerInsertRequest request) =>
        generation == _generation && ReferenceEquals(_request, request);

    public void Cancel()
    {
        _request?.Cancel();
        Interlocked.Exchange(ref _insertionInProgress, 0);
        _request = null;
        unchecked { _generation++; }
    }
}

public enum PickerSessionOpenResult { Started, Reused, InsertionInProgress }
