using TextExpander.Application.Features.Output;
using Xunit;

namespace TextExpander.Tests;

public sealed class CooperatingLabProtocolTests
{
    private static readonly Guid Session = Guid.ParseExact("2b0a8ef22b934bcb851e49b7c5138719", "N");

    [Fact]
    public void ReadyRequiresExactSessionAndChildPid()
    {
        var message = CooperatingLabProtocol.Ready(Session, 1421);
        Assert.True(CooperatingLabProtocol.IsReady(message, Session, 1421));
        Assert.False(CooperatingLabProtocol.IsReady(message, Session, 1422));
        Assert.False(CooperatingLabProtocol.IsReady(message, Guid.NewGuid(), 1421));
        Assert.False(CooperatingLabProtocol.IsReady(message + "|extra", Session, 1421));
        Assert.False(CooperatingLabProtocol.IsReady(null, Session, 1421));
    }

    [Fact]
    public void RoundTripCountsNeverContainsSnippetOrTypedText()
    {
        var message = CooperatingLabProtocol.Result(Session, new(3, 1, 2));
        Assert.True(CooperatingLabProtocol.TryReadResult(message, Session, out var counts));
        Assert.Equal(new CooperatingLabCounts(3, 1, 2), counts);
        Assert.DoesNotContain("firma", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("☕", message);
    }

    [Theory]
    [InlineData("RISTRETTO_LAB_RESULT|2b0a8ef22b934bcb851e49b7c5138719|-1|0|0")]
    [InlineData("RISTRETTO_LAB_RESULT|2b0a8ef22b934bcb851e49b7c5138719|1000001|0|0")]
    [InlineData("RISTRETTO_LAB_RESULT|2b0a8ef22b934bcb851e49b7c5138719|1|0|0|extra")]
    [InlineData("RISTRETTO_LAB_RESULT|2b0a8ef22b934bcb851e49b7c5138719|1.0|0|0")]
    [InlineData("RISTRETTO_LAB_RESULT|2b0a8ef22b934bcb851e49b7c5138719|+1|0|0")]
    [InlineData("RISTRETTO_LAB_RESULT|2b0a8ef22b934bcb851e49b7c5138719|1|0")]
    [InlineData("RISTRETTO_LAB_RESULT|2b0a8ef22b934bcb851e49b7c5138719|1|0|NaN")]
    public void InvalidResultsAreRejected(string message)
    {
        Assert.False(CooperatingLabProtocol.TryReadResult(message, Session, out var counts));
        Assert.Null(counts);
    }

    [Fact]
    public void WrongSessionAndArbitraryTextCannotForgeReceipt()
    {
        var message = CooperatingLabProtocol.Result(Guid.NewGuid(), new(1, 0, 0));
        Assert.False(CooperatingLabProtocol.TryReadResult(message, Session, out _));
        Assert.False(CooperatingLabProtocol.TryReadResult(";firma bello", Session, out _));
        Assert.False(CooperatingLabProtocol.TryReadResult(new string('a', 1024), Session, out _));
    }

    [Theory]
    [InlineData(1, 0, 0, true, 0, true)]
    [InlineData(0, 0, 0, true, 0, false)]
    [InlineData(1, 1, 0, true, 0, false)]
    [InlineData(1, 0, 1, true, 0, false)]
    [InlineData(1, 0, 0, false, 0, false)]
    [InlineData(1, 0, 0, true, 1, false)]
    public void SuccessRequiresNonzeroVerifiedNoOtherOutcomesAndCleanChildExit(
        int verified, int rejected, int uncertain, bool ready, int exitCode, bool expected)
    {
        Assert.Equal(expected, CooperatingLabProtocol.IsSuccessful(new(verified, rejected, uncertain), ready, exitCode));
    }

    [Fact]
    public void InvalidCountsCannotBeReported()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CooperatingLabProtocol.Result(Session, new(-1, 0, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => CooperatingLabProtocol.Result(Session, new(1_000_001, 0, 0)));
        Assert.False(CooperatingLabProtocol.IsSuccessful(null, true, 0));
    }
}
