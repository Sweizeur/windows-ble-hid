namespace BleHid.Core;

/// <summary>
/// Maps PC scan set 1 make codes to USB HID keyboard usages (physical key identity).
/// Hosts apply their own layout on top, so AZERTY PC matches AZERTY iPhone when both use the same physical positions.
/// </summary>
public static class ScanCodeMap
{
    public static bool TryGetUsage(uint scanCode, bool extended, out byte usage)
    {
        if (extended)
            return TryGetExtendedUsage(scanCode, out usage);

        // Modifier keys are reported only in the modifier byte, not as key usages.
        if (scanCode is 0x1D or 0x2A or 0x36 or 0x38 or 0x4A or 0x58 or 0x4E or 0x57)
        {
            usage = 0;
            return false;
        }

        usage = scanCode switch
        {
            0x01 => 0x29, // Escape
            0x02 => 0x1E, // 1
            0x03 => 0x1F, // 2
            0x04 => 0x20, // 3
            0x05 => 0x21, // 4
            0x06 => 0x22, // 5
            0x07 => 0x23, // 6
            0x08 => 0x24, // 7
            0x09 => 0x25, // 8
            0x0A => 0x26, // 9
            0x0B => 0x27, // 0
            0x0C => 0x2D, // -
            0x0D => 0x2E, // =
            0x0E => 0x2A, // Backspace
            0x0F => 0x2B, // Tab
            0x10 => 0x14, // Q
            0x11 => 0x1A, // W
            0x12 => 0x08, // E
            0x13 => 0x15, // R
            0x14 => 0x17, // T
            0x15 => 0x1C, // Y
            0x16 => 0x18, // U
            0x17 => 0x0C, // I
            0x18 => 0x12, // O
            0x19 => 0x13, // P
            0x1A => 0x2F, // [
            0x1B => 0x30, // ]
            0x1C => 0x28, // Enter
            0x1E => 0x04, // A
            0x1F => 0x16, // S
            0x20 => 0x07, // D
            0x21 => 0x09, // F
            0x22 => 0x0A, // G
            0x23 => 0x0B, // H
            0x24 => 0x0D, // J
            0x25 => 0x0E, // K
            0x26 => 0x0F, // L
            0x27 => 0x33, // ;
            0x28 => 0x34, // '
            0x29 => 0x35, // `
            0x2B => 0x31, // \
            0x2C => 0x1D, // Z
            0x2D => 0x1B, // X
            0x2E => 0x06, // C
            0x2F => 0x19, // V
            0x30 => 0x05, // B
            0x31 => 0x11, // N
            0x32 => 0x10, // M
            0x33 => 0x36, // ,
            0x34 => 0x37, // .
            0x35 => 0x38, // /
            0x37 => 0x46, // Print Screen
            0x39 => 0x2C, // Space
            0x3A => 0x39, // Caps Lock
            0x3B => 0x3A, // F1
            0x3C => 0x3B, // F2
            0x3D => 0x3C, // F3
            0x3E => 0x3D, // F4
            0x3F => 0x3E, // F5
            0x40 => 0x3F, // F6
            0x41 => 0x40, // F7
            0x42 => 0x41, // F8
            0x43 => 0x42, // F9
            0x44 => 0x43, // F10
            0x45 => 0x53, // Num Lock
            0x46 => 0x47, // Scroll Lock
            0x47 => 0x5F, // Keypad 7
            0x48 => 0x60, // Keypad 8
            0x49 => 0x61, // Keypad 9
            0x4B => 0x5C, // Keypad 4
            0x4C => 0x5D, // Keypad 5
            0x4D => 0x5E, // Keypad 6
            0x4F => 0x59, // Keypad 1
            0x50 => 0x5A, // Keypad 2
            0x51 => 0x5B, // Keypad 3
            0x52 => 0x62, // Keypad 0
            0x53 => 0x63, // Keypad .
            _ => (byte)0
        };

        return usage != 0;
    }

    private static bool TryGetExtendedUsage(uint scanCode, out byte usage)
    {
        usage = scanCode switch
        {
            0x1C => 0x58, // Keypad Enter
            0x1D => 0x54, // Keypad /
            0x47 => 0x4A, // Home
            0x48 => 0x52, // Up
            0x49 => 0x4B, // Page Up
            0x4B => 0x4E, // Page Down
            0x4D => 0x4F, // Right
            0x4F => 0x4D, // End
            0x50 => 0x51, // Down
            0x52 => 0x49, // Insert
            0x53 => 0x4C, // Delete
            _ => (byte)0
        };

        return usage != 0;
    }
}
