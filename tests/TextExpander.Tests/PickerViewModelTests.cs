using TextExpander.Application.Abstractions;
using TextExpander.Application.Features.Portability;
using TextExpander.Core;
using TextExpander.Desktop.Localization;
using TextExpander.Desktop.ViewModels;
using Xunit;

namespace TextExpander.Tests;

// Exercises the production ViewModel and localized resources. No Avalonia App,
// window, dispatcher backend, native input, clipboard or personal database starts.
public sealed class PickerViewModelTests
{
    private static TaskCompletionSource<LibrarySnapshot> Pending() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static readonly Guid Group = Guid.Parse("00000000-0000-4000-8000-000000000001");
    private static LibrarySnapshot Snapshot(string title, long revision = 1) => new([new(Group, "G", true)],
        [new(Guid.NewGuid(), Group, title, ";p", title, "", true, 1)], revision);
    private static PickerViewModel ViewModel(ReadStore store) => new(store, UiStrings.ForLanguage("it"));

    private static LibrarySnapshot EnterLibrary()
    {
        var alpha = new Snippet(Guid.NewGuid(), Group, "Match Alpha", ";alpha", "Cafe\u0301 👩🏽‍💻\n漢字", "", true, 1);
        var beta = new Snippet(Guid.NewGuid(), Group, "Match Beta", ";beta", "Second result", "", true, 1);
        var other = new Snippet(Guid.NewGuid(), Group, "Other", ";other", "Outside filter", "", true, 1);
        return new([new(Group, "G", true)], [other, beta, alpha], 1);
    }

    [Fact]
    public async Task EnterInsertsFirstFilteredResultEvenWhenSecondIsSelected()
    {
        var vm = ViewModel(new ReadStore(Task.FromResult(EnterLibrary())));
        vm.SetInsertAvailable(true);
        await vm.ReloadAsync();
        vm.Query = "Match";
        Assert.Equal(2, vm.Results.Count);
        var first = vm.Results[0];
        vm.Selected = vm.Results[1];
        string? inserted = null;
        await vm.InsertFirstAsync(text => { inserted = text; return Task.CompletedTask; });
        Assert.Equal(first.Content, inserted);
        Assert.Equal(first.SnippetId, vm.Selected?.SnippetId);
        Assert.False(vm.CanInsert);
    }

    [Fact]
    public async Task EnterUsesCurrentFilterInsteadOfFirstUnfilteredSnippet()
    {
        var vm = ViewModel(new ReadStore(Task.FromResult(EnterLibrary())));
        vm.SetInsertAvailable(true);
        await vm.ReloadAsync();
        vm.Query = ";beta";
        string? inserted = null;
        await vm.InsertFirstAsync(text => { inserted = text; return Task.CompletedTask; });
        Assert.Equal("Second result", inserted);
    }

    [Fact]
    public async Task EnterAcceptsFirstResultWhenListHasNoSelection()
    {
        var vm = ViewModel(new ReadStore(Task.FromResult(EnterLibrary())));
        vm.SetInsertAvailable(true);
        await vm.ReloadAsync();
        vm.Selected = null;
        string? inserted = null;
        await vm.InsertFirstAsync(text => { inserted = text; return Task.CompletedTask; });
        Assert.Equal(vm.Results[0].Content, inserted);
    }

    [Fact]
    public async Task EnterWithNoMatchDoesNotConsumeInsertAvailability()
    {
        var vm = ViewModel(new ReadStore(Task.FromResult(EnterLibrary())));
        vm.SetInsertAvailable(true);
        await vm.ReloadAsync();
        vm.Query = "No such result";
        var calls = 0;
        await vm.InsertFirstAsync(_ => { calls++; return Task.CompletedTask; });
        Assert.Equal(0, calls);
        vm.Query = "Match";
        Assert.True(vm.CanInsert);
    }

    [Fact]
    public async Task EnterWithoutExternalTargetDoesNotCopyOrInsert()
    {
        var vm = ViewModel(new ReadStore(Task.FromResult(EnterLibrary())));
        await vm.ReloadAsync();
        vm.Selected = vm.Results[1];
        var selected = vm.Selected;
        var calls = 0;
        await vm.InsertFirstAsync(_ => { calls++; return Task.CompletedTask; });
        Assert.Equal(0, calls);
        Assert.Same(selected, vm.Selected);
        Assert.True(vm.CanCopy);
    }

    [Fact]
    public async Task EnterDuringLoadingIsNotQueued()
    {
        var pending = Pending();
        var vm = ViewModel(new ReadStore(pending.Task));
        vm.SetInsertAvailable(true);
        var loading = vm.ReloadAsync();
        var calls = 0;
        await vm.InsertFirstAsync(_ => { calls++; return Task.CompletedTask; });
        pending.SetResult(EnterLibrary());
        Assert.True(await loading);
        Assert.Equal(0, calls);
        Assert.True(vm.CanInsert);
    }

    [Fact]
    public async Task EnterDuringCopyDoesNotChangeSelectionOrDispatchInput()
    {
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var vm = ViewModel(new ReadStore(Task.FromResult(EnterLibrary())));
        vm.SetInsertAvailable(true);
        await vm.ReloadAsync();
        vm.Selected = vm.Results[1];
        var selected = vm.Selected;
        var copy = vm.CopySelectedAsync(_ => pending.Task);
        var calls = 0;
        await vm.InsertFirstAsync(_ => { calls++; return Task.CompletedTask; });
        Assert.Same(selected, vm.Selected);
        Assert.Equal(0, calls);
        pending.SetResult();
        await copy;
        Assert.True(vm.CanInsert);
    }

    [Fact]
    public async Task RepeatedEnterDispatchesOnlyOnceAndButtonStillUsesSelection()
    {
        var library = EnterLibrary();
        var vm = ViewModel(new ReadStore(Task.FromResult(library)));
        vm.SetInsertAvailable(true);
        await vm.ReloadAsync();
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        Task Insert(string _) { calls++; return pending.Task; }
        var first = vm.InsertFirstAsync(Insert);
        await vm.InsertFirstAsync(Insert);
        pending.SetResult();
        await first;
        await vm.InsertFirstAsync(Insert);
        Assert.Equal(1, calls);

        var buttonVm = ViewModel(new ReadStore(Task.FromResult(library)));
        buttonVm.SetInsertAvailable(true);
        await buttonVm.ReloadAsync();
        buttonVm.Selected = buttonVm.Results[1];
        var chosen = buttonVm.Selected.Content;
        string? inserted = null;
        await buttonVm.InsertSelectedAsync(text => { inserted = text; return Task.CompletedTask; });
        Assert.Equal(chosen, inserted);
    }

    [Fact]
    public async Task CopyUsesOriginalUnicodeContentAndPreservesInsertAvailability()
    {
        var text = "Cafe\u0301 👩🏽‍💻\n漢字";
        var vm = ViewModel(new ReadStore(Task.FromResult(Snapshot(text))));
        vm.SetInsertAvailable(true);
        await vm.ReloadAsync();
        string? copied = null;
        await vm.CopySelectedAsync(value => { copied = value; return Task.CompletedTask; });
        Assert.Equal(text, copied);
        Assert.Equal(vm.Texts.PickerCopied, vm.Status);
        Assert.True(vm.CanCopy);
        Assert.True(vm.CanInsert);
        Assert.False(vm.IsClipboardBusy);
    }

    [Fact]
    public async Task NoClipboardCommandStartsWithoutALoadedSelection()
    {
        var pending = Pending();
        var vm = ViewModel(new ReadStore(pending.Task));
        vm.SetInsertAvailable(true);
        var calls = 0;
        Task Callback(string _) { calls++; return Task.CompletedTask; }
        await vm.CopySelectedAsync(Callback);
        await vm.InsertSelectedAsync(Callback);
        var loading = vm.ReloadAsync();
        await vm.CopySelectedAsync(Callback);
        await vm.InsertSelectedAsync(Callback);
        Assert.Equal(0, calls);
        vm.CancelReload();
        Assert.False(await loading);
        pending.SetResult(Snapshot("Unused"));
    }

    [Fact]
    public async Task PendingCopyBlocksBothCommandsWithoutQueueingThem()
    {
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var vm = ViewModel(new ReadStore(Task.FromResult(Snapshot("Old"))));
        vm.SetInsertAvailable(true);
        await vm.ReloadAsync();
        var calls = 0;
        Task Clipboard(string _) { calls++; return pending.Task; }
        var copy = vm.CopySelectedAsync(Clipboard);
        Assert.True(vm.IsClipboardBusy);
        Assert.False(vm.CanCopy);
        Assert.False(vm.CanInsert);
        await vm.CopySelectedAsync(Clipboard);
        await vm.InsertSelectedAsync(Clipboard);
        Assert.Equal(1, calls);
        pending.SetResult();
        await copy;
        Assert.Equal(1, calls);
        Assert.True(vm.CanCopy);
        Assert.True(vm.CanInsert);
    }

    [Fact]
    public async Task PendingInsertBlocksCopyAndRemainsConsumedAfterCompletion()
    {
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var vm = ViewModel(new ReadStore(Task.FromResult(Snapshot("Chosen"))));
        vm.SetInsertAvailable(true);
        await vm.ReloadAsync();
        var inserted = new List<string>();
        async Task Insert(string value)
        {
            inserted.Add(value);
            await pending.Task;
            vm.Status = vm.Texts.PickerInsertSubmitted; // simulated native callback, no input
        }
        var operation = vm.InsertSelectedAsync(Insert);
        Assert.True(vm.IsClipboardBusy);
        Assert.False(vm.CanCopy);
        var copies = 0;
        await vm.CopySelectedAsync(_ => { copies++; return Task.CompletedTask; });
        await vm.InsertSelectedAsync(Insert);
        pending.SetResult();
        await operation;
        Assert.Equal(new[] { "Chosen" }, inserted);
        Assert.Equal(0, copies);
        Assert.Equal(vm.Texts.PickerInsertSubmitted, vm.Status);
        Assert.True(vm.CanCopy);
        Assert.False(vm.CanInsert);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("it")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("es")]
    public async Task ClipboardFailureUsesLocalizedMessageWithoutExceptionContents(string language)
    {
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var vm = new PickerViewModel(new ReadStore(Task.FromResult(Snapshot("Chosen"))), UiStrings.ForLanguage(language));
        await vm.ReloadAsync();
        var copy = vm.CopySelectedAsync(_ => pending.Task);
        Assert.Equal(vm.Texts.PickerOperationPending, vm.Status);
        pending.SetException(new IOException("SYNTHETIC_PRIVATE_ERROR_DATA"));
        await copy;
        Assert.Equal(vm.Texts.PickerCopyFailed, vm.Status);
        Assert.DoesNotContain("SYNTHETIC_PRIVATE_ERROR_DATA", vm.Status);
        Assert.DoesNotContain("PickerCopyFailed", vm.Status);
        Assert.False(vm.IsClipboardBusy);
        Assert.True(vm.CanCopy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CopyCompletionAfterDismissalCannotChangeSessionStatus(bool fail)
    {
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var vm = ViewModel(new ReadStore(Task.FromResult(Snapshot("Old"))));
        await vm.ReloadAsync();
        var copy = vm.CopySelectedAsync(_ => pending.Task);
        vm.CancelReload();
        vm.Status = "Current session status";
        if (fail) pending.SetException(new IOException("Old error")); else pending.SetResult();
        await copy;
        Assert.Equal("Current session status", vm.Status);
        Assert.False(vm.IsClipboardBusy);
        Assert.False(vm.CanCopy);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task OldCopyCannotOverwriteANewerLoadOutcome(bool loadFails, bool copyFails)
    {
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var newer = loadFails ? Task.FromException<LibrarySnapshot>(new IOException("New load error"))
            : Task.FromResult(Snapshot("New", 2));
        var vm = ViewModel(new ReadStore(Task.FromResult(Snapshot("Old")), newer));
        vm.SetInsertAvailable(true);
        await vm.ReloadAsync();
        var copy = vm.CopySelectedAsync(_ => pending.Task);
        Assert.Equal(!loadFails, await vm.ReloadAsync());
        Assert.False(vm.CanCopy);
        Assert.False(vm.CanInsert);
        vm.Status = "New outcome";
        if (copyFails) pending.SetException(new IOException("Old copy error")); else pending.SetResult();
        await copy;
        Assert.Equal("New outcome", vm.Status);
        Assert.Equal(!loadFails, vm.CanCopy);
        Assert.Equal(!loadFails, vm.CanInsert);
    }

    [Fact]
    public async Task CopyKeepsClickedContentButDoesNotReportSuccessForAnotherSelection()
    {
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var alpha = Snapshot("Alpha").Snippets[0];
        var beta = Snapshot("Beta").Snippets[0];
        var library = new LibrarySnapshot([new(Group, "G", true)], [alpha, beta], 1);
        var vm = ViewModel(new ReadStore(Task.FromResult(library)));
        await vm.ReloadAsync();
        string? content = null;
        var copy = vm.CopySelectedAsync(value => { content = value; return pending.Task; });
        vm.Selected = vm.Results.Single(r => r.SnippetId == beta.Id);
        pending.SetResult();
        await copy;
        Assert.Equal("Alpha", content);
        Assert.Equal("Beta", vm.Selected?.Content);
        Assert.Equal(vm.Texts.PickerManualNotice, vm.Status);
    }

    [Fact]
    public async Task ReopenedPickerCannotWriteAgainUntilEarlierClipboardWriteFinishes()
    {
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var vm = ViewModel(new ReadStore(Task.FromResult(Snapshot("Old")), Task.FromResult(Snapshot("New", 2))));
        await vm.ReloadAsync();
        var writes = new List<string>();
        Task Writer(string text) { writes.Add(text); return text == "Old" ? pending.Task : Task.CompletedTask; }
        var old = vm.CopySelectedAsync(Writer);
        vm.CancelReload();
        await vm.ReloadAsync();
        await vm.CopySelectedAsync(Writer);
        Assert.Equal(new[] { "Old" }, writes);
        Assert.Equal(vm.Texts.PickerOperationPending, vm.Status);
        pending.SetResult();
        await old;
        Assert.Equal(vm.Texts.PickerManualNotice, vm.Status);
        await vm.CopySelectedAsync(Writer);
        Assert.Equal(new[] { "Old", "New" }, writes);
    }

    [Fact]
    public async Task UnexpectedInsertFailureIsUncertainAndDoesNotRetry()
    {
        var vm = ViewModel(new ReadStore(Task.FromResult(Snapshot("Chosen"))));
        vm.SetInsertAvailable(true);
        await vm.ReloadAsync();
        var calls = 0;
        Task Insert(string _) { calls++; throw new IOException("SYNTHETIC_PRIVATE_ERROR_DATA"); }
        await vm.InsertSelectedAsync(Insert);
        await vm.InsertSelectedAsync(Insert);
        Assert.Equal(1, calls);
        Assert.Equal(vm.Texts.PickerInsertUncertain, vm.Status);
        Assert.True(vm.CanCopy);
        Assert.False(vm.CanInsert);
    }

    [Fact]
    public async Task OldInsertCompletionReleasesGateWithoutConsumingNewSessionPermission()
    {
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var vm = ViewModel(new ReadStore(Task.FromResult(Snapshot("Old")), Task.FromResult(Snapshot("New", 2))));
        vm.SetInsertAvailable(true);
        await vm.ReloadAsync();
        var oldInsert = vm.InsertSelectedAsync(_ => pending.Task);
        vm.CancelReload();
        vm.SetInsertAvailable(false);
        vm.SetInsertAvailable(true); // a newly captured target in the reopened picker
        await vm.ReloadAsync();
        var calls = 0;
        string? inserted = null;
        Task Insert(string value) { calls++; inserted = value; return Task.CompletedTask; }
        await vm.InsertSelectedAsync(Insert);
        Assert.Equal(0, calls);
        pending.SetResult();
        await oldInsert;
        Assert.True(vm.CanInsert);
        await vm.InsertSelectedAsync(Insert);
        Assert.Equal(1, calls);
        Assert.Equal("New", inserted);
        Assert.False(vm.CanInsert);
    }

    [Fact]
    public async Task DismissalBeforeDispatchDoesNotStartClipboardWrite()
    {
        var vm = ViewModel(new ReadStore(Task.FromResult(Snapshot("Chosen"))));
        await vm.ReloadAsync();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(vm.IsClipboardBusy) && vm.IsClipboardBusy)
            {
                vm.CancelReload();
                vm.Status = "Closed";
            }
        };
        var calls = 0;
        await vm.CopySelectedAsync(_ => { calls++; return Task.CompletedTask; });
        Assert.Equal(0, calls);
        Assert.Equal("Closed", vm.Status);
        Assert.False(vm.IsClipboardBusy);
    }

    [Fact]
    public async Task ChangingLanguageUpdatesPendingAndCompletionMessages()
    {
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var vm = ViewModel(new ReadStore(Task.FromResult(Snapshot("Chosen"))));
        await vm.ReloadAsync();
        var copy = vm.CopySelectedAsync(_ => pending.Task);
        vm.UpdateLanguage(UiStrings.ForLanguage("de"));
        Assert.Equal(vm.Texts.PickerOperationPending, vm.Status);
        pending.SetResult();
        await copy;
        Assert.Equal(vm.Texts.PickerCopied, vm.Status);
    }

    [Fact]
    public async Task ClipboardBusyStateNotifiesBothActionBindings()
    {
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var vm = ViewModel(new ReadStore(Task.FromResult(Snapshot("Chosen"))));
        await vm.ReloadAsync();
        var changes = new List<string?>();
        vm.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
        var copy = vm.CopySelectedAsync(_ => pending.Task);
        foreach (var property in new[] { nameof(vm.IsClipboardBusy), nameof(vm.CanCopy), nameof(vm.CanInsert) })
            Assert.Contains(property, changes);
        changes.Clear();
        pending.SetResult();
        await copy;
        foreach (var property in new[] { nameof(vm.IsClipboardBusy), nameof(vm.CanCopy), nameof(vm.CanInsert) })
            Assert.Contains(property, changes);
    }

    [Fact]
    public async Task OlderReadCannotReplaceNewerResults()
    {
        var oldRead = Pending();
        var newRead = Pending();
        var store = new ReadStore(oldRead.Task, newRead.Task);
        var vm = ViewModel(store);
        var oldLoad = vm.ReloadAsync();
        var newLoad = vm.ReloadAsync();
        newRead.SetResult(Snapshot("New", 2));
        await newLoad;
        oldRead.SetResult(Snapshot("Old", 1));
        await oldLoad;
        Assert.Equal("New", Assert.Single(vm.Results).Content);
    }

    [Fact]
    public async Task OlderFailureCannotOverwriteNewerStatus()
    {
        var oldRead = Pending();
        var newRead = Pending();
        var vm = ViewModel(new ReadStore(oldRead.Task, newRead.Task));
        var oldLoad = vm.ReloadAsync();
        var newLoad = vm.ReloadAsync();
        newRead.SetResult(Snapshot("New", 2));
        await newLoad;
        oldRead.SetException(new IOException("Old synthetic failure"));
        await oldLoad;
        try { await oldRead.Task; } catch (IOException) { } // observe an uncooperative provider's late failure
        Assert.Equal(vm.Texts.PickerManualNotice, vm.Status);
        Assert.Equal("New", Assert.Single(vm.Results).Content);
    }

    [Fact]
    public async Task PendingReloadDoesNotOfferOldSelectionForCopyOrInsert()
    {
        var pending = Pending();
        var vm = ViewModel(new ReadStore(Task.FromResult(Snapshot("Old")), pending.Task));
        vm.SetInsertAvailable(true);
        await vm.ReloadAsync();
        Assert.True(vm.CanInsert);
        var reload = vm.ReloadAsync();
        try
        {
            Assert.Empty(vm.Results);
            Assert.Null(vm.Selected);
            Assert.False(vm.HasSelection);
            Assert.False(vm.CanInsert);
        }
        finally { pending.SetResult(Snapshot("New", 2)); await reload; }
    }

    [Fact]
    public async Task ClosingPickerCancelsReadAndIgnoresLateCompletion()
    {
        var pending = Pending();
        var store = new ReadStore(pending.Task);
        var vm = ViewModel(store);
        var loading = vm.ReloadAsync();
        Assert.True(vm.IsLoading);
        vm.CancelReload();
        Assert.False(await loading.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.True(Assert.Single(store.Tokens).IsCancellationRequested);
        pending.SetResult(Snapshot("Late"));
        vm.Query = "Late";
        Assert.False(vm.IsLoading);
        Assert.Empty(vm.Results);
        Assert.Equal("", vm.Status);
    }

    [Fact]
    public async Task IndexConstructionDoesNotBlockReloadCaller()
    {
        using var snippets = new BlockedSnippets(Snapshot("New").Snippets[0]);
        var snapshot = new LibrarySnapshot([new(Group, "G", true)], snippets, 1);
        var vm = ViewModel(new ReadStore(Task.FromResult(snapshot)));
        var loading = vm.ReloadAsync();
        try
        {
            await snippets.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(loading.IsCompleted);
            Assert.True(vm.IsLoading);
            Assert.Empty(vm.Results);
        }
        finally { snippets.Release.Set(); }
        Assert.True(await loading.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal("New", Assert.Single(vm.Results).Content);
    }

    [Fact]
    public async Task OldIndexBuildCannotEndNewerLoadingState()
    {
        using var snippets = new BlockedSnippets(Snapshot("Old").Snippets[0]);
        var oldSnapshot = new LibrarySnapshot([new(Group, "G", true)], snippets, 1);
        var next = Pending();
        var vm = ViewModel(new ReadStore(Task.FromResult(oldSnapshot), next.Task));
        var oldLoad = vm.ReloadAsync();
        await snippets.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var newLoad = vm.ReloadAsync();
        try
        {
            snippets.Release.Set();
            Assert.False(await oldLoad.WaitAsync(TimeSpan.FromSeconds(5)));
            Assert.True(vm.IsLoading);
            Assert.Equal(vm.Texts.PickerLoading, vm.Status);
            Assert.Empty(vm.Results);
        }
        finally { snippets.Release.Set(); next.TrySetResult(Snapshot("New", 2)); }
        Assert.True(await newLoad);
        Assert.Equal("New", Assert.Single(vm.Results).Content);
    }

    [Fact]
    public async Task CurrentFailureDoesNotAllowAnOldReadToRestoreResults()
    {
        var oldRead = Pending();
        var vm = ViewModel(new ReadStore(oldRead.Task,
            Task.FromException<LibrarySnapshot>(new IOException("Current synthetic failure"))));
        var oldLoad = vm.ReloadAsync();
        Assert.False(await vm.ReloadAsync());
        var errorStatus = vm.Status;
        oldRead.SetResult(Snapshot("Old"));
        Assert.False(await oldLoad);
        vm.Query = "Old";
        Assert.Empty(vm.Results);
        Assert.Equal(errorStatus, vm.Status);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task QueryTypedWhileLoadingAppliesToNewIndex()
    {
        var pending = Pending();
        var vm = ViewModel(new ReadStore(pending.Task));
        vm.SetInsertAvailable(true);
        var loading = vm.ReloadAsync();
        vm.Query = "Beta";
        var alpha = Snapshot("Alpha").Snippets[0];
        var beta = Snapshot("Beta").Snippets[0];
        pending.SetResult(new([new(Group, "G", true)], [alpha, beta], 2));
        Assert.True(await loading);
        Assert.Equal("Beta", Assert.Single(vm.Results).Content);
        Assert.Equal(beta.Id, vm.Selected?.SnippetId);
        Assert.True(vm.CanInsert);
    }

    [Fact]
    public async Task SuccessfulReloadPreservesSelectedIdWithFreshContent()
    {
        var alpha = Snapshot("Alpha").Snippets[0];
        var beta = Snapshot("Beta").Snippets[0];
        var before = new LibrarySnapshot([new(Group, "G", true)], [alpha, beta], 1);
        var after = before with { Snippets = [alpha, beta with { Content = "Updated Beta", Revision = 2 }], Revision = 2 };
        var vm = ViewModel(new ReadStore(Task.FromResult(before), Task.FromResult(after)));
        await vm.ReloadAsync();
        vm.Selected = vm.Results.Single(r => r.SnippetId == beta.Id);
        Assert.True(await vm.ReloadAsync());
        Assert.Equal(beta.Id, vm.Selected?.SnippetId);
        Assert.Equal("Updated Beta", vm.Selected?.Content);
    }

    [Fact]
    public async Task RemovedSelectionFallsBackToAvailableResult()
    {
        var old = Snapshot("Old");
        var next = Snapshot("New", 2);
        var vm = ViewModel(new ReadStore(Task.FromResult(old), Task.FromResult(next)));
        await vm.ReloadAsync();
        Assert.True(await vm.ReloadAsync());
        Assert.Equal(next.Snippets[0].Id, vm.Selected?.SnippetId);
    }

    [Fact]
    public async Task QueuedOldSelectionCannotEnableActionsAfterFailure()
    {
        var next = Pending();
        var vm = ViewModel(new ReadStore(Task.FromResult(Snapshot("Old")), next.Task));
        vm.SetInsertAvailable(true);
        await vm.ReloadAsync();
        var staleSelection = vm.Selected;
        var loading = vm.ReloadAsync();
        vm.Selected = staleSelection;
        Assert.False(vm.HasSelection);
        next.SetException(new IOException("Synthetic failure"));
        Assert.False(await loading);
        Assert.False(vm.HasSelection);
        Assert.False(vm.CanInsert);
        vm.Query = "Old";
        Assert.Empty(vm.Results);
    }

    [Fact]
    public async Task FailedReloadCanBeRetriedWithANewRead()
    {
        var vm = ViewModel(new ReadStore(Task.FromException<LibrarySnapshot>(new IOException("Synthetic failure")),
            Task.FromResult(Snapshot("Recovered", 2))));
        Assert.False(await vm.ReloadAsync());
        Assert.False(vm.IsLoading);
        Assert.True(await vm.ReloadAsync());
        Assert.Equal("Recovered", Assert.Single(vm.Results).Content);
        Assert.Equal(vm.Texts.PickerManualNotice, vm.Status);
    }

    [Fact]
    public async Task LoadingAndCancellationNotifyActionBindings()
    {
        var pending = Pending();
        var vm = ViewModel(new ReadStore(pending.Task));
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        var loading = vm.ReloadAsync();
        Assert.Contains(nameof(vm.IsLoading), changed);
        Assert.Contains(nameof(vm.HasSelection), changed);
        Assert.Contains(nameof(vm.CanInsert), changed);
        changed.Clear();
        vm.CancelReload();
        Assert.False(await loading);
        pending.SetResult(Snapshot("Unused"));
        Assert.Contains(nameof(vm.IsLoading), changed);
        Assert.Contains(nameof(vm.HasSelection), changed);
        Assert.Contains(nameof(vm.CanInsert), changed);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("it")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("es")]
    public async Task LoadingNoticeIsLocalizedWithoutStartingAvalonia(string language)
    {
        var pending = Pending();
        var texts = UiStrings.ForLanguage(language);
        var vm = new PickerViewModel(new ReadStore(pending.Task), texts);
        var loading = vm.ReloadAsync();
        Assert.Equal(texts.PickerLoading, vm.Status);
        Assert.NotEmpty(vm.Status);
        Assert.DoesNotContain("PickerLoading", vm.Status);
        vm.CancelReload();
        Assert.False(await loading);
        pending.SetResult(Snapshot("Unused"));
    }

    [Fact]
    public async Task EmptySnapshotIsASuccessfulLoadWithNoActions()
    {
        var vm = ViewModel(new ReadStore(Task.FromResult(new LibrarySnapshot([], [], 1))));
        vm.SetInsertAvailable(true);
        Assert.True(await vm.ReloadAsync());
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasSelection);
        Assert.False(vm.CanInsert);
        Assert.Equal(vm.Texts.PickerManualNotice, vm.Status);
    }

    [Fact]
    public async Task ReloadCompletionCannotRearmConsumedInsertAvailability()
    {
        var pending = Pending();
        var vm = ViewModel(new ReadStore(pending.Task));
        vm.SetInsertAvailable(true);
        var loading = vm.ReloadAsync();
        vm.SetInsertAvailable(false);
        pending.SetResult(Snapshot("New"));
        Assert.True(await loading);
        Assert.True(vm.HasSelection);
        Assert.False(vm.CanInsert);
    }

    private sealed class BlockedSnippets(Snippet item) : IReadOnlyList<Snippet>, IDisposable
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ManualResetEventSlim Release { get; } = new();
        public int Count => 1;
        public Snippet this[int index] => index == 0 ? item : throw new ArgumentOutOfRangeException(nameof(index));
        public IEnumerator<Snippet> GetEnumerator()
        {
            Entered.TrySetResult();
            if (!Release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Synthetic index barrier timed out");
            yield return item;
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        public void Dispose() => Release.Dispose();
    }

    private sealed class ReadStore(params Task<LibrarySnapshot>[] reads) : ILibraryStore
    {
        private readonly Queue<Task<LibrarySnapshot>> _reads = new(reads);
        public List<CancellationToken> Tokens { get; } = [];
        public Task<LibrarySnapshot> ReadAsync(CancellationToken ct = default)
        {
            Tokens.Add(ct);
            return _reads.Dequeue(); // Deliberately ignore cancellation to exercise late completions.
        }
        public Task InitializeAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<SnippetGroup> CreateGroupAsync(string name, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<SnippetGroup> UpdateGroupAsync(SnippetGroup previous, string name, bool enabled, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteGroupAsync(SnippetGroup previous, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ImportSummary> ImportPortableAsync(PortableLibraryDocument document, CancellationToken ct = default, long? expectedLibraryRevision = null) => throw new NotSupportedException();
        public Task<Snippet> SaveSnippetAsync(SnippetDraft draft, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteSnippetAsync(Guid id, long revision, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<string?> ReadSettingAsync(string key, CancellationToken ct = default) => throw new NotSupportedException();
        public Task WriteSettingAsync(string key, string value, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<string> CreateBackupAsync(string path, CancellationToken ct = default) => throw new NotSupportedException();
        public Task RestoreBackupAsync(string path, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
