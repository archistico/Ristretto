using TextExpander.Application.Features.Output;
using Xunit;

namespace TextExpander.Tests;

public sealed class RemoteOwnedEditProtocolTests
{
    private static readonly Guid Session = Guid.ParseExact("1e110ba9803345c9bd4388d214a81d52", "N");

    [Fact]
    public void ReadyIsBoundToExactSessionAndExpectedChildProcess()
    {
        var ready = RemoteOwnedEditProtocol.Ready(Session, 321);
        Assert.True(RemoteOwnedEditProtocol.IsReady(ready, Session, 321));
        Assert.False(RemoteOwnedEditProtocol.IsReady(ready, Session, 322));
        Assert.False(RemoteOwnedEditProtocol.IsReady(ready, Guid.NewGuid(), 321));
        Assert.False(RemoteOwnedEditProtocol.IsReady(ready + "|extra", Session, 321));
        Assert.False(RemoteOwnedEditProtocol.IsReady(ready.Replace("|321", "|+321"), Session, 321));
        Assert.False(RemoteOwnedEditProtocol.IsReady(null, Session, 321));
    }

    [Fact]
    public void RequestHasNoTextAndIsOneShot()
    {
        var request = RemoteOwnedEditProtocol.Request(Session);
        Assert.True(RemoteOwnedEditProtocol.IsRequest(request, Session));
        Assert.DoesNotContain("firma", request, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Caffè", request, StringComparison.OrdinalIgnoreCase);
        Assert.False(RemoteOwnedEditProtocol.IsRequest(request + "|2", Session));
        Assert.False(RemoteOwnedEditProtocol.IsRequest(request.Replace("|1", "|01"), Session));
        Assert.False(RemoteOwnedEditProtocol.IsRequest(request, Guid.NewGuid()));
        var gate = new RemoteOwnedEditOneShotGate(Session);
        Assert.True(gate.TryConsume(request));
        Assert.False(gate.TryConsume(request));
    }

    [Fact]
    public void MalformedFirstAttemptBurnsTheGateWithoutEffects()
    {
        var gate = new RemoteOwnedEditOneShotGate(Session);
        Assert.False(gate.TryConsume("bogus"));
        Assert.False(gate.TryConsume(RemoteOwnedEditProtocol.Request(Session)));
    }

    [Fact]
    public void BomPrefixedRequestIsRejectedAndBurnsTheGate()
    {
        var request = RemoteOwnedEditProtocol.Request(Session);
        var gate = new RemoteOwnedEditOneShotGate(Session);
        Assert.False(gate.TryConsume("\uFEFF" + request));
        Assert.False(gate.TryConsume(request));
    }

    [Theory]
    [InlineData(RemoteOwnedEditReceipt.Verified)]
    [InlineData(RemoteOwnedEditReceipt.RejectedNoChange)]
    [InlineData(RemoteOwnedEditReceipt.PartialOrUnknown)]
    [InlineData(RemoteOwnedEditReceipt.DuplicateOrInvalid)]
    public void ReceiptsRoundTripWithoutContent(RemoteOwnedEditReceipt expected)
    {
        var encoded = RemoteOwnedEditProtocol.Receipt(Session, expected);
        Assert.True(RemoteOwnedEditProtocol.TryReadReceipt(encoded, Session, out var actual));
        Assert.Equal(expected, actual);
        Assert.DoesNotContain("漢字", encoded);
        Assert.DoesNotContain(";firma", encoded);
    }

    [Theory]
    [InlineData("RISTRETTO_REMOTE_18|RESULT|1e110ba9803345c9bd4388d214a81d52|1|verified")]
    [InlineData("RISTRETTO_REMOTE_18|RESULT|1e110ba9803345c9bd4388d214a81d52|1|Unknown")]
    [InlineData("RISTRETTO_REMOTE_18|RESULT|1e110ba9803345c9bd4388d214a81d52|01|Verified")]
    [InlineData("RISTRETTO_REMOTE_18|RESULT|1e110ba9803345c9bd4388d214a81d52|1|Verified|extra")]
    [InlineData("RISTRETTO_REMOTE_18|RESULT|1e110ba9803345c9bd4388d214a81d52|1|Verified ")]
    public void InvalidReceiptsCannotEstablishSuccess(string data)
    {
        Assert.False(RemoteOwnedEditProtocol.TryReadReceipt(data, Session, out _));
    }

    [Fact]
    public void CrossSessionAndOversizeReceiptsAreRejected()
    {
        var value = RemoteOwnedEditProtocol.Receipt(Session, RemoteOwnedEditReceipt.Verified);
        Assert.False(RemoteOwnedEditProtocol.TryReadReceipt(value, Guid.NewGuid(), out _));
        Assert.False(RemoteOwnedEditProtocol.TryReadReceipt(new string('a', 1000), Session, out _));
        Assert.Throws<ArgumentException>(() => RemoteOwnedEditProtocol.Request(Guid.Empty));
        Assert.Throws<ArgumentOutOfRangeException>(() => RemoteOwnedEditProtocol.Ready(Session, 0));
    }
}
