using System.Runtime.InteropServices;
using TextExpander.Application.Features.Output;

namespace TextExpander.Infrastructure.Platform.Windows;

/// <summary>
/// Risorsa temporanea che identifica una specifica finestra esterna. NON prova che il campo
/// sia editabile né che l'IME o un campo password siano sicuri. Il componente non è attivato
/// dalla GUI fino a una futura integrazione con rilevamento del contesto.
/// </summary>
public sealed record WindowsOutputTarget(nint WindowHandle, uint ProcessId);

/// <summary>Confine per simulare le API Win32 nei test senza inviare tasti reali.</summary>
public interface IWindowsTextApi
{
    nint ForegroundWindow();
    uint WindowProcessId(nint window);
    bool IsAnyModifierPressed();
    int SendUnicodeInput(string utf16Text);
}

/// <summary>
/// Spike Windows inerte finché non chiamato da un caso d'uso approvato. Nessun hook globale,
/// nessuna cancellazione di trigger, nessun tentativo di ripristino e nessuna clipboard.
/// Una submission completa NON è una verifica di avvenuto inserimento nel controllo target.
/// </summary>
public sealed class WindowsUnicodeTextOutput
{
    private readonly IWindowsTextApi _api;
    private readonly Func<bool> _isWindows;
    private readonly uint _ourProcessId;

    public WindowsUnicodeTextOutput()
        : this(new Win32TextApi(), OperatingSystem.IsWindows, checked((uint)Environment.ProcessId)) { }

    // Dipendenze iniettate per test deterministici; nessuna P/Invoke invocata nei test.
    public WindowsUnicodeTextOutput(IWindowsTextApi api, Func<bool> isWindows, uint ourProcessId)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
        _ourProcessId = ourProcessId;
    }

    public WindowsOutputTarget? CaptureExternalForeground()
    {
        if (!_isWindows()) return null;
        try
        {
            var handle = _api.ForegroundWindow();
            if (handle == 0) return null;
            var process = _api.WindowProcessId(handle);
            if (process == 0 || process == _ourProcessId) return null;
            return new WindowsOutputTarget(handle, process);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    public TextSubmissionResult Submit(UnicodeOutputPlan? plan, WindowsOutputTarget? target)
    {
        if (!_isWindows()) return new(TextSubmissionStatus.Rejected, "not_windows");
        if (plan is null) return new(TextSubmissionStatus.Rejected, "invalid_plan");
        if (target is null || target.WindowHandle == 0 || target.ProcessId == 0 || target.ProcessId == _ourProcessId)
            return new(TextSubmissionStatus.Rejected, "invalid_target", plan.RequiredInputEvents);

        try
        {
            // Ricontrollo immediatamente prima della chiamata nativa. Non basta a garantire
            // il controllo dei campi sensibili, del focus durante l'input o degli IME.
            if (_api.ForegroundWindow() != target.WindowHandle ||
                _api.WindowProcessId(target.WindowHandle) != target.ProcessId)
                return new(TextSubmissionStatus.Rejected, "focus_changed", plan.RequiredInputEvents);
            if (_api.IsAnyModifierPressed())
                return new(TextSubmissionStatus.Rejected, "modifier_pressed", plan.RequiredInputEvents);

            var accepted = _api.SendUnicodeInput(plan.Text);
            if (accepted == plan.RequiredInputEvents)
                return new(TextSubmissionStatus.SubmittedUnverified, "os_stream_accepted",
                    plan.RequiredInputEvents, accepted);
            return new(TextSubmissionStatus.PartialOrUnknown, "os_partial_or_blocked",
                plan.RequiredInputEvents, Math.Max(0, accepted));
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // Potremmo non sapere se SendInput abbia già effettuato parte dell'operazione.
            return new(TextSubmissionStatus.PartialOrUnknown, "native_error_unknown_effect",
                plan.RequiredInputEvents);
        }
    }
}

/// <summary>Unico punto Win32. La pressione E il rilascio usano KEYEVENTF_UNICODE.</summary>
internal sealed class Win32TextApi : IWindowsTextApi
{
    private const uint InputKeyboard = 1;
    private const uint KeyEventUnicode = 0x0004;
    private const uint KeyEventKeyUp = 0x0002;
    private const int VkShift = 0x10;
    private const int VkControl = 0x11;
    private const int VkAlt = 0x12;
    private const int VkLWin = 0x5B;
    private const int VkRWin = 0x5C;

    [StructLayout(LayoutKind.Sequential)]
    private struct WinInput
    {
        public uint Type;
        public WinInputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct WinInputUnion
    {
        [FieldOffset(0)] public KeyboardInput Keyboard;
        [FieldOffset(0)] public MouseInput Mouse;
        [FieldOffset(0)] public HardwareInput Hardware;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public nuint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public nuint ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HardwareInput
    {
        public uint Message;
        public ushort ParamLow;
        public ushort ParamHigh;
    }

    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int virtualKey);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, [In] WinInput[] inputs, int inputSize);

    public nint ForegroundWindow() => GetForegroundWindow();
    public uint WindowProcessId(nint window)
    {
        _ = GetWindowThreadProcessId(window, out var id);
        return id;
    }

    public bool IsAnyModifierPressed() =>
        Down(VkShift) || Down(VkControl) || Down(VkAlt) || Down(VkLWin) || Down(VkRWin);

    private static bool Down(int virtualKey) => (GetAsyncKeyState(virtualKey) & 0x8000) != 0;

    public int SendUnicodeInput(string utf16Text)
    {
        // Un'unica chiamata evita di spezzare coppie surrogate / sequenze emoji
        // fra più invii. Il destinatario può comunque comportarsi diversamente.
        var input = new WinInput[checked(utf16Text.Length * 2)];
        for (var i = 0; i < utf16Text.Length; i++)
        {
            var unit = utf16Text[i];
            input[i * 2] = Key(unit, KeyEventUnicode);
            input[i * 2 + 1] = Key(unit, KeyEventUnicode | KeyEventKeyUp);
        }
        return checked((int)SendInput((uint)input.Length, input, Marshal.SizeOf<WinInput>()));
    }

    private static WinInput Key(char codeUnit, uint flags) => new()
    {
        Type = InputKeyboard,
        Data = new WinInputUnion
        {
            Keyboard = new KeyboardInput { VirtualKey = 0, ScanCode = codeUnit, Flags = flags }
        }
    };
}
