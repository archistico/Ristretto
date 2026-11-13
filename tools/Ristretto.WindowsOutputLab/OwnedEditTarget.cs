using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using TextExpander.Application.Features.Output;

namespace Ristretto.WindowsOutputLab;

/// <summary>
/// Cooperating native test editor, NOT an adapter for arbitrary HWNDs. Creates and owns
/// both windows, runs exclusively on their UI thread and never pumps messages between
/// comparison and edit. IME is deliberately disabled only for this disposable control.
/// </summary>
internal sealed class OwnedEditTarget : IConditionalReplacementTarget, IDisposable
{
    [ThreadStatic] private static bool _imeDisabledForOwnedThread;
    private const uint WmChar = 0x0102, WmKeyDown = 0x0100;
    private const uint WmCommand = 0x0111, EnChange = 0x0300, EnSetFocus = 0x0100, EnKillFocus = 0x0200;
    private const uint EmGetSel = 0x00b0, EmSetSel = 0x00b1, EmReplaceSel = 0x00c2;
    private const uint EmSetReadOnly = 0x00cf, EmSetPasswordChar = 0x00cc, EmGetPasswordChar = 0x00d2;
    private const uint EmSetLimitText = 0x00c5, EditPassword = 0x0020, EditReadOnly = 0x0800;
    private readonly uint _thread = GetCurrentThreadId();
    private readonly uint _process = checked((uint)Environment.ProcessId);
    private readonly Guid _token = Guid.NewGuid();
    private readonly string _className = "RistrettoOwnedEdit_" + Guid.NewGuid().ToString("N");
    private readonly WindowProcedure _procedure;
    private readonly nint _module;
    private ushort _atom;
    private nint _window, _edit, _oldImeContext;
    private long _revision, _epoch = 1;
    private bool _nativeEditBatch, _invalid, _closed, _disposed, _requireHumanFocusForEdits;
    private bool _pendingHighSurrogate, _highSurrogateEditChanged, _dispatchingHighSurrogate;
    private OwnedEditMutationSource _activeDispatchSource = OwnedEditMutationSource.Unknown;
    private OwnedEditMutationEvidence? _lastMutation;

    public OwnedEditTarget(bool multiline = true)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        if (!_imeDisabledForOwnedThread)
        {
            // This CLI mode owns the thread; call before its first top-level WM_CREATE.
            // Never apply this policy to the production GUI or another process.
            if (!ImmDisableIME(_thread)) throw new InvalidOperationException("Owned test thread IME isolation unavailable");
            _imeDisabledForOwnedThread = true;
        }
        _procedure = WindowProc;
        _module = GetModuleHandleW(null);
        try
        {
            var windowClass = new WindowClass
            {
                Procedure = Marshal.GetFunctionPointerForDelegate(_procedure),
                Instance = _module, Background = GetSysColorBrush(5), ClassName = _className
            };
            _atom = RegisterClassW(ref windowClass);
            if (_atom == 0) throw new Win32Exception(Marshal.GetLastPInvokeError());
            _window = CreateWindowExW(0, _className,
                "Ristretto — campo Windows controllato (;firma + spazio)", 0x00cf0000,
                180, 150, 720, 330, 0, 0, _module, 0);
            if (_window == 0) throw new Win32Exception(Marshal.GetLastPInvokeError());
            var style = 0x50010080u | 0x00800000u; // child, visible, tab stop, border, auto H scroll
            if (multiline) style |= 0x0004u | 0x0040u | 0x1000u | 0x00200000u; // multiline, auto V scroll, return, V scrollbar
            _edit = CreateWindowExW(0, "EDIT", "", style, 18, 20, 665, 235, _window, 1, _module, 0);
            if (_edit == 0) throw new Win32Exception(Marshal.GetLastPInvokeError());
            _oldImeContext = ImmAssociateContext(_edit, 0);
            SendMessageW(_edit, EmSetLimitText, TriggerReplacementPlanner.MaxTextUtf16Units, 0);
            ShowWindow(_window, 4); // SW_SHOWNOACTIVATE: never redirect another application's input.
        }
        catch { Dispose(); throw; }
    }

    public int TextReads { get; private set; }
    public bool IsOpen => !_disposed && !_closed && IsWindow(_window);

    /// <summary>Revision-bound metadata only; never exposes the message character.</summary>
    public OwnedEditMutationEvidence? LastMutation
    {
        get { EnsureOwnedThread(); return _lastMutation; }
    }

    /// <summary>True only when this owned window is foreground and its own EDIT has focus.</summary>
    public bool HasHumanInputFocus => !_disposed && GetCurrentThreadId() == _thread &&
        IsOurControl() && GetForegroundWindow() == _window && GetFocus() == _edit;

    /// <summary>Best-effort modifier check for the interactive owned demo; never derive text from keys.</summary>
    public bool HasHumanInputModifiers => IsPressed(0x10) || IsPressed(0x11) || IsPressed(0x12) ||
        IsPressed(0x5b) || IsPressed(0x5c); // SHIFT, CTRL, ALT (including AltGr), WIN

    private static bool IsPressed(int virtualKey) => (GetAsyncKeyState(virtualKey) & 0x8000) != 0;

    public void ActivateForHumanInput()
    {
        EnsureOwnedThread();
        _requireHumanFocusForEdits = true;
        ShowWindow(_window, 5);
        SetForegroundWindow(_window);
        SetFocus(_edit);
    }

    /// <summary>
    /// Pump this laboratory thread's messages. An optional callback is notified AFTER
    /// each coherent own-control turn, not after the entire queued burst. The callback
    /// runs on the same UI thread; it must not run a nested message pump.
    /// Incomplete WM_CHAR surrogate halves are deferred until a consecutive low half.
    /// </summary>
    public int PumpPendingMessages(Action? afterOwnTurn = null)
    {
        EnsureOwnedThread();
        var processed = 0;
        // Null HWND drains ALL messages of this isolated test thread. A window-only
        // filter could leave unrelated thread messages queued and prevent WaitMessage
        // from sleeping correctly. Never interpret other windows as trusted editors.
        while (PeekMessageW(out var message, 0, 0, 0, 1))
        {
            processed++;
            if (message.Id == 0x0012) // WM_QUIT: thread message, never dispatched.
            {
                _closed = true;
                _invalid = true;
                break;
            }
            var ownedEdit = message.Window == _edit;
            var ownedParent = message.Window == _window;
            var high = ownedEdit && message.Id == WmChar &&
                message.WParam >= (nuint)0xd800 && message.WParam <= (nuint)0xdbff;
            var low = ownedEdit && message.Id == WmChar &&
                message.WParam >= (nuint)0xdc00 && message.WParam <= (nuint)0xdfff;
            var oldRevision = _revision;
            var oldEpoch = _epoch;
            // Only an immediately consecutive low-surrogate character message may
            // complete a pending pair. An interruption invalidates the entire context.
            if (_pendingHighSurrogate && !low)
            {
                _pendingHighSurrogate = false;
                if (_highSurrogateEditChanged)
                    AdvanceRevision(OwnedEditMutationSource.Unknown, false);
                _highSurrogateEditChanged = false;
                InvalidateInputContinuity();
            }
            var previous = _activeDispatchSource;
            var previousHigh = _dispatchingHighSurrogate;
            if (ownedEdit)
            {
                var ctrlOrAlt = IsPressed(0x11) || IsPressed(0x12);
                var decision = OwnedEditDispatchPolicy.Classify(message.Id, message.WParam, ctrlOrAlt);
                _activeDispatchSource = decision.MutationSource;
                if (decision.BreaksContinuity) InvalidateInputContinuity();
            }
            else
            {
                _activeDispatchSource = OwnedEditMutationSource.Unknown;
                if (ownedParent &&
                    OwnedEditDispatchPolicy.Classify(message.Id, message.WParam, false).BreaksContinuity)
                    InvalidateInputContinuity();
            }
            _dispatchingHighSurrogate = high;
            try
            {
                TranslateMessage(ref message);
                DispatchMessageW(ref message);
                if (high) _pendingHighSurrogate = true;
                if (low && _pendingHighSurrogate)
                {
                    // Some classic EDIT implementations report EN_CHANGE for each
                    // UTF-16 half; others report only the completed pair. Treat one
                    // adjacent verified surrogate pair as ONE logical observation.
                    if (_highSurrogateEditChanged && _revision == oldRevision)
                        AdvanceRevision(OwnedEditMutationSource.OwnedCharacterMessage, true);
                    _pendingHighSurrogate = false;
                    _highSurrogateEditChanged = false;
                }
                if (!high && (ownedEdit || ownedParent) &&
                    (_revision != oldRevision || _epoch != oldEpoch))
                    afterOwnTurn?.Invoke();
            }
            finally
            {
                _dispatchingHighSurrogate = previousHigh;
                _activeDispatchSource = previous;
            }
        }
        return processed;
    }

    /// <summary>Wait without polling; only legal on this isolated Win32 lab UI thread.</summary>
    public void WaitForMessage()
    {
        EnsureOwnedThread();
        if (!_closed && !WaitMessage())
            throw new Win32Exception(Marshal.GetLastPInvokeError());
    }

    public ReplacementTargetSnapshot ReadSnapshot()
    {
        EnsureOwnedThread();
        var state = ReplacementFieldState.Unknown;
        if (!_invalid && IsOurControl() && IsWindowVisible(_edit))
        {
            var style = unchecked((uint)GetWindowLongW(_edit, -16));
            if ((style & EditPassword) != 0 || SendMessageW(_edit, EmGetPasswordChar, 0, 0) != 0)
                state = ReplacementFieldState.Protected;
            else if (!IsWindowEnabled(_edit)) state = ReplacementFieldState.Disabled;
            else if ((style & EditReadOnly) != 0) state = ReplacementFieldState.ReadOnly;
            else
            {
                var context = ImmGetContext(_edit);
                if (context == 0) state = ReplacementFieldState.VerifiedNormalEditable;
                else ImmReleaseContext(_edit, context); // unexpected association: fail closed, no text read.
            }
        }
        if (state != ReplacementFieldState.VerifiedNormalEditable)
            return new(_token, _epoch, _revision, null, -1, -1, state, ReplacementCapability.Unknown);
        var length = GetWindowTextLengthW(_edit);
        if (length < 0 || length > TriggerReplacementPlanner.MaxTextUtf16Units)
            return new(_token, _epoch, _revision, null, -1, -1, ReplacementFieldState.Unknown, ReplacementCapability.Unknown);
        var buffer = new StringBuilder(length + 1);
        var copied = GetWindowTextW(_edit, buffer, buffer.Capacity);
        if (copied != length) throw new InvalidOperationException("Controlled text changed during read");
        TextReads++;
        var text = buffer.ToString();
        if (!WellFormed(text))
            return new(_token, _epoch, _revision, null, -1, -1, ReplacementFieldState.Unknown, ReplacementCapability.Unknown);
        SendMessageSelection(_edit, EmGetSel, out var start, out var end);
        return new(_token, _epoch, _revision, text, checked((int)start), checked((int)end),
            state, ReplacementCapability.VerifiedConditionalReplace);
    }

    /// <summary>Loads synthetic fixture data in this component's own control only.</summary>
    public void SeedFixture(string text, int caret)
    {
        EnsureOwnedThread();
        if (text.Length > TriggerReplacementPlanner.MaxTextUtf16Units || text.Contains('\0') || caret < 0 || caret > text.Length)
            throw new ArgumentException("Invalid fixture");
        NativeEditBatch(() =>
        {
            if (!SetWindowTextW(_edit, text)) throw new Win32Exception(Marshal.GetLastPInvokeError());
            SendMessageW(_edit, EmSetSel, caret, caret);
        }, OwnedEditMutationSource.OwnedFixture);
    }

    /// <summary>
    /// Controlled test only: posts a synthetic WM_CHAR to our own EDIT, demonstrating
    /// that character-message provenance must NOT be described as human input.
    /// </summary>
    public void PostOwnedCharacterMessageFixture(char value) => PostOwnedCharacterSequenceFixture(value.ToString());

    /// <summary>Test-only queued UTF-16 character messages; never evidence of human typing.</summary>
    public void PostOwnedCharacterSequenceFixture(string text)
    {
        EnsureOwnedThread();
        if (string.IsNullOrEmpty(text) || text.Length > TriggerReplacementPlanner.MaxRawMatchUtf16Units ||
            !WellFormed(text) || text.Any(char.IsControl))
            throw new ArgumentException("A well-formed printable UTF-16 fixture is required", nameof(text));
        foreach (var unit in text)
            if (!PostMessageW(_edit, WmChar, (nuint)unit, 0))
                throw new Win32Exception(Marshal.GetLastPInvokeError());
    }

    public void InsertFixtureCommit(string text)
    {
        EnsureOwnedThread();
        if (text.Contains('\0')) throw new ArgumentException("Invalid fixture commit");
        NativeEditBatch(() => SendMessageText(_edit, EmReplaceSel, 0, text),
            OwnedEditMutationSource.OwnedFixture);
    }

    public void SetFixtureSelection(int start, int end)
    {
        EnsureOwnedThread();
        SendMessageW(_edit, EmSetSel, start, end);
        _epoch++;
    }

    public void SetFixtureReadOnly(bool value)
    {
        EnsureOwnedThread();
        SendMessageW(_edit, EmSetReadOnly, value ? 1 : 0, 0);
        _epoch++;
    }

    public void SetFixtureEnabled(bool value)
    {
        EnsureOwnedThread();
        EnableWindow(_edit, value);
        _epoch++;
    }

    public void SetFixturePassword(bool value)
    {
        EnsureOwnedThread();
        SendMessageW(_edit, EmSetPasswordChar, value ? '*' : 0, 0);
        _epoch++;
    }

    public ConditionalReplacementResult CompareAndReplace(TriggerReplacementPlan plan, CancellationToken cancellationToken)
    {
        // Reject on another OS thread before making any EDIT call. No cross-thread send/wait.
        if (_disposed || GetCurrentThreadId() != _thread || cancellationToken.IsCancellationRequested || !IsOurControl() ||
            (_requireHumanFocusForEdits && (!HasHumanInputFocus || HasHumanInputModifiers)))
            return new(ConditionalReplacementStatus.RejectedNoChange);
        var before = ReadSnapshot();
        if (!plan.MatchesExpectedTarget(before)) return new(ConditionalReplacementStatus.RejectedNoChange);
        if (cancellationToken.IsCancellationRequested ||
            (_requireHumanFocusForEdits && (!HasHumanInputFocus || HasHumanInputModifiers)))
            return new(ConditionalReplacementStatus.RejectedNoChange);

        // One cooperating UI-thread turn, with no message pump or user callbacks here.
        // Selection is already a possible effect: errors from this point are uncertain.
        NativeEditBatch(() =>
        {
            SendMessageW(_edit, EmSetSel, plan.RangeStart, plan.RangeStart + plan.RangeLength);
            SendMessageText(_edit, EmReplaceSel, 0, plan.ReplacementText); // no success return: read back below.
            SendMessageW(_edit, EmSetSel, plan.ExpectedResult.SelectionStart, plan.ExpectedResult.SelectionEnd);
        });
        var after = ReadSnapshot();
        return after == plan.ExpectedResult
            ? new(ConditionalReplacementStatus.Applied, after)
            : new(ConditionalReplacementStatus.PartialOrUnknown, after);
    }

    private void NativeEditBatch(Action edit, OwnedEditMutationSource source = OwnedEditMutationSource.ProgrammaticChange)
    {
        if (_nativeEditBatch) throw new InvalidOperationException("Reentrant native edit");
        _nativeEditBatch = true;
        try { edit(); }
        finally { _nativeEditBatch = false; AdvanceRevision(source, false); }
    }

    private void InvalidateInputContinuity()
    {
        // This epoch belongs only to the local laboratory. It is an observation
        // boundary, not a security credential. Overflow permanently disqualifies it.
        if (_epoch == long.MaxValue) { _invalid = true; _lastMutation = null; }
        else _epoch++;
    }

    /// <summary>Posts a navigation/edit command ONLY to our own test EDIT.</summary>
    public void PostOwnedContinuityBreakFixture(uint message, nuint parameter)
    {
        EnsureOwnedThread();
        if (message != WmKeyDown && message != WmChar)
            throw new ArgumentException("Only owned keydown and control character fixtures", nameof(message));
        var decision = OwnedEditDispatchPolicy.Classify(message, parameter, false);
        if (!decision.BreaksContinuity)
            throw new ArgumentException("Fixture must invalidate input continuity", nameof(parameter));
        if (!PostMessageW(_edit, message, parameter, 0))
            throw new Win32Exception(Marshal.GetLastPInvokeError());
    }

    private void AdvanceRevision(OwnedEditMutationSource source, bool duringOwnedMessage)
    {
        if (_revision == long.MaxValue) { _invalid = true; _lastMutation = null; }
        else
        {
            _revision++;
            _lastMutation = new OwnedEditMutationEvidence(_epoch, _revision, source, duringOwnedMessage);
        }
    }

    private nint WindowProc(nint window, uint message, nint wParam, nint lParam)
    {
        if (message == 0x0082) { _closed = true; _invalid = true; } // WM_NCDESTROY: never accept recycled HWNDs.
        if (message == WmCommand && lParam == _edit && _edit != 0)
        {
            var code = (uint)(((nuint)wParam >> 16) & 0xffff);
            if (code == EnChange && !_nativeEditBatch)
            {
                if (_dispatchingHighSurrogate && _activeDispatchSource == OwnedEditMutationSource.OwnedCharacterMessage)
                    _highSurrogateEditChanged = true; // Wait for the immediately following low half.
                else
                    AdvanceRevision(_activeDispatchSource,
                        _activeDispatchSource == OwnedEditMutationSource.OwnedCharacterMessage);
            }
            if (code is EnSetFocus or EnKillFocus) _epoch++;
        }
        return DefWindowProcW(window, message, wParam, lParam);
    }

    private bool IsOurControl()
    {
        if (_closed || _edit == 0 || !IsWindow(_window) || !IsWindow(_edit) || GetParent(_edit) != _window) return false;
        var parentThread = GetWindowThreadProcessId(_window, out var parentProcess);
        var thread = GetWindowThreadProcessId(_edit, out var process);
        return thread == _thread && process == _process && parentThread == _thread && parentProcess == _process;
    }

    private static bool WellFormed(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\0') return false;
            if (char.IsHighSurrogate(text[i]))
            {
                if (++i >= text.Length || !char.IsLowSurrogate(text[i])) return false;
            }
            else if (char.IsLowSurrogate(text[i])) return false;
        }
        return true;
    }

    private void EnsureOwnedThread()
    {
        if (_disposed || GetCurrentThreadId() != _thread || (_edit != 0 && !IsOurControl()))
            throw new InvalidOperationException("Owned control unavailable or wrong UI thread");
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (GetCurrentThreadId() != _thread) throw new InvalidOperationException("Wrong disposal thread");
        _disposed = true;
        if (_edit != 0 && IsWindow(_edit)) ImmAssociateContext(_edit, _oldImeContext);
        if (_window != 0 && IsWindow(_window)) DestroyWindow(_window);
        if (_atom != 0) UnregisterClassW(_className, _module);
        _window = _edit = 0;
        GC.KeepAlive(_procedure);
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate nint WindowProcedure(nint window, uint message, nint wParam, nint lParam);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WindowClass
    {
        public uint Style;
        public nint Procedure;
        public int ClassExtra, WindowExtra;
        public nint Instance, Icon, Cursor, Background, MenuName;
        [MarshalAs(UnmanagedType.LPWStr)] public string ClassName;
    }
    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct Message
    {
        public nint Window;
        public uint Id;
        public nuint WParam;
        public nint LParam;
        public uint Time;
        public Point Position;
        public uint Private;
    }
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern nint GetModuleHandleW(string? name);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern ushort RegisterClassW(ref WindowClass windowClass);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool UnregisterClassW(string name, nint instance);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(uint exStyle, string className, string title, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetSysColorBrush(int index);
    [DllImport("user32.dll")] private static extern bool IsWindow(nint window);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern bool IsWindowEnabled(nint window);
    [DllImport("user32.dll")] private static extern bool EnableWindow(nint window, bool enable);
    [DllImport("user32.dll")] private static extern nint GetParent(nint window);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowLongW(nint window, int index);
    [DllImport("user32.dll")] private static extern bool DestroyWindow(nint window);
    [DllImport("user32.dll")] private static extern bool ShowWindow(nint window, int show);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern nint SetFocus(nint window);
    [DllImport("user32.dll")] private static extern nint GetFocus();
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int virtualKey);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern nint DefWindowProcW(nint window, uint message, nint wParam, nint lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool SetWindowTextW(nint window, string text);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowTextLengthW(nint window);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowTextW(nint window, StringBuilder text, int capacity);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern nint SendMessageW(nint window, uint message, nint wParam, nint lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool PostMessageW(nint window, uint message, nuint wParam, nint lParam);
    [DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode)] private static extern nint SendMessageText(nint window, uint message, nint wParam, string text);
    [DllImport("user32.dll", EntryPoint = "SendMessageW")] private static extern nint SendMessageSelection(nint window, uint message, out uint start, out uint end);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool PeekMessageW(out Message message, nint window, uint minimum, uint maximum, uint remove);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool WaitMessage();
    [DllImport("user32.dll")] private static extern bool TranslateMessage(ref Message message);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern nint DispatchMessageW(ref Message message);
    [DllImport("imm32.dll")] private static extern nint ImmAssociateContext(nint window, nint context);
    [DllImport("imm32.dll")] private static extern nint ImmGetContext(nint window);
    [DllImport("imm32.dll")] private static extern bool ImmReleaseContext(nint window, nint context);
    [DllImport("imm32.dll")] private static extern bool ImmDisableIME(uint thread);
}
