using Avalonia.Input.Platform;
using Avalonia.Threading;
using TextExpander.Application.Features.Hotkeys;
using TextExpander.Core;
using TextExpander.Infrastructure.Platform.Windows;

namespace TextExpander.Desktop;

public partial class App
{
    private WindowsSnippetHotkeyService? _snippetHotkeys;
    private WindowsSnippetInsertRequest? _directInsertRequest;
    private bool _directInsertRunning;
    private long _snippetHotkeyGeneration;
    private SnippetHotkeyCatalog? _snippetCatalog;
    private LibrarySnapshot? _snippetSnapshot;
    private IReadOnlyDictionary<Guid, bool> _registeredSnippetHotkeys = new Dictionary<Guid, bool>();
    private readonly SemaphoreSlim _snippetHotkeyLock = new(1, 1);

    private void CancelSnippetInsertion() => _directInsertRequest?.Cancel();

    private async Task RefreshSnippetHotkeysAsync(LibrarySnapshot snapshot)
    {
        _snippetSnapshot = snapshot;
        CancelSnippetInsertion();
        var catalog = new SnippetHotkeyCatalog(snapshot, _activePickerHotkey,
            ++_snippetHotkeyGeneration);
        _snippetCatalog = catalog;
        _registeredSnippetHotkeys = new Dictionary<Guid, bool>();
        await _snippetHotkeyLock.WaitAsync();
        try
        {
            if (!ReferenceEquals(catalog, _snippetCatalog) || IsExiting || !OperatingSystem.IsWindows()) return;
            _pickerPaste ??= new WindowsPickerPasteService();
            var paste = _pickerPaste;
            _snippetHotkeys ??= new WindowsSnippetHotkeyService(binding =>
            {
                // Capture while handling WM_HOTKEY, before a delayed UI dispatch can change the recipient.
                var target = paste.CaptureExternalForeground();
                var sequence = paste.ClipboardSequence();
                if (target is null || sequence == 0) return;
                var expiresAt = Environment.TickCount64 + 1000;
                Dispatcher.UIThread.Post(() => InsertSnippetFromHotkeyAsync(binding, target, sequence, expiresAt));
            });
            var states = await _snippetHotkeys.ApplyAsync(catalog.Bindings);
            if (ReferenceEquals(catalog, _snippetCatalog) && !IsExiting) _registeredSnippetHotkeys = states;
        }
        catch (Exception)
        {
            // Save remains successful: a persisted shortcut can be unavailable in the current Windows session.
            // No snippet text or native exception detail is exposed.
        }
        finally
        {
            if (ReferenceEquals(catalog, _snippetCatalog)) _vm?.SetSnippetHotkeyStates(_registeredSnippetHotkeys);
            _snippetHotkeyLock.Release();
        }
    }

    private async void InsertSnippetFromHotkeyAsync(SnippetHotkeyBinding binding,
        WindowsPickerPasteTarget target, uint clipboardAtHotkey, long expiresAt)
    {
        if (IsExiting || Environment.TickCount64 > expiresAt || _directInsertRunning || _pickerSession.IsInsertionInProgress ||
            _picker?.IsVisible == true || _pickerVm?.IsClipboardBusy == true || _vm is null || _pickerPaste is null)
            return;
        var snippet = _snippetCatalog?.Resolve(binding);
        if (snippet is null || !_registeredSnippetHotkeys.TryGetValue(snippet.Id, out var active) || !active) return;
        var clipboard = _main?.Clipboard;
        if (clipboard is null) { _vm.SetManualInsertStatus(_vm.Texts.PickerClipboardUnavailable); return; }

        _directInsertRunning = true;
        var request = new WindowsSnippetInsertRequest(_pickerPaste, target, clipboardAtHotkey);
        _directInsertRequest = request;
        try
        {
            var result = await request.ExecuteAsync(
                () => WriteSnippetClipboardAsync(clipboard, snippet.Content, _pickerPaste),
                () => WindowsSnippetHotkeyService.IsTriggerKeyDown(binding), () => Task.Delay(20));
            if (IsExiting || _snippetCatalog?.Resolve(binding) is null) return;
            _vm.SetManualInsertStatus(result.Status switch
            {
                PickerPasteStatus.SubmittedUnverified => _vm.Texts.PickerInsertSubmitted,
                PickerPasteStatus.PartialOrUnknown => _vm.Texts.PickerInsertUncertain,
                _ => _vm.Texts.PickerInsertFocusChanged + " [" + result.Reason + "]"
            });
        }
        catch (Exception)
        {
            if (!IsExiting) _vm.SetManualInsertStatus(_vm.Texts.PickerInsertUncertain);
        }
        finally
        {
            _directInsertRequest = null;
            _directInsertRunning = false;
        }
    }

    private static async Task<uint> WriteSnippetClipboardAsync(IClipboard clipboard, string content,
        WindowsPickerPasteService paste)
    {
        var before = paste.ClipboardSequence();
        await clipboard.SetTextAsync(content);
        var afterWrite = paste.ClipboardSequence();
        if (!WindowsPickerPasteService.HasClipboardAdvanced(before, afterWrite)) return 0;
        var observed = await clipboard.TryGetTextAsync();
        return PickerClipboardReadbackVerifier.VerifiedSequence(content, observed, before, afterWrite, paste.ClipboardSequence());
    }
}
