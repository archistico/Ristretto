using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using TextExpander.Application.Features.Hotkeys;
using TextExpander.Core;
using TextExpander.Infrastructure.Platform.Windows;

namespace Ristretto.WindowsOutputLab;

/// <summary>Opt-in registration receipts from the production workers. No windows, hooks,
/// SendInput, clipboard, personal profile or synthetic WM_HOTKEY messages.</summary>
internal sealed class SnippetHotkeyRegistrationLab
{
    private static readonly string[] Candidates = Enumerable.Range(6, 6)
        .Select(n => $"Ctrl+Alt+Shift+F{n}").ToArray();
    private static readonly string[] CaseNames =
    [
        "register_two", "collision_isolated", "remove_releases_only_removed", "disable_snippet",
        "disable_group", "reenable_group", "change_hotkey", "remove_assignment", "delete_snippet",
        "dispose_releases", "cross_process_collision", "process_exit_releases", "picker_collision",
        "picker_failed_change_preserves_previous", "picker_cancel_releases_stage",
        "picker_commit_releases_previous", "picker_dispose_releases", "capture_cancel_restores",
        "capture_confirm_restores_saved", "capture_failure_restores", "immediate_dispose_20_cycles"
    ];
    private sealed record CaseResult(string Id, string Status, string Detail);
    private readonly Dictionary<string, CaseResult> _cases = CaseNames.ToDictionary(id => id,
        id => new CaseResult(id, "NOT_RUN", "Bloccato da verifica precedente"));
    private readonly Guid _group = Guid.NewGuid();
    private int _callbacks;
    private string[] _selected = [];
    private const int TimeoutSeconds = 5;

    private WindowsSnippetHotkeyService Worker() => new(_ => Interlocked.Increment(ref _callbacks));
    private WindowsPickerHotkeyService Picker() => new(() => Interlocked.Increment(ref _callbacks));
    private static Task<T> Bounded<T>(Task<T> task) => task.WaitAsync(TimeSpan.FromSeconds(TimeoutSeconds));
    private static void Require(bool condition) { if (!condition) throw new InvalidOperationException("Unexpected registration receipt"); }
    private static PickerHotkey Key(string text) => PickerHotkey.TryParse(text, out var key) ? key : throw new ArgumentException();
    private static void Expect(IReadOnlyDictionary<Guid, bool> states, params (Guid Id, bool Active)[] expected)
    {
        Require(states.Count == expected.Length);
        foreach (var (id, active) in expected) Require(states.TryGetValue(id, out var actual) && actual == active);
    }
    private static Task<IReadOnlyDictionary<Guid, bool>> Apply(WindowsSnippetHotkeyService service,
        params SnippetHotkeyBinding[] bindings) => Bounded(service.ApplyAsync(bindings));
    private Snippet Snippet(string hotkey) => new(Guid.NewGuid(), _group, "Synthetic", ";test", "Synthetic", "", true, 1, hotkey);
    private SnippetHotkeyBinding[] Catalog(bool groupEnabled, params Snippet[] snippets) =>
        new SnippetHotkeyCatalog(new LibrarySnapshot([new(_group, "Synthetic", groupEnabled)], snippets, 1),
            "None", 1).Bindings.ToArray();

    private async Task Case(string id, Func<Task> action)
    {
        try
        {
            await action();
            _cases[id] = new(id, "PASS", "Ricevute native conformi");
            Console.WriteLine("PASS " + id);
        }
        catch (Exception ex)
        {
            _cases[id] = new(id, "FAIL", ex.GetType().Name);
            Console.WriteLine("FAIL " + id + ": " + ex.GetType().Name);
            throw;
        }
    }

    public static async Task<int> RunAsync(string? reportPath)
    {
        // Reserve the report before doing anything native, never overwrite earlier evidence.
        FileStream? output;
        try { output = reportPath is null ? null : new FileStream(reportPath, FileMode.CreateNew, FileAccess.Write); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Console.Error.WriteLine("Report non creato: scegliere un file nuovo in una cartella accessibile.");
            return 2;
        }
        using var report = output;
        var lab = new SnippetHotkeyRegistrationLab();
        var result = "NOT_RUN";
        var detail = "Windows interattivo e tre combinazioni libere richiesti";
        try
        {
            if (!OperatingSystem.IsWindows() || !Environment.UserInteractive) return 3;
            Console.WriteLine("Registrazioni temporanee Ctrl+Alt+Shift+F6..F11; nessun input o appunti.");
            using (var discovery = lab.Worker())
            {
                var candidates = Candidates.Select(k => new SnippetHotkeyBinding(Guid.NewGuid(), 1, Key(k), 1)).ToArray();
                var states = await Bounded(discovery.ApplyAsync(candidates));
                lab._selected = candidates.Where(b => states.TryGetValue(b.SnippetId, out var ok) && ok)
                    .Take(3).Select(b => b.Gesture.Canonical).ToArray();
            }
            if (lab._selected.Length < 3) return 3;
            await lab.SuiteAsync();
            Require(Volatile.Read(ref lab._callbacks) == 0);
            result = "PASS";
            detail = "Registrazione, conflitto e rilascio; nessuna prova di digitazione o inserimento";
            return 0;
        }
        catch (Exception ex)
        {
            result = "FAIL";
            detail = ex.GetType().Name;
            return 1;
        }
        finally
        {
            var evidence = new
            {
                Schema = "ristretto/snippet-hotkey-registrations/1", TimestampUtc = DateTimeOffset.UtcNow,
                SourceVersion = typeof(SnippetHotkeyRegistrationLab).Assembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0],
                WindowsVersion = Environment.OSVersion.VersionString, UserInteractive = Environment.UserInteractive,
                Result = result, Detail = detail, SelectedSyntheticHotkeys = lab._selected,
                Cases = CaseNames.Select(id => lab._cases[id]).ToArray(), CallbacksObserved = lab._callbacks,
                PhysicalKeyboard = "NOT_RUN", Clipboard = "NOT_RUN", ExternalEditorInsertion = "NOT_RUN",
                DesktopAppOrchestration = "NOT_RUN"
            };
            if (report is not null) JsonSerializer.Serialize(report, evidence, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine("SNIPPET HOTKEY REGISTRATIONS: " + result);
        }
    }

    private async Task SuiteAsync()
    {
        var first = Snippet(_selected[0]);
        var second = Snippet(_selected[1]);
        var third = Snippet(_selected[2]);
        var a = Catalog(true, first)[0];
        var b = Catalog(true, second)[0];
        var c = Catalog(true, third)[0];
        WindowsSnippetHotkeyService? owner = Worker();
        WindowsPickerHotkeyService? picker = null;
        using var probe = Worker();
        try
        {
            await Case("register_two", async () => Expect(await Apply(owner, a, b), (a.SnippetId, true), (b.SnippetId, true)));
            await Case("collision_isolated", async () =>
            {
                Expect(await Apply(probe, a, c), (a.SnippetId, false), (c.SnippetId, true));
                await Apply(probe);
            });
            await Case("remove_releases_only_removed", async () =>
            {
                Expect(await Apply(owner, b), (b.SnippetId, true));
                Expect(await Apply(probe, a, b), (a.SnippetId, true), (b.SnippetId, false));
                await Apply(probe);
            });
            await Case("disable_snippet", async () =>
            {
                Expect(await Apply(owner, a, b), (a.SnippetId, true), (b.SnippetId, true));
                Expect(await Apply(owner, Catalog(true, first with { IsEnabled = false }, second)), (b.SnippetId, true));
                Expect(await Apply(probe, a), (a.SnippetId, true));
                await Apply(probe);
            });
            await Case("disable_group", async () =>
            {
                Expect(await Apply(owner, Catalog(false, first, second)));
                Expect(await Apply(probe, a, b), (a.SnippetId, true), (b.SnippetId, true));
                await Apply(probe);
            });
            await Case("reenable_group", async () => Expect(await Apply(owner, Catalog(true, first, second)),
                (a.SnippetId, true), (b.SnippetId, true)));
            await Case("change_hotkey", async () =>
            {
                var changed = Catalog(true, first with { Hotkey = c.Gesture.Canonical, Revision = 2 }, second);
                Expect(await Apply(owner, changed), (a.SnippetId, true), (b.SnippetId, true));
                Expect(await Apply(probe, a, c), (a.SnippetId, true), (c.SnippetId, false));
                await Apply(probe);
            });
            await Case("remove_assignment", async () =>
            {
                Expect(await Apply(owner, Catalog(true, first with { Hotkey = "" }, second)), (b.SnippetId, true));
                Expect(await Apply(probe, a, c), (a.SnippetId, true), (c.SnippetId, true));
                await Apply(probe);
            });
            await Case("delete_snippet", async () =>
            {
                Expect(await Apply(owner, Catalog(true, first)), (a.SnippetId, true));
                Expect(await Apply(probe, b), (b.SnippetId, true));
                await Apply(probe);
            });
            await Case("dispose_releases", async () =>
            {
                owner.Dispose();
                Expect(await Apply(owner, a), (a.SnippetId, false));
                Expect(await Apply(probe, a, b, c), (a.SnippetId, true), (b.SnippetId, true), (c.SnippetId, true));
                await Apply(probe);
                owner = Worker();
            });
            await Case("cross_process_collision", () => CrossProcessAsync(owner, a, b, kill: false));
            await Case("process_exit_releases", () => CrossProcessAsync(owner, a, b, kill: true));

            picker = Picker();
            await Case("picker_collision", async () =>
            {
                Require(await Bounded(picker.PrepareAsync(a.Gesture)) && await Bounded(picker.CommitAsync()));
                Expect(await Apply(owner, a, b), (a.SnippetId, false), (b.SnippetId, true));
            });
            await Case("picker_failed_change_preserves_previous", async () =>
            {
                Require(!await Bounded(picker.PrepareAsync(b.Gesture)));
                Expect(await Apply(probe, a), (a.SnippetId, false));
            });
            await Case("picker_cancel_releases_stage", async () =>
            {
                Require(await Bounded(picker.PrepareAsync(c.Gesture)) && await Bounded(picker.CancelAsync()));
                Expect(await Apply(probe, a, c), (a.SnippetId, false), (c.SnippetId, true));
                await Apply(probe);
            });
            await Case("picker_commit_releases_previous", async () =>
            {
                Require(await Bounded(picker.PrepareAsync(c.Gesture)) && await Bounded(picker.CommitAsync()));
                Expect(await Apply(probe, a, c), (a.SnippetId, true), (c.SnippetId, false));
                await Apply(probe);
            });
            await Case("picker_dispose_releases", async () =>
            {
                picker.Dispose();
                Require(!await Bounded(picker.PrepareAsync(c.Gesture)));
                Expect(await Apply(probe, c), (c.SnippetId, true));
                await Apply(probe);
            });

            foreach (var mode in new[] { "cancel", "confirm", "failure" })
            {
                var id = mode == "confirm" ? "capture_confirm_restores_saved" : "capture_" + mode + "_restores";
                await Case(id, async () =>
                {
                    Expect(await Apply(owner, a, b), (a.SnippetId, true), (b.SnippetId, true));
                    picker.Dispose();
                    picker = Picker();
                    Require(await Bounded(picker.PrepareAsync(c.Gesture)) && await Bounded(picker.CommitAsync()));
                    var coordinator = new HotkeyCaptureCoordinator();
                    string? result = null;
                    try
                    {
                        result = await coordinator.RunAsync(
                            () => { Require(coordinator.IsActive); owner.Dispose(); picker.Dispose(); return Task.CompletedTask; },
                            async () =>
                            {
                                Require(coordinator.IsActive);
                                owner = Worker();
                                picker = Picker();
                                Require(await Bounded(picker.PrepareAsync(c.Gesture)) && await Bounded(picker.CommitAsync()));
                                Expect(await Apply(owner, a, b), (a.SnippetId, true), (b.SnippetId, true));
                            }, async () =>
                            {
                                Require(coordinator.IsActive);
                                Expect(await Apply(probe, a, b, c), (a.SnippetId, true), (b.SnippetId, true), (c.SnippetId, true));
                                await Apply(probe);
                                if (mode == "failure") throw new SyntheticCaptureException();
                                return mode == "confirm" ? c.Gesture.Canonical : null;
                            });
                    }
                    catch (SyntheticCaptureException) when (mode == "failure") { }
                    Require(!coordinator.IsActive && result == (mode == "confirm" ? c.Gesture.Canonical : null));
                    Expect(await Apply(probe, a, b, c), (a.SnippetId, false), (b.SnippetId, false), (c.SnippetId, false));
                });
            }
            owner.Dispose();
            picker.Dispose();
            await Case("immediate_dispose_20_cycles", async () =>
            {
                for (var i = 0; i < 20; i++)
                {
                    using var early = Worker();
                    using var earlyPicker = Picker();
                    early.Dispose();
                    earlyPicker.Dispose();
                    Expect(await Apply(early, a), (a.SnippetId, false));
                    Require(!await Bounded(earlyPicker.PrepareAsync(c.Gesture)));
                    Expect(await Apply(probe, a, b, c), (a.SnippetId, true), (b.SnippetId, true), (c.SnippetId, true));
                    await Apply(probe);
                }
            });
        }
        finally { owner?.Dispose(); picker?.Dispose(); }
    }

    private sealed class SyntheticCaptureException : Exception;

    private async Task CrossProcessAsync(WindowsSnippetHotkeyService owner, SnippetHotkeyBinding a,
        SnippetHotkeyBinding b, bool kill)
    {
        await Apply(owner);
        var executable = Environment.ProcessPath ?? throw new InvalidOperationException();
        var info = new ProcessStartInfo(executable)
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true,
            RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8
        };
        if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            info.ArgumentList.Add(typeof(SnippetHotkeyRegistrationLab).Assembly.Location);
        info.ArgumentList.Add("--snippet-hotkey-holder");
        info.ArgumentList.Add(a.Gesture.Canonical);
        using var child = Process.Start(info) ?? throw new InvalidOperationException();
        try
        {
            Require(await Bounded(child.StandardOutput.ReadLineAsync()) == "HELD");
            Expect(await Apply(owner, a, b), (a.SnippetId, false), (b.SnippetId, true));
            if (kill) child.Kill(entireProcessTree: true); // Only the exact child created above.
            else
            {
                await child.StandardInput.WriteLineAsync("RELEASE");
                await child.StandardInput.FlushAsync();
                Require(await Bounded(child.StandardOutput.ReadLineAsync()) == "RELEASED");
            }
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(TimeoutSeconds));
            if (!kill) Require(child.ExitCode == 0);
            Expect(await Apply(owner, a, b), (a.SnippetId, true), (b.SnippetId, true));
            await Apply(owner);
        }
        finally
        {
            if (!child.HasExited) child.Kill(entireProcessTree: true);
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(TimeoutSeconds));
        }
    }

    public static async Task<int> RunHolderAsync(string hotkey)
    {
        if (!Candidates.Contains(hotkey, StringComparer.Ordinal)) return 2;
        using var service = new WindowsSnippetHotkeyService(_ => { });
        var binding = new SnippetHotkeyBinding(Guid.NewGuid(), 1, Key(hotkey), 1);
        var states = await Apply(service, binding);
        if (!states[binding.SnippetId]) { Console.WriteLine("UNAVAILABLE"); return 3; }
        Console.WriteLine("HELD");
        await Console.Out.FlushAsync();
        // EOF releases too, and a watchdog prevents an abandoned child retaining a registration.
        if (await Console.In.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15)) != "RELEASE") return 2;
        service.Dispose();
        Console.WriteLine("RELEASED");
        return 0;
    }
}
