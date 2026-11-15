using TextExpander.Infrastructure.Platform.Windows;
using Xunit;

namespace TextExpander.Tests;

public sealed class PickerClipboardReadbackTests
{
    [Theory]
    [InlineData("firma", "firma", 17u, 18u, 18u, 18u)]
    [InlineData("firma", "altro", 17u, 18u, 18u, 0u)]
    [InlineData("firma", null, 17u, 18u, 18u, 0u)]
    [InlineData("firma", "firma", 17u, 17u, 17u, 0u)]
    [InlineData("firma", "firma", 0u, 18u, 18u, 0u)]
    [InlineData("firma", "firma", 17u, 0u, 0u, 0u)]
    [InlineData("firma", "firma", 17u, 18u, 19u, 0u)]
    [InlineData("firma", "firma", 17u, 18u, 0u, 0u)]
    [InlineData("firma", "firma", uint.MaxValue, 1u, 1u, 1u)]
    [InlineData("", "", 17u, 18u, 18u, 18u)]
    public void RequiresMatchingClipboardContentsAndStableSequence(
        string requested, string? observed, uint before, uint afterWrite,
        uint afterRead, uint expected)
    {
        Assert.Equal(expected, PickerClipboardReadbackVerifier.VerifiedSequence(
            requested, observed, before, afterWrite, afterRead));
    }

    [Fact]
    public void MultilineWindowsLineEndingConversionIsAccepted()
    {
        Assert.Equal(21u, PickerClipboardReadbackVerifier.VerifiedSequence(
            "uno\ndue\r\ntre", "uno\r\ndue\r\ntre", 20, 21, 21));
    }

    [Fact]
    public void UnicodeEmojiCombiningAndNonLatinTextIsPreservedExactly()
    {
        const string input = "caffe\u0301 👩🏽‍💻 العربية 日本語\nseconda riga";
        Assert.Equal(42u, PickerClipboardReadbackVerifier.VerifiedSequence(
            input, input.Replace("\n", "\r\n", StringComparison.Ordinal), 41, 42, 42));
        Assert.Equal(0u, PickerClipboardReadbackVerifier.VerifiedSequence(
            input, input.Normalize(System.Text.NormalizationForm.FormC), 41, 42, 42));
    }

    [Theory]
    [InlineData("a\nb", "a\rb")]
    [InlineData("Abc", "abc")]
    [InlineData("a\0b", "ab")]
    [InlineData("😀", "😃")]
    [InlineData("hello", "hello ")]
    public void DifferentTextNeverAuthorizesPaste(string requested, string observed)
    {
        Assert.Equal(0u, PickerClipboardReadbackVerifier.VerifiedSequence(
            requested, observed, 100, 101, 101));
    }

    [Fact]
    public void NullExpectedTextIsRejectedRatherThanMatchingNullClipboard()
    {
        Assert.Throws<ArgumentNullException>(() =>
            PickerClipboardReadbackVerifier.VerifiedSequence(null!, null, 1, 2, 2));
    }
}
