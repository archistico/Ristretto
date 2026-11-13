using System.Text;
using TextExpander.Application.Features.Output;
using TextExpander.Core;
using TextExpander.Core.Matching;
using TextExpander.Infrastructure.Simulation;

namespace Ristretto.ReplacementLab;

internal static class Program
{
    private const string ExpectedText = "Prima 😀 Caffè ☕\r\nمرحبا 👨‍👩‍👧‍👦\u00a0 dopo 漢字";

    public static int Main(string[] args)
    {
        if (args.Length != 1 || args[0] != "--self-test")
        {
            Console.WriteLine("Uso: dotnet run -c Release --project tools/Ristretto.ReplacementLab -- --self-test");
            Console.WriteLine("Editor simulato in memoria: nessuna finestra, tastiera, clipboard o libreria utente.");
            return 2;
        }
        try
        {
            var request = RecognizeFixture();
            var plan = Prepare(request);
            var target = new SimulatedReplacementTarget(request.Target!);
            var result = TriggerReplacementExecutor.Execute(plan, target, 3);
            Require(result.Status == ReplacementExecutionStatus.VerifiedComplete && target.ReadSnapshot().Text == ExpectedText);
            Console.WriteLine("PASS: riconoscimento NFD -> piano -> sostituzione Unicode/multiriga, delimitatore preservato.");

            Require(TriggerReplacementExecutor.Execute(plan, target, 3).Reason == ReplacementExecutionReason.AlreadyAttempted);
            Require(target.ReadSnapshot().TextRevision == 43);
            Console.WriteLine("PASS: il piano consumato non viene eseguito una seconda volta.");

            var changed = request.Target! with { Text = "Synthetic intervening edit", TextRevision = 43 };
            var racedTarget = new SimulatedReplacementTarget(changed);
            Require(TriggerReplacementExecutor.Execute(Prepare(request), racedTarget, 3).Status == ReplacementExecutionStatus.BlockedNoChange);
            Require(racedTarget.ReadSnapshot() == changed);
            Console.WriteLine("PASS: modifica intervenuta -> rifiuto senza cambiare il testo.");

            var unverifiedTarget = new SimulatedReplacementTarget(request.Target!);
            var unverifiedPlan = Prepare(request);
            var unverifiedBackend = new MissingReceipt(unverifiedTarget);
            Require(TriggerReplacementExecutor.Execute(unverifiedPlan, unverifiedBackend, 3).Status == ReplacementExecutionStatus.SubmittedUnverified);
            Require(unverifiedTarget.ReadSnapshot().Text == ExpectedText);
            Require(TriggerReplacementExecutor.Execute(unverifiedPlan, unverifiedBackend, 3).Reason == ReplacementExecutionReason.AlreadyAttempted);
            Console.WriteLine("PASS: ricevuta mancante -> SubmittedUnverified, nessun retry.");

            var uncertainTarget = new SimulatedReplacementTarget(request.Target!);
            var uncertainPlan = Prepare(request);
            Require(TriggerReplacementExecutor.Execute(uncertainPlan, new ThrowAfterEffect(uncertainTarget), 3).Status == ReplacementExecutionStatus.PartialOrUnknown);
            Require(uncertainTarget.ReadSnapshot().Text == ExpectedText);
            Console.WriteLine("PASS: errore dopo effetto -> PartialOrUnknown, nessun rollback.");
            Console.WriteLine("5/5 scenari passati nel SOLO editor simulato. Nessuna compatibilita' nativa attestata.");
            return 0;
        }
        catch (InvalidOperationException)
        {
            Console.Error.WriteLine("FAIL: il laboratorio simulato non ha soddisfatto una verifica.");
            return 1;
        }
    }

    private static TriggerReplacementRequest RecognizeFixture()
    {
        const string raw = ";cafe\u0301";
        const string delimiter = "\u00a0";
        const string prefix = "Prima 😀 ";
        const string suffix = " dopo 漢字";
        var group = new SnippetGroup(Guid.Parse("19b2ddf3-95e1-4432-ac73-114ecfa4bd6a"), "Synthetic", true);
        var snippet = new Snippet(Guid.Parse("4ff20562-34d6-4ce6-8a30-7b37ff16cfc5"), group.Id,
            "Synthetic", ";café", "Caffè ☕\r\nمرحبا 👨‍👩‍👧‍👦", "", true, 1);
        var session = new TriggerActivationSession(new([group], [snippet], 3));
        TriggerActivationDecision? decision = null;
        long timestamp = 0;
        foreach (var rune in (raw + delimiter).EnumerateRunes())
            decision = session.Observe(new(7, rune.ToString(), TextObservationKind.VerifiedCommitted,
                ObservedFieldSafety.VerifiedNormalEditable), timestamp++);
        var token = Guid.Parse("71634cb0-383a-4d8c-a36d-f622fd98729d");
        var caret = prefix.Length + raw.Length + delimiter.Length;
        var snapshot = new ReplacementTargetSnapshot(token, 7, 42, prefix + raw + delimiter + suffix,
            caret, caret, ReplacementFieldState.VerifiedNormalEditable, ReplacementCapability.VerifiedConditionalReplace);
        return new(decision, new(token, 7, 42, 3), snapshot, raw, delimiter);
    }

    private static TriggerReplacementPlan Prepare(TriggerReplacementRequest request) =>
        TriggerReplacementPlanner.Prepare(request, 3).Plan ?? throw new InvalidOperationException();

    private static void Require(bool condition) { if (!condition) throw new InvalidOperationException(); }

    private sealed class MissingReceipt(SimulatedReplacementTarget target) : IConditionalReplacementTarget
    {
        public ConditionalReplacementResult CompareAndReplace(TriggerReplacementPlan plan, CancellationToken ct)
        {
            var result = target.CompareAndReplace(plan, ct);
            return result.Status == ConditionalReplacementStatus.Applied ? new(ConditionalReplacementStatus.Applied) : result;
        }
    }

    private sealed class ThrowAfterEffect(SimulatedReplacementTarget target) : IConditionalReplacementTarget
    {
        public ConditionalReplacementResult CompareAndReplace(TriggerReplacementPlan plan, CancellationToken ct)
        {
            target.CompareAndReplace(plan, ct);
            throw new InvalidOperationException();
        }
    }
}
