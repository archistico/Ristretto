using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Ristretto.DevTools;

public static class ProcessTool
{
    public static int Run(string executable, IEnumerable<string> arguments, string directory,
                          IDictionary<string, string>? environment = null, int timeoutMs = 600_000)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = executable, WorkingDirectory = directory, UseShellExecute = false,
        };
        foreach (var argument in arguments) process.StartInfo.ArgumentList.Add(argument);
        if (environment != null)
            foreach (var (key, value) in environment) process.StartInfo.Environment[key] = value;
        if (!process.Start()) throw new InvalidOperationException($"Impossibile avviare {executable}");
        if (!process.WaitForExit(timeoutMs))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            throw new TimeoutException($"Il processo {Path.GetFileName(executable)} ha superato il limite di tempo");
        }
        return process.ExitCode;
    }

    public static bool Available(string executable, string argument = "--version")
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo(executable)
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            process.StartInfo.ArgumentList.Add(argument);
            if (!process.Start()) return false;
            var outTask = process.StandardOutput.ReadToEndAsync();
            var errTask = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(8000)) { process.Kill(entireProcessTree: true); return false; }
            Task.WhenAll(outTask, errTask).GetAwaiter().GetResult();
            return process.ExitCode == 0;
        }
        catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception or InvalidOperationException)
        { return false; }
    }
}

public static class QualityGate
{
    public static readonly string[] Steps =
    [
        "source_audit", "csharp_regressions", "static_checks", "dotnet_restore", "solution_build", "xunit",
        "windows_lab_build", "benchmark_build", "replacement_lab_build", "ui_review_build", "devtools_build",
        "startup_cli", "node_tests", "export_contract", "native_windows"
    ];
    private static readonly string[] Tools =
    [
        "tools/Ristretto.WindowsOutputLab/Ristretto.WindowsOutputLab.csproj",
        "tools/Ristretto.CoreBenchmark/Ristretto.CoreBenchmark.csproj",
        "tools/Ristretto.ReplacementLab/Ristretto.ReplacementLab.csproj",
        "tools/Ristretto.UiReview/Ristretto.UiReview.csproj"
    ];
    public sealed record StepResult(string name, string status, string detail);

    public static IReadOnlyList<string> Plan(string mode) => mode.ToLowerInvariant() switch
    {
        "source" => Steps.Take(3).ToArray(),
        "core" => Steps.Take(12).ToArray(),
        "ci" => Steps.Take(14).ToArray(),
        "full" => Steps,
        _ => throw new ArgumentException("Gate sconosciuto: " + mode)
    };

    public static int Run(string root, string[] args)
    {
        var mode = (args.Length > 0 && !args[0].StartsWith('-') ? args[0] : Options.Value(args, "--mode") ?? "core").ToLowerInvariant();
        var selected = Plan(mode).ToHashSet(StringComparer.Ordinal);
        var version = File.ReadAllText(Path.Combine(root, "VERSION.txt")).Trim().Replace("Ristretto ", "", StringComparison.Ordinal);
        var reportPath = Options.Value(args, "--report") ?? $".local-cache/gates/{mode}.json";
        if (!Path.IsPathFullyQualified(reportPath)) reportPath = Path.Combine(root, reportPath);
        var restoreSource = Options.Value(args, "--restore-source") ?? "unknown";
        var results = Steps.ToDictionary(x => x, x => new StepResult(x, "NOT_RUN", selected.Contains(x) ? "Bloccato da verifica precedente" : "Escluso dal gate"));
        var git = Environment.GetEnvironmentVariable("GITHUB_SHA");
        if (git is null || !Regex.IsMatch(git, "^[0-9a-fA-F]{40}$")) git = null;
        var passed = true;
        var restoreStatus = "NOT_RUN";
        var dotnet = Environment.GetEnvironmentVariable("RISTRETTO_DOTNET") ?? "dotnet";
        var node = Environment.GetEnvironmentVariable("RISTRETTO_NODE") ?? "node";
        Console.WriteLine($"Ristretto {version} — GATE {mode.ToUpperInvariant()} (strumenti C#, senza Python)");
        bool Execute(string name, Func<int> action)
        {
            Console.WriteLine("RUN " + name);
            try
            {
                var code = action();
                var ok = code == 0;
                results[name] = new(name, ok ? "PASS" : "FAIL", ok ? "Codice di uscita 0" : "Codice di uscita " + code);
                Console.WriteLine($"{(ok ? "PASS" : "FAIL")} {name}: {results[name].detail}");
                if (!ok) passed = false;
                return ok;
            }
            catch (Exception ex)
            {
                results[name] = new(name, "FAIL", ex.GetType().Name + ": " + ex.Message);
                Console.Error.WriteLine("FAIL " + name + ": " + ex.Message);
                passed = false;
                return false;
            }
        }
        int Dotnet(params string[] command) => ProcessTool.Run(dotnet, command, root);
        try
        {
            if (!Execute("source_audit", () => SourceAudit.Run(root))) return 1;
            if (!Execute("csharp_regressions", () => DevTests.Run(root))) return 1;
            if (!Execute("static_checks", () => StaticChecks.Run(root))) return 1;
            if (mode == "source") return 0;
            if (!Execute("dotnet_restore", () => Dotnet("restore", "Ristretto.sln"))) return 1;
            restoreStatus = "PASS";
            if (!Execute("solution_build", () => Dotnet("build", "Ristretto.sln", "-c", "Release", "--no-restore"))) return 1;
            if (!Execute("xunit", () => Dotnet("test", "Ristretto.sln", "-c", "Release", "--no-build", "--logger", "console;verbosity=normal"))) return 1;
            foreach (var (step, project) in new[]
            {
                ("windows_lab_build", Tools[0]), ("benchmark_build", Tools[1]),
                ("replacement_lab_build", Tools[2]), ("ui_review_build", Tools[3])
            }) if (!Execute(step, () => Dotnet("build", project, "-c", "Release"))) return 1;
            if (!Execute("devtools_build", () => Dotnet("build", "tools/Ristretto.DevTools/Ristretto.DevTools.csproj", "-c", "Release"))) return 1;
            if (!Execute("startup_cli", () => StartupCli.Run(root,
                    ["--assembly", "src/TextExpander.Desktop/bin/Release/net10.0/Ristretto.dll",
                     "--output", ".local-cache/gates/startup-cli.json", "--dotnet-executable", dotnet]))) return 1;
            if (mode is "ci" or "full")
            {
                if (!Execute("node_tests", () =>
                    {
                        if (!ProcessTool.Available(node)) throw new InvalidOperationException("Node.js non disponibile (richiesto solo per VS Code)");
                        foreach (var file in new[] { "src/planner.js", "src/controller.js", "src/extension.js", "test/host/index.js" })
                            if (ProcessTool.Run(node, ["--check", file], Path.Combine(root, "tools/ristretto-vscode-extension")) != 0) return 1;
                        var tests = Directory.GetFiles(Path.Combine(root, "tools/ristretto-vscode-extension/test"), "*.test.js")
                            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
                        return ProcessTool.Run(node, ["--test", .. tests], Path.Combine(root, "tools/ristretto-vscode-extension"));
                    })) return 1;
                if (!Execute("export_contract", () =>
                    {
                        var output = Path.Combine(root, ".local-cache/gates/export-from-dotnet.json");
                        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                        if (File.Exists(output)) File.Delete(output);
                        try
                        {
                            var result = ProcessTool.Run(dotnet,
                            ["test", "tests/TextExpander.Tests/TextExpander.Tests.csproj", "-c", "Release", "--no-build",
                             "--filter", "FullyQualifiedName~VscodeExportContractTests"], root,
                            new Dictionary<string, string> { ["RISTRETTO_VSCODE_EXPORT_OUTPUT"] = output });
                            if (result != 0 || !File.Exists(output)) return 1;
                            return ProcessTool.Run(node,
                                ["tools/ristretto-vscode-extension/cli/verify-dotnet-export.js", output], root);
                        }
                        finally { if (File.Exists(output)) File.Delete(output); }
                    })) return 1;
            }
            if (mode == "full")
            {
                if (!Execute("native_windows", () =>
                    {
                        if (!OperatingSystem.IsWindows() || !Environment.UserInteractive)
                            throw new InvalidOperationException("Le prove native richiedono Windows con desktop interattivo");
                        foreach (var arg in new[] { "--replacement-test", "--cooperating-smoke", "--remote-owned-smoke" })
                            if (Dotnet("run", "-c", "Release", "--no-build", "--project", Tools[0], "--", arg) != 0) return 1;
                        return 0;
                    })) return 1;
            }
            return 0;
        }
        finally
        {
            var digest = SourceDigest(root);
            var info = new
            {
                schema = "ristretto/quality-gate/2", source_version = version,
                git_commit = git, source_sha256 = digest, utc = DateTimeOffset.UtcNow.ToString("O"),
                mode, platform = Environment.OSVersion.Platform.ToString(),
                steps = Steps.Select(x => results[x]).ToArray(),
                restore = new { source = restoreSource, status = restoreStatus },
                nuget_vulnerability_audit = new { status = "NOT_RUN", detail = "Serve un audit online separato" },
                vs_code_real_host = "NOT_RUN", desktop_picker_real_io = "NOT_RUN",
                result = passed ? "PASS" : "FAIL"
            };
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            File.WriteAllText(reportPath, JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true }) + "\n");
            Console.WriteLine("Report: " + reportPath);
            Console.WriteLine(string.Join(" | ", Steps.Select(x => $"{x}={results[x].status}")));
            Console.WriteLine($"GATE {mode.ToUpperInvariant()}: {(passed ? "PASS" : "FAIL")} (NOT_RUN non equivale a PASS)");
        }
    }

    public static string SourceDigest(string root)
    {
        var folders = new[] { "src", "tests", "tools", "scripts", ".github" };
        var excluded = new HashSet<string> { ".git", "bin", "obj", "node_modules", ".local-cache", "TestResults", "__pycache__" };
        var extensions = new HashSet<string> { ".cs", ".csproj", ".cmd", ".sh", ".ps1", ".js", ".json", ".yml", ".yaml" };
        var files = folders.SelectMany(x => Directory.EnumerateFiles(Path.Combine(root, x), "*", SearchOption.AllDirectories))
            .Where(x => !Path.GetRelativePath(root, x).Split(Path.DirectorySeparatorChar).Any(excluded.Contains) &&
                        extensions.Contains(Path.GetExtension(x)))
            .Concat(new[] { "VERSION.txt", "Directory.Build.props", "global.json" }.Select(x => Path.Combine(root, x)))
            .Where(File.Exists).OrderBy(x => Path.GetRelativePath(root, x).Replace('\\', '/'), StringComparer.Ordinal);
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var file in files)
        {
            var path = Path.GetRelativePath(root, file).Replace('\\', '/');
            sha.AppendData(Encoding.UTF8.GetBytes(path + "\0"));
            sha.AppendData(SHA256.HashData(File.ReadAllBytes(file)));
        }
        return Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant();
    }
}
