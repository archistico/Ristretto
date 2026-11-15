using System.Runtime.InteropServices;
using TextExpander.Application.Features.Input;
using TextExpander.Core;
using TextExpander.Core.Matching;

namespace Ristretto.WindowsOutputLab;

/// <summary>
/// R5 / Alpha 20.19. Esperimento opt-in e limitato nel tempo.
/// SetWinEventHook riceve SOLO variazioni foreground/focus, non caratteri,
/// keycode, contenuti del controllo, titoli, HWND o PID persistiti.
/// Non si collega al picker e non avvia sostituzioni.
/// </summary>
internal sealed class WindowsFocusBoundaryProbe : IDisposable
{
    private const uint EventSystemForeground = 0x0003;
    private const uint EventObjectFocus = 0x8005;
    private const uint WinEventOutOfContext = 0;
    private const uint WmQuit = 0x0012;
    private const uint PmNoRemove = 0;

    private readonly TaskCompletionSource<bool> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Thread _thread;
    private readonly WinEventCallback _callback;
    private readonly ExternalFocusBoundarySession _boundaries;
    private nint _foregroundHook;
    private nint _focusHook;
    private uint _threadId;
    private int _disposed;
    private int _callbackError;
    private long _foregroundSignals;
    private long _focusSignals;

    public WindowsFocusBoundaryProbe()
    {
        var empty = new LibrarySnapshot(Array.Empty<SnippetGroup>(), Array.Empty<Snippet>(), 1);
        _boundaries = new ExternalFocusBoundarySession(
            new TriggerActivationSession(empty), new CommittedInputSession(empty));
        _callback = OnWinEvent;
        _thread = new Thread(Run) { IsBackground = true, Name = "Ristretto focus-only probe" };
    }

    public Task<bool> StartAsync()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        _thread.Start();
        return _ready.Task;
    }

    public static async Task<int> RunCommandAsync(int seconds)
    {
        if (seconds is < 3 or > 30)
        {
            Console.Error.WriteLine("ERRORE: usare una durata compresa tra 3 e 30 secondi.");
            return 2;
        }
        if (!Environment.UserInteractive)
        {
            Console.WriteLine("NOT_RUN: è necessaria una sessione desktop interattiva.");
            return 3;
        }

        using var probe = new WindowsFocusBoundaryProbe();
        bool started;
        try { started = await probe.StartAsync().WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (TimeoutException)
        {
            Console.WriteLine("NOT_RUN: osservatore focus non avviato.");
            return 3;
        }
        if (!started)
        {
            Console.WriteLine("NOT_RUN: Windows ha rifiutato la sottoscrizione agli eventi focus.");
            return 3;
        }

        Console.WriteLine("Osservazione attiva per " + seconds + " secondi.");
        Console.WriteLine("Cambia finestra o campo attivo, per esempio tra Blocco note e Ristretto.");
        Console.WriteLine("Non vengono letti tasti, caratteri, testi, titoli, HWND o PID.");
        Console.WriteLine("Nessun hook di tastiera, nessuna modifica a documenti o appunti.");
        await Task.Delay(TimeSpan.FromSeconds(seconds));
        probe.Dispose();

        Console.WriteLine("Variazioni di finestra rilevate: " + Interlocked.Read(ref probe._foregroundSignals));
        Console.WriteLine("Variazioni di controllo/focus rilevate: " + Interlocked.Read(ref probe._focusSignals));
        Console.WriteLine("Barriere del riconoscimento: " + probe._boundaries.BoundaryCount);
        if (Volatile.Read(ref probe._callbackError) != 0)
        {
            Console.WriteLine("NOT_RUN: errore durante i callback; nessuna garanzia di copertura.");
            return 3;
        }
        if (Interlocked.Read(ref probe._foregroundSignals) +
            Interlocked.Read(ref probe._focusSignals) == 0)
        {
            Console.WriteLine("NOT_RUN: nessuna variazione focus osservata nella sessione; riprovare cambiando finestra.");
            return 3;
        }
        Console.WriteLine("PASS: notifiche focus osservate e osservatore arrestato; NON abilita espansione.");
        return 0;
    }

    private void Run()
    {
        try
        {
            // Il thread deve possedere una message queue prima di registrare il callback.
            PeekMessageW(out _, 0, 0, 0, PmNoRemove);
            _threadId = GetCurrentThreadId();
            _foregroundHook = SetWinEventHook(EventSystemForeground, EventSystemForeground,
                0, _callback, 0, 0, WinEventOutOfContext);
            if (_foregroundHook == 0) { _ready.TrySetResult(false); return; }
            _focusHook = SetWinEventHook(EventObjectFocus, EventObjectFocus,
                0, _callback, 0, 0, WinEventOutOfContext);
            if (_focusHook == 0) { _ready.TrySetResult(false); return; }
            _ready.TrySetResult(true);

            int result;
            while ((result = GetMessageW(out _, 0, 0, 0)) > 0) { }
            if (result < 0) Volatile.Write(ref _callbackError, 1);
        }
        catch
        {
            Volatile.Write(ref _callbackError, 1);
            _ready.TrySetResult(false);
        }
        finally
        {
            if (_focusHook != 0) UnhookWinEvent(_focusHook);
            if (_foregroundHook != 0) UnhookWinEvent(_foregroundHook);
            _ready.TrySetResult(false);
        }
    }

    private void OnWinEvent(nint hook, uint kind, nint window,
        int objectId, int childId, uint eventThread, uint eventTime)
    {
        // I parametri identificativi sono intenzionalmente ignorati.
        try
        {
            if (kind == EventSystemForeground)
            {
                _boundaries.Observe(ExternalFocusBoundaryKind.ForegroundChanged);
                Interlocked.Increment(ref _foregroundSignals);
            }
            else if (kind == EventObjectFocus)
            {
                _boundaries.Observe(ExternalFocusBoundaryKind.ControlFocusChanged);
                Interlocked.Increment(ref _focusSignals);
            }
        }
        catch { Volatile.Write(ref _callbackError, 1); }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        var threadId = _threadId;
        if (threadId != 0) PostThreadMessageW(threadId, WmQuit, 0, 0);
        if (_thread.IsAlive && Thread.CurrentThread != _thread)
            if (!_thread.Join(TimeSpan.FromSeconds(3))) Volatile.Write(ref _callbackError, 1);
    }

    private delegate void WinEventCallback(nint hook, uint kind, nint window,
        int objectId, int childId, uint eventThread, uint eventTime);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public nint Hwnd;
        public uint Message;
        public nuint WParam;
        public nint LParam;
        public uint Time;
        public int X;
        public int Y;
        public uint Private;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWinEventHook(uint min, uint max, nint module,
        WinEventCallback callback, uint processId, uint threadId, uint flags);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWinEvent(nint hook);
    [DllImport("user32.dll", EntryPoint = "PeekMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessageW(out NativeMessage message, nint window,
        uint first, uint last, uint remove);
    [DllImport("user32.dll", EntryPoint = "GetMessageW")]
    private static extern int GetMessageW(out NativeMessage message, nint window, uint first, uint last);
    [DllImport("user32.dll", EntryPoint = "PostThreadMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostThreadMessageW(uint threadId, uint message, nuint wParam, nint lParam);
    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}
