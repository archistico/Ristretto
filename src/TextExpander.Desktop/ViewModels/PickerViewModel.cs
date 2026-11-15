using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TextExpander.Application.Abstractions;
using TextExpander.Application.Features.Picker;
using TextExpander.Desktop.Localization;

namespace TextExpander.Desktop.ViewModels;

public sealed class PickerViewModel(ILibraryStore store, UiStrings texts) : INotifyPropertyChanged
{
    // Called on the UI dispatcher. Only index construction runs on a worker;
    // all collection/property publication resumes on the caller's UI context.
    private PickerSearchIndex? _index;
    private CancellationTokenSource? _pendingReload;
    private long _reloadGeneration;
    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<PickerSearch.Result> Results { get; } = [];

    private UiStrings _texts = texts;
    public UiStrings Texts { get => _texts; private set => Set(ref _texts, value); }
    public void UpdateLanguage(UiStrings value)
    {
        var previousPending = Texts.PickerOperationPending;
        Texts = value;
        if (IsClipboardBusy && !IsLoading && Status == previousPending)
            Status = Texts.PickerOperationPending;
    }

    private string _query = "";
    public string Query
    {
        get => _query;
        set { if (Set(ref _query, value)) Filter(); }
    }

    private PickerSearch.Result? _selected;
    public PickerSearch.Result? Selected
    {
        get => _selected;
        set { if (Set(ref _selected, value)) { Changed(nameof(HasSelection)); Changed(nameof(CanCopy)); Changed(nameof(CanInsert)); } }
    }
    public bool HasSelection => !IsLoading && Selected is { } chosen && Results.Contains(chosen);
    public bool CanCopy => HasSelection && !IsClipboardBusy;

    private bool _isClipboardBusy;
    public bool IsClipboardBusy
    {
        get => _isClipboardBusy;
        private set
        {
            if (!Set(ref _isClipboardBusy, value)) return;
            Changed(nameof(CanCopy));
            Changed(nameof(CanInsert));
        }
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (!Set(ref _isLoading, value)) return;
            Changed(nameof(HasSelection));
            Changed(nameof(CanCopy));
            Changed(nameof(CanInsert));
        }
    }

    private bool _canInsert;
    public bool CanInsert
    {
        get => _canInsert && HasSelection && !IsClipboardBusy;
        private set => Set(ref _canInsert, value);
    }

    public void SetInsertAvailable(bool value) => CanInsert = value;

    /// <summary>Serialize clipboard commands and publish only to the originating selection/session.
    /// A write already dispatched to the clipboard cannot be revoked by dismissal.</summary>
    public async Task CopySelectedAsync(Func<string, Task> writeClipboard)
    {
        ArgumentNullException.ThrowIfNull(writeClipboard);
        if (!CanCopy || Selected is not { } chosen) return;
        var generation = Volatile.Read(ref _reloadGeneration);
        IsClipboardBusy = true;
        try
        {
            if (!IsCurrentSelection(generation, chosen)) return;
            Status = Texts.PickerOperationPending;
            if (!IsCurrentSelection(generation, chosen)) return;
            await writeClipboard(chosen.Content);
            if (IsCurrentSelection(generation, chosen)) Status = Texts.PickerCopied;
        }
        catch (Exception)
        {
            // Adapter exceptions may contain paths or clipboard contents.
            if (IsCurrentSelection(generation, chosen)) Status = Texts.PickerCopyFailed;
        }
        finally { FinishClipboardOperation(); }
    }

    /// <summary>Hold the same clipboard gate through activation, preparation and native dispatch.
    /// The callback reports the native outcome; normal completion is not proof of a paste.</summary>
    public async Task InsertSelectedAsync(Func<string, Task> insert)
    {
        ArgumentNullException.ThrowIfNull(insert);
        if (!CanInsert || Selected is not { } chosen) return;
        var generation = Volatile.Read(ref _reloadGeneration);
        IsClipboardBusy = true;
        try
        {
            if (!IsCurrentSelection(generation, chosen)) return;
            SetInsertAvailable(false);
            if (!IsCurrentSelection(generation, chosen)) return;
            Status = Texts.PickerOperationPending;
            if (!IsCurrentSelection(generation, chosen)) return;
            await insert(chosen.Content);
        }
        catch (Exception)
        {
            // A native effect may have occurred. Do not rearm Insert or retry.
            if (generation == Volatile.Read(ref _reloadGeneration)) Status = Texts.PickerInsertUncertain;
        }
        finally { FinishClipboardOperation(); }
    }

    /// <summary>
    /// Navigazione dei risultati filtrati con le frecce. Nessuna operazione
    /// muta la selezione mentre è in corso una copia o un inserimento.
    /// </summary>
    public void MoveSelection(int direction)
    {
        if (direction is not (-1 or 1)) throw new ArgumentOutOfRangeException(nameof(direction));
        if (IsLoading || IsClipboardBusy || Results.Count == 0) return;

        var current = Selected is { } selected ? Results.IndexOf(selected) : -1;
        var next = current < 0
            ? direction > 0 ? 0 : Results.Count - 1
            : Math.Clamp(current + direction, 0, Results.Count - 1);
        Selected = Results[next];
    }

    /// <summary>
    /// Invio inserisce il risultato evidenziato con mouse/frecce; in mancanza
    /// di selezione usa il primo risultato. Usa lo stesso percorso monouso
    /// e le stesse protezioni del pulsante Inserisci.
    /// </summary>
    public async Task InsertForEnterAsync(Func<string, Task> insert)
    {
        ArgumentNullException.ThrowIfNull(insert);
        if (!_canInsert || IsLoading || IsClipboardBusy || Results.Count == 0) return;
        if (!HasSelection) Selected = Results[0];
        await InsertSelectedAsync(insert);
    }

    private bool IsCurrentSelection(long generation, PickerSearch.Result chosen) =>
        generation == Volatile.Read(ref _reloadGeneration) && HasSelection && Equals(Selected, chosen);

    private void FinishClipboardOperation()
    {
        IsClipboardBusy = false;
        if (!IsClipboardBusy && !IsLoading && Status == Texts.PickerOperationPending)
            Status = Texts.PickerManualNotice;
    }

    private string _status = "";
    public string Status { get => _status; set => Set(ref _status, value); }

    private void Changed(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        if (name is not null) Changed(name);
        return true;
    }

    public void CancelReload() => InvalidateReload();

    private long InvalidateReload()
    {
        var generation = Interlocked.Increment(ref _reloadGeneration);
        var pending = Interlocked.Exchange(ref _pendingReload, null);
        try { pending?.Cancel(); }
        catch (ObjectDisposedException) { /* The previous operation has already completed. */ }
        _index = null;
        Results.Clear();
        Selected = null;
        IsLoading = false;
        Status = "";
        return generation;
    }

    /// <returns>True only when this load successfully published the current snapshot.</returns>
    public async Task<bool> ReloadAsync()
    {
        var selectedId = Selected?.SnippetId;
        var generation = InvalidateReload();
        var cancellation = new CancellationTokenSource();
        _pendingReload = cancellation;
        var token = cancellation.Token;
        IsLoading = true;
        Status = Texts.PickerLoading;
        try
        {
            // WaitAsync also stops waiting for a provider which ignores cancellation.
            var snapshot = await store.ReadAsync(token).WaitAsync(token);
            if (generation != Volatile.Read(ref _reloadGeneration) || token.IsCancellationRequested) return false;
            var index = await Task.Run(() => new PickerSearchIndex(snapshot, token), token);
            if (generation != Volatile.Read(ref _reloadGeneration) || token.IsCancellationRequested) return false;
            _index = index;
            IsLoading = false;
            Filter(selectedId);
            Status = IsClipboardBusy ? Texts.PickerOperationPending : Texts.PickerManualNotice;
            return true;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { return false; }
        catch (Exception ex)
        {
            if (generation == Volatile.Read(ref _reloadGeneration))
            {
                IsLoading = false;
                Status = string.Format(Texts.StatusError, ex.Message);
            }
            return false;
        }
        finally
        {
            Interlocked.CompareExchange(ref _pendingReload, null, cancellation);
            cancellation.Dispose();
        }
    }

    private void Filter(Guid? preferredSelectionId = null)
    {
        var selectedId = preferredSelectionId ?? Selected?.SnippetId;
        var found = _index?.Find(Query) ?? [];
        Results.Clear();
        foreach (var item in found) Results.Add(item);
        Selected = Results.FirstOrDefault(x => x.SnippetId == selectedId) ?? Results.FirstOrDefault();
    }
}
