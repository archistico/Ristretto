namespace TextExpander.Desktop;

/// <summary>
/// Models a single unmodified Enter key gesture without touching the window,
/// clipboard or native input. Key repeat never starts a second insertion.
/// An Enter pressed while results are loading remains eligible until release;
/// the caller must check the current selection and insertion permission then.
/// </summary>
public sealed class PickerEnterGesture
{
    private bool _down;
    private bool _armed;
    private string? _deferredQuery;

    public bool IsDown => _down;

    public void Press(bool noModifiers)
    {
        if (_down) return;
        _down = true;
        _armed = noModifiers;
    }

    public bool Release(bool noModifiers)
    {
        if (!_down) return false; // Stray/duplicate KeyUp must not clear a deferred Enter.
        var canSubmit = _armed && noModifiers;
        _down = false;
        _armed = false;
        return canSubmit;
    }

    /// <summary>Remember a released Enter while the picker is loading.
    /// Never replay if focus, recipient, query or insert eligibility changed.</summary>
    public void DeferUntilLoaded(string query) => _deferredQuery = query;

    public bool ConsumeDeferred(string currentQuery, bool pickerActive, bool canInsert)
    {
        var pendingQuery = _deferredQuery;
        _deferredQuery = null;
        return pendingQuery is not null && pickerActive && canInsert &&
            string.Equals(pendingQuery, currentQuery, StringComparison.Ordinal);
    }

    public void Reset()
    {
        _down = false;
        _armed = false;
        _deferredQuery = null;
    }
}
