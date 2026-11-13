using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using System.ComponentModel;
using TextExpander.Core;
using TextExpander.Application.Features.Portability;
using TextExpander.Desktop.Localization;
using TextExpander.Desktop.ViewModels;

namespace TextExpander.Desktop;

public partial class MainWindow : Window
{
    private bool _initialized;
    private bool _synchronizingSelection;
    private bool _waitingForDecision;

    public MainWindow()
    {
        InitializeComponent();
        KeyDown += (_, e) =>
        {
            var modifier = OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control;
            if (e.Key == Key.K && (e.KeyModifiers & modifier) != 0)
            {
                (Avalonia.Application.Current as App)?.ShowPicker();
                e.Handled = true;
            }
        };
        Opened += async (_, _) =>
        {
            if (_initialized || DataContext is not MainViewModel vm) return;
            _initialized = true;
            vm.PropertyChanged += OnViewModelChanged;
            await vm.InitializeAsync();
            if (Avalonia.Application.Current is App app)
                await app.InitializePickerHotkeyAsync(vm);
            SynchronizeSelection();
        };
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.SelectedGroup) or nameof(MainViewModel.SelectedSnippet))
            SynchronizeSelection();
    }

    private void SynchronizeSelection()
    {
        if (DataContext is not MainViewModel vm) return;
        _synchronizingSelection = true;
        try
        {
            GroupsList.SelectedItem = vm.SelectedGroup;
            SnippetsList.SelectedItem = vm.SelectedSnippet;
        }
        finally { _synchronizingSelection = false; }
    }

    internal Task<bool> ConfirmPendingEditsAsync()
    {
        if (DataContext is not MainViewModel vm || !vm.HasUnsavedChanges)
            return Task.FromResult(true);
        return ConfirmAsync(vm.Texts.DiscardChangesQuestion);
    }

    private async void Group_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || vm.IsReloading ||
            _synchronizingSelection || _waitingForDecision) return;
        if (GroupsList.SelectedItem is not SnippetGroup next)
        {
            SynchronizeSelection();
            return;
        }
        if (next.Id == vm.SelectedGroup?.Id) return;
        _waitingForDecision = true;
        try
        {
            SynchronizeSelection();
            if (!await ConfirmPendingEditsAsync()) return;
            vm.SelectedGroup = next;
            vm.NewSnippet();
            SynchronizeSelection();
        }
        finally { _waitingForDecision = false; }
    }

    private async void Snippet_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || vm.IsReloading ||
            _synchronizingSelection || _waitingForDecision) return;
        if (SnippetsList.SelectedItem is not Snippet next || next.Id == vm.SelectedSnippet?.Id) return;
        _waitingForDecision = true;
        try
        {
            SynchronizeSelection();
            if (!await ConfirmPendingEditsAsync()) return;
            vm.ResetGroupDraft();
            vm.SelectedSnippet = next;
            SynchronizeSelection();
        }
        finally { _waitingForDecision = false; }
    }

    private async void NewSnippet_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && await ConfirmPendingEditsAsync())
        {
            vm.NewSnippet();
            SynchronizeSelection();
        }
    }

    private async void AddGroup_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && await ConfirmPendingEditsAsync())
        {
            if (await vm.AddGroupAsync())
            {
                vm.NewSnippet();
                SynchronizeSelection();
            }
        }
    }

    private async void RenameGroup_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.SelectedGroup is not null)
        {
            await vm.UpdateGroupAsync();
            SynchronizeSelection();
        }
    }

    private async void ToggleGroup_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.SelectedGroup is not null)
        {
            await vm.UpdateGroupAsync(!vm.SelectedGroup.IsEnabled);
            SynchronizeSelection();
        }
    }

    private async void DeleteGroup_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || vm.SelectedGroup is null) return;
        if (await ConfirmPendingEditsAsync() && await ConfirmAsync(vm.Texts.ConfirmDeleteGroup))
        {
            if (await vm.DeleteGroupAsync())
            {
                vm.NewSnippet();
                SynchronizeSelection();
            }
        }
    }

    private async void Import_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || !await ConfirmPendingEditsAsync()) return;
        var selection = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = vm.Texts.ChooseImport,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Beeftext / Ristretto JSON") { Patterns = ["*.json"] },
                new FilePickerFileType("Espanso YAML") { Patterns = ["*.yaml", "*.yml"] }
            ]
        });
        if (selection.Count == 0) return;
        var preview = await vm.PrepareExternalImportAsync(selection[0].Path.LocalPath);
        if (preview is not null)
        {
            var review = new ImportReviewWindow(vm.Texts, preview);
            var selectedIds = await review.ShowDialog<Guid[]?>(this);
            if (selectedIds is { Length: > 0 })
                await vm.ImportExternalAsync(preview, selectedIds);
        }
    }

    private async void Export_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (vm.HasUnsavedChanges && !await ConfirmAsync(vm.Texts.ExportUnsavedQuestion)) return;
        var destination = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = vm.Texts.ChooseExport,
            SuggestedFileName = $"TextExpander_Snippets_{DateTime.Now:yyyyMMdd_HHmmss}.json",
            FileTypeChoices = [new FilePickerFileType("JSON") { Patterns = ["*.json"] }]
        });
        if (destination is not null) await vm.ExportJsonAsync(destination.Path.LocalPath);
    }

    private async Task<bool> ConfirmAsync(string text)
    {
        if (DataContext is not MainViewModel vm) return false;

        // Shared semantic theme with the import review window. Cancel has initial focus:
        // pressing Enter inadvertently must not confirm deletion or restore.
        var dialog = new Window
        {
            Title = vm.Texts.Confirm,
            Width = 500,
            MinWidth = 430,
            MinHeight = 235,
            MaxHeight = 400,
            CanResize = false,
            SizeToContent = Avalonia.Controls.SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        dialog.Classes.Add("ristretto-dialog");

        var headline = new TextBlock { Text = vm.Texts.Confirm };
        headline.Classes.Add("dialog-title");
        var header = new Border
        {
            Padding = new Thickness(24, 18),
            Child = headline
        };
        header.Classes.Add("dialog-hero");

        var question = new TextBlock
        {
            Text = text,
            FontSize = 14,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };
        var body = new Border
        {
            Padding = new Thickness(24, 23),
            Child = question
        };

        var yes = new Button { Content = vm.Texts.Confirm, MinWidth = 110 };
        yes.Classes.Add("primary");
        var no = new Button { Content = vm.Texts.Cancel, MinWidth = 110 };
        no.Classes.Add("ghost");
        yes.Click += (_, _) => dialog.Close(true);
        no.Click += (_, _) => dialog.Close(false);
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Children = { no, yes }
        };
        var footer = new Border
        {
            Padding = new Thickness(22, 13),
            Child = actions
        };
        footer.Classes.Add("dialog-footer");

        var layout = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,Auto") };
        Grid.SetRow(header, 0);
        Grid.SetRow(body, 1);
        Grid.SetRow(footer, 2);
        layout.Children.Add(header);
        layout.Children.Add(body);
        layout.Children.Add(footer);
        dialog.Content = layout;
        dialog.Opened += (_, _) => no.Focus();
        dialog.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                dialog.Close(false);
                e.Handled = true;
            }
        };
        return await dialog.ShowDialog<bool>(this);
    }

    private async void ApplyPickerHotkey_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && Avalonia.Application.Current is App app)
            await app.SavePickerHotkeyAsync(vm);
    }

    private void Picker_Click(object? sender, RoutedEventArgs e) =>
        (Avalonia.Application.Current as App)?.ShowPicker();

    private async void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || vm.SelectedSnippet is null) return;
        if (await ConfirmAsync(vm.Texts.ConfirmDelete)) await vm.DeleteSelectedAsync();
    }

    private async void Restore_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        var selection = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = vm.Texts.ChooseBackup,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("SQLite") { Patterns = ["*.db", "*.sqlite", "*.sqlite3"] }]
        });
        if (selection.Count == 0) return;
        var source = selection[0].Path.LocalPath;
        if (await ConfirmPendingEditsAsync() && await ConfirmAsync(vm.Texts.ConfirmRestore))
        {
            await vm.RestoreAsync(source);
            SynchronizeSelection();
        }
    }
}
