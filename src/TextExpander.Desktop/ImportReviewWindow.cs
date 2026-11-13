using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using TextExpander.Application.Features.Portability;
using TextExpander.Desktop.Localization;

namespace TextExpander.Desktop;

/// <summary>
/// Anteprima non esecutiva: la selezione rimane in memoria sino alla conferma.
/// Paginazione a 60 righe e filtri non alterano gli identificativi degli elementi scelti.
/// </summary>
public sealed class ImportReviewWindow : Window
{
    private const int PageSize = 60;
    private readonly UiStrings _texts;
    private readonly ExternalImportPreview _preview;
    private readonly HashSet<Guid> _selected;
    private readonly StackPanel _rows = new() { Spacing = 0 };
    private readonly TextBox _filter = new() { MinWidth = 190 };
    private readonly TextBlock _selectionCount = new();
    private readonly TextBlock _pageInfo = new();
    private readonly Button _previous = new() { MinWidth = 94 };
    private readonly Button _next = new() { MinWidth = 94 };
    private readonly Button _proceed = new() { MinWidth = 154 };
    private int _page;

    public ImportReviewWindow(UiStrings texts, ExternalImportPreview preview)
    {
        _texts = texts;
        _preview = preview;
        _selected = preview.Candidates.Where(x => x.SuggestedForImport)
            .Select(x => x.Id).ToHashSet();

        Title = texts.ImportPreviewTitle;
        Width = 940;
        Height = 745;
        MinWidth = 680;
        MinHeight = 540;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Classes.Add("ristretto-dialog");

        var title = new TextBlock { Text = texts.ImportPreviewTitle };
        title.Classes.Add("dialog-title");
        var summary = new TextBlock
        {
            Text = string.Format(texts.ImportPreviewCounts, preview.Source.ToString(),
                preview.Document.Groups.Count, preview.Document.Snippets.Count, preview.SkippedItems),
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap
        };
        summary.Classes.Add("dialog-hero-muted");
        var titleStack = new StackPanel { Spacing = 7, Children = { title, summary } };
        var hero = new Border
        {
            Padding = new Thickness(26, 22),
            Child = titleStack
        };
        hero.Classes.Add("dialog-hero");

        var policy = new TextBlock
        {
            Text = preview.Source == ImportSource.StudioJson
                ? texts.ImportNativePolicy : texts.ImportExternalDisabled,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12
        };
        policy.Classes.Add("muted");
        var warningText = new TextBlock
        {
            Text = string.Format(texts.ImportWarnings, preview.Issues.Count) + " · " +
                texts.ImportReviewRiskPolicy,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12
        };
        warningText.Classes.Add("dialog-risk-text");
        var warning = new Border
        {
            Padding = new Thickness(13, 9),
            Child = warningText,
            IsVisible = preview.Issues.Count > 0
        };
        warning.Classes.Add("dialog-warning");

        var notices = new StackPanel { Spacing = 8, Children = { policy, warning } };
        var omitted = preview.Issues.Where(issue => issue.Kind is
            ImportIssueKind.SkippedItem or ImportIssueKind.IgnoredMetadata)
            .Take(4).Select(issue => $"{issue.Kind}: {issue.Reference}").ToArray();
        if (omitted.Length > 0)
        {
            var note = new TextBlock
            {
                Text = string.Join(" · ", omitted),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11,
                MaxLines = 2,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            note.Classes.Add("muted");
            notices.Children.Add(note);
        }

        _filter.Watermark = texts.ImportReviewFilter;
        _filter.TextChanged += (_, _) => { _page = 0; RefreshRows(); };
        var safe = MakeButton(texts.ImportReviewSafe, () =>
        {
            _selected.Clear();
            foreach (var candidate in preview.Candidates.Where(x => x.SuggestedForImport))
                _selected.Add(candidate.Id);
            RefreshRows();
        });
        var selectVisible = MakeButton(texts.ImportReviewSelectVisible, () =>
        {
            foreach (var item in VisiblePage()) _selected.Add(item.Id);
            RefreshRows();
        });
        var clearVisible = MakeButton(texts.ImportReviewClearVisible, () =>
        {
            foreach (var item in VisiblePage()) _selected.Remove(item.Id);
            RefreshRows();
        });
        var actionWrap = new WrapPanel { Orientation = Orientation.Horizontal, ItemSpacing = 8, LineSpacing = 6 };
        actionWrap.Children.Add(safe);
        actionWrap.Children.Add(selectVisible);
        actionWrap.Children.Add(clearVisible);
        var controlLayout = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto"),
            RowSpacing = 10
        };
        Grid.SetRow(_filter, 0);
        Grid.SetRow(actionWrap, 1);
        controlLayout.Children.Add(_filter);
        controlLayout.Children.Add(actionWrap);
        var filterArea = new Border
        {
            Padding = new Thickness(24, 14),
            Child = controlLayout
        };
        filterArea.Classes.Add("dialog-toolbar");

        var list = new ScrollViewer
        {
            Content = _rows,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
        var listFrame = new Border
        {
            Margin = new Thickness(22, 12, 22, 12),
            ClipToBounds = true,
            Child = list
        };
        listFrame.Classes.Add("dialog-card");

        _previous.Content = texts.ImportReviewPrevious;
        _next.Content = texts.ImportReviewNext;
        _previous.Click += (_, _) => { if (_page > 0) _page--; RefreshRows(); };
        _next.Click += (_, _) => { _page++; RefreshRows(); };
        _previous.Classes.Add("subtle");
        _next.Classes.Add("subtle");
        _previous.Classes.Add("dialog-small");
        _next.Classes.Add("dialog-small");
        _pageInfo.Classes.Add("muted");
        _selectionCount.Classes.Add("muted");

        var pages = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 10,
            Children = { _previous, _pageInfo, _next }
        };
        var cancel = new Button { Content = texts.Cancel, MinWidth = 110 };
        cancel.Classes.Add("ghost");
        cancel.Click += (_, _) => Close((Guid[]?)null);
        _proceed.Content = texts.ImportConfirm;
        _proceed.Classes.Add("primary");
        _proceed.Click += (_, _) => Close(_selected.ToArray());
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 10,
            Children = { cancel, _proceed }
        };
        var selectionBar = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        Grid.SetColumn(_selectionCount, 0);
        Grid.SetColumn(buttons, 1);
        _selectionCount.VerticalAlignment = VerticalAlignment.Center;
        selectionBar.Children.Add(_selectionCount);
        selectionBar.Children.Add(buttons);

        var footerGrid = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto"), RowSpacing = 11 };
        Grid.SetRow(pages, 0);
        Grid.SetRow(selectionBar, 1);
        footerGrid.Children.Add(pages);
        footerGrid.Children.Add(selectionBar);
        var footer = new Border { Padding = new Thickness(24, 15), Child = footerGrid };
        footer.Classes.Add("dialog-footer");

        var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto") };
        Grid.SetRow(hero, 0);
        Grid.SetRow(notices, 1);
        notices.Margin = new Thickness(24, 14, 24, 13);
        Grid.SetRow(filterArea, 2);
        Grid.SetRow(listFrame, 3);
        Grid.SetRow(footer, 4);
        grid.Children.Add(hero);
        grid.Children.Add(notices);
        grid.Children.Add(filterArea);
        grid.Children.Add(listFrame);
        grid.Children.Add(footer);
        Content = grid;

        Opened += (_, _) => _filter.Focus();
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Close((Guid[]?)null);
                e.Handled = true;
            }
        };
        RefreshRows();
    }

    private static Button MakeButton(string label, Action action)
    {
        var button = new Button { Content = label };
        button.Classes.Add("subtle");
        button.Classes.Add("dialog-small");
        button.Click += (_, _) => action();
        return button;
    }

    private List<ImportCandidate> Filtered() => _preview.Candidates.Where(item =>
        string.IsNullOrWhiteSpace(_filter.Text) ||
        item.Title.Contains(_filter.Text!, StringComparison.OrdinalIgnoreCase) ||
        item.Trigger.Contains(_filter.Text!, StringComparison.OrdinalIgnoreCase) ||
        item.GroupName.Contains(_filter.Text!, StringComparison.OrdinalIgnoreCase)).ToList();

    private IReadOnlyList<ImportCandidate> VisiblePage() =>
        Filtered().Skip(_page * PageSize).Take(PageSize).ToArray();

    private void RefreshRows()
    {
        var filtered = Filtered();
        var pageCount = Math.Max(1, (filtered.Count + PageSize - 1) / PageSize);
        _page = Math.Clamp(_page, 0, pageCount - 1);
        _rows.Children.Clear();
        if (filtered.Count == 0)
        {
            var empty = new TextBlock { Text = _texts.ImportReviewNoResults, FontSize = 14,
                Margin = new Thickness(16, 18) };
            empty.Classes.Add("muted");
            _rows.Children.Add(empty);
        }
        foreach (var item in filtered.Skip(_page * PageSize).Take(PageSize))
        {
            var checkbox = new CheckBox { IsChecked = _selected.Contains(item.Id) };
            checkbox.IsCheckedChanged += (_, _) =>
            {
                if (checkbox.IsChecked == true) _selected.Add(item.Id);
                else _selected.Remove(item.Id);
                RefreshSummary();
            };
            var title = new TextBlock { Text = item.Title, FontSize = 14, FontWeight = FontWeight.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis };
            var trigger = new TextBlock { Text = item.Trigger, FontSize = 12,
                TextTrimming = TextTrimming.CharacterEllipsis };
            trigger.Classes.Add("dialog-trigger-text");
            var meta = item.GroupName;
            if (item.Risk.HasFlag(ImportItemRisk.TriggerCollision))
                meta += " · " + _texts.ImportIssueCollision;
            if (item.Risk.HasFlag(ImportItemRisk.DynamicContent))
                meta += " · " + _texts.ImportIssueDynamic;
            if (item.Risk.HasFlag(ImportItemRisk.UnsupportedOptions))
                meta += " · " + _texts.ImportIssueUnsupported;
            var details = new TextBlock { Text = meta, FontSize = 12,
                TextWrapping = TextWrapping.Wrap, MaxLines = 2,
                TextTrimming = TextTrimming.CharacterEllipsis };
            details.Classes.Add(item.Risk == ImportItemRisk.None ? "muted" : "dialog-risk-text");
            var info = new StackPanel { Spacing = 4, Children = { title, trigger, details } };
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 13 };
            Grid.SetColumn(checkbox, 0);
            Grid.SetColumn(info, 1);
            checkbox.VerticalAlignment = VerticalAlignment.Top;
            info.VerticalAlignment = VerticalAlignment.Center;
            grid.Children.Add(checkbox);
            grid.Children.Add(info);
            var row = new Border { Child = grid };
            row.Classes.Add("review-row");
            if (item.Risk != ImportItemRisk.None) row.Classes.Add("review-row-risk");
            _rows.Children.Add(row);
        }
        _pageInfo.Text = string.Format(_texts.ImportReviewPage, _page + 1, pageCount);
        _previous.IsEnabled = _page > 0;
        _next.IsEnabled = _page < pageCount - 1;
        RefreshSummary();
    }

    private void RefreshSummary()
    {
        _selectionCount.Text = string.Format(_texts.ImportReviewSelected,
            _selected.Count, _preview.Candidates.Count);
        _proceed.IsEnabled = _selected.Count > 0;
    }
}
