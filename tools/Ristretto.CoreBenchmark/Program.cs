using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TextExpander.Core;
using TextExpander.Core.Matching;

namespace Ristretto.CoreBenchmark;

// Opt-in, synthetic Core measurements only. No native APIs, hooks, clipboard or library DB.
internal static class Program
{
    private const int WarmupEvents = 10_000;
    private const int MinimumMeasuredEvents = 10_000;
    private const int Repetitions = 5;
    private const double ProposedP99Microseconds = 1_000;

    public static int Main(string[] args)
    {
        if (args.Length != 2 || args[0] != "--output")
        {
            Console.Error.WriteLine("Uso: dotnet run -c Release --project tools/Ristretto.CoreBenchmark -- --output reports/core-benchmark.json");
            return 2;
        }
#if DEBUG
        Console.Error.WriteLine("Il benchmark richiede una build Release.");
        return 2;
#else
        if (Debugger.IsAttached)
        {
            Console.Error.WriteLine("Eseguire senza debugger.");
            return 2;
        }
        try
        {
            // CreateNew preserves previous evidence, also for invalid/cancelled executions.
            var path = Path.GetFullPath(args[1]);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var destination = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            var runs = new List<RunResult>();
            foreach (var size in new[] { 100, 10_000 })
            {
                var snapshot = CreateSnapshot(size);
                var cases = CreateCases(size);
                for (var repetition = 1; repetition <= Repetitions; repetition++)
                {
                    foreach (var scenario in cases)
                    {
                        var run = Measure(snapshot, scenario, repetition);
                        runs.Add(run);
                        Console.WriteLine(FormattableString.Invariant(
                            $"D{size} / {scenario.Name} / run {repetition}: p99={run.P99Microseconds:F2} us, alloc={run.AllocatedBytesPerEvent:F0} B/event, correct={run.Correct}"));
                    }
                }
            }
            var report = new
            {
                SchemaVersion = 1,
                Version = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                    .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
                    .FirstOrDefault()?.InformationalVersion.Split('+')[0] ?? "unknown",
                Status = runs.All(r => r.Correct) ? "CORRECTNESS_PASS" : "CORRECTNESS_FAIL",
                Scope = "SYNTHETIC_CORE_ONLY_NOT_NATIVE_OR_GUI",
                CreatedUtc = DateTimeOffset.UtcNow,
                Environment = new
                {
                    OS = RuntimeInformation.OSDescription,
                    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    Framework = RuntimeInformation.FrameworkDescription,
                    LogicalProcessors = System.Environment.ProcessorCount,
                    Cpu = System.Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "not_recorded",
                    PowerPlan = "not_recorded",
                    AvailableMemoryLimitBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
                    ServerGC = GCSettings.IsServerGC,
                    Stopwatch.Frequency,
                    Stopwatch.IsHighResolution,
                    TieredCompilation = "runtime_default",
                    CoreAssemblySha256 = Convert.ToHexString(SHA256.HashData(
                        File.ReadAllBytes(typeof(TriggerActivationSession).Assembly.Location))).ToLowerInvariant()
                },
                Method = new
                {
                    Repetitions,
                    WarmupEvents,
                    MinimumMeasuredEvents,
                    Percentiles = "nearest_rank_per_run_no_outlier_removal",
                    Timer = "Stopwatch.GetTimestamp_around_Observe_only",
                    Allocation = "managed_current_thread_around_Observe_only",
                    Instrumentation = "two_timestamps_and_two_allocation_reads_per_event_not_subtracted",
                    CatalogConstruction = "separate_session_constructor_time_and_managed_allocations_not_cold_start",
                    Input = "deterministic_synthetic_one_unicode_scalar_per_observation",
                    Dataset = "exactly_100_or_10000_enabled_snippets_5_or_200_groups_11_reserved_cases_remaining_20_percent_unicode",
                    SequenceIsolation = "Reset_outside_measurement_except_continuous_bounded_miss",
                    ProposedP99Microseconds,
                    BudgetIsReleaseGate = false
                },
                Runs = runs
            };
            JsonSerializer.Serialize(destination, report, new JsonSerializerOptions { WriteIndented = true });
            destination.Flush();
            Console.WriteLine($"Report: {path}");
            return runs.All(r => r.Correct) ? 0 : 1;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Console.Error.WriteLine($"Benchmark non completato: {exception.GetType().Name}.");
            return 2;
        }
#endif
    }

    private static LibrarySnapshot CreateSnapshot(int size)
    {
        var groupCount = size == 100 ? 5 : 200;
        var groups = Enumerable.Range(1, groupCount)
            .Select(i => new SnippetGroup(Id(-i), $"Synthetic group {i}", true)).ToArray();
        var special = new[]
        {
            ";sig", ";signature", ";résumé", ";漢字", ";👩🏽‍💻", ";مرحبا",
            ";é", ";e\u0301", ";" + new string('x', 125) + "a", ";" + new string('x', 125) + "b", ";safe"
        };
        var snippets = Enumerable.Range(0, size).Select(i =>
            new Snippet(Id(i + 1), groups[i % groupCount].Id, $"Synthetic {i}",
                i < special.Length ? special[i] : GeneratedTrigger(i),
                "Synthetic payload", "", true, 1)).ToArray();
        return new LibrarySnapshot(groups, snippets, 1);
    }

    private static Guid Id(int i) => new(i, 0, 0, new byte[8]);

    private static string GeneratedTrigger(int i) => i % 5 == 1 ? $";漢{i:D5}" : $";k{i:D5}";

    private sealed record Scenario(string Name, string Input, TriggerActivationPolicy Policy,
        TriggerActivationStatus Terminal, Guid? ExpectedSnippetId = null, bool Continuous = false,
        TextObservationKind Kind = TextObservationKind.VerifiedCommitted,
        ObservedFieldSafety Safety = ObservedFieldSafety.VerifiedNormalEditable);

    private static Scenario[] CreateCases(int size)
    {
        var delimiter = new TriggerActivationPolicy();
        return
        [
            new("ascii_delimiter", GeneratedTrigger(size - 5) + " ", delimiter,
                TriggerActivationStatus.Ready, Id(size - 4)),
            new("nfd_delimiter", ";re\u0301sume\u0301 ", delimiter, TriggerActivationStatus.Ready, Id(3)),
            new("cjk_delimiter", ";漢字 ", delimiter, TriggerActivationStatus.Ready, Id(4)),
            new("emoji_zwj_delimiter", ";👩🏽‍💻 ", delimiter, TriggerActivationStatus.Ready, Id(5)),
            new("rtl_delimiter", ";مرحبا ", delimiter, TriggerActivationStatus.Ready, Id(6)),
            new("canonical_ambiguity", ";e\u0301 ", delimiter, TriggerActivationStatus.Ambiguous),
            new("immediate_overlap", ";signature", new(Mode: TriggerActivationMode.Immediate),
                TriggerActivationStatus.Ready, Id(2)),
            new("long_shared_prefix", ";" + new string('x', 125) + "z ", delimiter, TriggerActivationStatus.None),
            new("bounded_continuous_miss", "q", delimiter, TriggerActivationStatus.None, Continuous: true),
            new("protected_field", ";safe ", delimiter, TriggerActivationStatus.Suppressed,
                Safety: ObservedFieldSafety.Protected),
            new("composing_input", ";safe ", delimiter, TriggerActivationStatus.Suppressed,
                Kind: TextObservationKind.Composing)
        ];
    }

    private sealed record RunResult(int SnippetCount, string Scenario, int Repetition, int MeasuredEvents,
        double SessionConstructionMilliseconds, long SessionConstructionAllocatedBytes,
        double P50Microseconds, double P95Microseconds, double P99Microseconds, double MaxMicroseconds,
        double AllocatedBytesPerEvent, int Gen0Collections, int Gen1Collections, int Gen2Collections,
        int CorrectnessFailures, bool Correct, bool MeetsProposedP99);

    private static RunResult Measure(LibrarySnapshot snapshot, Scenario scenario, int repetition)
    {
        // Fixtures, observation records and measurement arrays are outside the measured region.
        var observations = scenario.Input.EnumerateRunes().Select(r =>
            new CommittedInputObservation(1, r.ToString(), scenario.Kind, scenario.Safety)).ToArray();
        var cycles = (MinimumMeasuredEvents + observations.Length - 1) / observations.Length;
        var sampleCount = cycles * observations.Length;
        var ticks = new long[sampleCount];
        var constructionAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var constructionStarted = Stopwatch.GetTimestamp();
        var session = new TriggerActivationSession(snapshot, scenario.Policy);
        var constructionTicks = Stopwatch.GetTimestamp() - constructionStarted;
        var constructionAllocated = GC.GetAllocatedBytesForCurrentThread() - constructionAllocatedBefore;
        long clock = 0;
        var failures = 0;
        if (scenario.Continuous)
            session.Observe(new(1, new string('q', CommittedInputSession.MaxBufferedUtf16Units),
                TextObservationKind.VerifiedCommitted, ObservedFieldSafety.VerifiedNormalEditable), clock++);

        // No forced GC: normal collection pauses remain visible in the recorded samples.
        for (var cycle = 0; cycle < (WarmupEvents + observations.Length - 1) / observations.Length; cycle++)
        {
            if (!scenario.Continuous) session.Reset();
            for (var i = 0; i < observations.Length; i++)
                if (!Valid(session.Observe(observations[i], clock++), scenario, i == observations.Length - 1))
                    failures++;
        }

        var collections = new[] { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };
        long allocated = 0;
        var sample = 0;
        for (var cycle = 0; cycle < cycles; cycle++)
        {
            if (!scenario.Continuous) session.Reset();
            for (var i = 0; i < observations.Length; i++)
            {
                var observation = observations[i];
                var timestamp = clock++;
                var allocationStart = GC.GetAllocatedBytesForCurrentThread();
                var started = Stopwatch.GetTimestamp();
                var decision = session.Observe(observation, timestamp);
                var elapsed = Stopwatch.GetTimestamp() - started;
                allocated += GC.GetAllocatedBytesForCurrentThread() - allocationStart;
                ticks[sample++] = elapsed;
                if (!Valid(decision, scenario, i == observations.Length - 1)) failures++;
            }
        }
        var gen0 = GC.CollectionCount(0) - collections[0];
        var gen1 = GC.CollectionCount(1) - collections[1];
        var gen2 = GC.CollectionCount(2) - collections[2];
        Array.Sort(ticks);
        double Percentile(double p) => ticks[(int)Math.Ceiling(p * ticks.Length) - 1] * 1_000_000d / Stopwatch.Frequency;
        var p99 = Percentile(.99);
        return new(snapshot.Snippets.Count, scenario.Name, repetition, sampleCount,
            constructionTicks * 1_000d / Stopwatch.Frequency, constructionAllocated,
            Percentile(.50), Percentile(.95), p99, Percentile(1), (double)allocated / sampleCount,
            gen0, gen1, gen2, failures, failures == 0, p99 <= ProposedP99Microseconds);
    }

    private static bool Valid(TriggerActivationDecision decision, Scenario scenario, bool terminal)
    {
        if (!terminal)
            return scenario.Terminal == TriggerActivationStatus.Suppressed
                ? decision.Status == TriggerActivationStatus.Suppressed
                : decision.Status is TriggerActivationStatus.None or
                    TriggerActivationStatus.WaitingForMoreInput or TriggerActivationStatus.WaitingForDelimiter;
        if (decision.Status != scenario.Terminal || decision.Snippet?.Id != scenario.ExpectedSnippetId)
            return false;
        return scenario.Terminal switch
        {
            TriggerActivationStatus.Ready => decision.CandidateCount == 1,
            TriggerActivationStatus.Ambiguous => decision.CandidateCount == 2,
            _ => decision.CandidateCount == 0
        };
    }
}
