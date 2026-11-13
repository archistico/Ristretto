using TextExpander.Application.Features.Hotkeys;
using Xunit;

namespace TextExpander.Tests;

public sealed class PickerHotkeyTests
{
    [Theory]
    [InlineData("Ctrl+Shift+Space", "Ctrl+Shift+Space", 0x20u)]
    [InlineData("shift+control+spacebar", "Ctrl+Shift+Space", 0x20u)]
    [InlineData("Alt+Ctrl+K", "Ctrl+Alt+K", 0x4Bu)]
    [InlineData("Ctrl+F11", "Ctrl+F11", 0x7Au)]
    [InlineData("Alt+1", "Alt+1", 0x31u)]
    [InlineData("Windows+CTRL+z", "Ctrl+Win+Z", 0x5Au)]
    public void ValidChordsAreCanonicalAndUseExpectedVirtualKey(string input, string canonical, uint vk)
    {
        Assert.True(PickerHotkey.TryParse(input, out var chord));
        Assert.Equal(canonical, chord.Canonical);
        Assert.Equal(vk, chord.VirtualKey);
        Assert.False(chord.IsDisabled);
        Assert.True(PickerHotkey.TryParse(chord.Canonical, out var reparsed));
        Assert.Equal(chord, reparsed);
    }

    [Theory]
    [InlineData("None")]
    [InlineData(" none ")]
    public void NoneDisablesHotkey(string input)
    {
        Assert.True(PickerHotkey.TryParse(input, out var chord));
        Assert.True(chord.IsDisabled);
        Assert.Equal("None", chord.Canonical);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Ctrl")]
    [InlineData("Shift+Space")]
    [InlineData("Win+R")]
    [InlineData("Ctrl+Ctrl+K")]
    [InlineData("Ctrl+Alt")]
    [InlineData("Ctrl+K+P")]
    [InlineData("Ctrl++K")]
    [InlineData("Ctrl+F12")]
    [InlineData("Ctrl+F13")]
    [InlineData("Ctrl+F0")]
    [InlineData("Ctrl+Enter")]
    [InlineData("Ctrl+Delete")]
    [InlineData("Ctrl+😃")]
    [InlineData("Ctrl+Esc")]
    public void InvalidOrUnsafeChordsAreRejected(string input)
    {
        Assert.False(PickerHotkey.TryParse(input, out _));
    }

    [Fact]
    public void NullAndOverlongChordsAreRejected()
    {
        Assert.False(PickerHotkey.TryParse(null, out _));
        Assert.False(PickerHotkey.TryParse("Ctrl+" + new string('K', 120), out _));
    }
}
