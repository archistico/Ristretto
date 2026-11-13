using System.Reflection;
using System.Text.Json;
using TextExpander.Desktop.SingleInstance;

namespace TextExpander.Desktop.Startup;

/// <summary>Startup and diagnostic commands, testable without initializing Avalonia.</summary>
public static class StartupCommand
{
    public static string Version => typeof(StartupCommand).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];

    public static int Run(string[] args, TextWriter output, TextWriter error,
        Action<ApplicationProfile, SingleInstanceHost> startDesktop,
        Func<string>? localDataDirectory = null, string? workingDirectory = null)
    {
        StartupOptions options;
        try { options = StartupOptions.Parse(args); }
        catch (ArgumentException ex) { error.WriteLine(ex.Message); return 2; }

        if (options.ShowHelp)
        {
            output.WriteLine("Ristretto [--data-dir <cartella>] [--check-profile | --help | --version]");
            output.WriteLine("--data-dir       Usa una libreria e impostazioni separate nella cartella indicata.");
            output.WriteLine("--check-profile  Verifica la scrittura della cartella senza aprire database, GUI o hotkey.");
            output.WriteLine("Senza --data-dir viene mantenuto il profilo locale TextExpanderStudio esistente.");
            return 0;
        }
        if (options.ShowVersion) { output.WriteLine("Ristretto " + Version); return 0; }

        ApplicationProfile profile;
        SingleInstanceHost? instance = null;
        try
        {
            profile = ApplicationProfile.Resolve(options.DataDirectory, localDataDirectory, workingDirectory);
            profile.EnsureWritable();
            if (!options.CheckProfile) instance = new SingleInstanceHost(profile.DataDirectory);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException
                                   or NotSupportedException or System.Security.SecurityException)
        {
            error.WriteLine("Profilo dati non disponibile: " + ex.Message);
            error.WriteLine("Scegliere una cartella valida e scrivibile con --data-dir. Nessun profilo alternativo è stato aperto.");
            return 2;
        }

        if (options.CheckProfile)
        {
            output.WriteLine(JsonSerializer.Serialize(new
            {
                status = "profile_writable", version = Version,
                data_directory = profile.DataDirectory, database_path = profile.DatabasePath,
                backup_directory = profile.BackupDirectory,
                database_opened = false, desktop_started = false
            }));
            return 0;
        }
        using (instance!)
        {
            if (!instance!.IsPrimary)
            {
                if (instance.RequestActivationAsync().GetAwaiter().GetResult()) return 0;
                error.WriteLine("Ristretto è già in esecuzione per questo profilo, ma non risponde.");
                return 3;
            }
            startDesktop(profile, instance);
            return 0;
        }
    }
}
