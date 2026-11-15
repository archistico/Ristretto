using System.Text;

namespace Ristretto.DevTools;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        string root;
        // Il percorso non dipende dalla working directory dell'utente.
        // I progetti referenziati dal tool possono cambiare la posizione di output:
        // risalire fino alla directory contenente VERSION.txt e Ristretto.sln.
        DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null &&
               !(File.Exists(Path.Combine(current.FullName, "VERSION.txt")) &&
                 File.Exists(Path.Combine(current.FullName, "Ristretto.sln"))))
            current = current.Parent;
        if (current is null)
        {
            Console.Error.WriteLine("ERRORE: impossibile identificare la cartella radice di Ristretto.");
            return 2;
        }
        root = current.FullName;
        try
        {
            if (args.Length == 0)
            {
                Usage();
                return 2;
            }
            return args[0].ToLowerInvariant() switch
            {
                "audit" => SourceAudit.Run(root),
                "static" => StaticChecks.Run(root),
                "selftest" => DevTests.Run(root),
                "gate" => QualityGate.Run(root, args.Skip(1).ToArray()),
                "startup" => StartupCli.Run(root, args.Skip(1).ToArray()),
                "notepad" => NotepadQualification.Run(root, args.Skip(1).ToArray()),
                "package" => PackageTool.Run(root, args.Skip(1).ToArray()),
                _ => InvalidCommand(args[0])
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERRORE STRUMENTI RISTRETTO ({ex.GetType().Name}): {ex.Message}");
            return 2;
        }
    }

    private static int InvalidCommand(string command)
    {
        Console.Error.WriteLine($"Comando sconosciuto: {command}");
        Usage();
        return 2;
    }

    private static void Usage() => Console.WriteLine(@"Ristretto - strumenti di sviluppo C#/.NET 10
Uso: dotnet run --project tools/Ristretto.DevTools -- <comando> [opzioni]
  audit                  Controlli namespace, versioni e riferimenti
  static                 XAML, SQLite, risorse, GUI e documentazione
  selftest               Regressioni degli strumenti C#
  gate source|core|ci|full  Esegue i controlli e registra PASS/FAIL/NOT_RUN
  startup --assembly <DLL> [--output <JSON>]   Prove CLI senza GUI
  notepad prepare [--open] | status|verify|launch --session <TOKEN>
  package --output <PERCORSO_ZIP>  Crea pacchetto integrale e SHA-256
Nessuno di questi comandi richiede Python.");

}

internal static class Options
{
    public static string? Value(string[] args, string key)
    {
        var index = Array.IndexOf(args, key);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
    public static bool Has(string[] args, string flag) => args.Contains(flag, StringComparer.OrdinalIgnoreCase);
}
