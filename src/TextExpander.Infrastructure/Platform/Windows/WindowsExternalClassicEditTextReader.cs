using System.Runtime.InteropServices;
using System.Text;
using TextExpander.Application.Features.Input;

namespace TextExpander.Infrastructure.Platform.Windows;

/// <summary>
/// Solo ricerca diagnostica opt-in. L'identità HWND/PID è effimera e non deve
/// comparire in log, JSON o interfacce. Nessun permesso di espansione esterna.
/// </summary>
public sealed record ExternalClassicEditFocusToken(nint Foreground, nint Edit, uint ProcessId)
{
    public override string ToString() => nameof(ExternalClassicEditFocusToken);
}

public enum ExternalClassicEditReadStatus
{
    Observed,
    NotQualified,
    FocusChanged,
    MessageUnavailable,
    DocumentTooLong,
    InvalidSelection,
    TextChangedDuringRead
}

public sealed record ExternalClassicEditReadResult(
    ExternalClassicEditReadStatus Status,
    ExternalClassicEditFocusToken? Target = null,
    ExternalClassicEditTextObservation? Document = null)
{
    public bool IsObserved => Status == ExternalClassicEditReadStatus.Observed &&
        Target is not null && Document is not null;
    public override string ToString() => nameof(ExternalClassicEditReadResult);
}

/// <summary>
/// Lettura limitata di WM_GETTEXT / EM_GETSEL del SOLO Win32 Edit esterno in focus,
/// avviata esplicitamente dall'utente. WM_GETTEXT è un messaggio di sistema che
/// Windows può marshaliare fra processi; messaggi proprietari NON sono usati.
/// Il contenuto è conservato soltanto in RAM durante l'esperimento; non esistono
/// hook, clipboard, scritture, serializzazione o persistenza del testo.
/// Un risultato positivo NON prova che la modifica sia stata digitata da umani.
/// </summary>
public static class WindowsExternalClassicEditTextReader
{
    private const uint WmGetText = 0x000D;
    private const uint WmGetTextLength = 0x000E;
    private const uint EmGetSel = 0x00B0;
    private const uint SmtoAbortIfHung = 0x0002;
    private const uint GaRoot = 2;
    private const uint TimeoutMilliseconds = 150;

    public static ExternalClassicEditReadResult Capture() => CaptureForControl(0);

    /// <summary>
    /// Come Capture(), ma scarta gli eventi relativi a un altro HWND PRIMA
    /// di leggere il testo. Serve esclusivamente alla diagnostica opt-in.
    /// </summary>
    public static ExternalClassicEditReadResult CaptureForControl(nint expectedEdit)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Richiede Windows.");

        var assessment = WindowsExternalEditorProbePolicy.Evaluate(WindowsExternalEditorProbeReader.Capture());
        if (!assessment.CandidateForFurtherResearch)
            return new(ExternalClassicEditReadStatus.NotQualified);

        var target = CaptureFocus();
        if (expectedEdit != 0 && (target is null || target.Edit != expectedEdit))
            return new(ExternalClassicEditReadStatus.FocusChanged);
        if (target is null || !StillQualified(target))
            return new(ExternalClassicEditReadStatus.FocusChanged);

        if (SendMessageTimeoutW(target.Edit, WmGetTextLength, 0, 0,
            SmtoAbortIfHung, TimeoutMilliseconds, out var lengthResult) == 0)
            return new(ExternalClassicEditReadStatus.MessageUnavailable);
        var length = lengthResult.ToInt64();
        if (length < 0 || length > ExternalClassicEditTextDeltaPolicy.MaxDocumentUtf16Units)
            return new(ExternalClassicEditReadStatus.DocumentTooLong);

        if (SendMessageTimeoutW(target.Edit, EmGetSel, 0, 0,
            SmtoAbortIfHung, TimeoutMilliseconds, out var firstSelection) == 0)
            return new(ExternalClassicEditReadStatus.MessageUnavailable);

        // WM_GETTEXT è un messaggio Windows di sistema con marshalling supportato.
        // Non inviare buffer a EM_GETLINE o ad altri messaggi app-specific cross-process.
        var chars = new StringBuilder((int)length + 1);
        if (SendMessageTimeoutTextW(target.Edit, WmGetText, (nint)chars.Capacity, chars,
            SmtoAbortIfHung, TimeoutMilliseconds, out var copied) == 0)
            return new(ExternalClassicEditReadStatus.MessageUnavailable);

        if (SendMessageTimeoutW(target.Edit, EmGetSel, 0, 0,
            SmtoAbortIfHung, TimeoutMilliseconds, out var finalSelection) == 0)
            return new(ExternalClassicEditReadStatus.MessageUnavailable);

        if (!StillQualified(target))
            return new(ExternalClassicEditReadStatus.FocusChanged);

        var text = chars.ToString();
        if (text.Length != length || copied.ToInt64() != length || firstSelection != finalSelection)
            return new(ExternalClassicEditReadStatus.TextChangedDuringRead);

        var selection = unchecked((uint)firstSelection.ToInt64());
        var start = (int)(selection & 0xffff);
        var end = (int)(selection >> 16);
        if (selection == uint.MaxValue || start > end || end > text.Length)
            return new(ExternalClassicEditReadStatus.InvalidSelection);

        // Soltanto l'esperimento riceve il testo. NON stampare questo record
        // o passarlo al motore come osservazione verificata.
        return new(ExternalClassicEditReadStatus.Observed, target,
            new ExternalClassicEditTextObservation(text, start, end));
    }

    private static bool StillQualified(ExternalClassicEditFocusToken token) =>
        CaptureFocus() == token &&
        WindowsExternalEditorProbePolicy.Evaluate(WindowsExternalEditorProbeReader.Capture())
            .CandidateForFurtherResearch;

    private static ExternalClassicEditFocusToken? CaptureFocus()
    {
        var foreground = GetForegroundWindow();
        if (foreground == 0) return null;
        var thread = GetWindowThreadProcessId(foreground, out var pid);
        if (thread == 0 || pid == 0 || pid == (uint)Environment.ProcessId) return null;
        var gui = new GuiThreadInfo { Size = (uint)Marshal.SizeOf<GuiThreadInfo>() };
        if (!GetGUIThreadInfo(thread, ref gui) || gui.Focus == 0) return null;
        if (GetWindowThreadProcessId(gui.Focus, out var focusPid) == 0 || focusPid != pid) return null;
        var root = GetAncestor(gui.Focus, GaRoot);
        if (root == 0 || root != GetAncestor(foreground, GaRoot)) return null;
        return new(foreground, gui.Focus, pid);
    }

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
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint pid);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetGUIThreadInfo(uint threadId, ref GuiThreadInfo info);
    [DllImport("user32.dll")] private static extern nint GetAncestor(nint window, uint flags);

    [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", SetLastError = true)]
    private static extern nint SendMessageTimeoutW(nint window, uint message,
        nint wParam, nint lParam, uint flags, uint timeoutMilliseconds, out nint result);

    [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint SendMessageTimeoutTextW(nint window, uint message,
        nint wParam, StringBuilder buffer, uint flags, uint timeoutMilliseconds, out nint result);
}
