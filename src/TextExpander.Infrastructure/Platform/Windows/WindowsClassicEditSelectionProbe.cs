using System.Runtime.InteropServices;

namespace TextExpander.Infrastructure.Platform.Windows;

/// <summary>
/// A deliberately weak, read-only capability signal for a foreground Win32 Edit.
/// It never proves text provenance, document revision, IME state or write permission.
/// Even a collapsed/ranged selection is NOT an authorization to edit the target.
/// </summary>
public enum ClassicEditSelectionSignal
{
    Unknown,
    Collapsed,
    SelectedRange,
    Unrepresentable,
    InvalidRange
}

public enum ClassicEditSelectionProbeStatus
{
    NotResearchCandidate,
    FocusChanged,
    QueryUnavailable,
    RangeUnrepresentable,
    RangeInvalid,
    CaretObservedNotQualified,
    SelectionObservedNotQualified
}

/// <summary>
/// Privacy-bounded evidence: no HWND, PID, app name, title, text, offsets or length.
/// The native reader never returns its transient window handles to its caller.
/// </summary>
public sealed record ClassicEditSelectionEvidence(
    ExternalEditorProbeAssessment? Preflight,
    bool FocusConsistent,
    bool MessageCompleted,
    ClassicEditSelectionSignal Signal)
{
    public override string ToString() => nameof(ClassicEditSelectionEvidence);
}

public sealed record ClassicEditSelectionAssessment(ClassicEditSelectionProbeStatus Status)
{
    public bool SelectionQueryObserved => Status is
        ClassicEditSelectionProbeStatus.CaretObservedNotQualified or
        ClassicEditSelectionProbeStatus.SelectionObservedNotQualified;

    // A read-only probe does not authorize text observation or conditional replacement.
    public bool CanReadCommittedText => false;
    public bool CanReplaceText => false;
}

public static class WindowsClassicEditSelectionPolicy
{
    public static ClassicEditSelectionAssessment Evaluate(ClassicEditSelectionEvidence? evidence)
    {
        if (evidence?.Preflight?.CandidateForFurtherResearch != true)
            return new(ClassicEditSelectionProbeStatus.NotResearchCandidate);
        if (!evidence.FocusConsistent)
            return new(ClassicEditSelectionProbeStatus.FocusChanged);
        if (!evidence.MessageCompleted)
            return new(ClassicEditSelectionProbeStatus.QueryUnavailable);
        return evidence.Signal switch
        {
            ClassicEditSelectionSignal.Collapsed => new(ClassicEditSelectionProbeStatus.CaretObservedNotQualified),
            ClassicEditSelectionSignal.SelectedRange => new(ClassicEditSelectionProbeStatus.SelectionObservedNotQualified),
            ClassicEditSelectionSignal.Unrepresentable => new(ClassicEditSelectionProbeStatus.RangeUnrepresentable),
            ClassicEditSelectionSignal.InvalidRange => new(ClassicEditSelectionProbeStatus.RangeInvalid),
            _ => new(ClassicEditSelectionProbeStatus.QueryUnavailable)
        };
    }

    /// <summary>
    /// EM_GETSEL packs two 16-bit positions into its return value. -1 signals
    /// positions that cannot be represented, NOT a valid (65535, 65535) caret.
    /// Only the *category* escapes this method; numeric offsets are discarded.
    /// </summary>
    public static ClassicEditSelectionSignal DecodePackedRange(uint packed)
    {
        if (packed == uint.MaxValue) return ClassicEditSelectionSignal.Unrepresentable;
        var start = packed & 0xFFFFu;
        var end = packed >> 16;
        if (end < start) return ClassicEditSelectionSignal.InvalidRange;
        return start == end ? ClassicEditSelectionSignal.Collapsed : ClassicEditSelectionSignal.SelectedRange;
    }
}

/// <summary>
/// Explicit, one-shot, read-only external Win32 Edit experiment. Before and after
/// querying EM_GETSEL, independently rechecks the foreground/focus identity and
/// the existing metadata policy. Never calls WM_GETTEXT, reads keys or writes text.
/// This is NOT an atomic snapshot: a target can change between the checks.
/// </summary>
public static class WindowsClassicEditSelectionProbe
{
    private const uint EmGetSel = 0x00B0;
    private const uint SmtoAbortIfHung = 0x0002;
    private const uint GaRoot = 2;
    private const uint MaxMessageMilliseconds = 150;

    public static ClassicEditSelectionEvidence Capture()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Read-only Win32 selection probe requires Windows.");

        var initial = WindowsExternalEditorProbePolicy.Evaluate(WindowsExternalEditorProbeReader.Capture());
        if (!initial.CandidateForFurtherResearch)
            return new(initial, false, false, ClassicEditSelectionSignal.Unknown);

        var before = FocusIdentity.Capture();
        if (before is null)
            return new(initial, false, false, ClassicEditSelectionSignal.Unknown);

        // The foreground can change even between the metadata probe and the query.
        if (!WindowsExternalEditorProbePolicy.Evaluate(
                WindowsExternalEditorProbeReader.Capture()).CandidateForFurtherResearch ||
            before != FocusIdentity.Capture())
            return new(initial, false, false, ClassicEditSelectionSignal.Unknown);

        // EM_GETSEL uses no pointers when both parameters are zero, so no external
        // process-memory address is transferred. This can still fail (e.g. UIPI).
        var completed = SendMessageTimeoutW(before.Edit, EmGetSel, 0, 0,
            SmtoAbortIfHung, MaxMessageMilliseconds, out var packedResult) != 0;

        var after = FocusIdentity.Capture();
        var focusConsistent = after is not null && before == after &&
            WindowsExternalEditorProbePolicy.Evaluate(
                WindowsExternalEditorProbeReader.Capture()).CandidateForFurtherResearch;

        if (!focusConsistent)
            return new(initial, false, false, ClassicEditSelectionSignal.Unknown);

        return new(initial, true, completed,
            completed ? WindowsClassicEditSelectionPolicy.DecodePackedRange(unchecked((uint)packedResult.ToInt64()))
                : ClassicEditSelectionSignal.Unknown);
    }

    /// <summary>Ephemeral handles are private, never printed, serialized or exposed.</summary>
    private sealed record FocusIdentity(nint Foreground, nint Edit, uint ProcessId)
    {
        public static FocusIdentity? Capture()
        {
            var foreground = GetForegroundWindow();
            if (foreground == 0) return null;
            var thread = GetWindowThreadProcessId(foreground, out var pid);
            if (thread == 0 || pid == 0 || pid == (uint)Environment.ProcessId) return null;
            var gui = new GuiThreadInfo { Size = (uint)Marshal.SizeOf<GuiThreadInfo>() };
            if (!GetGUIThreadInfo(thread, ref gui) || gui.Focus == 0) return null;
            var focusThread = GetWindowThreadProcessId(gui.Focus, out var focusPid);
            if (focusThread == 0 || focusPid != pid) return null;
            var root = GetAncestor(gui.Focus, GaRoot);
            if (root == 0 || root != GetAncestor(foreground, GaRoot)) return null;
            return new(foreground, gui.Focus, pid);
        }
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
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetGUIThreadInfo(uint threadId, ref GuiThreadInfo info);
    [DllImport("user32.dll")] private static extern nint GetAncestor(nint window, uint flags);
    [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", SetLastError = true)]
    private static extern nint SendMessageTimeoutW(nint window, uint message,
        nint wParam, nint lParam, uint flags, uint timeoutMilliseconds, out nint result);
}
