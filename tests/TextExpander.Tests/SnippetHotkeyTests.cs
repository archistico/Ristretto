using TextExpander.Application.Features.Hotkeys;
using TextExpander.Core;
using TextExpander.Infrastructure.Platform.Windows;
using Xunit;

namespace TextExpander.Tests;

public sealed class SnippetHotkeyTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData(" ", "")]
    [InlineData("None", "")]
    [InlineData(" alt + ctrl + f ", "Ctrl+Alt+F")]
    [InlineData("Ctrl+Shift+F11", "Ctrl+Shift+F11")]
    public void OptionalHotkeysAreCanonicalized(string? input, string expected) =>
        Assert.Equal(expected, SnippetHotkeyRules.Normalize(input));

    [Theory]
    [InlineData("F1")]
    [InlineData("Shift+A")]
    [InlineData("Ctrl+Enter")]
    [InlineData("Ctrl+F12")]
    public void InvalidHotkeysAreRejected(string input) =>
        Assert.Throws<ArgumentException>(() => SnippetHotkeyRules.Normalize(input));

    private static readonly Guid Group = Guid.NewGuid();
    private static Snippet Item(string key, bool enabled = true) => new(Guid.NewGuid(), Group,
        "Test", ";test", "Caffè e\u0301 🧑🏽‍💻\nمرحبا", "", enabled, 1, key);
    private static LibrarySnapshot Snapshot(params Snippet[] items) => new([new(Group, "G", true)], items, 1);

    [Fact]
    public void CatalogExcludesDisabledMissingGroupsInvalidKeysAndPickerShortcut()
    {
        var active = Item("Ctrl+Alt+F");
        var hiddenGroup = Guid.NewGuid();
        var snapshot = new LibrarySnapshot([new(Group, "G", true), new(hiddenGroup, "Off", false)],
            [active, Item("Ctrl+Alt+G", false), Item(""), Item("Ctrl+Enter"),
             Item(PickerHotkey.Default), Item("Ctrl+Alt+H") with { GroupId = hiddenGroup },
             Item("Ctrl+Alt+J") with { GroupId = Guid.NewGuid() }], 1);
        var catalog = new SnippetHotkeyCatalog(snapshot, PickerHotkey.Default, 1);
        var binding = Assert.Single(catalog.Bindings);
        Assert.Equal(active, catalog.Resolve(binding));
    }

    [Fact]
    public void CanonicallyDuplicateBindingsHaveNoArbitraryWinner()
    {
        var catalog = new SnippetHotkeyCatalog(Snapshot(Item("Alt+Ctrl+F"), Item("Ctrl+Alt+F")), "None", 1);
        Assert.Empty(catalog.Bindings);
    }

    [Fact]
    public void QueuedBindingIsRejectedAfterRefreshDisableOrEdit()
    {
        var item = Item("Ctrl+Alt+F");
        var old = new SnippetHotkeyCatalog(Snapshot(item), "None", 1);
        var binding = Assert.Single(old.Bindings);
        Assert.Null(new SnippetHotkeyCatalog(Snapshot(item), "None", 2).Resolve(binding));
        Assert.Null(new SnippetHotkeyCatalog(Snapshot(item with { IsEnabled = false }), "None", 2).Resolve(binding));
        Assert.Null(new SnippetHotkeyCatalog(Snapshot(item with { Content = "Changed", Revision = 2 }), "None", 1).Resolve(binding));
        Assert.Equal(item.Content, old.Resolve(binding)!.Content);
    }

    [Fact]
    public void RegistrationCollisionCannotDispatchAndOtherShortcutsRemainUsable()
    {
        var catalog = new SnippetHotkeyCatalog(Snapshot(Item("Ctrl+Alt+F"), Item("Ctrl+Alt+G")), "None", 1);
        var set = new WindowsSnippetHotkeySet((id, _) => id == 2, _ => true);
        var result = set.Replace(catalog.Bindings, () => { });
        Assert.False(result[catalog.Bindings[0].SnippetId]);
        Assert.True(result[catalog.Bindings[1].SnippetId]);
        Assert.Null(set.Resolve(1, catalog.Bindings[0].Gesture.Modifiers, catalog.Bindings[0].Gesture.VirtualKey));
        var second = catalog.Bindings[1];
        Assert.Equal(second, set.Resolve(2, second.Gesture.Modifiers, second.Gesture.VirtualKey));
        Assert.Null(set.Resolve(2, 0, second.Gesture.VirtualKey));
    }

    [Fact]
    public void ReplacementUnregistersBeforeDrainingAndRegisteringNewBindings()
    {
        var calls = new List<string>();
        var set = new WindowsSnippetHotkeySet((id, _) => { calls.Add("register" + id); return true; },
            id => { calls.Add("unregister" + id); return true; });
        var first = new SnippetHotkeyCatalog(Snapshot(Item("Ctrl+Alt+F")), "None", 1).Bindings;
        var second = new SnippetHotkeyCatalog(Snapshot(Item("Ctrl+Alt+G")), "None", 2).Bindings;
        set.Replace(first, () => calls.Add("drain"));
        calls.Clear();
        set.Replace(second, () => calls.Add("drain"));
        Assert.Equal(new[] { "unregister1", "drain", "register1" }, calls);
        Assert.Null(set.Resolve(1, first[0].Gesture.Modifiers, first[0].Gesture.VirtualKey));
        set.Clear();
        Assert.Null(set.Resolve(1, second[0].Gesture.Modifiers, second[0].Gesture.VirtualKey));
    }

    [Fact]
    public void FailedUnregisterDoesNotReuseAnAmbiguousOsIdAndCanRecover()
    {
        var release = false;
        var registrations = 0;
        var set = new WindowsSnippetHotkeySet((_, _) => { registrations++; return true; }, _ => release);
        var first = new SnippetHotkeyCatalog(Snapshot(Item("Ctrl+Alt+F")), "None", 1).Bindings;
        var second = new SnippetHotkeyCatalog(Snapshot(Item("Ctrl+Alt+G")), "None", 2).Bindings;
        set.Replace(first, () => { });
        Assert.False(set.Replace(second, () => throw new Exception())[second[0].SnippetId]);
        Assert.Equal(1, registrations);
        Assert.Null(set.Resolve(1, first[0].Gesture.Modifiers, first[0].Gesture.VirtualKey));
        release = true;
        Assert.True(set.Replace(second, () => { })[second[0].SnippetId]);
        Assert.Equal(2, registrations);
    }
}
