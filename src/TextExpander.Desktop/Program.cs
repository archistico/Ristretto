using Avalonia;
using TextExpander.Desktop.SingleInstance;
using TextExpander.Desktop.Startup;

namespace TextExpander.Desktop;

internal static class Program
{
    internal static SingleInstanceHost? Instance { get; private set; }
    internal static ApplicationProfile? Profile { get; private set; }

    [STAThread]
    public static int Main(string[] args)
    {
        return StartupCommand.Run(args, Console.Out, Console.Error, (profile, instance) =>
        {
            Profile = profile;
            Instance = instance;
            try { BuildAvaloniaApp().StartWithClassicDesktopLifetime([]); }
            finally { Instance = null; Profile = null; }
        });
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
}
