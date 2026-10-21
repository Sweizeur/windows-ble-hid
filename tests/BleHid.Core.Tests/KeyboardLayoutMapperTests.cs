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
    public void Azerty_z_key_uses_physical_w_position_not_us_z_position()
    {
        // On AZERTY, the Z cap is the same physical key as W on a US keyboard (scan 0x11).
        var pressed = new HashSet<int> { 0x5A };
        Assert.True(KeyboardLayoutMapper.TryGetHidUsage(0x5A, 0x11, false, pressed, out var usage));
        Assert.Equal(0x1A, usage);
    }

    [Fact]
    public void Virtual_key_z_without_scan_mapping_would_be_wrong_for_azerty_hosts()
    {
        Assert.True(VirtualKeyMap.TryGetUsage(0x5A, out var usage));
        Assert.Equal(0x1D, usage);
    }
}
