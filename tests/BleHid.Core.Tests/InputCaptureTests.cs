using BleHid.Core;
using Xunit;

namespace BleHid.Core.Tests;

public sealed class InputCaptureTests
{
    private const int CapsLock = 0x14;
    private const int LeftControl = 0xA2;
    private const int LeftAlt = 0xA4;

    [Fact]
    public void Hook_timing_separates_event_age_from_callback_duration()
    {
        var timing = new InputCapture.HookTiming();

        timing.Record(100, 110, 20);
        timing.Record(200, 240, 30);

        Assert.Equal(2, timing.Count);
        Assert.Equal(50, timing.TotalEventAgeMs);
        Assert.Equal(40, timing.MaxEventAgeMs);
        Assert.Equal(50, timing.TotalCallbackTicks);
        Assert.Equal(30, timing.MaxCallbackTicks);
    }

    [Fact]
    public void Hook_timing_handles_tick_count_rollover()
    {
        var timing = new InputCapture.HookTiming();

        timing.Record(int.MaxValue - 4, int.MinValue + 5, 0);

        Assert.Equal(10, timing.TotalEventAgeMs);
        Assert.Equal(10, timing.MaxEventAgeMs);
    }

    [Fact]
    public void Hook_timing_clamps_future_event_timestamps()
    {
        var timing = new InputCapture.HookTiming();

        timing.Record(12, 10, 0);

        Assert.Equal(0, timing.TotalEventAgeMs);
        Assert.Equal(0, timing.MaxEventAgeMs);
    }

    [Theory]
    [InlineData(0xA2, 0xA4)]
    [InlineData(0xA2, 0xA5)]
    [InlineData(0xA3, 0xA4)]
    [InlineData(0xA3, 0xA5)]
    public void Switching_out_with_caps_lock_and_back_with_d_c_leaves_no_local_keys_pressed(
        int controlKey, int altKey)
    {
        using var capture = new InputCapture();
        capture.SetPassThrough(true);
        var localPressedKeys = new HashSet<int>();
        var switches = 0;
        capture.SwitchHostRequested += () =>
        {
            switches++;
            capture.SetPassThrough(!capture.PassThrough);
        };

        void HandleKey(int virtualKey, bool isDown)
        {
            if (capture.HandleKeyboardEvent(virtualKey, isDown)) return;
            if (isDown) localPressedKeys.Add(virtualKey);
            else localPressedKeys.Remove(virtualKey);
        }

        HandleKey(controlKey, true);
        HandleKey(altKey, true);
        HandleKey(CapsLock, true);
        Assert.False(capture.PassThrough);
        HandleKey(CapsLock, false);
        HandleKey(altKey, false);
        HandleKey(controlKey, false);

        HandleKey(controlKey, true);
        HandleKey(0x44, true);
        HandleKey(0x43, true);
        HandleKey(0x43, false);
        HandleKey(0x44, false);
        HandleKey(controlKey, false);

        Assert.True(capture.PassThrough);
        Assert.Equal(2, switches);
        Assert.Empty(localPressedKeys);
    }

    [Theory]
    [InlineData(0x41)]
    [InlineData(0xA0)]
    [InlineData(0xA2)]
    [InlineData(0xA3)]
    [InlineData(0xA4)]
    [InlineData(0xA5)]
    [InlineData(0x14)]
    [InlineData(0x5B)]
    public void Locally_started_keys_are_released_on_both_targets_when_capture_resumes(int virtualKey)
    {
        using var capture = new InputCapture();
        var reports = new List<(KeyModifiers Modifiers, byte[] Usages)>();
        capture.KeyboardReport += (modifiers, usages) => reports.Add((modifiers, usages));
        capture.SetPassThrough(true);
        Assert.False(capture.HandleKeyboardEvent(virtualKey, true));

        capture.SetPassThrough(false);
        Assert.True(capture.HandleKeyboardEvent(virtualKey, true));
        Assert.False(capture.HandleKeyboardEvent(virtualKey, false));

        Assert.Equal(2, reports.Count);
        Assert.True(reports[0].Modifiers != KeyModifiers.None || reports[0].Usages.Length > 0);
        Assert.Equal(KeyModifiers.None, reports[1].Modifiers);
        Assert.Empty(reports[1].Usages);
        Assert.True(capture.HandleKeyboardEvent(virtualKey, true));
        Assert.True(capture.HandleKeyboardEvent(virtualKey, false));
    }

    [Theory]
    [InlineData(0xA2, 0xA4, false)]
    [InlineData(0xA2, 0xA5, false)]
    [InlineData(0xA3, 0xA4, false)]
    [InlineData(0xA3, 0xA5, false)]
    [InlineData(0xA2, 0xA4, true)]
    [InlineData(0xA2, 0xA5, true)]
    [InlineData(0xA3, 0xA4, true)]
    [InlineData(0xA3, 0xA5, true)]
    public void Ctrl_alt_caps_lock_releases_keys_then_switches_without_forwarding_caps_lock(
        int controlKey, int altKey, bool passThrough)
    {
        using var capture = new InputCapture();
        capture.SetPassThrough(passThrough);
        capture.HandleKeyboardEvent(0x41, true);
        capture.HandleKeyboardEvent(controlKey, true);
        capture.HandleKeyboardEvent(altKey, true);
        var events = new List<string>();
        capture.KeyboardReport += (modifiers, usages) =>
        {
            Assert.Equal(KeyModifiers.None, modifiers);
            Assert.Empty(usages);
            events.Add("release");
        };
        capture.SwitchHostRequested += () => events.Add("switch");

        Assert.True(capture.HandleKeyboardEvent(CapsLock, true));
        Assert.True(capture.HandleKeyboardEvent(CapsLock, false));

        Assert.Equal(new[] { "release", "switch" }, events);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Caps_lock_cycles_once_until_released_even_when_modifiers_are_released_first(bool passThrough)
    {
        using var capture = new InputCapture();
        capture.SetPassThrough(passThrough);
        var switches = 0;
        capture.SwitchHostRequested += () => switches++;
        capture.HandleKeyboardEvent(LeftControl, true);
        capture.HandleKeyboardEvent(LeftAlt, true);

        Assert.True(capture.HandleKeyboardEvent(CapsLock, true));
        Assert.True(capture.HandleKeyboardEvent(CapsLock, true));
        capture.HandleKeyboardEvent(LeftControl, false);
        capture.HandleKeyboardEvent(LeftAlt, false);
        Assert.True(capture.HandleKeyboardEvent(CapsLock, true));
        Assert.Equal(1, switches);
        Assert.True(capture.HandleKeyboardEvent(CapsLock, false));

        capture.HandleKeyboardEvent(LeftControl, true);
        capture.HandleKeyboardEvent(LeftAlt, true);
        Assert.True(capture.HandleKeyboardEvent(CapsLock, true));
        Assert.True(capture.HandleKeyboardEvent(CapsLock, false));
        Assert.Equal(2, switches);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    public void Caps_lock_without_both_modifiers_keeps_normal_input_behavior(
        bool controlDown, bool altDown, bool passThrough)
    {
        using var capture = new InputCapture();
        capture.SetPassThrough(passThrough);
        if (controlDown) capture.HandleKeyboardEvent(LeftControl, true);
        if (altDown) capture.HandleKeyboardEvent(LeftAlt, true);
        var switches = 0;
        var reports = new List<byte[]>();
        capture.SwitchHostRequested += () => switches++;
        capture.KeyboardReport += (_, usages) => reports.Add(usages);

        Assert.Equal(!passThrough, capture.HandleKeyboardEvent(CapsLock, true));
        Assert.Equal(!passThrough, capture.HandleKeyboardEvent(CapsLock, false));

        Assert.Equal(0, switches);
        if (passThrough)
        {
            Assert.Empty(reports);
        }
        else
        {
            Assert.Equal(2, reports.Count);
            Assert.Equal(new byte[] { 0x39 }, reports[0]);
            Assert.Empty(reports[1]);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Ctrl_d_c_still_cycles_once_until_released(bool passThrough)
    {
        using var capture = new InputCapture();
        capture.SetPassThrough(passThrough);
        var switches = 0;
        capture.SwitchHostRequested += () => switches++;
        capture.HandleKeyboardEvent(LeftControl, true);
        capture.HandleKeyboardEvent(0x44, true);

        Assert.True(capture.HandleKeyboardEvent(0x43, true));
        Assert.True(capture.HandleKeyboardEvent(0x43, true));
        Assert.Equal(1, switches);

        capture.HandleKeyboardEvent(0x43, false);
        Assert.True(capture.HandleKeyboardEvent(0x43, true));
        Assert.Equal(2, switches);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Ctrl_alt_q_still_stops_without_switching(bool passThrough)
    {
        using var capture = new InputCapture();
        capture.SetPassThrough(passThrough);
        var switches = 0;
        var stops = 0;
        capture.SwitchHostRequested += () => switches++;
        capture.StopRequested += () => stops++;
        capture.HandleKeyboardEvent(LeftControl, true);
        capture.HandleKeyboardEvent(LeftAlt, true);

        Assert.True(capture.HandleKeyboardEvent(0x51, true));

        Assert.Equal(1, stops);
        Assert.Equal(0, switches);
    }

    [Fact]
    public void Shake_gesture_switches_the_target()
    {
        using var capture = new InputCapture();
        var now = 1000;
        capture.SetNow(() => now);
        capture.SetSwitchGesture(SwitchGestureOptions.ForTest(true, 80, 3, 800));

        var switches = 0;
        capture.SwitchHostRequested += () => switches++;

        var sign = 1;
        for (var stroke = 0; stroke < 4; stroke++)
        {
            for (var step = 0; step < 10; step++)
            {
                now += 5;
                capture.TrackGesture(sign * 10, 0);
            }
            sign = -sign;
        }

        Assert.Equal(1, switches);
    }

    [Fact]
    public void Shake_gesture_is_inert_when_it_is_not_enabled()
    {
        using var capture = new InputCapture();
        var now = 1000;
        capture.SetNow(() => now);
        capture.SetSwitchGesture(SwitchGestureOptions.ForTest(false, 80, 3, 800));

        var switches = 0;
        capture.SwitchHostRequested += () => switches++;

        var sign = 1;
        for (var stroke = 0; stroke < 8; stroke++)
        {
            for (var step = 0; step < 10; step++)
            {
                now += 5;
                capture.TrackGesture(sign * 10, 0);
            }
            sign = -sign;
        }

        Assert.Equal(0, switches);
    }

    [Fact]
    public void Shake_gesture_does_not_fire_on_ordinary_pointer_movement()
    {
        using var capture = new InputCapture();
        var now = 1000;
        capture.SetNow(() => now);
        capture.SetSwitchGesture(SwitchGestureOptions.ForTest(true, 80, 3, 800));

        var switches = 0;
        capture.SwitchHostRequested += () => switches++;

        // A long sweep across the screen, then a short correction back - the shape of reaching
        // for something and overshooting slightly.
        for (var step = 0; step < 100; step++)
        {
            now += 5;
            capture.TrackGesture(12, 1);
        }
        for (var step = 0; step < 3; step++)
        {
            now += 5;
            capture.TrackGesture(-6, 0);
        }

        Assert.Equal(0, switches);
    }

    [Fact]
    public void Switching_target_releases_a_held_mouse_button()
    {
        // A button still down on the outgoing host reads as a drag that never ends.
        using var capture = new InputCapture();
        capture.SetPassThrough(false);

        var buttonReports = new List<MouseButtons>();
        capture.MouseReport += (buttons, _, _, _) => buttonReports.Add(buttons);

        capture.HandleKeyboardEvent(LeftControl, true);
        capture.HandleKeyboardEvent(LeftAlt, true);
        capture.HandleKeyboardEvent(CapsLock, true);

        // No button was held, so no spurious release should be sent either.
        Assert.Empty(buttonReports);
    }
}
