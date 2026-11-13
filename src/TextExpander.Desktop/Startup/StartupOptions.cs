namespace TextExpander.Desktop.Startup;

public sealed record StartupOptions(string? DataDirectory, bool ShowHelp, bool ShowVersion, bool CheckProfile)
{
    public static StartupOptions Parse(IReadOnlyList<string> args)
    {
        string? directory = null;
        var help = false;
        var version = false;
        var check = false;
        for (var i = 0; i < args.Count; i++)
        {
            var argument = args[i];
            switch (argument)
            {
                case "--help" or "-h": help = true; break;
                case "--version": version = true; break;
                case "--check-profile": check = true; break;
                default:
                    string value;
                    if (argument == "--data-dir")
                    {
                        if (++i >= args.Count || args[i].StartsWith("--", StringComparison.Ordinal))
                            throw new ArgumentException("Specificare una cartella dopo --data-dir.");
                        value = args[i];
                    }
                    else if (argument.StartsWith("--data-dir=", StringComparison.Ordinal))
                        value = argument["--data-dir=".Length..];
                    else
                        throw new ArgumentException("Opzione di avvio non riconosciuta. Usare --help.");
                    if (directory is not null)
                        throw new ArgumentException("Specificare --data-dir una sola volta.");
                    if (string.IsNullOrWhiteSpace(value))
                        throw new ArgumentException("La directory dati non può essere vuota.");
                    directory = value;
                    break;
            }
        }
        if ((help ? 1 : 0) + (version ? 1 : 0) + (check ? 1 : 0) > 1)
            throw new ArgumentException("Usare una sola azione tra --help, --version e --check-profile.");
        return new(directory, help, version, check);
    }
}
