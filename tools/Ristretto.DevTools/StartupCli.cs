using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Ristretto.DevTools;

public static class StartupCli
{
    public sealed record Outcome(int ExitCode, byte[] StandardOutput, byte[] StandardError);
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static int Run(string root, string[] arguments)
    {
        var assembly = Options.Value(arguments, "--assembly") ?? "src/TextExpander.Desktop/bin/Release/net10.0/Ristretto.dll";
        var output = Options.Value(arguments, "--output") ?? ".local-cache/gates/startup-cli.json";
        var dotnet = Options.Value(arguments, "--dotnet-executable") ?? "dotnet";
        if (!Path.IsPathFullyQualified(assembly)) assembly = Path.Combine(root, assembly);
        if (!File.Exists(assembly)) throw new FileNotFoundException("Assembly Ristretto non trovato", assembly);
        var version = File.ReadAllText(Path.Combine(root, "VERSION.txt")).Trim();
        var passed = 0;
        var results = new List<object>();
        var temp = Path.Combine(Path.GetTempPath(), "RistrettoStartup_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            string Test(string name, int expectedExit, params string[] args)
            {
                var outcome = Invoke(dotnet, assembly, args, temp);
                if (outcome.ExitCode != expectedExit)
                    throw new InvalidOperationException($"{name}: uscita {outcome.ExitCode}, attesa {expectedExit}. {DecodeDiagnostic(outcome.StandardError)}");
                string stdout;
                if (expectedExit == 0)
                {
                    try { stdout = StrictUtf8.GetString(outcome.StandardOutput); }
                    catch (DecoderFallbackException) { throw new InvalidOperationException($"{name}: output di successo non UTF-8"); }
                    if (outcome.StandardError.Length != 0)
                        throw new InvalidOperationException($"{name}: output di errore inatteso");
                }
                else
                {
                    stdout = "";
                    if (outcome.StandardOutput.Length != 0 || outcome.StandardError.Length == 0)
                        throw new InvalidOperationException($"{name}: canali stdout/stderr inattesi");
                    // L'errore può usare la codifica della console Windows: non analizzare contenuto utente.
                    _ = DecodeDiagnostic(outcome.StandardError);
                }
                passed++;
                results.Add(new { @case = name, status = "PASS", exit_code = expectedExit });
                return stdout;
            }
            var help = Test("help", 0, "--help");
            if (!help.Contains("--data-dir")) throw new InvalidOperationException("--help non espone --data-dir");
            if (Test("version", 0, "--version").Trim() != version)
                throw new InvalidOperationException("Versione CLI diversa da VERSION.txt");
            if (Directory.EnumerateFileSystemEntries(temp).Any())
                throw new InvalidOperationException("I comandi informativi hanno creato file");
            const string relative = "prove con spazi/日本語 🌍";
            var expected = Path.GetFullPath(Path.Combine(temp, relative));
            foreach (var (name, args) in new[]
            {
                ("relative-unicode-directory", new[] { "--check-profile", "--data-dir", relative }),
                ("absolute-equals-directory", new[] { "--data-dir=" + expected, "--check-profile" })
            })
            {
                using var json = JsonDocument.Parse(Test(name, 0, args));
                var record = json.RootElement;
                if (record.GetProperty("status").GetString() != "profile_writable" ||
                    record.GetProperty("version").GetString() != version.Replace("Ristretto ", "") ||
                    Path.GetFullPath(record.GetProperty("data_directory").GetString()!) != expected ||
                    Path.GetFullPath(record.GetProperty("database_path").GetString()!) != Path.Combine(expected, "library.db") ||
                    Path.GetFullPath(record.GetProperty("backup_directory").GetString()!) != Path.Combine(expected, "Backups") ||
                    record.GetProperty("database_opened").GetBoolean() || record.GetProperty("desktop_started").GetBoolean())
                    throw new InvalidOperationException(name + ": esito JSON del preflight non coerente");
                if (Directory.EnumerateFileSystemEntries(expected).Any())
                    throw new InvalidOperationException(name + ": creati file durante il preflight");
            }
            foreach (var (name, args) in new[]
            {
                ("missing-directory-value", new[] { "--data-dir", "--check-profile" }),
                ("empty-directory", new[] { "--check-profile", "--data-dir=" }),
                ("duplicate-directory", new[] { "--check-profile", "--data-dir=a", "--data-dir=b" }),
                ("unknown-option", new[] { "--check-profile", "--unknown" }),
                ("conflicting-actions", new[] { "--check-profile", "--version" })
            }) Test(name, 2, args);
            var occupied = Path.Combine(temp, "occupied");
            File.WriteAllBytes(occupied, "synthetic sentinel"u8.ToArray());
            Test("file-as-directory", 2, "--check-profile", "--data-dir", occupied);
            if (!File.ReadAllBytes(occupied).SequenceEqual("synthetic sentinel"u8.ToArray()))
                throw new InvalidOperationException("File sentinella modificato");
            if (Directory.EnumerateFiles(temp, "*.db", SearchOption.AllDirectories).Any())
                throw new InvalidOperationException("Il preflight ha creato database");
            var report = new
            {
                schema = "ristretto/startup-cli-check/1", source_version = version.Replace("Ristretto ", ""),
                status = "PASS", passed, cases = results, gui_started = false, database_opened = false,
                scope = "Punto d'ingresso reale, cartelle temporanee e test diagnostici sintetici"
            };
            var target = Path.IsPathFullyQualified(output) ? output : Path.Combine(root, output);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllText(target, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Startup CLI: {passed}/{passed} PASS; nessuna GUI o database aperto");
            return 0;
        }
        finally
        {
            try { Directory.Delete(temp, true); }
            catch (IOException) { /* Al prossimo avvio la directory temporanea resta isolata. */ }
        }
    }

    public static string DecodeDiagnostic(byte[] data)
    {
        try { return StrictUtf8.GetString(data); }
        catch (DecoderFallbackException) { return Encoding.Latin1.GetString(data); }
    }

    public static Outcome Invoke(string dotnet, string assembly, IEnumerable<string> args, string workingDirectory)
    {
        using var process = new Process();
        process.StartInfo.FileName = dotnet;
        process.StartInfo.WorkingDirectory = workingDirectory;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.ArgumentList.Add(assembly);
        foreach (var arg in args) process.StartInfo.ArgumentList.Add(arg);
        if (!process.Start()) throw new InvalidOperationException("Impossibile avviare il processo CLI");
        using var stdout = new MemoryStream();
        using var stderr = new MemoryStream();
        var readOut = process.StandardOutput.BaseStream.CopyToAsync(stdout);
        var readErr = process.StandardError.BaseStream.CopyToAsync(stderr);
        if (!process.WaitForExit(20_000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("CLI senza risposta oltre 20 secondi");
        }
        Task.WhenAll(readOut, readErr).GetAwaiter().GetResult();
        return new Outcome(process.ExitCode, stdout.ToArray(), stderr.ToArray());
    }
}
