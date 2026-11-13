using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using TextExpander.Application.Features.Hotkeys;

namespace TextExpander.Infrastructure.Platform.Windows;

/// <summary>
/// Dedicated Windows message-queue thread. Only WM_HOTKEY is observed; no hooks or key contents.
/// Prepare/commit/rollback keeps the previous registration active when a change fails.
/// </summary>
public sealed class WindowsPickerHotkeyService : IDisposable
{
    private const uint WmHotkey = 0x0312;
    private const uint WmAppCommand = 0x8001;
    private const uint WmAppStop = 0x8002;
    private const uint ModNoRepeat = 0x4000;
    private const uint PmNoRemove = 0;
    private readonly ConcurrentQueue<Command> _commands = new();
    private readonly TaskCompletionSource<uint> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Thread _thread;
    private readonly Action _onHotkey;
    private volatile uint _threadId;
    private volatile bool _disposed;
    private int _activeId;
    private PickerHotkey? _active;
    private int _stagedId;
    private PickerHotkey? _staged;
    private bool _hasStage;

    private enum Operation { Prepare, Commit, Cancel }
    private sealed record Command(Operation Operation, PickerHotkey Gesture,
        TaskCompletionSource<bool> Completion);

    public WindowsPickerHotkeyService(Action onHotkey)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        _onHotkey = onHotkey ?? throw new ArgumentNullException(nameof(onHotkey));
        _thread = new Thread(Run) { IsBackground = true, Name = "Ristretto WM_HOTKEY" };
        _thread.Start();
    }

    public Task<bool> PrepareAsync(PickerHotkey gesture) => EnqueueAsync(Operation.Prepare, gesture);
    public Task<bool> CommitAsync() => EnqueueAsync(Operation.Commit, default);
    public Task<bool> CancelAsync() => EnqueueAsync(Operation.Cancel, default);

    private async Task<bool> EnqueueAsync(Operation operation, PickerHotkey gesture)
    {
        var threadId = await _ready.Task.ConfigureAwait(false);
        if (threadId == 0 || _disposed) return false;
        var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var command = new Command(operation, gesture, done);
        _commands.Enqueue(command);
        if (!PostThreadMessageW(threadId, WmAppCommand, 0, 0))
        {
            // The command is still queued, but will not be executed if the loop ended.
            done.TrySetResult(false);
        }
        return await done.Task.ConfigureAwait(false);
    }

    private void Run()
    {
        try
        {
            // Force the thread message queue into existence before another thread posts to it.
            PeekMessageW(out _, IntPtr.Zero, 0, 0, PmNoRemove);
            _threadId = GetCurrentThreadId();
            _ready.TrySetResult(_threadId);
            while (true)
            {
                var result = GetMessageW(out var message, IntPtr.Zero, 0, 0);
                if (result <= 0 || message.Message == WmAppStop) break;
                if (message.Message == WmAppCommand)
                {
                    while (_commands.TryDequeue(out var command))
                    {
                        if (command.Completion.Task.IsCompleted) continue;
                        bool ok;
                        try { ok = Process(command); }
                        catch { ok = false; }
                        command.Completion.TrySetResult(ok);
                    }
                }
                else if (message.Message == WmHotkey && _activeId != 0 &&
                         message.WParam == (nuint)_activeId)
                {
                    try { _onHotkey(); }
                    catch { /* The UI can already be shutting down. */ }
                }
            }
        }
        catch { _ready.TrySetResult(0); }
        finally
        {
            if (_stagedId != 0 && _stagedId != _activeId) UnregisterHotKey(IntPtr.Zero, _stagedId);
            if (_activeId != 0) UnregisterHotKey(IntPtr.Zero, _activeId);
            while (_commands.TryDequeue(out var command)) command.Completion.TrySetResult(false);
            _ready.TrySetResult(0);
        }
    }

    private bool Process(Command command) => command.Operation switch
    {
        Operation.Prepare => Prepare(command.Gesture),
        Operation.Commit => Commit(),
        Operation.Cancel => Cancel(),
        _ => false
    };

    private bool Prepare(PickerHotkey gesture)
    {
        if (_hasStage || gesture.Canonical is null) return false;
        if (_active == gesture)
        {
            _staged = gesture;
            _stagedId = _activeId;
            _hasStage = true;
            return true;
        }
        var newId = 0;
        if (!gesture.IsDisabled)
        {
            newId = _activeId == 1 ? 2 : 1;
            if (!RegisterHotKey(IntPtr.Zero, newId, gesture.Modifiers | ModNoRepeat, gesture.VirtualKey))
                return false; // Keep the old registration intact on collision / OS denial.
        }
        _staged = gesture;
        _stagedId = newId;
        _hasStage = true;
        return true;
    }

    private bool Commit()
    {
        if (!_hasStage) return false;
        if (_activeId != 0 && _activeId != _stagedId)
            UnregisterHotKey(IntPtr.Zero, _activeId);
        _activeId = _stagedId;
        _active = _staged;
        _staged = null;
        _stagedId = 0;
        _hasStage = false;
        return true;
    }

    private bool Cancel()
    {
        if (!_hasStage) return false;
        if (_stagedId != 0 && _stagedId != _activeId)
            UnregisterHotKey(IntPtr.Zero, _stagedId);
        _staged = null;
        _stagedId = 0;
        _hasStage = false;
        return true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        var threadId = _threadId;
        if (threadId != 0) PostThreadMessageW(threadId, WmAppStop, 0, 0);
        // Never block application shutdown indefinitely if Windows is unresponsive.
        if (Thread.CurrentThread != _thread) _thread.Join(TimeSpan.FromSeconds(2));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public IntPtr Hwnd;
        public uint Message;
        public nuint WParam;
        public nint LParam;
        public uint Time;
        public int X;
        public int Y;
        public uint Private;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr window, int id);

    [DllImport("user32.dll", EntryPoint = "PeekMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessageW(out NativeMessage message, IntPtr window,
        uint filterMin, uint filterMax, uint removeMessage);

    [DllImport("user32.dll", EntryPoint = "GetMessageW")]
    private static extern int GetMessageW(out NativeMessage message, IntPtr window,
        uint filterMin, uint filterMax);

    [DllImport("user32.dll", EntryPoint = "PostThreadMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostThreadMessageW(uint threadId, uint message, nuint wParam, nint lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}
