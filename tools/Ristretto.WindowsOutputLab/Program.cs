using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using TextExpander.Application.Features.Output;
using TextExpander.Infrastructure.Platform.Windows;

namespace Ristretto.WindowsOutputLab;

/// <summary>
/// Laboratorio F2a separato da Ristretto.exe. L'UNICO destinatario ammesso e' un EDIT Win32
/// creato da un processo figlio appena lanciato da questo laboratorio, identificato con
/// handle + PID + handle del controllo. F2a SendInput resta separata dalle prove F2b
/// --replacement-test/--replacement-demo, limitate a controlli propri sul loro thread UI.
/// Nessun hook globale o clipboard.
/// </summary>
internal static class Program
{
    private const string Fixture = "Caffè ☕ cafe\u0301 漢字 مرحبا 🧑🏽‍💻";
    private const string PickerFixture = "Caffè ☕ cafe\u0301 漢字 مرحبا 🧑🏽‍💻\r\nSeconda riga: Ω 🚀";
    private const uint Visible = 0x10000000;
    private const uint Child = 0x40000000;
    private const uint Border = 0x00800000;
    private const uint OverlappedWindow = 0x00CF0000;
    private const uint TabStop = 0x00010000;
    private const uint EditAutoHScroll = 0x00000080;
    private const int GwlStyle = -16;
    private const uint EditPassword = 0x0020;
    private const uint EditReadOnly = 0x0800;
    private const uint EditMultiline = 0x0004;
    private const uint EditAutoVScroll = 0x0040;
    private const uint EditWantReturn = 0x1000;

    public static async Task<int> Main(string[] args)
    {
        // The synthetic fixture seeder never uses Win32 APIs or user profiles.
        if (args.Length == 2 && args[0] == "--seed-picker-profile")
        {
            try
            {
                var profile = await PickerNotepadProfileSeeder.SeedAsync(Directory.GetCurrentDirectory(), args[1]);
                Console.WriteLine("Synthetic SQLite profile: " + profile);
                Console.WriteLine("Trigger: " + PickerNotepadProfileSeeder.Trigger);
                Console.WriteLine("Run Ristretto with --data-dir \"" + profile + "\"");
                return 0;
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException
                                      or InvalidOperationException or System.Text.Json.JsonException
                                      or KeyNotFoundException)
            {
                Console.Error.WriteLine("Synthetic profile NOT created: " + ex.GetType().Name);
                return 2;
            }
        }
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("F2a: laboratorio nativo disponibile solo su Windows.");
            return 2;
        }
        if (args.Length == 1 && args[0] == "--remote-owned-smoke")
            return await RemoteOwnedEditLab.RunParent(null);
        if (args.Length == 3 && args[0] == "--remote-owned-smoke" && args[1] == "--report")
            return await RemoteOwnedEditLab.RunParent(args[2]);
        if (args.Length == 3 && args[0] == "--remote-owned-child")
            return RemoteOwnedEditLab.RunChild(args[1], args[2]);
        if (args.Length == 1 && args[0] == "--assess-external-adapter")
            return await ExternalAdapterCapabilityCommand.Run(null);
        if (args.Length == 3 && args[0] == "--assess-external-adapter" && args[1] == "--report")
            return await ExternalAdapterCapabilityCommand.Run(args[2]);
        if (args.Length == 1 && args[0] == "--inspect-classic-edit")
            return await ExternalClassicEditSelectionCommand.Run(null);
        if (args.Length == 3 && args[0] == "--inspect-classic-edit" && args[1] == "--report")
            return await ExternalClassicEditSelectionCommand.Run(args[2]);
        if (args.Length == 1 && args[0] == "--probe-external")
            return await ExternalEditorProbeCommand.Run(null);
        if (args.Length == 3 && args[0] == "--probe-external" && args[1] == "--report")
            return await ExternalEditorProbeCommand.Run(args[2]);
        if (args.Length == 1 && args[0] == "--picker-insert-demo")
            return await RunPickerInsertDemo();
        if (args.Length == 2 && args[0] == "--picker-target" &&
            args[1].Length == 32 && args[1].All(Uri.IsHexDigit))
            return RunChildTarget(args[1], multiline: true);
        if (args.Length == 1 && args[0] == "--self-test") return await RunSelfTest();
        if (args.Length == 1 && args[0] == "--replacement-test") return NativeReplacementLab.RunSelfTests();
        if (args.Length == 3 && args[0] == "--replacement-test" && args[1] == "--report")
            return NativeReplacementLab.RunSelfTests(args[2]);
        if (args.Length == 1 && args[0] == "--cooperating-smoke")
            return await CooperatingProcessLab.RunParent(null, smoke: true);
        if (args.Length == 1 && args[0] == "--cooperating-demo")
            return await CooperatingProcessLab.RunParent(null);
        if (args.Length == 3 && args[0] == "--cooperating-demo" && args[1] == "--report")
            return await CooperatingProcessLab.RunParent(args[2]);
        if (args.Length == 2 && args[0] == "--cooperating-child")
            return CooperatingProcessLab.RunChild(args[1]);
        if (args.Length == 2 && args[0] == "--cooperating-child-smoke")
            return CooperatingProcessLab.RunChild(args[1], smoke: true);
        if (args.Length == 1 && args[0] == "--replacement-demo") return NativeReplacementLab.RunDemo();
        if (args.Length == 3 && args[0] == "--replacement-demo" && args[1] == "--report")
            return NativeReplacementLab.RunDemo(args[2]);
        if (args.Length == 2 && args[0] == "--target" &&
            args[1].Length == 32 && args[1].All(Uri.IsHexDigit))
            return RunChildTarget(args[1]);
        Console.WriteLine("Uso: dotnet run --project tools/Ristretto.WindowsOutputLab -- --self-test");
        Console.WriteLine("Il test apre soltanto un proprio controllo Windows EDIT in un secondo processo.");
        Console.WriteLine("--probe-external [--report nuovo-file.json]: ricognizione Win32 in SOLA LETTURA, senza input.");
        Console.WriteLine("--inspect-classic-edit [--report nuovo-file.json]: EM_GETSEL read-only, senza testo/offset.");
        Console.WriteLine("--assess-external-adapter [--report nuovo-file.json]: matrice delle garanzie mancanti; nessuna sostituzione.");
        Console.WriteLine("--replacement-test: prove automatiche su EDIT Win32 propri, senza SendInput.");
        Console.WriteLine("--cooperating-demo [--report file-nuovo.json]: prova umana in secondo processo EDIT cooperante.");
        Console.WriteLine("--cooperating-smoke: prova nativa sintetica automatica in secondo processo.");
        Console.WriteLine("--remote-owned-smoke [--report file-nuovo.json]: controller chiede sostituzione sintetica in secondo processo, verifica esito e stale.");
        Console.WriteLine("--replacement-demo [--report file-nuovo.json]: digita ;firma + spazio nel proprio EDIT Win32.");
        Console.WriteLine("--picker-insert-demo: prova MANUALE Inserisci in un EDIT isolato, senza cancellare gli appunti dell'utente dal laboratorio.");
        Console.WriteLine("--seed-picker-profile TOKEN: profilo SQLite isolato per sessione Notepad preparata.");
        return 2;
    }

    private static async Task<int> RunSelfTest()
    {
        if (!UnicodeOutputPlan.TryCreate(Fixture, out var plan, out var reason) || plan is null)
        {
            Console.Error.WriteLine($"Fixture rifiutata: {reason}");
            return 2;
        }
        var path = Environment.ProcessPath ?? throw new InvalidOperationException("Process path unavailable");
        var info = new ProcessStartInfo(path)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };
        // In modalita' "dotnet <assembly>.dll" il processo corrente e' il dotnet host.
        if (string.Equals(Path.GetFileNameWithoutExtension(path), "dotnet", StringComparison.OrdinalIgnoreCase))
            info.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        info.ArgumentList.Add("--target");
        info.ArgumentList.Add(Guid.NewGuid().ToString("N"));

        using var child = Process.Start(info);
        if (child is null)
        {
            Console.Error.WriteLine("Impossibile creare il processo di test.");
            return 2;
        }
        try
        {
            var ready = await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10));
            if (!TryReadReady(ready, out var window, out var edit))
            {
                Console.Error.WriteLine("Processo target non pronto.");
                return 2;
            }
            Console.WriteLine("F2a: finestra di test isolata avviata. Nessun input e' inviato a questa console.");
            Console.WriteLine("Clicca nel campo bianco della finestra 'Ristretto F2a Test Target'.");
            Console.WriteLine("Il tentativo avviene tra 5 secondi SOLO se quel preciso campo e' in primo piano.");
            await Task.Delay(TimeSpan.FromSeconds(5));

            var snapshot = InspectTestTarget(window, edit, checked((uint)child.Id));
            var rejection = WindowsExplicitTestTargetGate.RejectionReason(snapshot);
            if (rejection is not null)
            {
                Console.WriteLine($"TEST NON ESEGUITO: preflight={rejection}. Nessun SendInput chiamato.");
                return 3;
            }

            // La cattura dell'adapter rilegge la finestra corrente, e Submit ricontrolla
            // handle, PID e modificatori. Non e' una transazione atomica di focus.
            var output = new WindowsUnicodeTextOutput();
            var captured = output.CaptureExternalForeground();
            if (captured is null || captured.WindowHandle != window || captured.ProcessId != (uint)child.Id)
            {
                Console.WriteLine("TEST NON ESEGUITO: il destinatario e' cambiato.");
                return 3;
            }
            var result = output.Submit(plan, captured);
            Console.WriteLine($"Invio: {result.Status} / {result.Reason} ({result.AcceptedInputEvents}/{result.RequiredInputEvents} eventi).");
            // Non assumiamo che SendInput abbia prodotto testo: confrontiamo il valore
            // dell'EDIT nello stesso processo figlio. Solo questo lab dispone del confronto.
            child.StandardInput.WriteLine("VERIFY");
            child.StandardInput.Flush();
            var verifiedLine = await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5));
            if (verifiedLine is null || !verifiedLine.StartsWith("RESULT|", StringComparison.Ordinal))
            {
                Console.WriteLine("ESITO NON VERIFICABILE: il target non ha risposto.");
                return 4;
            }
            string actual;
            try { actual = Encoding.Unicode.GetString(Convert.FromBase64String(verifiedLine[7..])); }
            catch (FormatException)
            {
                Console.WriteLine("ESITO NON VERIFICABILE: risposta non valida.");
                return 4;
            }
            // Non registrare mai il testo del controllo; stampiamo solo un booleano.
            var identical = string.Equals(actual, plan.Text, StringComparison.Ordinal);
            Console.WriteLine($"Confronto esatto UTF-16 nel controllo di test: {(identical ? "PASS" : "FAIL")}");
            return identical && result.Status == TextSubmissionStatus.SubmittedUnverified ? 0 : 4;
        }
        catch (TimeoutException)
        {
            Console.Error.WriteLine("Timeout del laboratorio; nessun retry automatico.");
            return 4;
        }
        finally
        {
            try
            {
                if (!child.HasExited)
                {
                    child.StandardInput.WriteLine("STOP");
                    child.StandardInput.Flush();
                    if (!child.WaitForExit(2000)) child.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException) { }
            catch (System.IO.IOException) { }
        }
    }

    /// <summary>
    /// Manually exercises the real Ristretto picker, its clipboard route and
    /// the Windows focus handoff against a dedicated synthetic multiline EDIT.
    /// This lab never changes the system clipboard itself, never sends keys,
    /// and cannot accidentally substitute the user's existing document.
    /// The user explicitly initiates the insertion through the running app.
    /// </summary>
    private static async Task<int> RunPickerInsertDemo()
    {
        if (Console.IsInputRedirected)
        {
            Console.WriteLine("NOT_RUN: la prova richiede una console interattiva e consenso esplicito.");
            return 3;
        }

        var path = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(path)) return 2;
        var start = new ProcessStartInfo(path)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };
        if (string.Equals(Path.GetFileNameWithoutExtension(path), "dotnet", StringComparison.OrdinalIgnoreCase))
            start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        var sessionToken = Guid.NewGuid().ToString("N");
        start.ArgumentList.Add("--picker-target");
        start.ArgumentList.Add(sessionToken);
        using var child = Process.Start(start);
        if (child is null) return 2;

        try
        {
            var ready = await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10));
            if (!PickerOwnedEditReceipt.TryReadReady(ready, sessionToken, out var window, out var edit))
            {
                Console.WriteLine("NOT_RUN: il controllo sintetico non e' pronto o la sessione non corrisponde.");
                return 3;
            }
            _ = GetWindowThreadProcessId(window, out var pid);
            _ = GetWindowThreadProcessId(edit, out var editPid);
            if (pid != checked((uint)child.Id) || editPid != pid || !IsWindow(window) || !IsWindow(edit))
            {
                Console.WriteLine("NOT_RUN: identita' del destinatario non verificata.");
                return 3;
            }
            Console.WriteLine("R2 laboratorio picker - nessuna modifica automatica agli appunti o ad altre finestre.");
            Console.WriteLine("1) Crea in un profilo sintetico uno snippet con questo contenuto ESATTO:");
            Console.WriteLine(PickerFixture);
            Console.WriteLine("2) Clicca nel campo del laboratorio e apri il picker con la hotkey.");
            Console.WriteLine("3) Seleziona lo snippet e premi Inserisci. NON usare Copia o un incolla manuale.");
            Console.WriteLine("4) Torna a questa console e premi Invio per verificare. Ctrl+C annulla.");
            if (Console.ReadLine() is null)
            {
                Console.WriteLine("NOT_RUN: conferma mancante.");
                return 3;
            }
            // The user has returned to the console, so focus cannot be asserted at
            // verification time. Only confirm the original child/control still exists.
            if (child.HasExited || !IsWindow(window) || !IsWindow(edit) ||
                GetWindowThreadProcessId(window, out var currentPid) == 0 ||
                currentPid != checked((uint)child.Id) ||
                GetWindowThreadProcessId(edit, out var currentEditPid) == 0 ||
                currentEditPid != currentPid)
            {
                Console.WriteLine("NOT_RUN: il controllo destinatario non e' piu' disponibile.");
                return 3;
            }
            child.StandardInput.WriteLine("VERIFY");
            await child.StandardInput.FlushAsync();
            var receipt = await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5));
            if (!PickerOwnedEditReceipt.TryReadResult(receipt, sessionToken, out var outcome))
            {
                Console.WriteLine("NOT_RUN: risposta assente, malformata o appartenente a un'altra sessione.");
                return 3;
            }
            switch (outcome)
            {
                case PickerOwnedEditReceipt.Outcome.Match:
                    Console.WriteLine("PASS: il processo proprietario ha confrontato esattamente il testo UTF-16 del controllo sintetico.");
                    return 0;
                case PickerOwnedEditReceipt.Outcome.Mismatch:
                    Console.WriteLine("FAIL: testo del controllo diverso dalla fixture; nessun retry automatico.");
                    return 1;
                default:
                    Console.WriteLine("NOT_RUN: il processo proprietario non ha potuto verificare il controllo.");
                    return 3;
            }
        }
        catch (TimeoutException)
        {
            Console.WriteLine("NOT_RUN: timeout; nessun nuovo invio automatico.");
            return 3;
        }
        catch (IOException)
        {
            Console.WriteLine("NOT_RUN: comunicazione con il laboratorio interrotta.");
            return 3;
        }
        finally
        {
            try
            {
                if (!child.HasExited)
                {
                    child.StandardInput.WriteLine("STOP");
                    await child.StandardInput.FlushAsync();
                    if (!child.WaitForExit(2000)) child.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException) { }
            catch (IOException) { }
        }
    }

    private static bool TryReadReady(string? text, out nint window, out nint edit)
    {
        window = 0;
        edit = 0;
        if (text is null) return false;
        var parts = text.Split('|');
        if (parts.Length != 3 || parts[0] != "READY" ||
            !long.TryParse(parts[1], out var w) || !long.TryParse(parts[2], out var e) ||
            w == 0 || e == 0) return false;
        window = (nint)w;
        edit = (nint)e;
        return true;
    }

    private static WindowsExplicitTestTargetSnapshot InspectTestTarget(nint expectedWindow, nint expectedEdit, uint pid)
    {
        var foreground = GetForegroundWindow();
        _ = GetWindowThreadProcessId(foreground, out var actualPid);
        var gui = new GuiThreadInfo { Size = (uint)Marshal.SizeOf<GuiThreadInfo>() };
        var focused = GetGUIThreadInfo(0, ref gui) ? gui.Focus : 0;
        var buffer = new StringBuilder(64);
        var className = GetClassNameW(expectedEdit, buffer, buffer.Capacity) > 0
            ? buffer.ToString() : string.Empty;
        var style = (uint)GetWindowLongW(expectedEdit, GwlStyle);
        return new WindowsExplicitTestTargetSnapshot(
            expectedWindow, foreground, pid, actualPid, expectedEdit, focused,
            string.Equals(className, "Edit", StringComparison.OrdinalIgnoreCase),
            IsWindowVisible(expectedEdit), IsWindowEnabled(expectedEdit),
            (style & EditPassword) != 0, (style & EditReadOnly) != 0,
            IsComposingOrUnknown: false); // Solo campo EDIT controllato dal laboratorio.
    }

    private sealed class ChildCommands
    {
        public volatile bool Stop;
        public volatile bool Verify;
    }

    private static int RunChildTarget(string token, bool multiline = false)
    {
        // Un controllo classico EDIT, creato nel processo figlio. No dati utente,
        // nessun gancio e nessuna persistenza. La finestra serve soltanto al test.
        var window = CreateWindowExW(0, "STATIC",
            (multiline ? "Ristretto Picker Test Target " : "Ristretto F2a Test Target ") + token[..6],
            OverlappedWindow | Visible, 200, 170,
            multiline ? 840 : 680, multiline ? 350 : 180, 0, 0, 0, 0);
        if (window == 0)
        {
            Console.WriteLine("ERROR|window");
            return 2;
        }
        var editStyle = Child | Visible | Border | TabStop |
            (multiline ? EditMultiline | EditWantReturn | EditAutoVScroll : EditAutoHScroll);
        var edit = CreateWindowExW(0, "EDIT", "", editStyle,
            22, 40, multiline ? 785 : 625, multiline ? 240 : 33, window, 0, 0, 0);
        if (edit == 0)
        {
            DestroyWindow(window);
            Console.WriteLine("ERROR|edit");
            return 2;
        }
        ShowWindow(window, 5);
        UpdateWindow(window);
        SetForegroundWindow(window);
        SetFocus(edit);
        Console.WriteLine(multiline
            ? PickerOwnedEditReceipt.Ready(token, window, edit)
            : $"READY|{window.ToInt64()}|{edit.ToInt64()}");
        Console.Out.Flush();
        var commands = new ChildCommands();
        var pickerAlreadyVerified = false;
        _ = Task.Run(() =>
        {
            while (!commands.Stop)
            {
                var command = Console.ReadLine();
                if (command is null || command == "STOP") { commands.Stop = true; break; }
                if (command == "VERIFY") commands.Verify = true;
            }
        });
        while (!commands.Stop)
        {
            while (PeekMessageW(out var msg, 0, 0, 0, 1))
            {
                if (msg.MessageId == 0x0012) { commands.Stop = true; break; }
                TranslateMessage(ref msg);
                DispatchMessageW(ref msg);
            }
            if (commands.Verify)
            {
                commands.Verify = false;
                if (multiline && pickerAlreadyVerified)
                {
                    // A second VERIFY must never reissue a successful receipt.
                    Console.WriteLine(PickerOwnedEditReceipt.Result(token, PickerOwnedEditReceipt.Outcome.Unavailable));
                    Console.Out.Flush();
                    continue;
                }
                if (multiline) pickerAlreadyVerified = true;
                if (multiline)
                {
                    // Only the owner accesses the EDIT text. No contents, offsets,
                    // clipboard data or typed characters leave this process.
                    var available = IsWindow(window) && IsWindow(edit);
                    string? text = null;
                    if (available)
                    {
                        var length = GetWindowTextLengthW(edit);
                        var sb = new StringBuilder(Math.Max(1, length + 1));
                        var copied = GetWindowTextW(edit, sb, sb.Capacity);
                        if (copied == length) text = sb.ToString();
                    }
                    var outcome = PickerOwnedEditReceipt.Compare(text, PickerFixture, available);
                    Console.WriteLine(PickerOwnedEditReceipt.Result(token, outcome));
                }
                else
                {
                    // Legacy F2a smoke test remains unchanged.
                    var length = GetWindowTextLengthW(edit);
                    var sb = new StringBuilder(Math.Max(1, length + 1));
                    _ = GetWindowTextW(edit, sb, sb.Capacity);
                    Console.WriteLine("RESULT|" + Convert.ToBase64String(Encoding.Unicode.GetBytes(sb.ToString())));
                }
                Console.Out.Flush();
            }
            Thread.Sleep(10);
        }
        DestroyWindow(window);
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect { public int Left; public int Top; public int Right; public int Bottom; }

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
        public Rect CaretRect;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Message
    {
        public nint Hwnd;
        public uint MessageId;
        public nint WParam;
        public nint LParam;
        public uint Time;
        public Point Pt;
        public uint Private;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "CreateWindowExW", SetLastError = true)]
    private static extern nint CreateWindowExW(uint exStyle, string className, string title, uint style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint param);
    [DllImport("user32.dll")] private static extern bool DestroyWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern bool ShowWindow(nint hwnd, int show);
    [DllImport("user32.dll")] private static extern bool UpdateWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern nint SetFocus(nint hwnd);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint hwnd, out uint pid);
    [DllImport("user32.dll")] private static extern bool GetGUIThreadInfo(uint threadId, ref GuiThreadInfo info);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetClassNameW")]
    private static extern int GetClassNameW(nint hwnd, StringBuilder className, int maxCount);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLongW(nint hwnd, int index);
    [DllImport("user32.dll")] private static extern bool IsWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")] private static extern bool IsWindowEnabled(nint hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetWindowTextLengthW")]
    private static extern int GetWindowTextLengthW(nint hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetWindowTextW")]
    private static extern int GetWindowTextW(nint hwnd, StringBuilder output, int maxCount);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "PeekMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessageW(out Message msg, nint hwnd, uint min, uint max, uint remove);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "TranslateMessage")]
    private static extern bool TranslateMessage(ref Message msg);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "DispatchMessageW")]
    private static extern nint DispatchMessageW(ref Message msg);
}
