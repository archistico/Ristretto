using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using TextExpander.Core;
using TextExpander.Application.Features.Output;

namespace Ristretto.WindowsOutputLab;

/// <summary>
/// Explicit second-process integration exercise: the CHILD owns the Win32 EDIT and edits
/// it on its own UI thread. The parent receives ONLY ready and outcome counters.
/// No handles, text, keystrokes, or replacement plans are sent across processes.
/// This is NOT a native adapter for arbitrary other applications.
/// </summary>
internal static class CooperatingProcessLab
{
    public static int RunChild(string encodedSession, bool smoke = false)
    {
        if (!Guid.TryParseExact(encodedSession, "N", out var session) || session == Guid.Empty) return 2;
        if (smoke) return RunChildSmoke(session);
        return NativeReplacementLab.RunDemo(
            onReady: () =>
            {
                Console.WriteLine(CooperatingLabProtocol.Ready(session, Environment.ProcessId));
                Console.Out.Flush();
            },
            onCompleted: counts =>
            {
                Console.WriteLine(CooperatingLabProtocol.Result(session,
                    new CooperatingLabCounts(counts.Verified, counts.Rejected, counts.Uncertain)));
                Console.Out.Flush();
            },
            protocolOnly: true);
    }

    private static int RunChildSmoke(Guid session)
    {
        using var target = new OwnedEditTarget();
        target.SeedFixture("", 0);
        var group = new SnippetGroup(Guid.NewGuid(), "Synthetic", true);
        var library = new LibrarySnapshot([group],
            [new Snippet(Guid.NewGuid(), group.Id, "Test", ";firma", "Caffè ☕ 漢字", "", true, 1)], 1);
        var recognition = new OwnedEditRecognitionSession(target, library);
        Console.WriteLine(CooperatingLabProtocol.Ready(session, Environment.ProcessId));
        Console.Out.Flush();
        ReplacementExecutionOutcome? outcome = null;
        foreach (var rune in ";firma ".EnumerateRunes())
        {
            target.InsertFixtureCommit(rune.ToString());
            var current = recognition.ObserveCurrent();
            if (current is not null) outcome = current;
        }
        var verified = outcome?.Status == ReplacementExecutionStatus.VerifiedComplete &&
                       target.ReadSnapshot().Text == "Caffè ☕ 漢字 ";
        Console.WriteLine(CooperatingLabProtocol.Result(session,
            new CooperatingLabCounts(verified ? 1 : 0, 0, verified ? 0 : 1)));
        Console.Out.Flush();
        return verified ? 0 : 1;
    }

    public static async Task<int> RunParent(string? reportPath, bool smoke = false)
    {
        // CreateNew: no old evidence is overwritten and no user text enters the report.
        using var report = reportPath is null ? null : new FileStream(reportPath, FileMode.CreateNew, FileAccess.Write);
        var session = Guid.NewGuid();
        var path = Environment.ProcessPath ?? throw new InvalidOperationException("Missing executable path");
        var start = new ProcessStartInfo(path)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardInput = false,
            RedirectStandardError = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };
        if (string.Equals(Path.GetFileNameWithoutExtension(path), "dotnet", StringComparison.OrdinalIgnoreCase))
            start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        start.ArgumentList.Add(smoke ? "--cooperating-child-smoke" : "--cooperating-child");
        start.ArgumentList.Add(session.ToString("N"));

        using var child = Process.Start(start);
        if (child is null) return 2;
        var pid = child.Id;
        var ready = false;
        CooperatingLabCounts? counts = null;
        Console.WriteLine("Alpha 9 — laboratorio cooperante in un SECONDO processo Windows.");
        if (!smoke) Console.WriteLine("Nel campo della finestra figlia digita ;firma seguito da spazio, poi chiudila.");
        else Console.WriteLine("Smoke automatico con fixture sintetica nel controllo del processo figlio.");
        Console.WriteLine("Il processo principale non riceve tasti, testo, snapshot o clipboard.");
        Console.WriteLine("Nessun editor esterno arbitrario viene modificato.");

        try
        {
            // Startup is bounded; actual manual testing has no arbitrary time limit.
            var first = await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15));
            ready = CooperatingLabProtocol.IsReady(first, session, pid);
            if (ready)
            {
                Console.WriteLine($"Campo cooperante avviato (PID {pid}). Chiudere la finestra per terminare.");
                // Child prints only status lines and the final structured, text-free receipt.
                while ((smoke
                    ? await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(20))
                    : await child.StandardOutput.ReadLineAsync()) is { } line)
                {
                    if (CooperatingLabProtocol.TryReadResult(line, session, out var candidate))
                    {
                        counts = candidate;
                        break;
                    }
                }
            }
            // Never block forever waiting for a misbehaving child AFTER its receipt.
            if (!child.HasExited)
                await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (TimeoutException)
        {
            Console.Error.WriteLine("Timeout / prova non verificata. Non si effettua alcun retry.");
        }
        finally
        {
            if (!child.HasExited)
            {
                try { child.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { }
                await child.WaitForExitAsync();
            }
        }

        var exitCode = child.ExitCode;
        var success = CooperatingLabProtocol.IsSuccessful(counts, ready, exitCode);
        Console.WriteLine($"Esito secondo processo: {(success ? "VERIFICATO" : "NON VERIFICATO")}. " +
            $"ready={ready}, verified={counts?.Verified ?? 0}, rejected={counts?.Rejected ?? 0}, " +
            $"uncertain={counts?.Uncertain ?? 0}, childExit={exitCode}.");
        if (report is not null)
        {
            JsonSerializer.Serialize(report, new
            {
                Schema = "ristretto/cooperating-process-demo/1",
                Mode = smoke ? "SyntheticChildSmoke" : "InteractiveChildDemo",
                TimestampUtc = DateTimeOffset.UtcNow,
                OS = RuntimeInformation.OSDescription,
                ChildProcessId = pid,
                ReadyVerified = ready,
                Result = success ? "VerifiedInCooperatingChildOnly" : "NotVerified",
                Verified = counts?.Verified ?? 0,
                Rejected = counts?.Rejected ?? 0,
                Uncertain = counts?.Uncertain ?? 0,
                ExitCode = exitCode,
                ContainsUserText = false,
                Qualification = "Cooperating Win32 EDIT in separate lab process ONLY; no third-party editor, global observer or general IME"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        return success ? 0 : 1;
    }
}
