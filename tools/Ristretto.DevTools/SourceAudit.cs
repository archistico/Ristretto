using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Ristretto.DevTools;

public static class SourceAudit
{
    private static readonly Regex Namespace = new(@"(?m)^\s*namespace\s+([\w.]+)\s*[;{]", RegexOptions.CultureInvariant);
    private static readonly Regex Using = new(@"(?m)^\s*(?:global\s+)?using\s+(?!static\s)([\w.]+)\s*;", RegexOptions.CultureInvariant);

    public static int Run(string root)
    {
        var issues = Check(root);
        foreach (var issue in issues) Console.Error.WriteLine("  - " + issue);
        Console.WriteLine($"AUDIT C#: {(issues.Count == 0 ? "PASS" : "FAIL")} ({issues.Count} problemi)");
        return issues.Count == 0 ? 0 : 1;
    }

    public static List<string> Check(string root)
    {
        var issues = new List<string>();
        var versionFile = Path.Combine(root, "VERSION.txt");
        var props = Path.Combine(root, "Directory.Build.props");
        if (!File.Exists(versionFile) || !File.Exists(props)) return ["VERSION.txt o Directory.Build.props mancante"];
        var version = File.ReadAllText(versionFile).Trim().Replace("Ristretto ", "", StringComparison.Ordinal);
        var declared = XDocument.Load(props).Descendants("Version").Select(x => x.Value.Trim()).ToArray();
        if (declared.Length != 1 || declared[0] != version)
            issues.Add("Versione diversa tra VERSION.txt e Directory.Build.props");
        if (!Regex.IsMatch(version, @"^[0-9]+\.[0-9]+\.[0-9]+-alpha(?:\.[0-9]+)+$"))
            issues.Add("Formato versione non riconosciuto: " + version);
        var projects = new[] { "src", "tests", "tools" }
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(root, folder), "*.csproj", SearchOption.AllDirectories))
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var roots = projects.ToDictionary(p => p, p => Path.GetDirectoryName(p)!, StringComparer.OrdinalIgnoreCase);
        var refs = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var namespaces = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var usages = new List<(string Project, string File, string Name)>();
        foreach (var project in projects)
        {
            XDocument xml;
            try { xml = XDocument.Load(project); }
            catch (Exception ex) { issues.Add($"XML non valido {Relative(root, project)}: {ex.Message}"); continue; }
            var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var reference in xml.Descendants("ProjectReference"))
            {
                var name = (string?)reference.Attribute("Include") ?? "";
                var path = Path.GetFullPath(Path.Combine(roots[project], name.Replace('\\', Path.DirectorySeparatorChar)));
                if (!File.Exists(path)) issues.Add($"Riferimento mancante {Relative(root, project)} -> {name}");
                else referenced.Add(path);
            }
            refs[project] = referenced;
            if (xml.Descendants("Version").Any()) issues.Add("Versione duplicata nel progetto " + Relative(root, project));
            foreach (var linked in xml.Descendants("Compile"))
            {
                var include = (string?)linked.Attribute("Include") ?? "";
                if (include.Length == 0 || include.Contains('*')) continue;
                var path = Path.GetFullPath(Path.Combine(roots[project], include.Replace('\\', Path.DirectorySeparatorChar)));
                if (!File.Exists(path)) issues.Add($"Sorgente collegato mancante {Relative(root, project)} -> {include}");
            }
            foreach (var file in Directory.EnumerateFiles(roots[project], "*.cs", SearchOption.AllDirectories)
                         .Where(p => !p.Split(Path.DirectorySeparatorChar).Any(x => x is "bin" or "obj")))
            {
                var source = File.ReadAllText(file);
                var ns = Namespace.Matches(source);
                if (ns.Count != 1) issues.Add("Namespace non univoco: " + Relative(root, file));
                foreach (Match m in ns)
                {
                    var name = m.Groups[1].Value;
                    if (!namespaces.TryGetValue(name, out var owners)) namespaces[name] = owners = new(StringComparer.OrdinalIgnoreCase);
                    owners.Add(project);
                }
                foreach (Match m in Using.Matches(source))
                    if (m.Groups[1].Value.StartsWith("TextExpander.", StringComparison.Ordinal) ||
                        m.Groups[1].Value.StartsWith("Ristretto.", StringComparison.Ordinal))
                        usages.Add((project, file, m.Groups[1].Value));
                // Evita la regressione CS9174: per i controlli BOM usare byte espliciti,
                // non collection expression ambigue nell'overload StartsWith/EndsWith.
                if (Regex.IsMatch(source, @"\.(?:StartsWith|EndsWith)\s*\(\s*\[\s*0x[0-9a-fA-F]+"))
                    issues.Add("CS9174 potenziale: collection expression BOM ambigua in " + Relative(root, file));
                if (Regex.IsMatch(source, @"\.GetTextAsync\s*\("))
                    issues.Add("API clipboard obsoleta: " + Relative(root, file));
                if (project.Contains("Tests", StringComparison.Ordinal) &&
                    Regex.IsMatch(source, @"\[(?:Fact|Theory)(?:\]|\(|\s)") && !source.Contains("using Xunit;"))
                    issues.Add("using Xunit mancante: " + Relative(root, file));
            }
        }
        foreach (var (project, file, name) in usages)
        {
            var visible = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { project };
            var pending = new Stack<string>(); pending.Push(project);
            while (pending.Count > 0)
                foreach (var dep in refs.GetValueOrDefault(pending.Pop()) ?? [])
                    if (visible.Add(dep)) pending.Push(dep);
            var owners = namespaces.Where(x => x.Key == name || x.Key.StartsWith(name + ".", StringComparison.Ordinal))
                .SelectMany(x => x.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (owners.Count == 0) issues.Add($"using interno sconosciuto: {Relative(root, file)} -> {name}");
            else if (!owners.Overlaps(visible)) issues.Add($"using di progetto non referenziato: {Relative(root, file)} -> {name}");
        }
        // Controllo di regressione specifico per l'errore CS0117 già verificato su Windows.
        var coreSession = Path.Combine(root, "src/TextExpander.Core/Matching/CommittedInputSession.cs");
        if (File.Exists(coreSession) && File.ReadAllText(coreSession).Contains("internal static bool IsValidText(") &&
            File.ReadAllText(Path.Combine(root, "src/TextExpander.Application/Features/Output/CommittedEditDeltaVerifier.cs"))
                .Contains("CommittedInputSession.IsValidText("))
            issues.Add("CS0117 potenziale: metodo IsValidText internal richiamato cross-assembly");
        // Il warning xUnit2000 deve restare corretto.
        var warningTest = Path.Combine(root, "tests/TextExpander.Tests/WindowsExternalAdapterCapabilityTests.cs");
        if (File.ReadAllText(warningTest).Contains("Assert.Equal(requirements, WindowsExternalAdapterCapabilityPolicy.EveryWriteProof);"))
            issues.Add("xUnit2000: argomenti expected e actual invertiti");
        issues.AddRange(SourceContracts.Check(root));
        return issues;
    }

    private static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
}
