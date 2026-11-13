using TextExpander.Application.Features.Output;
using TextExpander.Infrastructure.Platform.Windows;
using Xunit;

namespace TextExpander.Tests;

public sealed class WindowsOutputContractTests
{
    private sealed class FakeWindowsApi : IWindowsTextApi
    {
        public nint Handle { get; set; } = 0x1234;
        public uint Process { get; set; } = 123;
        public bool Modifier { get; set; }
        public int? AcceptedOverride { get; set; }
        public int Calls { get; private set; }
        public string? SentText { get; private set; }
        public nint ForegroundWindow() => Handle;
        public uint WindowProcessId(nint window) => window == Handle ? Process : 0;
        public bool IsAnyModifierPressed() => Modifier;
        public int SendUnicodeInput(string utf16Text)
        {
            Calls++;
            SentText = utf16Text;
            return AcceptedOverride ?? utf16Text.Length * 2;
        }
    }

    private static UnicodeOutputPlan Plan(string text)
    {
        Assert.True(UnicodeOutputPlan.TryCreate(text, out var plan, out _));
        return Assert.IsType<UnicodeOutputPlan>(plan);
    }

    private static WindowsUnicodeTextOutput Output(FakeWindowsApi fake) =>
        new(fake, () => true, ourProcessId: 99);

    [Theory]
    [InlineData("àéü 漢字 👨‍👩‍👧‍👦")]
    [InlineData("cafe\u0301 👍🏽 🧑🏽‍💻")]
    [InlineData("مرحبا שלום Привет")]
    public void UnicodeIsPreservedExactly(string original)
    {
        var input = Plan(original);
        var fake = new FakeWindowsApi();
        var target = Output(fake).CaptureExternalForeground();
        var result = Output(fake).Submit(input, target);
        Assert.Equal(TextSubmissionStatus.SubmittedUnverified, result.Status);
        Assert.Equal(original, fake.SentText);
        Assert.Equal(original.Length * 2, result.RequiredInputEvents);
    }

    [Theory]
    [InlineData("text\r\nother")]
    [InlineData("a\tb")]
    [InlineData("line\u2028break")]
    [InlineData("line\u2029break")]
    [InlineData("")]
    public void UnsafeOrUnsupportedInputIsRejected(string original)
    {
        Assert.False(UnicodeOutputPlan.TryCreate(original, out var plan, out _));
        Assert.Null(plan);
    }

    // Valori UTF-16 malformati costruiti a runtime. Evita gli InlineData(string)
    // (il compilatore puo convertirli in U+FFFD nei metadati xUnit).
    [Fact]
    public void UnpairedHighSurrogateIsRejected() => AssertUnpairedSurrogate((char)0xD83D);

    [Fact]
    public void UnpairedLowSurrogateIsRejected() => AssertUnpairedSurrogate((char)0xDC4D);

    private static void AssertUnpairedSurrogate(char codeUnit)
    {
        var original = new string(codeUnit, 1);
        Assert.False(UnicodeOutputPlan.TryCreate(original, out var plan, out var reason));
        Assert.Null(plan);
        Assert.Equal("invalid_utf16", reason);
    }

    [Fact]
    public void SurrogatesAreRejectedWhenEmbeddedInOtherwiseValidText()
    {
        var high = new string((char)0xD83D, 1);
        var low = new string((char)0xDC4D, 1);
        foreach (var original in new[] { $"ciao{high}!", $"a{low}b", $"{low}{high}" })
        {
            Assert.False(UnicodeOutputPlan.TryCreate(original, out var plan, out var reason));
            Assert.Null(plan);
            Assert.Equal("invalid_utf16", reason);
        }
    }

    [Fact]
    public void UnicodeReplacementCharacterRemainsValidText()
    {
        // U+FFFD è un carattere valido, a differenza di un surrogate isolato.
        Assert.True(UnicodeOutputPlan.TryCreate("\uFFFD", out var plan, out var reason));
        Assert.NotNull(plan);
        Assert.Equal("ok", reason);
        Assert.Equal("\uFFFD", plan.Text);
    }

    [Fact]
    public void OversizedTextIsRejectedWithoutTruncation()
    {
        Assert.False(UnicodeOutputPlan.TryCreate(new string('x', UnicodeOutputPlan.MaxUtf16Units + 1),
            out _, out var reason));
        Assert.Equal("too_long", reason);
    }

    [Fact]
    public void OwnApplicationCannotBeCapturedOrTargeted()
    {
        var fake = new FakeWindowsApi { Process = 99 };
        Assert.Null(Output(fake).CaptureExternalForeground());
        var result = Output(fake).Submit(Plan("hello"), new WindowsOutputTarget(fake.Handle, 99));
        Assert.Equal(TextSubmissionStatus.Rejected, result.Status);
        Assert.Equal(0, fake.Calls);
    }

    [Fact]
    public void WrongForegroundWindowRejectsWithoutAnyEffect()
    {
        var fake = new FakeWindowsApi();
        var expected = Output(fake).CaptureExternalForeground();
        fake.Handle = 0x5678;
        var result = Output(fake).Submit(Plan("hello"), expected);
        Assert.Equal(TextSubmissionStatus.Rejected, result.Status);
        Assert.Equal("focus_changed", result.Reason);
        Assert.Equal(0, fake.Calls);
    }

    [Fact]
    public void PressedModifiersRejectWithoutAnyEffect()
    {
        var fake = new FakeWindowsApi { Modifier = true };
        var result = Output(fake).Submit(Plan("hello"), Output(fake).CaptureExternalForeground());
        Assert.Equal(TextSubmissionStatus.Rejected, result.Status);
        Assert.Equal(0, fake.Calls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void PartialOrBlockedWindowsInputIsNeverReportedAsSuccess(int accepted)
    {
        var fake = new FakeWindowsApi { AcceptedOverride = accepted };
        var result = Output(fake).Submit(Plan("abc"), Output(fake).CaptureExternalForeground());
        Assert.Equal(TextSubmissionStatus.PartialOrUnknown, result.Status);
        Assert.Equal(accepted, result.AcceptedInputEvents);
        Assert.Equal(1, fake.Calls); // Nessun retry automatico.
    }

    [Fact]
    public void UnsupportedPlatformNeverUsesNativeApi()
    {
        var fake = new FakeWindowsApi();
        var output = new WindowsUnicodeTextOutput(fake, () => false, ourProcessId: 99);
        Assert.Null(output.CaptureExternalForeground());
        Assert.Equal(TextSubmissionStatus.Rejected,
            output.Submit(Plan("hello"), new WindowsOutputTarget(fake.Handle, fake.Process)).Status);
        Assert.Equal(0, fake.Calls);
    }
}
