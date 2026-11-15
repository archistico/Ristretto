using System.Runtime.InteropServices;
using TextExpander.Core;
using TextExpander.Infrastructure.Platform.Windows;

namespace Ristretto.WindowsOutputLab;

/// <summary>
/// Esperimento ESPLICITO e TEMPORANEO di osservazione WinEvent, in sola lettura.
/// Non installa WH_KEYBOARD_LL, non registra testo/tasti e non sostituisce contenuto.
/// SetWinEventHook non garantisce che ogni edit invii EVENT_OBJECT_VALUECHANGE.
/// </summary>
internal sealed class ExternalClassicEditLiveCommand : IDisposable
{
    private const uint EventSystemForeground = 0x0003;
    private const uint EventObjectFocus = 0x8005;
    private const uint EventObjectValueChange = 0x800E;
    private const uint WinEventOutOfContext = 0;
    private const uint WinEventSkipOwnProcess = 0x0002;
    private const uint WmQuit = 0x0012;
    private readonly ExternalClassicEditLiveSession _session = new();
    private readonly ExternalClassicEditTriggerPreviewSession? _preview;
    private readonly WinEventCallback _callback;
    private readonly Thread _thread;
    private readonly TaskCompletionSource<bool> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private nint _valueHook;
    private nint _focusHook;
    private nint _foregroundHook;
    private uint _threadId;
    private int _disposed;
    private int _callbackError;
    private long _valueSignals;
    private long _candidates;
    private long _rejected;
    private long _boundaries;
    private long _unavailable;
    private long _potentialTriggers;
    private long _ambiguousTriggers;

    private ExternalClassicEditLiveCommand(ExternalClassicEditReadResult initial, bool preview)
    {
        _session.Observe(initial);
        if (preview)
        {
            _preview = new ExternalClassicEditTriggerPreviewSession(SyntheticPreviewCatalog());
            _preview.Observe(initial, Environment.TickCount64);
        }
        _callback = OnWinEvent;
        _thread = new Thread(Pump) { Name = "Ristretto R5 Edit live (solo ricerca)", IsBackground = true };
    }

    private static LibrarySnapshot SyntheticPreviewCatalog()
    {
        // Catalogo FISSO del laboratorio, senza accedere a SQLite personale.
        var groupId = Guid.Parse("a276db2f-0f7f-4eea-8de3-0712afc44a01");
        var group = new SnippetGroup(groupId, "Solo prova", true);
        var snippet = new Snippet(
            Guid.Parse("ba4c0902-83c2-482e-8bb5-b0a5cb3d5e01"), groupId,
            "Firma sintetica", ";firma", "CONTENUTO SINTETICO", "", true, 1);
        return new LibrarySnapshot([group], [snippet], 1);
    }

    public static async Task<int> RunAsync(int seconds, bool preview = false)
    {
        if (seconds is < 5 or > 30)
        {
            Console.Error.WriteLine("ERRORE: la prova deve durare da 5 a 30 secondi.");
            return 2;
        }
        if (!OperatingSystem.IsWindows() || !Environment.UserInteractive)
        {
            Console.WriteLine("NOT_RUN: la prova richiede Windows e un desktop interattivo.");
            return 3;
        }
        Console.WriteLine("R5 — Osservazione TEMPORANEA delle modifiche in un Edit Win32 esterno.");
        Console.WriteLine("ATTENZIONE: durante la prova legge fino a 4096 unità UTF-16 in RAM.");
        Console.WriteLine("Usa solo un campo di PROVA senza informazioni personali, password o contenuti riservati.");
        Console.WriteLine("Non registra il testo, non installa hook di tastiera e non invia modifiche.");
        Console.WriteLine("Entro 5 secondi attiva il controllo Edit di prova e non digitare ancora.");
        await Task.Delay(TimeSpan.FromSeconds(5));
        var first = WindowsExternalClassicEditTextReader.Capture();
        if (!first.IsObserved)
        {
            Console.WriteLine($"NOT_RUN: impossibile stabilire un contesto iniziale affidabile ({first.Status}).");
            Console.WriteLine("I browser, i RichEdit e il Notepad moderno possono non usare un Edit Win32 classico.");
            return 3;
        }

        using var probe = new ExternalClassicEditLiveCommand(first, preview);
        bool started;
        try { started = await probe.StartAsync().WaitAsync(TimeSpan.FromSeconds(5)); }
        catch (TimeoutException)
        {
            Console.WriteLine("NOT_RUN: scaduto il tempo per l'avvio dell'osservatore.");
            return 3;
        }
        if (!started)
        {
            Console.WriteLine("NOT_RUN: WinEvent non ha autorizzato l'osservazione.");
            return 3;
        }

        Console.WriteLine($"Osservazione attiva per {seconds} secondi: digita TESTO SINTETICO nel campo di prova.");
        Console.WriteLine("Una modifica compatibile NON prova la digitazione umana e NON autorizza l'espansione.");
        if (preview)
            Console.WriteLine("ANTEPRIMA: nel campo sintetico digita ;firma seguito da UNO spazio. Nessun testo verrà sostituito.");
        await Task.Delay(TimeSpan.FromSeconds(seconds));
        probe.Dispose();
        var valueSignals = Interlocked.Read(ref probe._valueSignals);
        var candidates = Interlocked.Read(ref probe._candidates);
        Console.WriteLine($"Notifiche di modifica ricevute: {valueSignals}");
        Console.WriteLine($"Inserimenti plausibili (senza prova d'origine): {candidates}");
        Console.WriteLine($"Modifiche non ammesse: {Interlocked.Read(ref probe._rejected)}");
        Console.WriteLine($"Cambi di focus e barriere: {Interlocked.Read(ref probe._boundaries)}");
        Console.WriteLine($"Letture non disponibili: {Interlocked.Read(ref probe._unavailable)}");
        if (preview)
        {
            Console.WriteLine($"Possibili trigger sintetici riconosciuti: {Interlocked.Read(ref probe._potentialTriggers)}");
            Console.WriteLine($"Trigger ambigui: {Interlocked.Read(ref probe._ambiguousTriggers)}");
            Console.WriteLine("L'origine delle modifiche NON è verificata: anche un incollamento può apparire come trigger.");
        }
        if (Volatile.Read(ref probe._callbackError) != 0)
        {
            Console.WriteLine("NOT_RUN: l'osservatore ha rilevato un errore, risultati non affidabili.");
            return 3;
        }
        if (preview && Interlocked.Read(ref probe._potentialTriggers) == 0)
        {
            Console.WriteLine("NOT_RUN: nessun trigger di prova completo osservato. Nessuna espansione effettuata.");
            return 3;
        }
        if (valueSignals == 0 || candidates == 0)
        {
            Console.WriteLine("NOT_RUN: notifiche assenti o nessun inserimento qualificabile. La copertura WinEvent non è universale.");
            return 3;
        }
        Console.WriteLine("PASS: osservato almeno un candidato Unicode in un Edit classico. Sostituzione ESTERNA NON autorizzata.");
        return 0;
    }

    private Task<bool> StartAsync()
    {
        _thread.Start();
        return _ready.Task;
    }

    private void Pump()
    {
        try
        {
            // La queue deve esistere prima della registrazione degli hook out-of-context.
            PeekMessageW(out _, 0, 0, 0, 0);
            _threadId = GetCurrentThreadId();
            var flags = WinEventOutOfContext | WinEventSkipOwnProcess;
            _foregroundHook = SetWinEventHook(EventSystemForeground, EventSystemForeground, 0, _callback, 0, 0, flags);
            if (_foregroundHook == 0) { _ready.TrySetResult(false); return; }
            _focusHook = SetWinEventHook(EventObjectFocus, EventObjectFocus, 0, _callback, 0, 0, flags);
            if (_focusHook == 0) { _ready.TrySetResult(false); return; }
            _valueHook = SetWinEventHook(EventObjectValueChange, EventObjectValueChange, 0, _callback, 0, 0, flags);
            if (_valueHook == 0) { _ready.TrySetResult(false); return; }
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
            if (_valueHook != 0) UnhookWinEvent(_valueHook);
            if (_focusHook != 0) UnhookWinEvent(_focusHook);
            if (_foregroundHook != 0) UnhookWinEvent(_foregroundHook);
            _session.Reset();
            _preview?.Reset();
            _ready.TrySetResult(false);
        }
    }

    private void OnWinEvent(nint hook, uint kind, nint window,
        int objectId, int childId, uint eventThread, uint eventTime)
    {
        try
        {
            if (kind is EventSystemForeground or EventObjectFocus)
            {
                _session.Reset();
                _preview?.Reset();
                Interlocked.Increment(ref _boundaries);
                return;
            }
            if (kind != EventObjectValueChange || window == 0 || childId != 0 || (objectId != 0 && objectId != -4))
                return;
            Interlocked.Increment(ref _valueSignals);
            // CaptureForControl verifica HWND, processo, focus e stati protetti PRIMA di leggere il testo.
            var read = WindowsExternalClassicEditTextReader.CaptureForControl(window);
            if (read.Status == ExternalClassicEditReadStatus.FocusChanged)
            {
                _session.Reset(); // Non mantenere un contesto attraversando un focus non riconosciuto.
                _preview?.Reset();
                Interlocked.Increment(ref _boundaries);
                return;
            }
            var step = _session.Observe(read);
            var previewStep = _preview?.Observe(read, Environment.TickCount64);
            if (previewStep?.Status == ExternalClassicEditTriggerPreviewStatus.PotentialTrigger)
                Interlocked.Increment(ref _potentialTriggers);
            if (previewStep?.Status == ExternalClassicEditTriggerPreviewStatus.AmbiguousTrigger)
                Interlocked.Increment(ref _ambiguousTriggers);
            switch (step.Status)
            {
                case ExternalClassicEditLiveStatus.CandidateInsertion:
                    Interlocked.Increment(ref _candidates);
                    break;
                case ExternalClassicEditLiveStatus.DeltaRejected:
                    Interlocked.Increment(ref _rejected);
                    break;
                case ExternalClassicEditLiveStatus.ObservationUnavailable:
                    Interlocked.Increment(ref _unavailable);
                    break;
            }
        }
        catch
        {
            _session.Reset();
            _preview?.Reset();
            Volatile.Write(ref _callbackError, 1);
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        var threadId = _threadId;
        if (threadId != 0 && !PostThreadMessageW(threadId, WmQuit, 0, 0))
            Volatile.Write(ref _callbackError, 1);
        if (_thread.IsAlive && Thread.CurrentThread != _thread && !_thread.Join(TimeSpan.FromSeconds(3)))
            Volatile.Write(ref _callbackError, 1);
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
