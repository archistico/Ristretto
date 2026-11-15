using Avalonia.Controls;
using Avalonia.Threading;
using TextExpander.Application.Features.Hotkeys;
using TextExpander.Desktop.Localization;
using TextExpander.Infrastructure.Platform.Windows;

namespace TextExpander.Desktop;

public partial class App
{
    private readonly HotkeyCaptureCoordinator _hotkeyCapture = new();
    private long _pickerHotkeyGeneration;

    private WindowsPickerHotkeyService CreatePickerHotkeyService()
    {
        var generation = ++_pickerHotkeyGeneration;
        return new WindowsPickerHotkeyService(() =>
        {
            if (_hotkeyCapture.IsActive) return;
            Dispatcher.UIThread.Post(() =>
            {
                if (generation == _pickerHotkeyGeneration && !_hotkeyCapture.IsActive) ShowPicker();
            });
        });
    }

    internal Task<string?> CaptureSnippetHotkeyAsync(Window owner, UiStrings texts)
    {
        if (IsExiting || _hotkeyCapture.IsActive) return Task.FromResult<string?>(null);
        return _hotkeyCapture.RunAsync(PauseHotkeysForCaptureAsync, RestoreHotkeysAfterCaptureAsync,
            () => IsExiting ? Task.FromResult<string?>(null) : new HotkeyCaptureWindow(texts).ShowDialog<string?>(owner));
    }

    private async Task PauseHotkeysForCaptureAsync()
    {
        CancelSnippetInsertion();
        CancelPickerInsertion();
        ++_pickerHotkeyGeneration; // Invalidate notifications already queued by the previous worker.
        _picker?.Hide();
        // Stop the native workers so an already assigned chord reaches the focused capture control.
        // Stored settings, drafts and the previously active picker gesture are left intact.
        await _snippetHotkeyLock.WaitAsync();
        try
        {
            var service = _snippetHotkeys;
            _snippetHotkeys = null;
            _registeredSnippetHotkeys = new Dictionary<Guid, bool>();
            if (service is not null) await Task.Run(service.Dispose);
        }
        finally { _snippetHotkeyLock.Release(); }

        await _pickerHotkeyLock.WaitAsync();
        try
        {
            var service = _pickerHotkey;
            _pickerHotkey = null;
            if (service is not null) await Task.Run(service.Dispose);
        }
        finally { _pickerHotkeyLock.Release(); }
    }

    private async Task RestoreHotkeysAfterCaptureAsync()
    {
        if (IsExiting || !OperatingSystem.IsWindows()) return;
        await _pickerHotkeyLock.WaitAsync();
        try
        {
            if (!IsExiting && PickerHotkey.TryParse(string.IsNullOrEmpty(_activePickerHotkey) ? "None" : _activePickerHotkey,
                    out var previous))
            {
                _pickerHotkey = CreatePickerHotkeyService();
                if (!await _pickerHotkey.PrepareAsync(previous) || !await _pickerHotkey.CommitAsync())
                {
                    await _pickerHotkey.CancelAsync();
                    _activePickerHotkey = "";
                    _vm?.SetPickerHotkeyState("", _vm.Texts.PickerHotkeyUnavailable);
                }
            }
        }
        catch (Exception)
        {
            _activePickerHotkey = "";
            _vm?.SetPickerHotkeyState("", _vm.Texts.PickerHotkeyUnavailable);
        }
        finally { _pickerHotkeyLock.Release(); }

        // A save/import already in flight may finish while the modal is opening/closing.
        // Restore the latest snapshot and keep notifications suppressed until restoration completes.
        while (!IsExiting && _snippetSnapshot is not null)
        {
            var snapshot = _snippetSnapshot;
            await RefreshSnippetHotkeysCoreAsync(snapshot, restoringCapture: true);
            if (ReferenceEquals(snapshot, _snippetSnapshot)) break;
        }
    }
}
