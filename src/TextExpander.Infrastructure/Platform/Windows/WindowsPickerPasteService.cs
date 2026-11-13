using System.Runtime.InteropServices;
using System.Text;

namespace TextExpander.Infrastructure.Platform.Windows;

/// <summary>One explicit picker-insert request targets exactly the window and focused child
/// observed BEFORE the picker took focus. No global hooks, trigger deletion or retries.</summary>
public sealed record WindowsPickerPasteTarget(nint Window, uint ProcessId, nint FocusedControl);

public enum PickerPasteStatus { Rejected, SubmittedUnverified, PartialOrUnknown }

public sealed record PickerPasteResult(PickerPasteStatus Status, string Reason);

/// <summary>Injectable Windows boundary. Clipboard contents are handled by Avalonia, not this service.</summary>
public interface IWindowsPickerPasteApi
{
    nint ForegroundWindow();
    uint WindowProcessId(nint window);
    nint FocusedControl(nint foregroundWindow);
    bool IsWindowAvailable(nint window);
    bool IsSameOrChild(nint root, nint candidate);
    bool IsProtectedEditor(nint focusedControl);
    bool ActivateWindow(nint window);
    bool IsAnyModifierPressed();
    uint ClipboardSequence();
    int SendPasteShortcut();
}

/// <summary>
/// User-initiated paste into a previously focused external application. Uses the clipboard
/// deliberately (supports multiline Unicode) and leaves the snippet in the clipboard.
/// A full SendInput count is NOT proof the recipient inserted the text.
/// </summary>
public sealed class WindowsPickerPasteService
{
    private readonly IWindowsPickerPasteApi _api;
    private readonly Func<bool> _isWindows;
    private readonly uint _ourProcessId;

    public WindowsPickerPasteService()
        : this(new Win32PickerPasteApi(), OperatingSystem.IsWindows, checked((uint)Environment.ProcessId)) { }

    public WindowsPickerPasteService(IWindowsPickerPasteApi api, Func<bool> isWindows, uint ourProcessId)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
        _ourProcessId = ourProcessId;
    }

    public WindowsPickerPasteTarget? CaptureExternalForeground()
    {
        if (!_isWindows()) return null;
        try
        {
            var hwnd = _api.ForegroundWindow();
            if (hwnd == 0 || !_api.IsWindowAvailable(hwnd)) return null;
            var pid = _api.WindowProcessId(hwnd);
            if (pid == 0 || pid == _ourProcessId) return null;
            var focused = _api.FocusedControl(hwnd);
            if (focused == 0 || !_api.IsSameOrChild(hwnd, focused) ||
                _api.IsProtectedEditor(focused)) return null;
            return new WindowsPickerPasteTarget(hwnd, pid, focused);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return null;
        }
    }

    public bool TryActivate(WindowsPickerPasteTarget? target)
    {
        try { return ValidTarget(target) && _api.ActivateWindow(target!.Window); }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        { return false; }
    }

    /// <summary>Take AFTER Avalonia has written the snippet to the clipboard.</summary>
    public uint ClipboardSequence() => _isWindows() ? _api.ClipboardSequence() : 0;

    public PickerPasteResult Paste(WindowsPickerPasteTarget? target, uint expectedClipboardSequence)
    {
        var dispatchAttempted = false;
        try
        {
            if (!ValidTarget(target)) return new(PickerPasteStatus.Rejected, "target_unavailable");
            if (expectedClipboardSequence == 0) return new(PickerPasteStatus.Rejected, "clipboard_unverified");
            // All checks happen immediately before SendInput. No input if focus or clipboard changed.
            if (_api.ForegroundWindow() != target!.Window ||
                _api.FocusedControl(target.Window) != target.FocusedControl ||
                !_api.IsSameOrChild(target.Window, target.FocusedControl))
                return new(PickerPasteStatus.Rejected, "focus_changed");
            if (_api.IsProtectedEditor(target.FocusedControl))
                return new(PickerPasteStatus.Rejected, "protected_editor");
            if (_api.IsAnyModifierPressed())
                return new(PickerPasteStatus.Rejected, "modifier_pressed");
            if (_api.ClipboardSequence() != expectedClipboardSequence)
                return new(PickerPasteStatus.Rejected, "clipboard_changed");
            dispatchAttempted = true;
            var sent = _api.SendPasteShortcut();
            return sent == 4
                ? new(PickerPasteStatus.SubmittedUnverified, "paste_shortcut_sent")
                : new(PickerPasteStatus.PartialOrUnknown, "paste_input_partial_or_blocked");
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return dispatchAttempted
                ? new(PickerPasteStatus.PartialOrUnknown, "native_error_unknown_effect")
                : new(PickerPasteStatus.Rejected, "native_preflight_failed");
        }
    }

    private bool ValidTarget(WindowsPickerPasteTarget? target) =>
        _isWindows() && target is not null && target.Window != 0 && target.FocusedControl != 0 &&
        target.ProcessId != 0 && target.ProcessId != _ourProcessId &&
        _api.IsWindowAvailable(target.Window) &&
        _api.WindowProcessId(target.Window) == target.ProcessId &&
        _api.IsSameOrChild(target.Window, target.FocusedControl);
}

internal sealed class Win32PickerPasteApi : IWindowsPickerPasteApi
{
    private const uint InputKeyboard = 1;
    private const uint KeyUp = 0x0002;
    private const int VkControl = 0x11;
    private const int VkV = 0x56;
    private const int VkShift = 0x10;
    private const int VkAlt = 0x12;
    private const int VkLWin = 0x5B;
    private const int VkRWin = 0x5C;
    private const int GwlStyle = -16;
    private const long EsPassword = 0x20;
    private const long EsReadOnly = 0x0800;

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct GuiThreadInfo
    {
        public uint Size;
        public uint Flags;
        // Keep all six HWND fields from GUITHREADINFO, including hwndCaret:
        // cbSize must be 72 bytes on x64 and 48 bytes on x86.
        public nint Active, Focus, Capture, MenuOwner, MoveSize, Caret;
        public Rect CaretRect;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputData Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputData
    {
        [FieldOffset(0)] public KeyboardInput Keyboard;
        [FieldOffset(0)] public MouseInput Mouse;
        [FieldOffset(0)] public HardwareInput Hardware;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey, ScanCode;
        public uint Flags, Time;
        public nuint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int X, Y;
        public uint MouseData, Flags, Time;
        public nuint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HardwareInput
    {
        public uint Message;
        public ushort ParamLow, ParamHigh;
    }

    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint hwnd, out uint processId);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetGUIThreadInfo(uint threadId, ref GuiThreadInfo info);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindow(nint hwnd);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsChild(nint parent, nint child);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int virtualKey);
    [DllImport("user32.dll")] private static extern uint GetClipboardSequenceNumber();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassNameW(nint hwnd, StringBuilder name, int maxCount);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLongW(nint hwnd, int index);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, [In] Input[] inputs, int size);

    public nint ForegroundWindow() => GetForegroundWindow();
    public uint WindowProcessId(nint hwnd)
    { _ = GetWindowThreadProcessId(hwnd, out var pid); return pid; }
    public nint FocusedControl(nint hwnd)
    {
        var threadId = GetWindowThreadProcessId(hwnd, out _);
        if (threadId == 0) return 0;
        var info = new GuiThreadInfo { Size = (uint)Marshal.SizeOf<GuiThreadInfo>() };
        return GetGUIThreadInfo(threadId, ref info) ? info.Focus : 0;
    }
    public bool IsWindowAvailable(nint hwnd) => IsWindow(hwnd) && IsWindowVisible(hwnd);
    public bool IsSameOrChild(nint root, nint child) => root == child || IsChild(root, child);
    public bool IsProtectedEditor(nint focusedControl)
    {
        var className = new StringBuilder(64);
        if (GetClassNameW(focusedControl, className, className.Capacity) == 0) return true;
        var type = className.ToString();
        if (type.Equals("Shell_TrayWnd", StringComparison.OrdinalIgnoreCase) ||
            type.Equals("Shell_SecondaryTrayWnd", StringComparison.OrdinalIgnoreCase) ||
            type.Equals("NotifyIconOverflowWindow", StringComparison.OrdinalIgnoreCase) ||
            type.Equals("Progman", StringComparison.OrdinalIgnoreCase) ||
            type.Equals("WorkerW", StringComparison.OrdinalIgnoreCase)) return true;
        if (!type.Equals("Edit", StringComparison.OrdinalIgnoreCase) &&
            !type.StartsWith("RichEdit", StringComparison.OrdinalIgnoreCase)) return false;
        var style = (uint)GetWindowLongW(focusedControl, GwlStyle);
        return ((long)style & (EsPassword | EsReadOnly)) != 0;
    }
    public bool ActivateWindow(nint hwnd) => SetForegroundWindow(hwnd);
    public bool IsAnyModifierPressed() => Down(VkShift) || Down(VkControl) || Down(VkAlt) || Down(VkLWin) || Down(VkRWin);
    private static bool Down(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;
    public uint ClipboardSequence() => GetClipboardSequenceNumber();
    public int SendPasteShortcut()
    {
        // One atomic SendInput batch; still never claim that the target pasted the text.
        var input = new[] { Key(VkControl, 0), Key(VkV, 0), Key(VkV, KeyUp), Key(VkControl, KeyUp) };
        return checked((int)SendInput((uint)input.Length, input, Marshal.SizeOf<Input>()));
    }
    private static Input Key(int key, uint flags) => new()
    {
        Type = InputKeyboard,
        Data = new InputData { Keyboard = new KeyboardInput { VirtualKey = checked((ushort)key), Flags = flags } }
    };
}
