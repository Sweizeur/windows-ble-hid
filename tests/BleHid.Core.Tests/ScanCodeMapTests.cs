using BleHid.Core;
using Xunit;

namespace BleHid.Core.Tests;

public sealed class ScanCodeMapTests
{
    [Theory]
    [InlineData(0x11, false, 0x1A)] // W position
    [InlineData(0x2C, false, 0x1D)] // Z position
    [InlineData(0x1E, false, 0x04)] // A
    public void Maps_pc_scan_codes_to_hid_usages(uint scan, bool extended, byte expected)
    {
        Assert.True(ScanCodeMap.TryGetUsage(scan, extended, out var usage));
        Assert.Equal(expected, usage);
    }

    [Fact]
    public void Modifier_scan_codes_do_not_produce_key_usages()
    {
        Assert.False(ScanCodeMap.TryGetUsage(0x1D, false, out _));
        Assert.False(ScanCodeMap.TryGetUsage(0x2A, false, out _));
    }
}
