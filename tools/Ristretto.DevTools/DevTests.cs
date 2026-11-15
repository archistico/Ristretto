using System.Text;

namespace Ristretto.DevTools;

public static class DevTests
{
    public static int Run(string root)
    {
        var errors = new List<string>();
        var count = 0;
        void Test(string name, Action check)
        {
            count++;
            try { check(); Console.WriteLine("  PASS " + name); }
            catch (Exception ex) { errors.Add(name + ": " + ex.Message); Console.WriteLine("  FAIL " + name); }
        }
        static void Expect(bool condition) { if (!condition) throw new InvalidOperationException("Asserzione non soddisfatta"); }
        var example = "Riga 1\ncafé e\u0301 🌍 🧑🏽‍💻 مرحبا\r\nRiga 3";
        var baseBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("RISTRETTO NOTEPAD TEST\n")).ToArray();
        var baseline = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(baseBytes)).ToLowerInvariant();
        var prefix = "RISTRETTO NOTEPAD TEST\n";
        Test("Notepad: documento invariato", () => Expect(NotepadQualification.Compare(baseBytes, baseline, example).Status == "NOT_RUN"));
        Test("Notepad: file assente", () => Expect(NotepadQualification.Compare(null, baseline, example).Status == "NOT_RUN"));
        Test("Notepad: Unicode LF", () => Expect(NotepadQualification.Compare(Encoding.UTF8.GetBytes(prefix + example), baseline, example).Status == "PASS"));
        Test("Notepad: documento UTF-8 con BOM", () => Expect(NotepadQualification.Compare(
            new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes(prefix + example)).ToArray(),
            baseline, example).Status == "PASS"));
        Test("Notepad: BOM incompleto viene rifiutato", () => Expect(NotepadQualification.Compare(
            new byte[] { 0xEF, 0xBB }, baseline, example).Reason == "saved_document_not_utf8"));
        Test("Notepad: fixture UTF-8 con BOM, preparazione e riapertura", () =>
        {
            var temporaryRoot = Path.Combine(Path.GetTempPath(), "ristretto-devtools-bom-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(temporaryRoot, "data"));
            try
            {
                File.Copy(Path.Combine(root, "VERSION.txt"), Path.Combine(temporaryRoot, "VERSION.txt"));
                File.WriteAllBytes(Path.Combine(temporaryRoot, "data", "PICKER_INSERT_SYNTHETIC_FIXTURE.txt"),
                    new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes(example)).ToArray());
                var session = NotepadQualification.Prepare(temporaryRoot);
                Expect(session.Fixture == example);
                Expect(NotepadQualification.ReadSession(temporaryRoot, session.Token).Fixture == example);
            }
            finally { if (Directory.Exists(temporaryRoot)) Directory.Delete(temporaryRoot, recursive: true); }
        });
        Test("Notepad: Unicode CRLF", () => Expect(NotepadQualification.Compare(Encoding.UTF8.GetBytes((prefix + example).Replace("\r\n", "\n").Replace("\n", "\r\n")), baseline, example).Status == "PASS"));
        Test("Notepad: CRCRLF alterato", () => Expect(NotepadQualification.Compare(Encoding.UTF8.GetBytes((prefix + example).Replace("\r\n", "\r\r\n")), baseline, example).Status == "FAIL"));
        Test("Notepad: inserimento doppio", () => Expect(NotepadQualification.Compare(Encoding.UTF8.GetBytes(prefix + example + example), baseline, example).Reason == "snippet_inserted_twice"));
        Test("Notepad: intestazione modificata", () => Expect(NotepadQualification.Compare(Encoding.UTF8.GetBytes("ALTRO\n" + example), baseline, example).Reason == "synthetic_header_changed"));
        Test("Notepad: testo incompleto", () => Expect(NotepadQualification.Compare(Encoding.UTF8.GetBytes(prefix + "X"), baseline, example).Reason == "snippet_missing_or_incomplete"));
        Test("Notepad: UTF-8 corrotto", () => Expect(NotepadQualification.Compare([0xFF, 0xFE], baseline, example).Reason == "saved_document_not_utf8"));
        Test("Notepad: conversione NFC/NFD non ammessa", () =>
        {
            const string nfd = "e\u0301";
            Expect(NotepadQualification.Compare(Encoding.UTF8.GetBytes(prefix + "é"), baseline, nfd).Reason == "unicode_normalization_changed");
        });
        Test("Versione: componenti numeriche consentite", () => Expect(
            System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(Path.Combine(root, "VERSION.txt")).Trim().Replace("Ristretto ", "", StringComparison.Ordinal),
                @"^3\.9\.0-alpha(?:\.\d+)+$")));
        Test("Gate source: soli controlli C#", () => Expect(QualityGate.Plan("source").SequenceEqual(
            ["source_audit", "csharp_regressions", "static_checks"])));
        Test("Gate core: non richiede Node", () => Expect(!QualityGate.Plan("core").Contains("node_tests")));
        Test("Gate CI: non autorizza test nativi", () => Expect(!QualityGate.Plan("ci").Contains("native_windows")));
        Test("Gate full: include test nativi", () => Expect(QualityGate.Plan("full").Contains("native_windows")));
        Test("Uscita CLI CP1252 non UTF-8 accettata soltanto come diagnostica", () =>
            Expect(StartupCli.DecodeDiagnostic([0x95]).Length > 0));
        Test("Uscita CLI Unicode UTF-8 conservata", () =>
            Expect(StartupCli.DecodeDiagnostic(Encoding.UTF8.GetBytes("日本語 🌍")) == "日本語 🌍"));
        Test("Schema documentazione: file di prova presente", () =>
            Expect(File.Exists(Path.Combine(root, "data/PICKER_INSERT_SYNTHETIC_FIXTURE.txt"))));
        Test("Nessuno script Python eseguibile", () => Expect(
            !Directory.GetFiles(Path.Combine(root, "scripts"), "*.py").Any()));
        Test("Fixture C#: confronto differenza conclusiva", () => Expect(
            NotepadQualification.Compare(Encoding.UTF8.GetBytes(prefix + "testo diverso"), baseline, example).Status == "FAIL"));
        foreach (var error in errors) Console.Error.WriteLine("  - " + error);
        Console.WriteLine($"TEST STRUMENTI C#: {count - errors.Count}/{count} {(errors.Count == 0 ? "PASS" : "FAIL")}");
        return errors.Count == 0 ? 0 : 1;
    }
}
