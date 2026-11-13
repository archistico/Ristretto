using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Text.Json;
using TextExpander.Application.Abstractions;
using TextExpander.Application.Features.Portability;
using TextExpander.Core;
using TextExpander.Desktop;
using TextExpander.Desktop.Localization;
using TextExpander.Desktop.ViewModels;

namespace Ristretto.UiReview;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length is < 2 or > 3 || args[0] != "--output" ||
            (args.Length == 3 && args[2] != "--baseline"))
        {
            Console.Error.WriteLine("Uso: Ristretto.UiReview --output <nuova-cartella> [--baseline]");
            return 2;
        }
        var directory = Path.GetFullPath(args[1]);
        if (Directory.Exists(directory))
        {
            Console.Error.WriteLine("La cartella di output deve essere nuova.");
            return 2;
        }
        Directory.CreateDirectory(directory);
        AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont()
            .SetupWithoutStarting(); // No desktop lifetime: App never opens DB, tray or single-instance host.
        var captures = new List<object>();
        var languages = args.Length == 3 ? new[] { "it" } : new[] { "it", "en", "fr", "de", "es" };
        foreach (var theme in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            Avalonia.Application.Current!.RequestedThemeVariant = theme;
            foreach (var language in languages)
            {
                var store = new SyntheticStore(language);
                var vm = new MainViewModel(store, directory);
                vm.InitializeAsync().GetAwaiter().GetResult();
                var main = new MainWindow { DataContext = vm };
                main.Show();
                Capture(main, $"main-{theme.Key.ToString()!.ToLowerInvariant()}-{language}");
                if (args.Length == 2 && language is "it" or "de")
                {
                    main.Width = main.MinWidth;
                    main.Height = main.MinHeight;
                    Capture(main, $"main-min-{theme.Key.ToString()!.ToLowerInvariant()}-{language}");
                }
                main.Close();

                var pickerVm = new PickerViewModel(store, UiStrings.ForLanguage(language));
                var pickerLoad = pickerVm.ReloadAsync();
                var loadDeadline = System.Diagnostics.Stopwatch.StartNew();
                while (!pickerLoad.IsCompleted)
                {
                    // Index construction now runs on a worker. Pump its UI
                    // continuation instead of blocking the dispatcher awaiting it.
                    Dispatcher.UIThread.RunJobs();
                    if (loadDeadline.Elapsed > TimeSpan.FromSeconds(30))
                        throw new TimeoutException("Synthetic picker load did not complete.");
                    Thread.Sleep(1);
                }
                if (!pickerLoad.GetAwaiter().GetResult())
                    throw new InvalidOperationException("Synthetic picker load failed.");
                var picker = new PickerWindow(pickerVm);
                picker.Show();
                Capture(picker, $"picker-{theme.Key.ToString()!.ToLowerInvariant()}-{language}");
                if (args.Length == 2 && language is "it" or "de")
                {
                    picker.Width = picker.MinWidth;
                    picker.Height = picker.MinHeight;
                    Capture(picker, $"picker-min-{theme.Key.ToString()!.ToLowerInvariant()}-{language}");
                }
                if (args.Length == 2 && language == "it")
                {
                    picker.Width = 600;
                    picker.Height = 390;
                    pickerVm.Query = "no-synthetic-result";
                    Capture(picker, $"picker-empty-{theme.Key.ToString()!.ToLowerInvariant()}");
                }
                picker.Hide(); // Picker's production close handler intentionally hides it.
            }
            if (args.Length == 2)
            {
                var samples = new StackPanel { Spacing = 16, Margin = new Thickness(24) };
                samples.Children.Add(new TextBlock { Text = "Ristretto / control states", FontSize = 20, FontWeight = FontWeight.SemiBold });
                foreach (var kind in new[] { "primary", "subtle", "ghost", "danger" })
                {
                    var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
                    foreach (var enabled in new[] { true, false })
                    {
                        var button = new Button { Content = enabled ? kind : kind + " disabled", IsEnabled = enabled, MinWidth = 160 };
                        button.Classes.Add(kind);
                        row.Children.Add(button);
                    }
                    samples.Children.Add(row);
                }
                samples.Children.Add(new TextBox { Text = "Caffè ☕ · 漢字 · مرحبا", Watermark = "Input", MinWidth = 360 });
                samples.Children.Add(new TextBox { Watermark = "Placeholder / inactive", IsEnabled = false });
                samples.Children.Add(new CheckBox { Content = "Enabled / Abilitato", IsChecked = true });
                var states = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
                var hover = new Button { Content = "primary / hover", MinWidth = 160 };
                hover.Classes.Add("primary");
                ((IPseudoClasses)hover.Classes).Set(":pointerover", true);
                var pressed = new Button { Content = "primary / pressed", MinWidth = 160 };
                pressed.Classes.Add("primary");
                ((IPseudoClasses)pressed.Classes).Set(":pressed", true);
                states.Children.Add(hover);
                states.Children.Add(pressed);
                samples.Children.Add(states);
                var focus = new Button { Content = "Keyboard focus / Tab", MinWidth = 160 };
                focus.Classes.Add("ghost");
                samples.Children.Add(focus);
                var board = new Window { Width = 440, Height = 550, Content = samples };
                board.Bind(Window.BackgroundProperty, Avalonia.Application.Current!.GetResourceObservable("RSurface"));
                board.Show();
                focus.Focus(NavigationMethod.Tab);
                // Render the focus style deterministically; this is not a native keyboard test.
                ((IPseudoClasses)focus.Classes).Set(":focus", true);
                ((IPseudoClasses)focus.Classes).Set(":focus-visible", true);
                Capture(board, $"controls-{theme.Key.ToString()!.ToLowerInvariant()}");
                board.Close();
            }
        }
        File.WriteAllText(Path.Combine(directory, "review.json"), JsonSerializer.Serialize(new
        {
            Status = "AVALONIA_CLIENT_RENDERED", Data = "SYNTHETIC_MEMORY_ONLY", NativeDesktopManualValidation = "NOT_RUN",
            Avalonia = typeof(Window).Assembly.GetName().Version?.ToString(),
            Captures = captures
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Rendered {captures.Count} PNG previews with Avalonia/Skia. Synthetic data only.");
        return 0;

        void Capture(Window window, string name)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            var actionCount = 0;
            // Application actions only: internal scrollbar RepeatButtons may legitimately
            // have zero-sized page-up/down regions at the ends of a scroll range.
            foreach (var button in window.GetVisualDescendants().OfType<Button>()
                .Where(b => b.GetType() == typeof(Button) && b.IsEffectivelyVisible))
            {
                var location = button.TranslatePoint(default, window);
                if (location is not { } point || button.Bounds.Width <= 0 || button.Bounds.Height <= 0 ||
                    point.X < -1 || point.Y < -1 || point.X + button.Bounds.Width > window.Bounds.Width + 1 ||
                    point.Y + button.Bounds.Height > window.Bounds.Height + 1)
                    throw new InvalidOperationException($"Action outside window: {name}; {button.GetType().Name}/{button.Name}; local={button.Bounds}; translated={location}; window={window.Bounds}");
                actionCount++;
            }
            using var frame = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height), new Vector(96, 96));
            frame.Render(window);
            frame.Save(Path.Combine(directory, name + ".png"));
            captures.Add(new { File = name + ".png", Width = frame.PixelSize.Width, Height = frame.PixelSize.Height,
                VisibleActionsChecked = actionCount, ActionBounds = "PASS" });
        }
    }

    private sealed class SyntheticStore(string language) : ILibraryStore
    {
        private readonly Dictionary<string, string> _settings = new() { ["language"] = language, ["theme"] = "Light" };
        private static readonly SnippetGroup Work = new(Guid.Parse("319d63c5-b686-4559-b1f0-9f95f1f60001"), "Lavoro", true);
        private static readonly SnippetGroup Personal = new(Guid.Parse("319d63c5-b686-4559-b1f0-9f95f1f60002"), "Personali", true);
        private static readonly SnippetGroup Sql = new(Guid.Parse("319d63c5-b686-4559-b1f0-9f95f1f60003"), "SQL", false);
        private static readonly LibrarySnapshot Snapshot = new([Work, Personal, Sql],
        [
            Item(1, "Firma professionale", ";firma", "Un saluto,\nGiulia Rossi\nProduct Designer\ngiulia@example.test", "Firma per le comunicazioni di lavoro."),
            Item(2, "Conferma appuntamento", ";conferma", "Buongiorno,\nconfermo la mia disponibilità per il nostro incontro.\nA presto!", "Risposta per appuntamenti e riunioni."),
            Item(3, "Invio documenti", ";documenti", "In allegato trovi i documenti richiesti.\nResto a disposizione per qualsiasi chiarimento.", ""),
            Item(4, "Ringraziamento", ";grazie", "Grazie per il tuo tempo e per il confronto di oggi.", ""),
            Item(5, "Risposta internazionale", ";hello", "Caffè ☕ · café / cafe\u0301\nこんにちは · 漢字\nمرحبا 👩🏽‍💻", "Testo di esempio multilingue."),
            new(Guid.NewGuid(), Personal.Id, "Indirizzo", ";casa", "Via delle Rose 12\n00100 Roma", "Synthetic", true, 1),
            new(Guid.NewGuid(), Sql.Id, "Query di esempio", ";select", "SELECT id, title\nFROM snippets;", "Synthetic", true, 1)
        ], 1);
        private static Snippet Item(int index, string title, string trigger, string content, string description) =>
            new(new Guid(index, 0, 0, new byte[8]), Work.Id, title, trigger, content, description, true, 1);
        public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<LibrarySnapshot> ReadAsync(CancellationToken ct = default) => Task.FromResult(Snapshot);
        public Task<string?> ReadSettingAsync(string key, CancellationToken ct = default) => Task.FromResult(_settings.GetValueOrDefault(key));
        public Task WriteSettingAsync(string key, string value, CancellationToken ct = default) { _settings[key] = value; return Task.CompletedTask; }
        public Task<SnippetGroup> CreateGroupAsync(string name, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<SnippetGroup> UpdateGroupAsync(SnippetGroup previous, string name, bool enabled, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteGroupAsync(SnippetGroup previous, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ImportSummary> ImportPortableAsync(PortableLibraryDocument document, CancellationToken ct = default, long? expectedLibraryRevision = null) => throw new NotSupportedException();
        public Task<Snippet> SaveSnippetAsync(SnippetDraft draft, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteSnippetAsync(Guid id, long revision, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<string> CreateBackupAsync(string path, CancellationToken ct = default) => throw new NotSupportedException();
        public Task RestoreBackupAsync(string path, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
