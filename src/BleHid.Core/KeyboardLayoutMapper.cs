using System.Runtime.InteropServices;

namespace BleHid.Core;

/// <summary>
/// Maps keystrokes using the active Windows keyboard layout so the remote receives the same
/// characters this PC would type, instead of treating virtual-key letters as US QWERTY positions.
/// </summary>
public static class KeyboardLayoutMapper
{
    private const uint MapvkVkToVsc = 0;

    /// <summary>Test hook: replaces Win32 layout translation.</summary>
    internal static Func<int, uint, bool, byte[], IntPtr, char?>? TranslateHook { get; set; }

    /// <summary>Active input locale for the current thread (matches what desktop apps use).</summary>
    public static IntPtr ActiveLayout => GetKeyboardLayout(0);

    /// <summary>Two-letter ISO language name of the active layout, e.g. "fr" or "en".</summary>
    public static string ActiveLayoutName
    {
        get
        {
            var layout = ActiveLayout;
            if (layout == IntPtr.Zero) return "unknown";
            var id = unchecked((int)layout.ToInt64());
            var primary = id & 0xFFFF;
            var lang = primary switch
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
            return lang;
        }
    }

    /// <summary>
    /// True when the key should be resolved through the layout (character) rather than virtual-key position.
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

    /// <summary>
    /// Resolves a key press to a HID usage that types the same character on a US QWERTY host.
    /// </summary>
    public static bool TryGetHidUsage(
        int virtualKey,
        uint scanCode,
        bool extended,
        IReadOnlyCollection<int> pressedVirtualKeys,
        out byte usage)
    {
        usage = 0;
        if (!ShouldTranslate(virtualKey, pressedVirtualKeys)) return false;

        var state = new byte[256];
        foreach (var vk in pressedVirtualKeys)
        {
            if (vk is >= 0 and <= 255) state[vk] = 0x80;
        }

        var scan = scanCode | (extended ? 0xE000u : 0u);
        var layout = ActiveLayout;

        char ch;
        if (TranslateHook is { } hook)
        {
            var translated = hook(virtualKey, scan, extended, state, layout);
            if (translated is null) return false;
            ch = translated.Value;
        }
        else
        {
            var length = ToUnicodeEx((uint)virtualKey, scan, state, _charBuffer, _charBuffer.Length, 0, layout);
            if (length != 1) return false;
            ch = _charBuffer[0];
        }

        if (ch < ' ') return false;

        return HidReports.TryMapChar(ch, out usage, out _);
    }

    private static readonly char[] _charBuffer = new char[4];

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ToUnicodeEx(
        uint wVk,
        uint wScan,
        byte[] lpKeyState,
        [Out] char[] pwszBuff,
        int cchBuff,
        uint wFlags,
        IntPtr dwhkl);
}
