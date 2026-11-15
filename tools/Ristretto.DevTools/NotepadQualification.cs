using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace Ristretto.DevTools;

public static class NotepadQualification
{
    private const string Basename = "notepad-synthetic.txt";
    private const string BaseText = "RISTRETTO NOTEPAD TEST\n";
    private const string Schema = "ristretto/notepad-qualification-session/1";
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly Dictionary<string, string> Reasons = new()
    {
        ["synthetic_document_unavailable"] = "Documento sintetico mancante: prepara una nuova sessione.",
        ["file_unchanged_no_saved_insertion"] = "Il file non è cambiato: premi Inserisci o Invio nel picker e salva con Ctrl+S.",
        ["saved_document_not_utf8"] = "Il documento salvato non contiene UTF-8 valido.",
        ["saved_synthetic_document_matches_unicode_fixture"] = "Il file salvato corrisponde esattamente al testo Unicode atteso.",
        ["snippet_inserted_twice"] = "Lo snippet è presente due volte.",
        ["snippet_inserted_at_unexpected_position"] = "Lo snippet è stato inserito in una posizione diversa dalla fine del documento.",
        ["unicode_normalization_changed"] = "Differenza di normalizzazione Unicode NFC/NFD.",
        ["extra_trailing_newline"] = "È stata aggiunta una riga vuota finale.",
        ["synthetic_header_changed"] = "Intestazione del documento sintetico modificata.",
        ["snippet_missing_or_incomplete"] = "Snippet assente o incompleto nel file salvato.",
        ["saved_document_differs_from_fixture"] = "Il testo salvato non coincide con la fixture."
    };

    public sealed record Comparison(string Status, string Reason, string Document);
    public sealed record Session(string Token, string Folder, string Document, string Fixture, string BaselineHash);

    public static int Run(string root, string[] args)
    {
        if (args.Length == 0) throw new ArgumentException("Uso: notepad prepare [--open] | status|verify|launch --session TOKEN");
        switch (args[0])
        {
            case "prepare":
                var s = Prepare(root, Options.Has(args, "--open"));
                Console.WriteLine("Sessione di prova: " + s.Token);
                Console.WriteLine("Documento sintetico: " + s.Document);
                Console.WriteLine("Contenuto dello snippet: " + Path.Combine(root, "data/PICKER_INSERT_SYNTHETIC_FIXTURE.txt"));
                Console.WriteLine("PASSO 1 — Crea il profilo SQLite separato:");
                Console.WriteLine("  scripts\\seed-picker-notepad.cmd " + s.Token);
                Console.WriteLine("PASSO 2 — Chiudi altre istanze di Ristretto e avvia:");
                Console.WriteLine("  scripts\\validate-picker-notepad.cmd launch --session " + s.Token);
                Console.WriteLine("PASSO 3 — In Blocco note apri il documento sintetico; posiziona il cursore in fondo (Ctrl+End).");
                Console.WriteLine("PASSO 4 — Richiama il picker, seleziona ;ristretto-test e premi Invio o Inserisci.");
                Console.WriteLine("PASSO 5 — Salva il file in Blocco note con Ctrl+S.");
                Console.WriteLine("PASSO 6 — Verifica con:");
                Console.WriteLine("  scripts\\validate-picker-notepad.cmd verify --session " + s.Token);
                Console.WriteLine("Questo strumento non invia tasti. File invariato = NOT_RUN.");
                return 0;
            case "status":
            case "verify":
            case "launch":
                var token = Options.Value(args, "--session") ?? throw new ArgumentException("Specificare --session TOKEN");
                var session = ReadSession(root, token);
                var fixture = ReadFixture(root);
                var cmp = Compare(File.Exists(session.Document) ? File.ReadAllBytes(session.Document) : null, session.BaselineHash, fixture);
                var profile = CheckProfile(Path.Combine(session.Folder, "profile"), fixture);
                if (args[0] == "status")
                {
                    Console.WriteLine("Sessione: " + token);
                    Console.WriteLine("Profilo SQLite sintetico: " + profile);
                    Console.WriteLine("Documento sintetico salvato: " + cmp.Document);
                    Console.WriteLine("Spiegazione: " + Reasons[cmp.Reason]);
                    Console.WriteLine("Il programma non registra tasti né contenuti digitati.");
                    if (profile == "NOT_CREATED") Console.WriteLine("PROSSIMO PASSO — esegui scripts\\seed-picker-notepad.cmd " + token);
                    else if (cmp.Document == "UNCHANGED") Console.WriteLine("PROSSIMO PASSO — inserisci dal picker, salva con Ctrl+S, poi verifica.");
                    else if (cmp.Status == "PASS") Console.WriteLine("PROSSIMO PASSO — esegui verify per produrre la ricevuta PASS.");
                    return 0;
                }
                if (args[0] == "launch")
                {
                    if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Il lancio Notepad richiede Windows.");
                    if (profile != "READY" || cmp.Document == "MISSING") throw new InvalidOperationException("Profilo non pronto; eseguire seed-picker-notepad.cmd TOKEN.");
                    var project = Path.Combine(root, "src/TextExpander.Desktop/TextExpander.Desktop.csproj");
                    Console.WriteLine("Avvio di Ristretto con il SOLO profilo sintetico: " + Path.Combine(session.Folder, "profile"));
                    Console.WriteLine("Non vengono inviati tasti né incollato testo da questo comando.");
                    return ProcessTool.Run("dotnet", ["run", "-c", "Release", "--project", project, "--", "--data-dir", Path.Combine(session.Folder, "profile")], root);
                }
                var receipt = new
                {
                    schema = "ristretto/notepad-qualification-result/1",
                    version = File.ReadAllText(Path.Combine(root, "VERSION.txt")).Trim(), session = token,
                    timestamp_utc = DateTimeOffset.UtcNow.ToString("O"), status = cmp.Status,
                    reason = cmp.Reason, evidence = "saved_synthetic_file_only",
                    desktop_action_proven = false, global_expansion_proven = false,
                    editor_version_verified = false, contains_snippet_text = false,
                    contains_clipboard_text = false, desktop_automation_used = false
                };
                var receiptPath = Path.Combine(session.Folder, "receipt-" + Guid.NewGuid().ToString("N") + ".json");
                using (var stream = new FileStream(receiptPath, FileMode.CreateNew))
                    JsonSerializer.Serialize(stream, receipt, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine($"Esito: {cmp.Status} | codice: {cmp.Reason}");
                Console.WriteLine("Spiegazione: " + Reasons[cmp.Reason]);
                Console.WriteLine("Ricevuta senza testo digitato: " + receiptPath);
                Console.WriteLine("PASS dimostra solo il file salvato, non l'espansione automatica.");
                return cmp.Status switch { "PASS" => 0, "NOT_RUN" => 3, _ => 1 };
            default:
                throw new ArgumentException("Comando Notepad sconosciuto: " + args[0]);
        }
    }

    public static Session Prepare(string root, bool openEditor = false)
    {
        var fixture = ReadFixture(root);
        var basePath = SessionBase(root);
        Directory.CreateDirectory(basePath);
        var token = Guid.NewGuid().ToString("N");
        var folder = Path.Combine(basePath, token);
        Directory.CreateDirectory(folder);
        var document = Path.Combine(folder, Basename);
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes(BaseText)).ToArray();
        File.WriteAllBytes(document, bytes);
        var baseline = Digest(bytes);
        var meta = new Dictionary<string, string>
        {
            ["schema"] = Schema, ["session"] = token,
            ["version"] = File.ReadAllText(Path.Combine(root, "VERSION.txt")).Trim(),
            ["baseline_sha256"] = baseline, ["fixture_sha256"] = Digest(Encoding.UTF8.GetBytes(fixture)),
            ["document_name"] = Basename
        };
        using (var stream = new FileStream(Path.Combine(folder, "session.json"), FileMode.CreateNew))
            JsonSerializer.Serialize(stream, meta, new JsonSerializerOptions { WriteIndented = true });
        if (openEditor)
        {
            if (!OperatingSystem.IsWindows()) Console.WriteLine("ATTENZIONE: apertura di Blocco note disponibile soltanto su Windows.");
            else
                try
                {
                    using var process = Process.Start(new ProcessStartInfo("notepad.exe")
                    {
                        UseShellExecute = false, ArgumentList = { document }
                    });
                    Console.WriteLine("Apertura richiesta: controlla che Blocco note sia visibile.");
                }
                catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception)
                { Console.WriteLine("ATTENZIONE: Blocco note non avviato; la sessione resta valida."); }
        }
        return new Session(token, folder, document, fixture, baseline);
    }

    public static Session ReadSession(string root, string token)
    {
        if (!Regex.IsMatch(token, "^[0-9a-f]{32}$")) throw new ArgumentException("Token di sessione non valido");
        var folder = Path.Combine(SessionBase(root), token);
        if (!Directory.Exists(folder) || IsLink(folder)) throw new InvalidOperationException("Sessione inesistente o collegamento non sicuro");
        var metadataFile = Path.Combine(folder, "session.json");
        if (!File.Exists(metadataFile) || IsLink(metadataFile)) throw new InvalidOperationException("Metadati di sessione mancanti o non sicuri");
        using var json = JsonDocument.Parse(File.ReadAllText(metadataFile));
        string Field(string name) => json.RootElement.GetProperty(name).GetString() ?? "";
        var fixture = ReadFixture(root);
        if (Field("schema") != Schema || Field("session") != token || Field("document_name") != Basename ||
            Field("version") != File.ReadAllText(Path.Combine(root, "VERSION.txt")).Trim() ||
            Field("fixture_sha256") != Digest(Encoding.UTF8.GetBytes(fixture)))
            throw new InvalidOperationException("Metadati versione, sessione o fixture non corrispondenti");
        var doc = Path.Combine(folder, Basename);
        if (IsLink(doc)) throw new InvalidOperationException("Documento sintetico collegato a un'altra directory");
        return new Session(token, folder, doc, fixture, Field("baseline_sha256"));
    }

    public static Comparison Compare(byte[]? raw, string baseline, string fixture)
    {
        if (raw is null) return new("NOT_RUN", "synthetic_document_unavailable", "MISSING");
        if (Digest(raw) == baseline) return new("NOT_RUN", "file_unchanged_no_saved_insertion", "UNCHANGED");
        string saved;
        try
        {
            var bytes = WithoutUtf8Bom(raw);
            saved = Normalize(StrictUtf8.GetString(bytes));
        }
        catch (DecoderFallbackException) { return new("FAIL", "saved_document_not_utf8", "CHANGED_INVALID"); }
        var prefix = Normalize(BaseText);
        var snippet = Normalize(fixture);
        var expected = prefix + snippet;
        if (saved == expected) return new("PASS", "saved_synthetic_document_matches_unicode_fixture", "MATCHES_EXPECTED");
        var reason = saved == prefix + snippet + snippet ? "snippet_inserted_twice" :
            saved == expected + "\n" ? "extra_trailing_newline" :
            saved.Normalize(NormalizationForm.FormC) == expected.Normalize(NormalizationForm.FormC) ? "unicode_normalization_changed" :
            !saved.StartsWith(prefix) && saved.EndsWith(snippet) ? "synthetic_header_changed" :
            snippet.Length > 0 && saved.Contains(snippet) ? "snippet_inserted_at_unexpected_position" :
            !saved.StartsWith(prefix) ? "synthetic_header_changed" :
            saved.StartsWith(prefix) ? "snippet_missing_or_incomplete" : "saved_document_differs_from_fixture";
        return new("FAIL", reason, "CHANGED_DIFFERENT");
    }

    // Confronto diretto dei byte: evita l'ambiguità di overload di Span<T>.StartsWith
    // con le collection expression (CS9174). Non alloca buffer temporanei.
    private static ReadOnlySpan<byte> WithoutUtf8Bom(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF
            ? bytes[3..] : bytes;

    private static string Normalize(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal);
    private static string Digest(byte[] value) => Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();
    private static string ReadFixture(string root)
    {
        var bytes = File.ReadAllBytes(Path.Combine(root, "data/PICKER_INSERT_SYNTHETIC_FIXTURE.txt"));
        if (bytes.Length == 0) throw new InvalidOperationException("Fixture vuota");
        var span = WithoutUtf8Bom(bytes);
        var value = StrictUtf8.GetString(span);
        if (value.Contains('\0')) throw new InvalidOperationException("Fixture non valida");
        return value;
    }

    private static string SessionBase(string root)
    {
        var cache = Path.Combine(root, ".local-cache");
        var basePath = Path.Combine(cache, "notepad-qualification");
        foreach (var path in new[] { cache, basePath })
            if (IsLink(path)) throw new InvalidOperationException("Directory sintetica reindirizzata con junction/link");
        return basePath;
    }

    private static bool IsLink(string path) =>
        (File.Exists(path) || Directory.Exists(path)) &&
        File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);

    public static string CheckProfile(string path, string expectedFixture)
    {
        if (!Directory.Exists(path)) return "NOT_CREATED";
        if (IsLink(path)) return "INVALID";
        var db = Path.Combine(path, "library.db");
        if (!File.Exists(db) || IsLink(db)) return "INVALID";
        try
        {
            var b = new SqliteConnectionStringBuilder { DataSource = db, Mode = SqliteOpenMode.ReadOnly };
            using var connection = new SqliteConnection(b.ToString());
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT s.content FROM snippets s JOIN groups g ON g.id=s.group_id " +
                "WHERE s.trigger=$trigger AND s.enabled=1 AND g.enabled=1";
            cmd.Parameters.AddWithValue("$trigger", ";ristretto-test");
            var actual = cmd.ExecuteScalar() as string;
            cmd.CommandText = "SELECT COUNT(*) FROM snippets";
            cmd.Parameters.Clear();
            var count = Convert.ToInt64(cmd.ExecuteScalar());
            return count == 1 && actual == expectedFixture ? "READY" : "INVALID";
        }
        catch (Exception ex) when (ex is SqliteException or IOException or InvalidOperationException)
        { return "INVALID"; }
    }
}
