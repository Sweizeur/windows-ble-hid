using BleHid.Core;
using Xunit;

namespace BleHid.Core.Tests;

public sealed class KeyboardLayoutMapperTests
{
    [Fact]
    public void Should_translate_skips_shortcuts_with_control_held()
    {
        Assert.False(KeyboardLayoutMapper.ShouldTranslate(0x41, [0xA2]));
        Assert.False(KeyboardLayoutMapper.ShouldTranslate(0x41, [0xA3]));
    }

    [Fact]
    public void Should_translate_skips_left_alt_without_right_alt()
    {
        Assert.False(KeyboardLayoutMapper.ShouldTranslate(0x41, [0xA4]));
        Assert.True(KeyboardLayoutMapper.ShouldTranslate(0x41, [0xA5]));
    }

    [Fact]
    public void French_azerty_a_key_maps_to_hid_a_not_hid_q()
    {
        KeyboardLayoutMapper.TranslateHook = (vk, _, _, _, _) =>
            vk == 0x51 ? 'a' : null;

        var pressed = new HashSet<int> { 0x51 };
        Assert.True(KeyboardLayoutMapper.TryGetHidUsage(0x51, 0x10, false, pressed, out var usage));
        Assert.Equal(0x04, usage);
    }

    [Fact]
    public void Layout_translation_falls_back_when_to_unicode_returns_nothing()
    {
        KeyboardLayoutMapper.TranslateHook = (_, _, _, _, _) => null;

        Assert.False(KeyboardLayoutMapper.TryGetHidUsage(0x41, 0x1E, false, [0x41], out _));
    }

    public KeyboardLayoutMapperTests()
    {
        KeyboardLayoutMapper.TranslateHook = null;
    }
}
