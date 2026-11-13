using System.Runtime.InteropServices;
using System.Text;
using TextExpander.Application.Features.Output;

namespace TextExpander.Infrastructure.Platform.Windows;

/// <summary>Only a technology hint from Win32 window metadata, NOT a capability to read or edit text.</summary>
public enum ExternalControlFamily { Unknown, ClassicEdit, RichEdit, Other }

public enum ExternalEditorProbeStatus
{
    MissingObservation, ForegroundUnavailable, FocusUnavailable, OwnProcess,
    FocusProcessMismatch, FocusOutsideForeground, ControlUnavailable,
    ControlStateUnknown, PasswordControl, ReadOnlyControl,
    TechnologyNotQualified, ClassicEditCandidateNotQualified
}

/// <summary>
/// Privacy-minimising snapshot: no HWND, PID, title, window text, cursor location,
/// typed characters, control contents or raw class names are retained.
/// Nullable style flags mean that the operating system did not establish the state.
/// </summary>
public sealed record ExternalEditorProbeObservation(
    bool ForegroundAvailable,
    bool FocusAvailable,
    bool ExternalProcess,
    bool FocusProcessMatchesForeground,
    bool FocusInsideForeground,
    bool Visible,
    bool Enabled,
    ExternalControlFamily Family,
    bool? IsPassword,
    bool? IsReadOnly)
{
    public override string ToString() => nameof(ExternalEditorProbeObservation);
}

/// <summary>
/// An observation can prioritize later adapter research but NEVER qualifies an external
/// application for conditional replacement, committed-text observation or injection.
/// </summary>
public sealed record ExternalEditorProbeAssessment(ExternalEditorProbeStatus Status)
{
    public bool CandidateForFurtherResearch => Status == ExternalEditorProbeStatus.ClassicEditCandidateNotQualified;
    public ReplacementCapability OutputCapability => ReplacementCapability.Unsupported;
    public bool CanReadCommittedText => false;
    public bool CanReplaceText => false;
}

public static class WindowsExternalEditorProbePolicy
{
    public static ExternalEditorProbeAssessment Evaluate(ExternalEditorProbeObservation? observed)
    {
        if (observed is null) return new(ExternalEditorProbeStatus.MissingObservation);
        if (!observed.ForegroundAvailable) return new(ExternalEditorProbeStatus.ForegroundUnavailable);
        if (!observed.FocusAvailable) return new(ExternalEditorProbeStatus.FocusUnavailable);
        if (!observed.ExternalProcess) return new(ExternalEditorProbeStatus.OwnProcess);
        if (!observed.FocusProcessMatchesForeground) return new(ExternalEditorProbeStatus.FocusProcessMismatch);
        if (!observed.FocusInsideForeground) return new(ExternalEditorProbeStatus.FocusOutsideForeground);
        if (!observed.Visible || !observed.Enabled) return new(ExternalEditorProbeStatus.ControlUnavailable);
        // Arbitrary browser, RichEdit, custom controls and unknown technologies are never
        // assumed to be plain EDIT fields, even if they appear to accept keyboard input.
        if (observed.Family != ExternalControlFamily.ClassicEdit)
            return new(ExternalEditorProbeStatus.TechnologyNotQualified);
        if (observed.IsPassword is null || observed.IsReadOnly is null)
            return new(ExternalEditorProbeStatus.ControlStateUnknown);
        if (observed.IsPassword.Value) return new(ExternalEditorProbeStatus.PasswordControl);
        if (observed.IsReadOnly.Value) return new(ExternalEditorProbeStatus.ReadOnlyControl);
        return new(ExternalEditorProbeStatus.ClassicEditCandidateNotQualified);
    }
}

/// <summary>
/// Explicit Win32 foreground metadata probe. It NEVER reads GetWindowText, edit contents,
/// selection, clipboard or keystrokes; it never installs hooks or writes to any HWND.
/// GUI-thread focus is queried for the *foreground* thread, not the probe's own thread.
/// This is a snapshot, not an atomic or continuously trustworthy observation.
/// </summary>
public static class WindowsExternalEditorProbeReader
{
    private const int GwlStyle = -16;
    private const uint EsPassword = 0x0020;
    private const uint EsReadOnly = 0x0800;
    private const uint GaRoot = 2;

    public static ExternalEditorProbeObservation Capture()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Win32 foreground probe requires Windows.");

        var foreground = GetForegroundWindow();
        if (foreground == 0) return Empty(foreground: false);
        var thread = GetWindowThreadProcessId(foreground, out var processId);
        if (thread == 0 || processId == 0) return Empty(foreground: false);
        var external = processId != (uint)Environment.ProcessId;

        var info = new GuiThreadInfo { Size = (uint)Marshal.SizeOf<GuiThreadInfo>() };
        if (!GetGUIThreadInfo(thread, ref info) || info.Focus == 0)
            return Empty(foreground: true, external: external);
        var focus = info.Focus;
        var focusThread = GetWindowThreadProcessId(focus, out var focusProcessId);
        var sameProcess = focusThread != 0 && focusProcessId == processId;
        var root = GetAncestor(focus, GaRoot);
        var foregroundRoot = GetAncestor(foreground, GaRoot);
        var sameRoot = root != 0 && root == foregroundRoot;

        var buffer = new StringBuilder(128);
        var length = GetClassNameW(focus, buffer, buffer.Capacity);
        var family = length <= 0 ? ExternalControlFamily.Unknown : ClassifyControl(buffer.ToString());
        bool? password = null;
        bool? readOnly = null;
        if (family == ExternalControlFamily.ClassicEdit && sameProcess && sameRoot)
        {
            // Zero style bits are legal: reset and inspect Win32 last error explicitly.
            Marshal.SetLastPInvokeError(0);
            var style = GetWindowLongW(focus, GwlStyle);
            if (style != 0 || Marshal.GetLastPInvokeError() == 0)
            {
                password = ((uint)style & EsPassword) != 0;
                readOnly = ((uint)style & EsReadOnly) != 0;
            }
        }
        return new ExternalEditorProbeObservation(
            true, true, external, sameProcess, sameRoot,
            IsWindowVisible(focus), IsWindowEnabled(focus), family, password, readOnly);
    }

    public static ExternalControlFamily ClassifyControl(string? className)
    {
        if (string.Equals(className, "Edit", StringComparison.OrdinalIgnoreCase))
            return ExternalControlFamily.ClassicEdit;
        if (className is not null && className.StartsWith("RichEdit", StringComparison.OrdinalIgnoreCase))
            return ExternalControlFamily.RichEdit;
        return string.IsNullOrWhiteSpace(className) ? ExternalControlFamily.Unknown : ExternalControlFamily.Other;
    }

    private static ExternalEditorProbeObservation Empty(bool foreground, bool external = false) =>
        new(foreground, false, external, false, false, false, false,
            ExternalControlFamily.Unknown, null, null);

    [StructLayout(LayoutKind.Sequential)]
    private struct WinRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct GuiThreadInfo
    {
        public uint Size;
        public uint Flags;
        public nint Active;
        public nint Focus;
        public nint Capture;
        public nint MenuOwner;
        public nint MoveSize;
        public nint Caret;
        public WinRect CaretRect;
    }

    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint processId);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetGUIThreadInfo(uint threadId, ref GuiThreadInfo info);
    [DllImport("user32.dll")] private static extern nint GetAncestor(nint window, uint flags);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(nint window);
    [DllImport("user32.dll", EntryPoint = "GetClassNameW", CharSet = CharSet.Unicode)]
    private static extern int GetClassNameW(nint window, StringBuilder className, int capacity);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    private static extern int GetWindowLongW(nint window, int index);
}
