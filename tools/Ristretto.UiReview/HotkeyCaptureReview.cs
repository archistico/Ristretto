using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Text.Json;
using TextExpander.Desktop;
using TextExpander.Desktop.Localization;

namespace Ristretto.UiReview;

/// <summary>Production dialog and styles rendered offscreen with Skia. No desktop lifetime,
/// native windows, registrations, clipboard or database; keystrokes stay in the headless backend.</summary>
internal static class HotkeyCaptureReview
{
    public static int Run(string directory)
    {
        AppBuilder.Configure<App>().UseSkia().WithInterFont()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .SetupWithoutStarting();
        var captures = new List<object>();
        foreach (var theme in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        foreach (var language in new[] { "it", "en", "fr", "de", "es" })
        foreach (var minimum in new[] { false, true })
        {
            Avalonia.Application.Current!.RequestedThemeVariant = theme;
            var window = new HotkeyCaptureWindow(UiStrings.ForLanguage(language));
            if (minimum) window.Width = window.MinWidth;
            try
            {
                window.Show();
                Capture("waiting");
                window.KeyPress(Key.B, RawInputModifiers.Control | RawInputModifiers.Alt | RawInputModifiers.Shift, PhysicalKey.None, null);
                Capture("release");
                window.KeyRelease(Key.B, RawInputModifiers.None, PhysicalKey.None, null);
                Capture("ready");
                window.KeyPress(Key.F12, RawInputModifiers.Control, PhysicalKey.None, null);
                window.KeyRelease(Key.F12, RawInputModifiers.None, PhysicalKey.None, null);
                Capture("invalid");
            }
            finally { window.Close(); }

            void Capture(string state)
            {
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                foreach (var button in window.GetVisualDescendants().OfType<Button>())
                {
                    var at = button.TranslatePoint(new Point(), window);
                    if (at is not { } p || button.Bounds.Width <= 0 || button.Bounds.Height <= 0 ||
                        p.X < -1 || p.Y < -1 || p.X + button.Bounds.Width > window.ClientSize.Width + 1 ||
                        p.Y + button.Bounds.Height > window.ClientSize.Height + 1)
                        throw new InvalidOperationException($"Hotkey action outside client: {language}/{theme}/{state}");
                }
                var filename = $"hotkey-{theme.Key.ToString()!.ToLowerInvariant()}-{language}-{(minimum ? "min" : "normal")}-{state}.png";
                using var frame = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(window.ClientSize.Width),
                    (int)Math.Ceiling(window.ClientSize.Height)), new Vector(96, 96));
                frame.Render(window);
                frame.Save(Path.Combine(directory, filename));
                var display = window.GetVisualDescendants().OfType<TextBlock>()
                    .Single(t => AutomationProperties.GetAutomationId(t) == "HotkeyCaptureDisplay");
                captures.Add(new { File = filename, Width = frame.PixelSize.Width, Height = frame.PixelSize.Height,
                    State = state, DisplayFontSize = display.FontSize, WindowFontSize = window.FontSize, ActionBounds = "PASS" });
            }
        }
        File.WriteAllText(Path.Combine(directory, "review.json"), JsonSerializer.Serialize(new
        {
            Status = "AVALONIA_HEADLESS_SKIA_RENDERED", NativeDesktopManualValidation = "NOT_RUN",
            PhysicalKeyboard = "NOT_RUN", Data = "SYNTHETIC_ONLY", Captures = captures
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Rendered {captures.Count} hotkey PNG previews; no native window or input.");
        return 0;
    }
}
