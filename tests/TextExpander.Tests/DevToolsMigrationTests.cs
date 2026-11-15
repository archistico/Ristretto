using System.Security.Cryptography;
using System.Text;
using Ristretto.DevTools;
using Xunit;

namespace TextExpander.Tests;

public sealed class DevToolsMigrationTests
{
    private const string Header = "RISTRETTO NOTEPAD TEST\n";
    private const string Fixture = "Café\ne\u0301 🧑🏽‍💻 مرحبا\n日本語 🌍";

    private static string Baseline => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Header))).ToLowerInvariant();
    private static NotepadQualification.Comparison Compare(string contents) =>
        NotepadQualification.Compare(Encoding.UTF8.GetBytes(contents), Baseline, Fixture);

    [Fact]
    public void GateSourceIncludesOnlyCSharpChecks() =>
        Assert.Equal(new[] { "source_audit", "csharp_regressions", "static_checks" }, QualityGate.Plan("source"));

    [Fact]
    public void GateCoreDoesNotRequireNodeOrPython()
    {
        Assert.Contains("xunit", QualityGate.Plan("core"));
        Assert.Contains("startup_cli", QualityGate.Plan("core"));
        Assert.DoesNotContain("node_tests", QualityGate.Plan("core"));
        Assert.DoesNotContain(QualityGate.Plan("core"), name => name.Contains("python", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GateCiDoesNotClaimNativeDesktopTests()
    {
        Assert.Contains("export_contract", QualityGate.Plan("ci"));
        Assert.DoesNotContain("native_windows", QualityGate.Plan("ci"));
        Assert.Contains("native_windows", QualityGate.Plan("full"));
    }

    [Fact]
    public void NotepadUnicodeExactlyMatches()
    {
        var result = Compare(Header + Fixture);
        Assert.Equal("PASS", result.Status);
        Assert.Equal("MATCHES_EXPECTED", result.Document);
    }

    [Fact]
    public void NotepadCrLfMatchesWithoutChangingUnicode()
    {
        var win = (Header + Fixture).Replace("\n", "\r\n");
        Assert.Equal("PASS", Compare(win).Status);
    }

    [Fact]
    public void NotepadCrCrLfIsRejected()
    {
        var broken = (Header + Fixture).Replace("\n", "\r\r\n");
        Assert.Equal("FAIL", Compare(broken).Status);
    }

    [Fact]
    public void NotepadDoubleInsertionIsRejected() =>
        Assert.Equal("snippet_inserted_twice", Compare(Header + Fixture + Fixture).Reason);

    [Fact]
    public void NotepadUnchangedDocumentIsNotRun()
    {
        var bytes = Encoding.UTF8.GetBytes(Header);
        Assert.Equal("NOT_RUN", NotepadQualification.Compare(bytes, Baseline, Fixture).Status);
    }

    [Fact]
    public void NotepadInvalidUtf8IsRejected() =>
        Assert.Equal("saved_document_not_utf8", NotepadQualification.Compare([0xFF, 0xFE], Baseline, Fixture).Reason);

    [Fact]
    public void NotepadMissingDocumentIsNotRun() =>
        Assert.Equal("NOT_RUN", NotepadQualification.Compare(null, Baseline, Fixture).Status);

    [Fact]
    public void NotepadHeaderModificationIsRejected() =>
        Assert.Equal("synthetic_header_changed", Compare("MODIFICATO\n" + Fixture).Reason);

    [Fact]
    public void CliErrorDiagnosticsAcceptNonUtf8Bytes() =>
        Assert.NotEmpty(StartupCli.DecodeDiagnostic([0x95]));

    [Fact]
    public void CliValidUtf8RemainsExact() =>
        Assert.Equal("日本語 🧑🏽‍💻", StartupCli.DecodeDiagnostic(Encoding.UTF8.GetBytes("日本語 🧑🏽‍💻")));
}
