using Xunit;

namespace Ristretto.WindowsOutputLab;

/// <summary>Compiles the small, platform-neutral production lab protocol as linked source.</summary>
public sealed class PickerOwnedEditReceiptTests
{
    private const string Token = "0123456789abcdef0123456789abcdef";
    private const string Other = "fedcba9876543210fedcba9876543210";

    [Fact]
    public void ValidReadyIsBoundToTokenAndTwoDistinctHandles()
    {
        var line = PickerOwnedEditReceipt.Ready(Token, (nint)101, (nint)202);
        Assert.True(PickerOwnedEditReceipt.TryReadReady(line, Token, out var parent, out var edit));
        Assert.Equal((nint)101, parent);
        Assert.Equal((nint)202, edit);
        Assert.False(PickerOwnedEditReceipt.TryReadReady(line, Other, out _, out _));
    }

    [Theory]
    [InlineData("PICKER_READY|0123456789abcdef0123456789abcdef|1|1")]
    [InlineData("PICKER_READY|0123456789abcdef0123456789abcdef|0|2")]
    [InlineData("PICKER_READY|0123456789abcdef0123456789abcdef|-1|2")]
    [InlineData("READY|0123456789abcdef0123456789abcdef|1|2")]
    [InlineData("PICKER_READY|0123456789abcdef0123456789abcdef|1|2|extra")]
    [InlineData("PICKER_READY|0123456789abcdef0123456789abcdef|oops|2")]
    public void ReadyRejectsMalformedAndDuplicateHandles(string value) =>
        Assert.False(PickerOwnedEditReceipt.TryReadReady(value, Token, out _, out _));

    [Fact]
    public void ReceiptRoundTripsOnlyDefinedOutcomes()
    {
        foreach (var expected in Enum.GetValues<PickerOwnedEditReceipt.Outcome>())
        {
            var line = PickerOwnedEditReceipt.Result(Token, expected);
            Assert.True(PickerOwnedEditReceipt.TryReadResult(line, Token, out var actual));
            Assert.Equal(expected, actual);
            Assert.False(PickerOwnedEditReceipt.TryReadResult(line, Other, out _));
        }
    }

    [Theory]
    [InlineData("PICKER_RESULT|0123456789abcdef0123456789abcdef|SUCCESS")]
    [InlineData("PICKER_RESULT|0123456789abcdef0123456789abcdef|MATCH|MATCH")]
    [InlineData("RESULT|0123456789abcdef0123456789abcdef|MATCH")]
    [InlineData("PICKER_RESULT|wrong|MATCH")]
    public void NeverTreatsMalformedOrUnknownReceiptAsSuccess(string text) =>
        Assert.False(PickerOwnedEditReceipt.TryReadResult(text, Token, out _));

    [Fact]
    public void CompareIsExactForUnicodeCodeUnitsAndLineEndings()
    {
        const string expected = "Caffè ☕ cafe\u0301 漢字 مرحبا 🧑🏽‍💻\r\nSeconda riga: Ω 🚀";
        Assert.Equal(PickerOwnedEditReceipt.Outcome.Match,
            PickerOwnedEditReceipt.Compare(expected, expected, true));
        Assert.Equal(PickerOwnedEditReceipt.Outcome.Mismatch,
            PickerOwnedEditReceipt.Compare(expected.Replace("\r\n", "\n"), expected, true));
        Assert.Equal(PickerOwnedEditReceipt.Outcome.Mismatch,
            PickerOwnedEditReceipt.Compare(expected.Replace("e\u0301", "é"), expected, true));
        Assert.Equal(PickerOwnedEditReceipt.Outcome.Mismatch,
            PickerOwnedEditReceipt.Compare(expected[..^1], expected, true));
    }

    [Fact]
    public void MissingControlOrTextIsUnavailableNotPass()
    {
        Assert.Equal(PickerOwnedEditReceipt.Outcome.Unavailable,
            PickerOwnedEditReceipt.Compare(null, "fixture", true));
        Assert.Equal(PickerOwnedEditReceipt.Outcome.Unavailable,
            PickerOwnedEditReceipt.Compare("fixture", "fixture", false));
    }
}
