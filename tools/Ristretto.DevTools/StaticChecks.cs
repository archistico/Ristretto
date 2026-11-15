using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Data.Sqlite;

namespace Ristretto.DevTools;

public static class StaticChecks
{
    public static int Run(string root)
    {
        var errors = Check(root);
        foreach (var error in errors) Console.Error.WriteLine("  - " + error);
        Console.WriteLine($"CONTROLLI STATICI: {(errors.Count == 0 ? "PASS" : "FAIL")} ({errors.Count} problemi)");
        return errors.Count == 0 ? 0 : 1;
    }

    public static List<string> Check(string root)
    {
        var errors = new List<string>();
        static string Read(string root, string path) => File.ReadAllText(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)));
        var excluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bin", "obj", ".git", ".local-cache", "TestResults", "node_modules" };
        bool Relevant(string file) => !Path.GetRelativePath(root, file).Split(Path.DirectorySeparatorChar).Any(excluded.Contains);
        var xmlFiles = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(Relevant).Where(f => new[] { ".csproj", ".resx", ".axaml", ".props" }.Contains(Path.GetExtension(f))).ToArray();
        foreach (var file in xmlFiles)
            try { _ = XDocument.Load(file); }
            catch (Exception ex) { errors.Add($"XML non valido in {Path.GetRelativePath(root, file)}: {ex.Message}"); }
        var localization = Path.Combine(root, "src/TextExpander.Desktop/Localization");
        var sets = Directory.EnumerateFiles(localization, "Strings*.resx").OrderBy(x => x, StringComparer.Ordinal).ToArray();
        if (sets.Length != 5) errors.Add($"Attese cinque localizzazioni, trovate {sets.Length}");
        var keysByName = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var file in sets)
        {
            var data = XDocument.Load(file).Descendants("data").Select(d => (string?)d.Attribute("name") ?? "").ToArray();
            if (data.Length != data.Distinct(StringComparer.Ordinal).Count()) errors.Add("Chiavi duplicate in " + Path.GetFileName(file));
            keysByName[file] = data.ToHashSet(StringComparer.Ordinal);
        }
        if (sets.Length > 0)
        {
            var baseline = keysByName[sets[0]];
            foreach (var pair in keysByName)
                if (!pair.Value.SetEquals(baseline)) errors.Add("Localizzazione disallineata: " + Path.GetFileName(pair.Key));
            var ui = File.ReadAllText(Path.Combine(localization, "UiStrings.cs"));
            foreach (Match match in Regex.Matches(ui, "Get\\(\"([^\"]+)\"\\)"))
                if (!baseline.Contains(match.Groups[1].Value)) errors.Add("Chiave UI mancante: " + match.Groups[1].Value);
        }
        var sln = Read(root, "Ristretto.sln");
        foreach (var p in new[] { "Core", "Application", "Infrastructure", "Desktop", "Tests" })
            if (!sln.Contains("TextExpander." + p, StringComparison.Ordinal)) errors.Add("Progetto assente in solution: " + p);
        var store = Read(root, "src/TextExpander.Infrastructure/Storage/SqliteLibraryStore.cs");
        var schema = Regex.Match(store, "using \\(var schema = Cmd\\(db, @\"(.*?)\", tx\\)\\)", RegexOptions.Singleline);
        if (!schema.Success) errors.Add("Schema SQLite DDL non trovato nel sorgente");
        else
        {
            try
            {
                using var db = new SqliteConnection("Data Source=:memory:");
                db.Open();
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA foreign_keys=ON;" + schema.Groups[1].Value;
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "INSERT INTO groups(id,name,enabled) VALUES('1','Prova 🌍',1);" +
                        "INSERT INTO snippets(id,group_id,title,trigger,content,description,enabled,revision) " +
                        "VALUES('2','1','Accenti',';café','مرحبا 🧑🏽‍💻','',1,1);";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "SELECT content FROM snippets WHERE id='2'";
                    if ((string?)cmd.ExecuteScalar() != "مرحبا 🧑🏽‍💻") errors.Add("SQLite Unicode non preservato");
                    cmd.CommandText = "INSERT INTO snippets(id,group_id,title,trigger,content,description,enabled,revision) " +
                        "VALUES('3','missing','Invalid',';i','Text','',1,1);";
                    try { cmd.ExecuteNonQuery(); errors.Add("Vincolo di chiave esterna SQLite non applicato"); }
                    catch (SqliteException ex) when (ex.SqliteErrorCode == 19) { /* rigetto atteso */ }
                }
            }
            catch (Exception ex) { errors.Add("Test schema SQLite in memoria: " + ex.Message); }
        }
        foreach (var view in new[] { "MainWindow", "PickerWindow" })
        {
            var xaml = Read(root, $"src/TextExpander.Desktop/{view}.axaml");
            var behind = Read(root, $"src/TextExpander.Desktop/{view}.axaml.cs");
            foreach (Match m in Regex.Matches(xaml, "(?:Click|SelectionChanged)=\"([A-Za-z_]\\w*)\""))
                if (!Regex.IsMatch(behind, @"\b" + Regex.Escape(m.Groups[1].Value) + @"\s*\("))
                    errors.Add($"{view}: handler {m.Groups[1].Value} mancante");
        }
        var pickerXaml = Read(root, "src/TextExpander.Desktop/PickerWindow.axaml");
        var pickerCs = Read(root, "src/TextExpander.Desktop/PickerWindow.axaml.cs");
        var appCs = Read(root, "src/TextExpander.Desktop/App.axaml.cs");
        foreach (var marker in new[] { "Click=\"Insert_Click\"", "IsEnabled=\"{Binding CanInsert}\"", "IsEnabled=\"{Binding CanCopy}\"", "Name=\"SearchBox\"", "Name=\"ResultsList\"" })
            if (!pickerXaml.Contains(marker, StringComparison.Ordinal)) errors.Add("Picker XAML: contratto mancante " + marker);
        foreach (var marker in new[] { "vm.InsertSelectedAsync(app.InsertFromPickerAsync)", "vm.InsertFirstAsync(app.InsertFromPickerAsync)", "AddHandler(KeyUpEvent, OnPickerEnterKeyUp" })
            if (!pickerCs.Contains(marker, StringComparison.Ordinal)) errors.Add("Picker: percorso Invio/Inserisci mancante " + marker);
        foreach (var marker in new[] { "CaptureExternalForeground()", "CancelPickerInsertion()" })
            if (!appCs.Contains(marker, StringComparison.Ordinal)) errors.Add("App: protezione inserimento mancante " + marker);
        if (appCs.Contains("WindowsUnicodeTextOutput", StringComparison.Ordinal))
            errors.Add("Input globale sperimentale collegato prematuramente alla GUI");
        if (!File.Exists(Path.Combine(root, "src/TextExpander.Desktop/Assets/app-icon.ico")))
            errors.Add("Icona tray mancante");
        var main = Read(root, "src/TextExpander.Desktop/MainWindow.axaml");
        foreach (var binding in new[] { "EditorTitle", "EditorTrigger", "EditorContent", "EditorDescription", "SaveCommand" })
            if (!main.Contains("{Binding " + binding, StringComparison.Ordinal)) errors.Add("Binding GUI mancante: " + binding);
        var app = Read(root, "src/TextExpander.Desktop/App.axaml");
        var tokens = Regex.Matches(app, "x:Key=\"(R[A-Za-z]+)\"").Cast<Match>().Select(m => m.Groups[1].Value).ToHashSet();
        foreach (var (name, xaml) in new[] { ("MainWindow", main), ("PickerWindow", pickerXaml) })
        {
            if (Regex.IsMatch(xaml, "#[0-9A-Fa-f]{6,8}")) errors.Add(name + ": colore esadecimale hardcoded");
            foreach (Match m in Regex.Matches(xaml, "DynamicResource (R[A-Za-z]+)"))
                if (!tokens.Contains(m.Groups[1].Value)) errors.Add(name + ": token grafico sconosciuto " + m.Groups[1].Value);
        }
        try
        {
            var markdown = Read(root, "docs/JSON_PORTABILITY.md");
            var example = Regex.Match(markdown, "```json\\s*(.*?)```", RegexOptions.Singleline);
            if (!example.Success) errors.Add("Esempio JSON portabilità mancante");
            else
            {
                using var json = JsonDocument.Parse(example.Groups[1].Value);
                if (json.RootElement.GetProperty("format").GetString() != "text-expander-studio/snippets" ||
                    json.RootElement.GetProperty("version").GetInt32() != 1) errors.Add("Formato esempio JSON inatteso");
            }
        }
        catch (Exception ex) { errors.Add("Esempio JSON non valido: " + ex.Message); }
        var links = 0;
        foreach (var md in Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories).Where(Relevant))
        {
            var source = File.ReadAllText(md);
            foreach (Match m in Regex.Matches(source, @"\]\(([^)]+)\)"))
            {
                var link = m.Groups[1].Value;
                if (link.StartsWith("http:") || link.StartsWith("https:") || link.StartsWith("mailto:") ||
                    link.StartsWith("sandbox:") || link.StartsWith("data:") || link.StartsWith('#')) continue;
                var path = Uri.UnescapeDataString(link.Split('#')[0].Split(' ')[0]);
                if (path.Length == 0) continue;
                links++;
                if (!File.Exists(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(md)!, path))) &&
                    !Directory.Exists(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(md)!, path))))
                    errors.Add($"Collegamento Markdown mancante: {Path.GetRelativePath(root, md)} -> {path}");
            }
        }
        foreach (var folder in new[] { "archive", "beeftext-screenshots" })
            if (Directory.EnumerateDirectories(root, folder, SearchOption.AllDirectories).Any())
                errors.Add("Directory esclusa presente: " + folder);
        Console.WriteLine($"Statici: {xmlFiles.Length} XML, {sets.Length} lingue, {links} link Markdown, DDL SQLite in memoria");
        return errors;
    }
}
