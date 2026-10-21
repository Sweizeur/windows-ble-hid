using System.Runtime.InteropServices;

namespace BleHid.Core;

/// <summary>
/// Resolves captured keys to HID usages using the physical scan code from the hook, so the
/// remote host applies its own layout (AZERTY iPhone matches AZERTY PC key caps).
/// </summary>
public static class KeyboardLayoutMapper
{
    /// <summary>Active input locale for the current thread (matches what desktop apps use).</summary>
    public static IntPtr ActiveLayout => GetKeyboardLayout(0);

    /// <summary>Display name of the active layout, e.g. fr-FR.</summary>
    public static string ActiveLayoutName
    {
        get
        {
            var layout = ActiveLayout;
            if (layout == IntPtr.Zero) return "unknown";
            var id = unchecked((int)layout.ToInt64());
            var primary = id & 0xFFFF;
            return primary switch
            {
                0x0409 => "en-US",
                0x040C => "fr-FR",
                0x0809 => "en-GB",
                0x0407 => "de-DE",
                0x0410 => "it-IT",
                0x040A => "es-ES",
                0x0416 => "pt-BR",
                0x0419 => "ru-RU",
                0x0411 => "ja-JP",
                _ => $"0x{primary:X4}"
            };
        }
    }

    /// <summary>
    /// True when the key should be resolved from its physical scan code rather than virtual-key letter positions.
    /// </summary>
    public static bool ShouldTranslate(int virtualKey, IReadOnlyCollection<int> pressedVirtualKeys)
    {
        if (pressedVirtualKeys.Contains(0xA2) || pressedVirtualKeys.Contains(0xA3)) return false;
        if (pressedVirtualKeys.Contains(0xA4) && !pressedVirtualKeys.Contains(0xA5)) return false;
        if (pressedVirtualKeys.Contains(0x5B) || pressedVirtualKeys.Contains(0x5C)) return false;

        return virtualKey switch
        {
            >= 0x70 and <= 0x7B => false,
            0x2D or 0x2E or 0x24 or 0x23 or 0x21 or 0x22 or 0x27 or 0x25 or 0x28 or 0x26 => false,
            0x90 or 0x91 or 0x14 => false,
            _ => true
        };
    }

    /// <summary>Maps a hooked key to the HID usage for its physical scan code.</summary>
    public static bool TryGetHidUsage(
        int virtualKey,
        uint scanCode,
        bool extended,
        IReadOnlyCollection<int> pressedVirtualKeys,
        out byte usage)
    {
        usage = 0;
        if (!ShouldTranslate(virtualKey, pressedVirtualKeys)) return false;

        if (ScanCodeMap.TryGetUsage(scanCode, extended, out usage)) return true;

        return VirtualKeyMap.TryGetUsage(virtualKey, scanCode, extended, out usage);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);
}
