using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(TextExpander.Tests.HeadlessTestAppBuilder))]

namespace TextExpander.Tests;

public static class HeadlessTestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<HeadlessTestApp>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

// Load the production styles without starting Desktop.App or a classic desktop lifetime.
// Real MainWindow instances therefore never register native hotkeys or open a personal profile.
public sealed class HeadlessTestApp : Avalonia.Application
{
    public override void Initialize()
    {
        var styles = new TextExpander.Desktop.App();
        styles.Initialize();
        var resources = styles.Resources;
        styles.Resources = new Avalonia.Controls.ResourceDictionary();
        Resources = resources;
        var themes = styles.Styles.ToArray();
        styles.Styles.Clear();
        foreach (var style in themes) Styles.Add(style);
    }
}
