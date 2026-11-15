using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using TextExpander.Application.Features.Hotkeys;

namespace TextExpander.Infrastructure.Platform.Windows;

/// <summary>All snippet registrations share one message thread. Only registered WM_HOTKEY messages
/// are handled, with MOD_NOREPEAT; no keyboard hook or observation of ordinary typing.</summary>
public sealed class WindowsSnippetHotkeyService : IDisposable
{
    private const uint WmHotkey = 0x0312, WmApply = 0x8001, WmStop = 0x8002;
    private readonly ConcurrentQueue<Update> _updates = new();
    private readonly TaskCompletionSource<uint> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Action<SnippetHotkeyBinding> _onHotkey;
    private readonly Thread _thread;
    private readonly WindowsSnippetHotkeySet _registrations = new(
        (id, key) => RegisterHotKey(0, id, key.Modifiers | 0x4000, key.VirtualKey), id => UnregisterHotKey(0, id));
    private volatile uint _threadId;
    private volatile bool _disposed;
    private sealed record Update(SnippetHotkeyBinding[] Bindings,
        TaskCompletionSource<IReadOnlyDictionary<Guid, bool>> Completion);

    public WindowsSnippetHotkeyService(Action<SnippetHotkeyBinding> onHotkey)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        _onHotkey = onHotkey;
        _thread = new Thread(Run) { IsBackground = true, Name = "Ristretto snippet hotkeys" };
        _thread.Start();
    }

    public async Task<IReadOnlyDictionary<Guid, bool>> ApplyAsync(IReadOnlyList<SnippetHotkeyBinding> bindings)
    {
        var id = await _ready.Task.ConfigureAwait(false);
        if (_disposed || id == 0) return bindings.ToDictionary(b => b.SnippetId, _ => false);
        var completion = new TaskCompletionSource<IReadOnlyDictionary<Guid, bool>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _updates.Enqueue(new(bindings.ToArray(), completion));
        if (_disposed || !PostThreadMessageW(id, WmApply, 0, 0)) completion.TrySetResult(bindings.ToDictionary(b => b.SnippetId, _ => false));
        return await completion.Task.ConfigureAwait(false);
    }

    public static bool IsTriggerKeyDown(SnippetHotkeyBinding binding) =>
        OperatingSystem.IsWindows() && (GetAsyncKeyState((int)binding.Gesture.VirtualKey) & 0x8000) != 0;

    private void Run()
    {
        try
        {
            PeekMessageW(out _, 0, 0, 0, 0);
            _threadId = GetCurrentThreadId();
            _ready.TrySetResult(_threadId);
            if (_disposed) return;
            while (GetMessageW(out var message, 0, 0, 0) > 0)
            {
                if (message.Message == WmStop) break;
                if (message.Message == WmApply)
                {
                    while (_updates.TryDequeue(out var update))
                    {
                        if (update.Completion.Task.IsCompleted) continue;
                        try
                        {
                            var result = _registrations.Replace(update.Bindings, () =>
                            {
                                while (PeekMessageW(out _, 0, WmHotkey, WmHotkey, 1)) { }
                            });
                            update.Completion.TrySetResult(result);
                        }
                        catch { update.Completion.TrySetResult(update.Bindings.ToDictionary(b => b.SnippetId, _ => false)); }
                    }
                }
                else if (message.Message == WmHotkey)
                {
                    var chord = unchecked((uint)message.LParam);
                    var binding = _registrations.Resolve((int)message.WParam, chord & 0x000f, chord >> 16);
                    if (binding is null || unchecked((uint)Environment.TickCount64 - message.Time) > 1000) continue;
                    try { _onHotkey(binding); }
                    catch { /* Native callback must not terminate the message pump. */ }
                }
            }
        }
        catch { _ready.TrySetResult(0); }
        finally
        {
            _registrations.Clear();
            _disposed = true;
            while (_updates.TryDequeue(out var update))
                update.Completion.TrySetResult(update.Bindings.ToDictionary(b => b.SnippetId, _ => false));
            _ready.TrySetResult(0);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_threadId != 0) PostThreadMessageW(_threadId, WmStop, 0, 0);
        if (Thread.CurrentThread != _thread) _thread.Join(TimeSpan.FromSeconds(2));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public nint Hwnd;
        public uint Message;
        public nuint WParam;
        public nint LParam;
        public uint Time;
        public int X, Y;
        public uint Private;
    }
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(nint window, int id, uint modifiers, uint key);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(nint window, int id);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessageW(out NativeMessage message, nint window, uint min, uint max, uint remove);
    [DllImport("user32.dll")]
    private static extern int GetMessageW(out NativeMessage message, nint window, uint min, uint max);
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostThreadMessageW(uint threadId, uint message, nuint wParam, nint lParam);
    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int key);
}
