using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TextExpander.Application.Abstractions;
using TextExpander.Application.Features.Backups;
using TextExpander.Application.Features.Library;
using TextExpander.Application.Features.Hotkeys;
using TextExpander.Application.Features.Portability;
using TextExpander.Core;
using TextExpander.Core.Matching;
using TextExpander.Desktop.Localization;

namespace TextExpander.Desktop.ViewModels;

public sealed record LanguageChoice(string Code, string Name);
public sealed record ThemeChoice(string Code, string Name);

public sealed class UiCommand(Func<Task> action) : ICommand
{
    public bool CanExecute(object? parameter) => true;
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public async void Execute(object? parameter)
    {
        // Le azioni MainViewModel intercettano e mostrano i propri errori.
        await action();
    }
}

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ILibraryStore _store;
    private readonly LibraryService _library;
    private readonly BackupService _backups;
    private readonly PortableLibraryService _portability;
    private readonly ExternalLibraryImportService _externalImport;
    private readonly string _dataDirectory;
    private LibrarySnapshot _snapshot = new([], [], 0);
    private sealed record EditorValues(string Title, string Trigger, string Content, string Description, bool Enabled);
    private EditorValues _baseline = new("", "", "", "", true);
    private bool _loadingEditor;
    private bool _isSaving;
    public bool IsReloading { get; private set; }
    public bool HasUnsavedChanges => !_loadingEditor &&
        (CurrentEditor() != _baseline ||
         GroupEditName != (SelectedGroup?.Name ?? ""));
    private EditorValues CurrentEditor() => new(EditorTitle, EditorTrigger, EditorContent,
        EditorDescription, EditorEnabled);
    private void DirtyChanged()
    {
        if (!_loadingEditor) Changed(nameof(HasUnsavedChanges));
    }
    private void ResetBaseline()
    {
        _baseline = CurrentEditor();
        Changed(nameof(HasUnsavedChanges));
    }


    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<string>? ThemeModeChanged;

    public ObservableCollection<SnippetGroup> Groups { get; } = [];
    public ObservableCollection<Snippet> FilteredSnippets { get; } = [];
    public IReadOnlyList<LanguageChoice> Languages { get; } =
    [new("it", "🇮🇹 Italiano"), new("en", "🇬🇧 English"), new("fr", "🇫🇷 Français"),
     new("de", "🇩🇪 Deutsch"), new("es", "🇪🇸 Español")];
    public ObservableCollection<ThemeChoice> Themes { get; } = [];

    private string _languageCode = "en";
    private string _themeMode = "System";
    private UiStrings _texts = UiStrings.ForLanguage("en");
    public UiStrings Texts { get => _texts; private set => Set(ref _texts, value); }

    private LanguageChoice? _selectedLanguage;
    public LanguageChoice? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (!Set(ref _selectedLanguage, value) || value is null || value.Code == _languageCode) return;
            _ = ChangeLanguageAsync(value.Code);
        }
    }

    private ThemeChoice? _selectedTheme;
    public ThemeChoice? SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (!Set(ref _selectedTheme, value) || value is null || value.Code == _themeMode) return;
            _ = ChangeThemeAsync(value.Code);
        }
    }

    public MainViewModel(ILibraryStore store, string dataDirectory)
    {
        _store = store;
        _library = new LibraryService(store);
        _backups = new BackupService(store);
        _portability = new PortableLibraryService(store);
        _externalImport = new ExternalLibraryImportService(store);
        _dataDirectory = dataDirectory;
        SaveCommand = new UiCommand(SaveAsync);
        PreviewCommand = new UiCommand(() => { Preview(); return Task.CompletedTask; });
        BackupCommand = new UiCommand(CreateBackupAsync);
        StatusMessage = Texts.Notice;
        RefreshThemeChoices();
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? property = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        return true;
    }
    private void Changed(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private SnippetGroup? _selectedGroup;
    public SnippetGroup? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (!Set(ref _selectedGroup, value)) return;
            GroupEditName = value?.Name ?? "";
            Changed(nameof(GroupToggleLabel));
            RefreshFiltered();
            if (_selectedSnippet is not null && _selectedSnippet.GroupId != value?.Id)
                SelectedSnippet = null;
        }
    }

    public string EditorHeading => SelectedSnippet is null ? Texts.NewSnippet : Texts.EditSnippet;
    // Titolo documentale: il nome dello snippet ha priorita rispetto alla didascalia generica.
    public string EditorDisplayTitle => string.IsNullOrWhiteSpace(EditorTitle) ? Texts.NewSnippet : EditorTitle;
    public string GroupToggleLabel => SelectedGroup?.IsEnabled == true ? Texts.GroupDisable : Texts.GroupEnable;

    private Snippet? _selectedSnippet;
    public Snippet? SelectedSnippet
    {
        get => _selectedSnippet;
        set
        {
            if (!Set(ref _selectedSnippet, value)) return;
            Changed(nameof(EditorHeading));
            Changed(nameof(EditorDisplayTitle));
            _loadingEditor = true;
            try
            {
                EditorTitle = value?.Title ?? "";
                EditorTrigger = value?.Trigger ?? "";
                EditorContent = value?.Content ?? "";
                EditorDescription = value?.Description ?? "";
                EditorEnabled = value?.IsEnabled ?? true;
            }
            finally { _loadingEditor = false; }
            ResetBaseline();
        }
    }

    private string _groupEditName = "";
    public string GroupEditName
    {
        get => _groupEditName;
        set { if (Set(ref _groupEditName, value)) DirtyChanged(); }
    }
    public void ResetGroupDraft() => GroupEditName = SelectedGroup?.Name ?? "";
    private string _newGroupName = "";
    public string NewGroupName { get => _newGroupName; set => Set(ref _newGroupName, value); }
    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set { if (Set(ref _searchText, value)) RefreshFiltered(); }
    }
    private string _editorTitle = "";
    public string EditorTitle { get => _editorTitle; set { if (Set(ref _editorTitle, value)) { DirtyChanged(); Changed(nameof(EditorDisplayTitle)); } } }
    private string _editorTrigger = "";
    public string EditorTrigger { get => _editorTrigger; set { if (Set(ref _editorTrigger, value)) DirtyChanged(); } }
    private string _editorContent = "";
    public string EditorContent { get => _editorContent; set { if (Set(ref _editorContent, value)) DirtyChanged(); } }
    private string _editorDescription = "";
    public string EditorDescription { get => _editorDescription; set { if (Set(ref _editorDescription, value)) DirtyChanged(); } }
    private bool _editorEnabled = true;
    public bool EditorEnabled { get => _editorEnabled; set { if (Set(ref _editorEnabled, value)) DirtyChanged(); } }
    private string _previewInput = "";
    public string PreviewInput { get => _previewInput; set => Set(ref _previewInput, value); }
    private string _previewResult = "";
    public string PreviewResult { get => _previewResult; set => Set(ref _previewResult, value); }
    private string _pickerHotkeyDraft = PickerHotkey.Default;
    public string PickerHotkeyDraft { get => _pickerHotkeyDraft; set => Set(ref _pickerHotkeyDraft, value); }
    private string _pickerHotkeyActive = "";
    public string PickerHotkeyActive { get => _pickerHotkeyActive; private set => Set(ref _pickerHotkeyActive, value); }
    private string _pickerHotkeyFeedback = "";
    public string PickerHotkeyFeedback { get => _pickerHotkeyFeedback; private set => Set(ref _pickerHotkeyFeedback, value); }

    // Same SQLite settings store already used for language and theme. No schema migration.
    public async Task SavePickerHotkeyAsync(string canonical)
    {
        await _store.WriteSettingAsync("picker.hotkey", canonical);
        PickerHotkeyDraft = canonical;
    }

    public void SetPickerHotkeyState(string active, string feedback)
    {
        PickerHotkeyActive = active;
        PickerHotkeyFeedback = feedback;
    }

    private string _statusMessage = "";
    public string StatusMessage { get => _statusMessage; private set => Set(ref _statusMessage, value); }

    public ICommand SaveCommand { get; }
    public ICommand PreviewCommand { get; }
    public ICommand BackupCommand { get; }

    public async Task InitializeAsync()
    {
        try
        {
            await _library.InitializeAsync();
            var systemLanguage = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            _languageCode = Languages.Any(x => x.Code == systemLanguage) ? systemLanguage : "en";
            _languageCode = await _store.ReadSettingAsync("language") ?? _languageCode;
            if (!Languages.Any(x => x.Code == _languageCode)) _languageCode = "en";
            Texts = UiStrings.ForLanguage(_languageCode);
            Changed(nameof(EditorHeading));
            Changed(nameof(EditorDisplayTitle));
            SelectedLanguage = Languages.First(x => x.Code == _languageCode);
            _themeMode = await _store.ReadSettingAsync("theme") ?? "System";
            if (_themeMode is not ("System" or "Light" or "Dark")) _themeMode = "System";
            RefreshThemeChoices();
            ThemeModeChanged?.Invoke(this, _themeMode);
            PickerHotkeyDraft = await _store.ReadSettingAsync("picker.hotkey") ?? PickerHotkey.Default;
            await ReloadAsync();
            // All'avvio mostrare un elemento esistente invece di lasciare aperto
            // un editor vuoto quando la libreria contiene gia degli snippet.
            // Questo vale solo per l'inizializzazione: "Nuovo snippet" resta
            // una scelta esplicita e ReloadAsync non cambia il draft corrente.
            if (SelectedSnippet is null)
                SelectedSnippet = FilteredSnippets.FirstOrDefault();
            StatusMessage = Texts.StatusReady;
        }
        catch (Exception ex) { Report(ex); }
    }

    public async Task ChangeLanguageAsync(string code)
    {
        try
        {
            if (!Languages.Any(x => x.Code == code)) return;
            _languageCode = code;
            Texts = UiStrings.ForLanguage(code);
            Changed(nameof(EditorHeading));
            Changed(nameof(EditorDisplayTitle));
            Changed(nameof(GroupToggleLabel));
            RefreshThemeChoices();
            await _store.WriteSettingAsync("language", code);
            StatusMessage = Texts.StatusReady;
        }
        catch (Exception ex) { Report(ex); }
    }

    private void RefreshThemeChoices()
    {
        Themes.Clear();
        Themes.Add(new("System", Texts.ThemeSystem));
        Themes.Add(new("Light", Texts.ThemeLight));
        Themes.Add(new("Dark", Texts.ThemeDark));
        SelectedTheme = Themes.First(x => x.Code == _themeMode);
    }

    public async Task ChangeThemeAsync(string code)
    {
        try
        {
            if (code is not ("System" or "Light" or "Dark")) return;
            _themeMode = code;
            ThemeModeChanged?.Invoke(this, code);
            await _store.WriteSettingAsync("theme", code);
        }
        catch (Exception ex) { Report(ex); }
    }

    public async Task ReloadAsync()
    {
        var priorGroup = SelectedGroup?.Id;
        IsReloading = true;
        try
        {
            _snapshot = await _library.ReadAsync();
            Groups.Clear();
            foreach (var group in _snapshot.Groups) Groups.Add(group);
            var resolved = Groups.FirstOrDefault(x => x.Id == priorGroup) ?? Groups.FirstOrDefault();
            if (_selectedGroup?.Id != resolved?.Id)
                SelectedGroup = resolved;
            else
            {
                // Dopo il ricaricamento usare l'istanza aggiornata, anche se il record è uguale.
                _selectedGroup = resolved;
                Changed(nameof(SelectedGroup));
                GroupEditName = resolved?.Name ?? "";
                Changed(nameof(GroupToggleLabel));
            }
            RefreshFiltered();
        }
        finally { IsReloading = false; }
    }

    private void RefreshFiltered()
    {
        FilteredSnippets.Clear();
        if (SelectedGroup is null) return;
        foreach (var item in _snapshot.Snippets.Where(x => x.GroupId == SelectedGroup.Id &&
            (string.IsNullOrWhiteSpace(SearchText) ||
             x.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
             x.Trigger.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
             x.Content.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase))))
            FilteredSnippets.Add(item);
    }

    public void NewSnippet()
    {
        SelectedSnippet = null;
        ResetGroupDraft();
        // Anche se SelectedSnippet era già null, ripristinare la bozza iniziale.
        _loadingEditor = true;
        try
        {
            EditorTitle = EditorTrigger = EditorContent = EditorDescription = "";
            EditorEnabled = true;
        }
        finally { _loadingEditor = false; }
        ResetBaseline();
    }

    public async Task<bool> AddGroupAsync()
    {
        try
        {
            var newGroup = await _library.CreateGroupAsync(NewGroupName);
            NewGroupName = "";
            await ReloadAsync();
            SelectedGroup = Groups.FirstOrDefault(x => x.Id == newGroup.Id);
            StatusMessage = Texts.StatusGroupCreated;
            return true;
        }
        catch (Exception ex) { Report(ex); return false; }
    }

    public async Task SaveAsync()
    {
        if (_isSaving) return;
        _isSaving = true;
        try
        {
            if (SelectedGroup is null) throw new InvalidOperationException(Texts.SelectGroup);
            var groupNameDraft = GroupEditName;
            var draft = new SnippetDraft(SelectedSnippet?.Id, SelectedGroup.Id,
                EditorTitle, EditorTrigger, EditorContent, EditorDescription,
                EditorEnabled, SelectedSnippet?.Revision);
            var saved = await _library.SaveAsync(draft);
            await ReloadAsync();
            SelectedSnippet = FilteredSnippets.FirstOrDefault(x => x.Id == saved.Id);
            if (SelectedSnippet is null) NewSnippet();
            GroupEditName = groupNameDraft; // Il salvataggio dello snippet non scarta la rinomina non salvata.
            StatusMessage = Texts.StatusSaved;
        }
        catch (Exception ex) { Report(ex); }
        finally { _isSaving = false; }
    }

    public async Task DeleteSelectedAsync()
    {
        try
        {
            if (SelectedSnippet is null) return;
            await _library.DeleteAsync(SelectedSnippet);
            NewSnippet();
            await ReloadAsync();
            StatusMessage = Texts.StatusDeleted;
        }
        catch (Exception ex) { Report(ex); }
    }

    public async Task UpdateGroupAsync(bool? enabled = null)
    {
        try
        {
            if (SelectedGroup is null) return;
            var id = SelectedGroup.Id;
            await _library.UpdateGroupAsync(SelectedGroup, GroupEditName, enabled ?? SelectedGroup.IsEnabled);
            await ReloadAsync();
            SelectedGroup = Groups.FirstOrDefault(g => g.Id == id);
            Changed(nameof(GroupToggleLabel));
            StatusMessage = Texts.StatusGroupUpdated;
        }
        catch (Exception ex) { Report(ex); }
    }

    public async Task<bool> DeleteGroupAsync()
    {
        try
        {
            if (SelectedGroup is null) return false;
            await _library.DeleteGroupAsync(SelectedGroup);
            SelectedGroup = null;
            await ReloadAsync();
            StatusMessage = Texts.StatusGroupDeleted;
            return true;
        }
        catch (Exception ex) { Report(ex); return false; }
    }

    public async Task ExportJsonAsync(string destination)
    {
        try
        {
            var saved = await _portability.ExportAsync(destination);
            StatusMessage = $"{Texts.StatusExported}: {saved}";
        }
        catch (Exception ex) { Report(ex); }
    }

    public async Task<ExternalImportPreview?> PrepareExternalImportAsync(string source)
    {
        try { return await _externalImport.PreviewAsync(source); }
        catch (Exception ex) { Report(ex); return null; }
    }

    public Task ImportExternalAsync(ExternalImportPreview preview) =>
        ImportExternalAsync(preview, preview.Candidates.Select(x => x.Id).ToArray());

    public async Task ImportExternalAsync(ExternalImportPreview preview, IReadOnlyCollection<Guid> selectedIds)
    {
        try
        {
            var result = await _externalImport.ImportAsync(preview, selectedIds);
            NewSnippet();
            await ReloadAsync();
            StatusMessage = string.Format(Texts.StatusImported,
                result.GroupsImported, result.SnippetsImported);
        }
        catch (Exception ex) { Report(ex); }
    }

    // API v3.1 mantenuta per i test e per futuri usi del formato nativo.
    public async Task ImportJsonAsync(string source)
    {
        try
        {
            var result = await _portability.ImportAsync(source);
            NewSnippet();
            await ReloadAsync();
            StatusMessage = string.Format(Texts.StatusImported,
                result.GroupsImported, result.SnippetsImported);
        }
        catch (Exception ex) { Report(ex); }
    }

    public void Preview()
    {
        try
        {
            var result = _library.Preview(PreviewInput, _snapshot);
            PreviewResult = result.Status switch
            {
                MatchStatus.Matched => string.Format(Texts.StatusMatched,
                    result.Snippet!.Content.Length > 400 ? result.Snippet.Content[..400] + "…" : result.Snippet.Content),
                MatchStatus.Ambiguous => Texts.StatusAmbiguous,
                _ => Texts.StatusNoMatch
            };
        }
        catch (Exception ex) { Report(ex); }
    }

    public async Task CreateBackupAsync()
    {
        try
        {
            var path = Path.Combine(_dataDirectory, "Backups",
                $"TextExpander_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.db");
            var saved = await _backups.CreateAsync(path);
            StatusMessage = $"{Texts.StatusBackup}: {saved}";
        }
        catch (Exception ex) { Report(ex); }
    }

    public async Task RestoreAsync(string sourcePath)
    {
        try
        {
            await _backups.RestoreAsync(sourcePath);
            NewSnippet();
            await ReloadAsync();
            StatusMessage = Texts.StatusRestored;
        }
        catch (Exception ex) { Report(ex); }
    }

    private void Report(Exception ex) => StatusMessage = string.Format(Texts.StatusError, ex.Message);
}
