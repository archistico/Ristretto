using System.Collections.Concurrent;
using TextExpander.Application.Features.Output;
using TextExpander.Core;
using TextExpander.Core.Matching;
using TextExpander.Infrastructure.Simulation;
using Xunit;

namespace TextExpander.Tests;

public sealed class TriggerReplacementTests
{
    private static readonly Guid TargetId = Guid.Parse("e7983fd2-6d64-48f4-9029-d902f57209d9");

    private static TriggerReplacementRequest Request(string trigger = ";sig", string? raw = null,
        string delimiter = " ", string body = "Caffè ☕", string prefix = "prima ", string suffix = "dopo")
    {
        raw ??= trigger;
        var group = new SnippetGroup(Guid.NewGuid(), "Synthetic", true);
        var snippet = new Snippet(Guid.NewGuid(), group.Id, "Synthetic", trigger, body, "", true, 1);
        var text = prefix + raw + delimiter + suffix;
        var caret = prefix.Length + raw.Length + delimiter.Length;
        var target = new ReplacementTargetSnapshot(TargetId, 7, 42, text, caret, caret,
            ReplacementFieldState.VerifiedNormalEditable, ReplacementCapability.VerifiedConditionalReplace);
        return new(new(TriggerActivationStatus.Ready, snippet, 1), new(TargetId, 7, 42, 3), target, raw, delimiter);
    }

    private static TriggerReplacementPlan Plan(TriggerReplacementRequest? request = null)
    {
        var result = TriggerReplacementPlanner.Prepare(request ?? Request(), 3);
        Assert.True(result.IsReady, result.Rejection.ToString());
        return Assert.IsType<TriggerReplacementPlan>(result.Plan);
    }

    [Theory]
    [InlineData(";café", ";cafe\u0301", "\u00a0", "مرحبا 👨‍👩‍👧‍👦\r\n漢字")]
    [InlineData("👩🏽‍💻", "👩🏽‍💻", " ", "cafe\u0301")]
    [InlineData(";漢字", ";漢字", "。", "العربية")]
    [InlineData(";sig", ";sig", "", "one\ttwo\nthree")]
    public void ExactRawRangeIsReplacedAndDelimiterAndSurroundingsArePreserved(
        string trigger, string raw, string delimiter, string body)
    {
        var request = Request(trigger, raw, delimiter, body, "Prima 😀 ", " 👩🏽‍💻 dopo");
        var plan = Plan(request);
        var target = new SimulatedReplacementTarget(request.Target!);
        var outcome = TriggerReplacementExecutor.Execute(plan, target, 3);
        Assert.Equal(ReplacementExecutionStatus.VerifiedComplete, outcome.Status);
        var after = target.ReadSnapshot();
        Assert.Equal("Prima 😀 " + body + delimiter + " 👩🏽‍💻 dopo", after.Text);
        Assert.Equal("Prima 😀 ".Length + body.Length + delimiter.Length, after.SelectionEnd);
        Assert.Equal(after.SelectionEnd, after.SelectionStart);
        Assert.Equal(43, after.TextRevision);
        Assert.Equal(raw.Length, plan.RangeLength);
        Assert.False(outcome.RetryAllowedAutomatically);
    }

    [Fact]
    public void RealRecognitionCanPrepareAndExecuteSyntheticReplacement()
    {
        var request = Request(";café", ";cafe\u0301", "\u00a0", "Caffè ☕\r\nمرحبا 👩🏽‍💻");
        var group = new SnippetGroup(request.Decision!.Snippet!.GroupId, "Synthetic", true);
        var session = new TriggerActivationSession(new([group], [request.Decision.Snippet], 3));
        TriggerActivationDecision? decision = null;
        var clock = 0L;
        foreach (var rune in (request.RawMatchedText + request.CommittedDelimiter).EnumerateRunes())
            decision = session.Observe(new(7, rune.ToString(), TextObservationKind.VerifiedCommitted,
                ObservedFieldSafety.VerifiedNormalEditable), clock++);
        var plan = Plan(request with { Decision = decision });
        var target = new SimulatedReplacementTarget(request.Target!);
        Assert.Equal(ReplacementExecutionStatus.VerifiedComplete,
            TriggerReplacementExecutor.Execute(plan, target, 3).Status);
        Assert.Equal("prima Caffè ☕\r\nمرحبا 👩🏽‍💻\u00a0dopo", target.ReadSnapshot().Text);
    }

    [Theory]
    [InlineData(TriggerActivationStatus.None)]
    [InlineData(TriggerActivationStatus.WaitingForMoreInput)]
    [InlineData(TriggerActivationStatus.WaitingForDelimiter)]
    [InlineData(TriggerActivationStatus.Ambiguous)]
    [InlineData(TriggerActivationStatus.Suppressed)]
    public void OnlyOneReadyCandidateCanBePrepared(TriggerActivationStatus status)
    {
        var request = Request();
        Assert.Equal(ReplacementRejection.ActivationNotReady,
            TriggerReplacementPlanner.Prepare(request with { Decision = request.Decision! with { Status = status } }, 3).Rejection);
        Assert.Equal(ReplacementRejection.ActivationNotReady,
            TriggerReplacementPlanner.Prepare(request with { Decision = request.Decision! with { CandidateCount = 2 } }, 3).Rejection);
    }

    [Theory]
    [InlineData(ReplacementFieldState.Unknown)]
    [InlineData(ReplacementFieldState.Protected)]
    [InlineData(ReplacementFieldState.ReadOnly)]
    [InlineData(ReplacementFieldState.Disabled)]
    [InlineData(ReplacementFieldState.Composing)]
    [InlineData((ReplacementFieldState)999)]
    public void UnqualifiedFieldsAreRejectedBeforeTextInspection(ReplacementFieldState field)
    {
        var request = Request();
        var result = TriggerReplacementPlanner.Prepare(request with { Target = request.Target! with { FieldState = field, Text = null } }, 3);
        Assert.Equal(ReplacementRejection.FieldNotEditable, result.Rejection);
        Assert.Null(result.Plan);
    }

    [Theory]
    [InlineData(ReplacementCapability.Unknown)]
    [InlineData(ReplacementCapability.Unsupported)]
    [InlineData((ReplacementCapability)999)]
    public void ConditionalReplacementMustBeQualified(ReplacementCapability capability)
    {
        var request = Request();
        Assert.Equal(ReplacementRejection.CapabilityUnavailable,
            TriggerReplacementPlanner.Prepare(request with { Target = request.Target! with { Capability = capability } }, 3).Rejection);
    }

    [Fact]
    public void MissingAndStaleIdentityCannotProduceAPlan()
    {
        var request = Request();
        Assert.Equal(ReplacementRejection.MissingRequest, TriggerReplacementPlanner.Prepare(null, 3).Rejection);
        Assert.Equal(ReplacementRejection.InvalidOrigin, TriggerReplacementPlanner.Prepare(request with { Origin = null }, 3).Rejection);
        Assert.Equal(ReplacementRejection.InvalidOrigin, TriggerReplacementPlanner.Prepare(request with { Origin = request.Origin! with { TextRevision = long.MaxValue } }, 3).Rejection);
        Assert.Equal(ReplacementRejection.TargetChanged, TriggerReplacementPlanner.Prepare(request with { Target = null }, 3).Rejection);
        Assert.Equal(ReplacementRejection.TargetChanged, TriggerReplacementPlanner.Prepare(request with { Target = request.Target! with { TargetToken = Guid.NewGuid() } }, 3).Rejection);
        Assert.Equal(ReplacementRejection.TargetChanged, TriggerReplacementPlanner.Prepare(request with { Target = request.Target! with { TargetEpoch = 8 } }, 3).Rejection);
        Assert.Equal(ReplacementRejection.TextRevisionChanged, TriggerReplacementPlanner.Prepare(request with { Target = request.Target! with { TextRevision = 43 } }, 3).Rejection);
        Assert.Equal(ReplacementRejection.LibraryChanged, TriggerReplacementPlanner.Prepare(request, 4).Rejection);
        Assert.Equal(ReplacementRejection.ModifiersPressed, TriggerReplacementPlanner.Prepare(request with { Target = request.Target! with { HasPendingModifiers = true } }, 3).Rejection);
    }

    [Theory]
    [InlineData("\t")]
    [InlineData("\r\n")]
    [InlineData("\u2028")]
    [InlineData("_")]
    [InlineData("ab")]
    [InlineData("  ")]
    public void UncommittedOrNonterminalDelimiterIsRejected(string delimiter)
    {
        Assert.Equal(ReplacementRejection.InvalidDelimiter,
            TriggerReplacementPlanner.Prepare(Request(delimiter: delimiter), 3).Rejection);
    }

    [Fact]
    public void CanonicallyEquivalentSourceIsNotSubstitutedForOriginalRawEvidence()
    {
        var request = Request(";café", ";cafe\u0301");
        var changedText = request.Target!.Text!.Replace("cafe\u0301", "café", StringComparison.Ordinal);
        var changed = request.Target with { Text = changedText, SelectionStart = request.Target.SelectionStart - 1, SelectionEnd = request.Target.SelectionEnd - 1 };
        Assert.Equal(ReplacementRejection.RawTextChanged,
            TriggerReplacementPlanner.Prepare(request with { Target = changed }, 3).Rejection);
    }

    [Fact]
    public void ActiveSelectionAndMalformedTextAreRejected()
    {
        var request = Request();
        Assert.Equal(ReplacementRejection.InvalidSelection, TriggerReplacementPlanner.Prepare(request with { Target = request.Target! with { SelectionStart = -1 } }, 3).Rejection);
        Assert.Equal(ReplacementRejection.SelectionNotCollapsed, TriggerReplacementPlanner.Prepare(request with { Target = request.Target! with { SelectionStart = 0 } }, 3).Rejection);
        Assert.Equal(ReplacementRejection.InvalidText, TriggerReplacementPlanner.Prepare(request with { Target = request.Target! with { Text = new string((char)0xd800, 1) } }, 3).Rejection);
        Assert.Equal(ReplacementRejection.InvalidText, TriggerReplacementPlanner.Prepare(request with { Target = request.Target! with { Text = "\0" } }, 3).Rejection);
    }

    [Fact]
    public void ATriggerInsideAnExistingGraphemeCannotBeRemoved()
    {
        Assert.Equal(ReplacementRejection.GraphemeBoundary,
            TriggerReplacementPlanner.Prepare(Request("\u0301", "\u0301", prefix: "e"), 3).Rejection);
        var request = Request("e", "e", "", prefix: "", suffix: "\u0301");
        Assert.Equal(ReplacementRejection.GraphemeBoundary, TriggerReplacementPlanner.Prepare(request, 3).Rejection);
    }

    [Fact]
    public void AResultCaretInsideNewZwjSequenceIsRejectedBeforeEffects()
    {
        Assert.Equal(ReplacementRejection.GraphemeBoundary,
            TriggerReplacementPlanner.Prepare(Request(delimiter: "", body: "👩\u200d", suffix: "💻"), 3).Rejection);
    }

    [Fact]
    public void TextAndResultSizesAreBounded()
    {
        Assert.Equal(ReplacementRejection.TextTooLong,
            TriggerReplacementPlanner.Prepare(Request(prefix: new string('a', TriggerReplacementPlanner.MaxTextUtf16Units)), 3).Rejection);
        Assert.Equal(ReplacementRejection.TextTooLong,
            TriggerReplacementPlanner.Prepare(Request(body: new string('a', TriggerReplacementPlanner.MaxTextUtf16Units)), 3).Rejection);
    }

    [Fact]
    public void TextCaretFocusRevisionAndProtectionAreRecheckedInTheAtomicTarget()
    {
        var request = Request();
        var original = request.Target!;
        var changed = new[]
        {
            original with { Text = original.Text + "changed" },
            original with { SelectionStart = 0, SelectionEnd = 0 },
            original with { TargetToken = Guid.NewGuid() },
            original with { TargetEpoch = 8 },
            original with { TextRevision = 43 },
            original with { FieldState = ReplacementFieldState.Protected },
            original with { Capability = ReplacementCapability.Unknown },
            original with { HasPendingModifiers = true }
        };
        foreach (var snapshot in changed)
        {
            var plan = Plan(request);
            var target = new SimulatedReplacementTarget(original);
            target.SetSnapshot(snapshot);
            var outcome = TriggerReplacementExecutor.Execute(plan, target, 3);
            Assert.Equal(ReplacementExecutionStatus.BlockedNoChange, outcome.Status);
            Assert.Equal(snapshot, target.ReadSnapshot());
            Assert.False(outcome.MayHaveModifiedTarget);
        }
    }

    [Fact]
    public void CancelledOrStaleLibraryPlansNeverEnterBackendAndCannotBeReplayed()
    {
        var backend = new CallbackTarget((_, _) => throw new InvalidOperationException());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var plan = Plan();
        Assert.Equal(ReplacementExecutionReason.CancelledBeforeEffect,
            TriggerReplacementExecutor.Execute(plan, backend, 3, cancellation.Token).Reason);
        Assert.Equal(ReplacementExecutionReason.AlreadyAttempted, TriggerReplacementExecutor.Execute(plan, backend, 3).Reason);
        Assert.Equal(ReplacementExecutionReason.LibraryChanged, TriggerReplacementExecutor.Execute(Plan(), backend, 4).Reason);
        Assert.Equal(0, backend.Calls);
    }

    [Fact]
    public void OnePlanCanEnterBackendOnlyOnceUnderConcurrentCalls()
    {
        var plan = Plan();
        var target = new SimulatedReplacementTarget(plan.ExpectedTarget);
        var backend = new CallbackTarget(target.CompareAndReplace);
        var outcomes = new ConcurrentBag<ReplacementExecutionOutcome>();
        Parallel.For(0, 20, _ => outcomes.Add(TriggerReplacementExecutor.Execute(plan, backend, 3)));
        Assert.Equal(1, backend.Calls);
        Assert.Single(outcomes, r => r.Status == ReplacementExecutionStatus.VerifiedComplete);
        Assert.Equal(19, outcomes.Count(r => r.Reason == ReplacementExecutionReason.AlreadyAttempted));
    }

    [Fact]
    public void SeparatePlansForTheSameRevisionCannotApplyTwice()
    {
        var request = Request();
        var target = new SimulatedReplacementTarget(request.Target!);
        var outcomes = new ConcurrentBag<ReplacementExecutionOutcome>();
        Parallel.Invoke(
            () => outcomes.Add(TriggerReplacementExecutor.Execute(Plan(request), target, 3)),
            () => outcomes.Add(TriggerReplacementExecutor.Execute(Plan(request), target, 3)));
        Assert.Single(outcomes, r => r.Status == ReplacementExecutionStatus.VerifiedComplete);
        Assert.Single(outcomes, r => r.Status == ReplacementExecutionStatus.BlockedNoChange);
        Assert.Equal(43, target.ReadSnapshot().TextRevision);
    }

    [Fact]
    public void AcceptedWithoutReceiptIsUnverifiedAndNeverRetried()
    {
        var plan = Plan();
        var backend = new CallbackTarget((_, _) => new(ConditionalReplacementStatus.Applied));
        var outcome = TriggerReplacementExecutor.Execute(plan, backend, 3);
        Assert.Equal(ReplacementExecutionStatus.SubmittedUnverified, outcome.Status);
        Assert.True(outcome.MayHaveModifiedTarget);
        Assert.False(outcome.RetryAllowedAutomatically);
        Assert.Equal(ReplacementExecutionReason.AlreadyAttempted, TriggerReplacementExecutor.Execute(plan, backend, 3).Reason);
        Assert.Equal(1, backend.Calls);
    }

    [Fact]
    public void WrongTextCaretIdentityOrRevisionCannotBeReportedAsVerified()
    {
        var expected = Plan().ExpectedResult;
        foreach (var changed in new[] { expected with { Text = "wrong" }, expected with { SelectionEnd = 0 },
            expected with { TargetToken = Guid.NewGuid() }, expected with { TextRevision = 999 } })
        {
            var plan = Plan();
            var backend = new CallbackTarget((_, _) => new(ConditionalReplacementStatus.Applied, changed));
            var outcome = TriggerReplacementExecutor.Execute(plan, backend, 3);
            Assert.Equal(ReplacementExecutionReason.VerificationMismatch, outcome.Reason);
            Assert.Equal(ReplacementExecutionStatus.PartialOrUnknown, outcome.Status);
        }
    }

    [Theory]
    [InlineData(ConditionalReplacementStatus.PartialOrUnknown)]
    [InlineData((ConditionalReplacementStatus)999)]
    public void UnknownEffectsNeverBecomeSuccess(ConditionalReplacementStatus status)
    {
        var outcome = TriggerReplacementExecutor.Execute(Plan(), new CallbackTarget((_, _) => new(status)), 3);
        Assert.Equal(ReplacementExecutionStatus.PartialOrUnknown, outcome.Status);
        Assert.False(outcome.RetryAllowedAutomatically);
    }

    [Fact]
    public void BackendExceptionAfterAnEditDoesNotTriggerReplayOrRollback()
    {
        var plan = Plan();
        var simulated = new SimulatedReplacementTarget(plan.ExpectedTarget);
        var backend = new CallbackTarget((proposal, ct) =>
        {
            simulated.CompareAndReplace(proposal, ct);
            throw new OperationCanceledException();
        });
        var outcome = TriggerReplacementExecutor.Execute(plan, backend, 3);
        Assert.Equal(ReplacementExecutionStatus.PartialOrUnknown, outcome.Status);
        Assert.Equal("prima Caffè ☕ dopo", simulated.ReadSnapshot().Text);
        Assert.Equal(ReplacementExecutionReason.AlreadyAttempted, TriggerReplacementExecutor.Execute(plan, backend, 3).Reason);
        Assert.Equal(1, backend.Calls);
    }

    [Fact]
    public void InvalidPayloadAndRawMatchAreRejectedWithoutPreparingAnEffect()
    {
        var request = Request();
        foreach (var malformed in new[] { new string((char)0xd800, 1), new string((char)0xdc00, 1), "\0" })
        {
            Assert.Equal(ReplacementRejection.InvalidSnippet,
                TriggerReplacementPlanner.Prepare(Request(body: malformed), 3).Rejection);
            Assert.Equal(ReplacementRejection.InvalidRawMatch,
                TriggerReplacementPlanner.Prepare(request with { RawMatchedText = malformed }, 3).Rejection);
        }
        Assert.Equal(ReplacementRejection.InvalidSnippet,
            TriggerReplacementPlanner.Prepare(Request(body: ""), 3).Rejection);
        Assert.Equal(ReplacementRejection.InvalidRawMatch,
            TriggerReplacementPlanner.Prepare(request with { RawMatchedText = ";other" }, 3).Rejection);
        Assert.Equal(ReplacementRejection.InvalidDelimiter,
            TriggerReplacementPlanner.Prepare(request with { CommittedDelimiter = null }, 3).Rejection);
    }

    [Fact]
    public void AllVerificationMetadataMustMatchThePreparedResult()
    {
        var request = Request();
        var prepared = Plan(request);
        foreach (var changed in new[]
        {
            prepared.ExpectedResult with { TargetEpoch = 8 },
            prepared.ExpectedResult with { SelectionStart = 0 },
            prepared.ExpectedResult with { FieldState = ReplacementFieldState.Unknown },
            prepared.ExpectedResult with { Capability = ReplacementCapability.Unknown },
            prepared.ExpectedResult with { HasPendingModifiers = true }
        })
        {
            var backend = new CallbackTarget((_, _) => new(ConditionalReplacementStatus.Applied, changed));
            Assert.Equal(ReplacementExecutionReason.VerificationMismatch,
                TriggerReplacementExecutor.Execute(Plan(request), backend, 3).Reason);
        }
        Assert.Equal(ReplacementExecutionReason.BackendUnknown,
            TriggerReplacementExecutor.Execute(Plan(request), new CallbackTarget((_, _) => null!), 3).Reason);
    }

    [Fact]
    public void DiagnosticsDoNotPrintSourceOrSnippetContent()
    {
        var request = Request(body: "PRIVATE_CONTENT");
        var preparation = TriggerReplacementPlanner.Prepare(request, 3);
        var plan = preparation.Plan!;
        foreach (var text in new[] { request.ToString(), request.Target!.ToString(), plan.ToString(), preparation.ToString(),
            new ConditionalReplacementResult(ConditionalReplacementStatus.Applied, plan.ExpectedResult).ToString() })
        {
            Assert.DoesNotContain("PRIVATE_CONTENT", text!);
            Assert.DoesNotContain("prima", text!);
        }
    }

    private sealed class CallbackTarget(Func<TriggerReplacementPlan, CancellationToken, ConditionalReplacementResult> callback)
        : IConditionalReplacementTarget
    {
        private int _calls;
        public int Calls => _calls;
        public ConditionalReplacementResult CompareAndReplace(TriggerReplacementPlan plan, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            return callback(plan, cancellationToken);
        }
    }
}
