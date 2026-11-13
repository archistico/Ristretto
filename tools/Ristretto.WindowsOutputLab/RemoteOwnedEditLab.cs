using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using TextExpander.Application.Features.Output;
using TextExpander.Core;
using TextExpander.Core.Matching;

namespace Ristretto.WindowsOutputLab;

/// <summary>
/// Alpha 18: isolated cross-PROCESS command and conditional replacement. Unlike
/// Alpha 9 (which only collects a child's own recognition result), the controller
/// explicitly requests an edit and receives an outcome. The ONLY possible edit is
/// a synthetic fixture in this laboratory's exact newly spawned child. This is
/// NOT an adapter for Notepad, Word, browsers or an arbitrary external HWND.
/// </summary>
internal static class RemoteOwnedEditLab
{
    private const string Trigger = ";firma";
    private const string TextBefore = "Prima ;firma ";
    private const string Replacement = "Caffè ☕ 漢字";
    private const string TextAfter = "Prima Caffè ☕ 漢字 ";

    public static int RunChild(string rawSession, string scenario)
    {
        if (!Guid.TryParseExact(rawSession, "N", out var session) || session == Guid.Empty ||
            scenario is not ("valid" or "stale")) return 2;

        // Everything below stays on the Win32 EDIT's owner UI thread. No global
        // hook, clipboard, SendInput, cross-process HWND write or user document.
        using var target = new OwnedEditTarget();
        target.SeedFixture(TextBefore, TextBefore.Length);
        var plan = PrepareFixture(target);
        if (scenario == "stale") target.InsertFixtureCommit("x");
        var before = target.ReadSnapshot();
        var gate = new RemoteOwnedEditOneShotGate(session);
        Console.WriteLine(RemoteOwnedEditProtocol.Ready(session, Environment.ProcessId));
        Console.Out.Flush();

        var command = Console.ReadLine();
        if (!gate.TryConsume(command))
        {
            Console.WriteLine(RemoteOwnedEditProtocol.Receipt(session, RemoteOwnedEditReceipt.DuplicateOrInvalid));
            Console.Out.Flush();
            return 4;
        }

        var outcome = TriggerReplacementExecutor.Execute(plan, target, currentLibraryRevision: 1);
        var after = target.ReadSnapshot();
        var result = outcome.Status switch
        {
            ReplacementExecutionStatus.VerifiedComplete => RemoteOwnedEditReceipt.Verified,
            ReplacementExecutionStatus.BlockedNoChange when after == before => RemoteOwnedEditReceipt.RejectedNoChange,
            _ => RemoteOwnedEditReceipt.PartialOrUnknown
        };
        Console.WriteLine(RemoteOwnedEditProtocol.Receipt(session, result));
        Console.Out.Flush();

        // Try the exact same request a second time. A duplicated operation MUST
        // be rejected before the target or executor can be entered again.
        var sameCommand = Console.ReadLine();
        var acceptedAgain = gate.TryConsume(sameCommand);
        var finalSnapshot = target.ReadSnapshot();
        var unchangedByReplay = finalSnapshot == after;
        Console.WriteLine(RemoteOwnedEditProtocol.Receipt(session,
            !acceptedAgain && unchangedByReplay ? RemoteOwnedEditReceipt.DuplicateOrInvalid : RemoteOwnedEditReceipt.PartialOrUnknown));
        Console.Out.Flush();
        var correct = scenario == "valid"
            ? result == RemoteOwnedEditReceipt.Verified && after.Text == TextAfter
            : result == RemoteOwnedEditReceipt.RejectedNoChange && after == before &&
              before.Text == TextBefore + "x";
        return correct && !acceptedAgain && unchangedByReplay ? 0 : 1;
    }

    private static TriggerReplacementPlan PrepareFixture(OwnedEditTarget target)
    {
        var snapshot = target.ReadSnapshot();
        var raw = OwnedEditRecognitionSession.ResolveRawSuffix(snapshot, Trigger, out var delimiter)
            ?? throw new InvalidOperationException("Synthetic raw suffix unavailable");
        var group = new SnippetGroup(Guid.NewGuid(), "Remote owned fixture", true);
        var snippet = new Snippet(Guid.NewGuid(), group.Id, "Fixture", Trigger, Replacement, "", true, 1);
        var library = new LibrarySnapshot([group], [snippet], 1);
        var recognizer = new TriggerActivationSession(library);
        var decision = recognizer.Observe(new(snapshot.TargetEpoch, raw + delimiter,
            TextObservationKind.VerifiedCommitted, ObservedFieldSafety.VerifiedNormalEditable), 0);
        var origin = new ReplacementOrigin(snapshot.TargetToken, snapshot.TargetEpoch, snapshot.TextRevision, 1);
        var result = TriggerReplacementPlanner.Prepare(new(decision, origin, snapshot, raw, delimiter), 1);
        return result.Plan ?? throw new InvalidOperationException($"Synthetic plan rejected: {result.Rejection}");
    }

    public static async Task<int> RunParent(string? reportPath)
    {
        // Prepare report before side effects; preserve previous runs.
        using var report = reportPath is null ? null : new FileStream(reportPath, FileMode.CreateNew, FileAccess.Write);
        var valid = await RunScenario("valid", RemoteOwnedEditReceipt.Verified);
        var stale = await RunScenario("stale", RemoteOwnedEditReceipt.RejectedNoChange);
        var success = valid && stale;
        Console.WriteLine($"Alpha 18 remote owned EDIT: verified={valid}; stale-rejected={stale}; " +
            $"result={(success ? "PASS" : "NOT VERIFIED")}. No third-party editors modified.");
        if (report is not null)
        {
            JsonSerializer.Serialize(report, new
            {
                Schema = "ristretto/remote-owned-edit-lab/1",
                TimestampUtc = DateTimeOffset.UtcNow,
                VerifiedScenario = valid,
                StaleSnapshotRejected = stale,
                AllPassed = success,
                ContainsUserText = false,
                ContainsWindowOrProcessIdentifiers = false,
                Qualification = "Synthetic EDIT on exact owned child UI thread only; not a general external-editor adapter"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        return success ? 0 : 1;
    }

    private static async Task<bool> RunScenario(string scenario, RemoteOwnedEditReceipt expected)
    {
        var session = Guid.NewGuid();
        var path = Environment.ProcessPath ?? throw new InvalidOperationException("Missing executable path");
        var start = new ProcessStartInfo(path)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = false,
            CreateNoWindow = true,
            // A pipe carries protocol lines, not a UTF-8 document. A BOM would
            // prefix the first APPLY and correctly burn the child's one-shot gate.
            StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardOutputEncoding = Encoding.UTF8
        };
        if (string.Equals(Path.GetFileNameWithoutExtension(path), "dotnet", StringComparison.OrdinalIgnoreCase))
            start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        start.ArgumentList.Add("--remote-owned-child");
        start.ArgumentList.Add(session.ToString("N"));
        start.ArgumentList.Add(scenario);
        using var child = Process.Start(start);
        if (child is null) return false;
        try
        {
            var ready = await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15));
            if (!RemoteOwnedEditProtocol.IsReady(ready, session, child.Id)) return false;
            var request = RemoteOwnedEditProtocol.Request(session);
            await child.StandardInput.WriteLineAsync(request);
            await child.StandardInput.FlushAsync();
            var first = await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10));
            if (!RemoteOwnedEditProtocol.TryReadReceipt(first, session, out var status) || status != expected)
                return false;
            // Replaying the command is a negative test only. The child's gate
            // must reject it. Never replay a timed-out/uncertain FIRST attempt.
            await child.StandardInput.WriteLineAsync(request);
            await child.StandardInput.FlushAsync();
            var duplicate = await child.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10));
            if (!RemoteOwnedEditProtocol.TryReadReceipt(duplicate, session, out var replay) ||
                replay != RemoteOwnedEditReceipt.DuplicateOrInvalid) return false;
            await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            return child.ExitCode == 0;
        }
        catch (Exception error) when (error is TimeoutException or IOException or InvalidOperationException)
        {
            // A write may already have occurred: no retries, no rollback.
            Console.Error.WriteLine($"Remote owned laboratory scenario {scenario}: {error.GetType().Name}; outcome unknown.");
            return false;
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
    }
}
