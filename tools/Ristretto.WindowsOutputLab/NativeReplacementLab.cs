using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using TextExpander.Application.Features.Output;
using TextExpander.Core;
using TextExpander.Core.Matching;

namespace Ristretto.WindowsOutputLab;

internal static class NativeReplacementLab
{
    private const string Payload = "Caffè ☕ cafe\u0301\r\n漢字 مرحبا 👩🏽‍💻";

    public static int RunSelfTests(string? reportPath = null)
    {
        // Preserve existing evidence, and fail before creating windows if output cannot be opened.
        using var report = reportPath is null ? null : new FileStream(reportPath, FileMode.CreateNew, FileAccess.Write);
        var cases = new List<object>();
        var passed = 0;
        var failed = 0;
        void Case(string name, Action test)
        {
            try { test(); passed++; cases.Add(new { Name = name, Passed = true, Error = "" }); Console.WriteLine($"PASS: {name}"); }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                failed++;
                cases.Add(new { Name = name, Passed = false, Error = exception.GetType().Name });
                Console.WriteLine($"FAIL: {name} / {exception.GetType().Name}" +
                    (exception is Win32Exception native ? $" / win32={native.NativeErrorCode}" : ""));
            }
        }

        foreach (var (name, trigger, raw, delimiter) in new[]
        {
            ("ascii_unicode_payload", ";firma", ";firma", " "),
            ("nfd_raw_nbsp", ";café", ";cafe\u0301", "\u00a0"),
            ("cjk_trigger", ";漢字", ";漢字", " "),
            ("emoji_zwj_trigger", ";👩🏽‍💻", ";👩🏽‍💻", " "),
            ("rtl_trigger", ";مرحبا", ";مرحبا", " ")
        })
        {
            Case(name, () =>
            {
                using var target = new OwnedEditTarget();
                const string prefix = "Prima 😀 ", suffix = " dopo 漢字";
                target.SeedFixture(prefix + suffix, prefix.Length);
                var session = new OwnedEditRecognitionSession(target, Library(trigger));
                ReplacementExecutionOutcome? outcome = null;
                foreach (var rune in (raw + delimiter).EnumerateRunes())
                {
                    target.InsertFixtureCommit(rune.ToString());
                    var observed = session.ObserveCurrent();
                    if (observed is not null) { Check(outcome is null); outcome = observed; }
                }
                Check(outcome?.Status == ReplacementExecutionStatus.VerifiedComplete);
                var actual = target.ReadSnapshot();
                Check(actual.Text == prefix + Payload + delimiter + suffix);
                Check(actual.SelectionStart == prefix.Length + Payload.Length + delimiter.Length);
                Check(actual.SelectionEnd == actual.SelectionStart);
                Check(session.ObserveCurrent() is null); // No recursion from generated output.
            });
        }

        Case("alphabetic_trigger_after_verified_word_boundary", () =>
        {
            using var target = new OwnedEditTarget();
            target.SeedFixture("Prima ", "Prima ".Length);
            var session = new OwnedEditRecognitionSession(target, Library("brb"));
            ReplacementExecutionOutcome? outcome = null;
            foreach (var rune in "brb ".EnumerateRunes())
            {
                target.InsertFixtureCommit(rune.ToString());
                var observed = session.ObserveCurrent();
                if (observed is not null) outcome = observed;
            }
            Check(outcome?.Status == ReplacementExecutionStatus.VerifiedComplete);
            Check(target.ReadSnapshot().Text == "Prima " + Payload + " ");
        });

        Case("alphabetic_trigger_inside_word_must_not_expand", () =>
        {
            using var target = new OwnedEditTarget();
            target.SeedFixture("foobar", "foobar".Length);
            var session = new OwnedEditRecognitionSession(target, Library("brb"));
            foreach (var rune in "brb ".EnumerateRunes())
            {
                target.InsertFixtureCommit(rune.ToString());
                Check(session.ObserveCurrent() is null);
            }
            Check(target.ReadSnapshot().Text == "foobarbrb ");
        });

        Case("synthetic_fixture_provenance_is_revision_correlated", () =>
        {
            using var target = new OwnedEditTarget();
            target.SeedFixture("", 0);
            target.InsertFixtureCommit("x");
            var snapshot = target.ReadSnapshot();
            var evidence = target.LastMutation;
            Check(evidence is { Source: OwnedEditMutationSource.OwnedFixture });
            Check(OwnedEditProvenancePolicy.Evaluate(snapshot, evidence, true).Accepted);
            Check(!OwnedEditProvenancePolicy.Evaluate(snapshot, evidence, false).Accepted);
        });

        Case("queued_character_message_is_not_human_proof", () =>
        {
            using var target = new OwnedEditTarget();
            target.SeedFixture("", 0);
            target.PostOwnedCharacterMessageFixture('x'); // synthetic, NOT a physical keyboard proof
            target.PumpPendingMessages();
            var snapshot = target.ReadSnapshot();
            var evidence = target.LastMutation;
            Check(snapshot.Text == "x");
            Check(evidence is { Source: OwnedEditMutationSource.OwnedCharacterMessage,
                ObservedDuringOwnedMessageDispatch: true });
            Check(OwnedEditProvenancePolicy.Evaluate(snapshot, evidence, false).Accepted);
        });

        Case("alpha16_queued_burst_observed_one_committed_turn_at_a_time", () =>
        {
            using var target = new OwnedEditTarget();
            target.SeedFixture("", 0);
            var session = new OwnedEditRecognitionSession(target, Library(";firma", "OK"));
            ReplacementExecutionOutcome? outcome = null;
            target.PostOwnedCharacterSequenceFixture(";firma "); // All messages queued before observation.
            target.PumpPendingMessages(() =>
            {
                var current = session.ObserveCurrent();
                if (current is not null) { Check(outcome is null); outcome = current; }
            });
            Check(outcome?.Status == ReplacementExecutionStatus.VerifiedComplete);
            Check(target.ReadSnapshot().Text == "OK ");
            Check(session.ObserveCurrent() is null); // The generated output is not another input.
        });

        Case("alpha16_queued_navigation_breaks_prefix_before_remaining_characters", () =>
        {
            using var target = new OwnedEditTarget();
            target.SeedFixture("", 0);
            var session = new OwnedEditRecognitionSession(target, Library(";firma", "OK"));
            ReplacementExecutionOutcome? outcome = null;
            target.PostOwnedCharacterSequenceFixture(";fir");
            target.PostOwnedContinuityBreakFixture(0x0100, (nuint)0x25); // VK_LEFT.
            target.PostOwnedCharacterSequenceFixture("ma ");
            target.PumpPendingMessages(() => outcome ??= session.ObserveCurrent());
            Check(outcome is null);
            Check(!target.ReadSnapshot().Text!.Contains("OK", StringComparison.Ordinal));
        });

        Case("alpha16_queued_utf16_surrogate_pair_not_observed_half_formed", () =>
        {
            using var target = new OwnedEditTarget();
            target.SeedFixture("", 0);
            var session = new OwnedEditRecognitionSession(target, Library(";😀", "OK"));
            ReplacementExecutionOutcome? outcome = null;
            target.PostOwnedCharacterSequenceFixture(";😀 ");
            target.PumpPendingMessages(() =>
            {
                var current = session.ObserveCurrent();
                if (current is not null) { Check(outcome is null); outcome = current; }
            });
            Check(outcome?.Status == ReplacementExecutionStatus.VerifiedComplete);
            Check(target.ReadSnapshot().Text == "OK ");
        });

        Case("navigation_message_invalidates_epoch_without_document_change", () =>
        {
            using var target = new OwnedEditTarget();
            target.SeedFixture("", 0);
            var before = target.ReadSnapshot();
            target.PostOwnedContinuityBreakFixture(0x0100, (nuint)0x25); // WM_KEYDOWN VK_LEFT
            target.PumpPendingMessages();
            var after = target.ReadSnapshot();
            Check(after.TargetEpoch > before.TargetEpoch);
            Check(after.Text == before.Text && after.TextRevision == before.TextRevision);
        });

        Case("pending_prefix_is_discarded_after_navigation_message", () =>
        {
            using var target = new OwnedEditTarget();
            target.SeedFixture("", 0);
            var session = new OwnedEditRecognitionSession(target, Library(";firma"));
            target.InsertFixtureCommit(";fir");
            Check(session.ObserveCurrent() is null);
            target.PostOwnedContinuityBreakFixture(0x0100, (nuint)0x25);
            target.PumpPendingMessages();
            Check(session.ObserveCurrent() is null);
            target.SetFixtureSelection(4, 4);
            target.InsertFixtureCommit("ma ");
            Check(session.ObserveCurrent() is null);
            Check(target.ReadSnapshot().Text == ";firma "); // not expanded
        });

        Case("backspace_character_message_invalidates_even_empty_document", () =>
        {
            using var target = new OwnedEditTarget();
            target.SeedFixture("", 0);
            var before = target.ReadSnapshot();
            target.PostOwnedContinuityBreakFixture(0x0102, (nuint)0x08); // WM_CHAR BS
            target.PumpPendingMessages();
            var after = target.ReadSnapshot();
            Check(after.TargetEpoch > before.TargetEpoch && after.Text == "");
        });

        Case("selection_32bit_at_65536", () =>
        {
            using var target = new OwnedEditTarget();
            var prefix = new string('a', TriggerReplacementPlanner.MaxTextUtf16Units - ";firma ".Length);
            target.SeedFixture(prefix + ";firma ", TriggerReplacementPlanner.MaxTextUtf16Units);
            var plan = Prepare(target, ";firma", "OK");
            Check(plan.ExpectedTarget.SelectionEnd == 65_536);
            Check(TriggerReplacementExecutor.Execute(plan, target, 1).Status == ReplacementExecutionStatus.VerifiedComplete);
            Check(target.ReadSnapshot().Text == prefix + "OK ");
        });

        Case("intervening_text_rejected", () =>
        {
            using var target = Seeded();
            var plan = Prepare(target);
            target.InsertFixtureCommit("x");
            var before = target.ReadSnapshot();
            Check(TriggerReplacementExecutor.Execute(plan, target, 1).Status == ReplacementExecutionStatus.BlockedNoChange);
            Check(target.ReadSnapshot() == before);
        });

        Case("intervening_selection_rejected", () =>
        {
            using var target = Seeded();
            var plan = Prepare(target);
            target.SetFixtureSelection(0, 3);
            var before = target.ReadSnapshot();
            Check(TriggerReplacementExecutor.Execute(plan, target, 1).Status == ReplacementExecutionStatus.BlockedNoChange);
            Check(target.ReadSnapshot() == before);
        });

        Case("revision_aba_same_text_rejected", () =>
        {
            using var target = Seeded();
            var plan = Prepare(target);
            target.InsertFixtureCommit("x");
            target.SeedFixture("prima ;firma dopo", "prima ;firma ".Length);
            var before = target.ReadSnapshot();
            Check(before.Text == plan.ExpectedTarget.Text && before.SelectionEnd == plan.ExpectedTarget.SelectionEnd);
            Check(before.TextRevision != plan.ExpectedTarget.TextRevision);
            Check(TriggerReplacementExecutor.Execute(plan, target, 1).Status == ReplacementExecutionStatus.BlockedNoChange);
            Check(target.ReadSnapshot() == before);
        });

        Case("different_owned_control_rejected", () =>
        {
            using var first = Seeded();
            using var second = Seeded();
            var plan = Prepare(first);
            var before = second.ReadSnapshot();
            Check(TriggerReplacementExecutor.Execute(plan, second, 1).Status == ReplacementExecutionStatus.BlockedNoChange);
            Check(second.ReadSnapshot() == before);
        });

        Case("readonly_before_effect", () =>
        {
            using var target = Seeded();
            var plan = Prepare(target);
            target.SetFixtureReadOnly(true);
            var reads = target.TextReads;
            Check(target.ReadSnapshot().FieldState == ReplacementFieldState.ReadOnly);
            Check(TriggerReplacementExecutor.Execute(plan, target, 1).Status == ReplacementExecutionStatus.BlockedNoChange);
            Check(target.TextReads == reads);
            target.SetFixtureReadOnly(false);
            Check(target.ReadSnapshot().Text == "prima ;firma dopo");
        });

        Case("disabled_before_effect", () =>
        {
            using var target = Seeded();
            var plan = Prepare(target);
            target.SetFixtureEnabled(false);
            var reads = target.TextReads;
            Check(target.ReadSnapshot().FieldState == ReplacementFieldState.Disabled);
            Check(TriggerReplacementExecutor.Execute(plan, target, 1).Status == ReplacementExecutionStatus.BlockedNoChange);
            Check(target.TextReads == reads);
        });

        Case("password_without_text_read", () =>
        {
            using var target = new OwnedEditTarget(multiline: false);
            target.SeedFixture("Synthetic password", 0);
            target.SetFixturePassword(true);
            var reads = target.TextReads;
            var snapshot = target.ReadSnapshot();
            Check(snapshot.FieldState == ReplacementFieldState.Protected && snapshot.Text is null);
            Check(target.TextReads == reads);
        });

        Case("password_change_rejects_prepared_plan", () =>
        {
            using var target = new OwnedEditTarget(multiline: false);
            target.SeedFixture("prima ;firma dopo", "prima ;firma ".Length);
            var plan = Prepare(target, body: "OK");
            target.SetFixturePassword(true);
            var reads = target.TextReads;
            Check(TriggerReplacementExecutor.Execute(plan, target, 1).Status == ReplacementExecutionStatus.BlockedNoChange);
            Check(target.TextReads == reads);
        });

        Case("cancellation_never_enters_native_target", () =>
        {
            using var target = Seeded();
            var plan = Prepare(target);
            var reads = target.TextReads;
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            Check(TriggerReplacementExecutor.Execute(plan, target, 1, cancellation.Token).Reason == ReplacementExecutionReason.CancelledBeforeEffect);
            Check(target.TextReads == reads);
        });

        Case("wrong_ui_thread_rejected", () =>
        {
            using var target = Seeded();
            var plan = Prepare(target);
            var before = target.ReadSnapshot();
            var outcome = Task.Run(() => TriggerReplacementExecutor.Execute(plan, target, 1)).GetAwaiter().GetResult();
            Check(outcome.Status == ReplacementExecutionStatus.BlockedNoChange);
            Check(target.ReadSnapshot() == before);
        });

        Case("single_use_no_second_native_call", () =>
        {
            using var target = Seeded();
            var plan = Prepare(target);
            Check(TriggerReplacementExecutor.Execute(plan, target, 1).Status == ReplacementExecutionStatus.VerifiedComplete);
            var reads = target.TextReads;
            Check(TriggerReplacementExecutor.Execute(plan, target, 1).Reason == ReplacementExecutionReason.AlreadyAttempted);
            Check(target.TextReads == reads);
        });

        Case("lost_receipt_is_unverified", () =>
        {
            using var target = Seeded();
            var plan = Prepare(target);
            var backend = new ReceiptFault(target, throwAfterEffect: false);
            var outcome = TriggerReplacementExecutor.Execute(plan, backend, 1);
            Check(outcome.Status == ReplacementExecutionStatus.SubmittedUnverified && !outcome.RetryAllowedAutomatically);
            Check(target.ReadSnapshot().Text == "prima " + Payload + " dopo");
            Check(TriggerReplacementExecutor.Execute(plan, backend, 1).Reason == ReplacementExecutionReason.AlreadyAttempted);
        });

        Case("exception_after_native_effect_no_rollback", () =>
        {
            using var target = Seeded();
            var plan = Prepare(target);
            var backend = new ReceiptFault(target, throwAfterEffect: true);
            Check(TriggerReplacementExecutor.Execute(plan, backend, 1).Status == ReplacementExecutionStatus.PartialOrUnknown);
            Check(target.ReadSnapshot().Text == "prima " + Payload + " dopo");
            Check(TriggerReplacementExecutor.Execute(plan, backend, 1).Reason == ReplacementExecutionReason.AlreadyAttempted);
        });

        Case("navigation_invalidates_pending_prefix", () =>
        {
            using var target = new OwnedEditTarget();
            target.SeedFixture("", 0);
            var session = new OwnedEditRecognitionSession(target, Library(";firma"));
            target.InsertFixtureCommit(";fir");
            Check(session.ObserveCurrent() is null);
            target.SetFixtureSelection(0, 0);
            Check(session.ObserveCurrent() is null);
            target.InsertFixtureCommit("ma ");
            Check(session.ObserveCurrent() is null);
            Check(target.ReadSnapshot().Text == "ma ;fir");
        });

        Case("disposed_native_target_rejected", () =>
        {
            using var target = Seeded();
            var plan = Prepare(target);
            var reads = target.TextReads;
            target.Dispose();
            Check(!target.IsOpen);
            Check(TriggerReplacementExecutor.Execute(plan, target, 1).Status == ReplacementExecutionStatus.BlockedNoChange);
            Check(target.TextReads == reads);
        });

        Console.WriteLine($"NATIVE OWNED EDIT: {passed}/{passed + failed} PASS; failed={failed}.");
        Console.WriteLine("Solo controlli creati dal laboratorio; nessun SendInput, clipboard o hook globale. IME non qualificato.");
        if (report is not null)
            JsonSerializer.Serialize(report, new
            {
                SchemaVersion = 1, Status = failed == 0 ? "PASS_OWNED_NATIVE_EDIT_ONLY" : "FAIL",
                CreatedUtc = DateTimeOffset.UtcNow, OS = RuntimeInformation.OSDescription,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(), Runtime = RuntimeInformation.FrameworkDescription,
                LabAssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(NativeReplacementLab).Assembly.Location))).ToLowerInvariant(),
                Passed = passed, Failed = failed, Cases = cases,
                Target = "Real Win32 EDIT created and owned by this laboratory on its UI thread",
                Input = "Synthetic commits sent to owned control; recognizer consumes observed native text deltas",
                Output = "EM_SETSEL / EM_REPLACESEL / EM_GETSEL + exact UTF-16 readback",
                Revision = "Owned edit batches + EN_CHANGE for other edits; no message pump during compare/edit",
                Ime = "ImmDisableIME on owned CLI thread before first window; context removed for fixture; general IME NOT_QUALIFIED",
                HumanTyping = "NOT_RUN", ArbitraryExternalEditors = "NOT_QUALIFIED",
                SendInput = "NOT_USED_NOT_VERIFIED", Clipboard = "NOT_USED", GlobalHooks = "NOT_INSTALLED"
            }, new JsonSerializerOptions { WriteIndented = true });
        return failed == 0 ? 0 : 1;
    }

    internal sealed record DemoCounts(int Verified, int Rejected, int Uncertain);

    public static int RunDemo(string? reportPath = null, Action? onReady = null,
        Action<DemoCounts>? onCompleted = null, bool protocolOnly = false)
    {
        // CreateNew prevents accidentally overwriting earlier manual evidence.
        using var report = reportPath is null ? null : new FileStream(reportPath, FileMode.CreateNew, FileAccess.Write);
        using var target = new OwnedEditTarget();
        target.SeedFixture("", 0);
        target.ActivateForHumanInput();
        target.PumpPendingMessages();
        var session = new OwnedEditRecognitionSession(target, Library(";firma"), requireHumanFocus: true);
        onReady?.Invoke(); // Only after own window/control exist and are activated.
        var attempts = 0;
        var verified = 0;
        var rejected = 0;
        var uncertain = 0;
        if (!protocolOnly)
        {
            Console.WriteLine("Demo nel solo EDIT del laboratorio: digita ;firma seguito da spazio.");
            Console.WriteLine("Solo input osservato nel nostro controllo; nessun hook, clipboard o modifica di altre app.");
            Console.WriteLine("Chiudi la finestra per terminare; nessun testo digitato viene incluso nel rapporto.");
        }
        // Observe every coherent edit turn AS IT IS DISPATCHED. A 10 ms polling loop
        // could collapse a burst of several committed characters into one delta and
        // incorrectly reject the trigger. No global hooks and no background reader.
        void AfterOwnTurn()
        {
            if (!target.IsOpen) return;
            var outcome = session.ObserveCurrent();
            if (outcome is null) return;
            attempts++;
            switch (outcome.Status)
            {
                case ReplacementExecutionStatus.VerifiedComplete: verified++; break;
                case ReplacementExecutionStatus.BlockedNoChange: rejected++; break;
                default: uncertain++; break;
            }
            if (!protocolOnly)
                Console.WriteLine($"Tentativo {attempts}: {outcome.Status} / {outcome.Reason}");
        }
        while (target.IsOpen)
        {
            var processed = target.PumpPendingMessages(AfterOwnTurn);
            if (!target.IsOpen) break;
            // Windows blocks this test UI thread until a new message arrives. We
            // drain the entire thread queue first, so no 10 ms busy-loop remains.
            if (processed == 0) target.WaitForMessage();
        }
        var success = verified > 0 && rejected == 0 && uncertain == 0;
        onCompleted?.Invoke(new DemoCounts(verified, rejected, uncertain));
        if (!protocolOnly)
            Console.WriteLine($"Demo: {(success ? "PASS" : "NON VERIFICATA")} - verificati={verified}, rifiutati={rejected}, incerti={uncertain}.");
        if (report is not null)
        {
            JsonSerializer.Serialize(report, new
            {
                Schema = "ristretto/owned-edit-interactive/1",
                RecordedUtc = DateTimeOffset.UtcNow,
                OperatingSystem = RuntimeInformation.OSDescription,
                Runtime = RuntimeInformation.FrameworkDescription,
                Mode = "Interactive owned Win32 EDIT; actual human typing must be confirmed by tester",
                ObservationLoop = "MessageDispatchPerTurnWithWaitMessage_NoPolling",
                Application = "WindowsOutputLab (NOT Ristretto.exe)",
                Result = success ? "ObservedVerifiedReplacement" : "NotVerified",
                Attempts = attempts, Verified = verified, Rejected = rejected, Uncertain = uncertain,
                Limitations = "No global observer; no external editor, IME, SendInput or keyboard provenance guarantee",
                ContainsUserText = false
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        return success ? 0 : 1;
    }

    private static LibrarySnapshot Library(string trigger, string body = Payload)
    {
        var group = new SnippetGroup(Guid.NewGuid(), "Synthetic", true);
        return new([group], [new(Guid.NewGuid(), group.Id, "Synthetic", trigger, body, "", true, 1)], 1);
    }

    private static OwnedEditTarget Seeded()
    {
        var target = new OwnedEditTarget();
        target.SeedFixture("prima ;firma dopo", "prima ;firma ".Length);
        return target;
    }

    private static TriggerReplacementPlan Prepare(OwnedEditTarget target, string trigger = ";firma", string body = Payload)
    {
        var snapshot = target.ReadSnapshot();
        var raw = OwnedEditRecognitionSession.ResolveRawSuffix(snapshot, trigger, out var delimiter);
        Check(raw is not null);
        var recognizer = new TriggerActivationSession(Library(trigger, body));
        var decision = recognizer.Observe(new(snapshot.TargetEpoch, raw + delimiter,
            TextObservationKind.VerifiedCommitted, ObservedFieldSafety.VerifiedNormalEditable), 0);
        var origin = new ReplacementOrigin(snapshot.TargetToken, snapshot.TargetEpoch, snapshot.TextRevision, 1);
        var result = TriggerReplacementPlanner.Prepare(new(decision, origin, snapshot, raw, delimiter), 1);
        Check(result.IsReady);
        return result.Plan!;
    }

    private static void Check(bool condition) { if (!condition) throw new InvalidOperationException("Native assertion failed"); }

    private sealed class ReceiptFault(OwnedEditTarget target, bool throwAfterEffect) : IConditionalReplacementTarget
    {
        public ConditionalReplacementResult CompareAndReplace(TriggerReplacementPlan plan, CancellationToken cancellationToken)
        {
            var result = target.CompareAndReplace(plan, cancellationToken);
            if (result.Status != ConditionalReplacementStatus.Applied) return result;
            if (throwAfterEffect) throw new InvalidOperationException("Injected fixture failure");
            return new(ConditionalReplacementStatus.Applied);
        }
    }
}
