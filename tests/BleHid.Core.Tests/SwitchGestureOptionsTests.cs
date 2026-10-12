using BleHid.Core;
using Xunit;

namespace BleHid.Core.Tests;

public sealed class SwitchGestureOptionsTests
{
    [Fact]
    public void A_missing_file_leaves_the_gesture_disabled_without_complaining()
    {
        var path = Path.Combine(Path.GetTempPath(), $"blehid-gesture-{Guid.NewGuid():N}.json");

        var options = SwitchGestureOptions.Load(path);

        Assert.False(options.Enabled);
        Assert.Null(options.Warning);
    }

    [Fact]
    public void An_unreadable_file_disables_the_gesture_and_explains_why()
    {
        var path = Path.Combine(Path.GetTempPath(), $"blehid-gesture-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{ not json");
        try
        {
            var options = SwitchGestureOptions.Load(path);

            Assert.False(options.Enabled);
            Assert.NotNull(options.Warning);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void The_gesture_stays_off_unless_the_file_turns_it_on()
    {
        var options = SwitchGestureOptions.Parse("""{"minStrokePx":120}""");

        Assert.False(options.Enabled);
        Assert.Equal(120, options.MinStrokePx);
    }

    [Fact]
    public void Unset_values_fall_back_to_the_defaults()
    {
        var options = SwitchGestureOptions.Parse("""{"enabled":true}""");

        Assert.True(options.Enabled);
        Assert.Equal(80, options.MinStrokePx);
        Assert.Equal(3, options.RequiredReversals);
        Assert.Equal(250, options.WindowMs);
        Assert.Null(options.Warning);
    }

    [Fact]
    public void Configured_values_are_kept()
    {
        var options = SwitchGestureOptions.Parse(
            """{"enabled":true,"minStrokePx":150,"requiredReversals":5,"windowMs":1200}""");

        Assert.Equal(150, options.MinStrokePx);
        Assert.Equal(5, options.RequiredReversals);
        Assert.Equal(1200, options.WindowMs);
        Assert.Null(options.Warning);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(5000, 500)]
    public void An_out_of_range_stroke_length_is_clamped_and_reported(int configured, int expected)
    {
        var options = SwitchGestureOptions.Parse($$"""{"enabled":true,"minStrokePx":{{configured}}}""");

        Assert.Equal(expected, options.MinStrokePx);
        Assert.Contains("minStrokePx", options.Warning);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(99, 10)]
    public void An_out_of_range_reversal_count_is_clamped_and_reported(int configured, int expected)
    {
        var options = SwitchGestureOptions.Parse(
            $$"""{"enabled":true,"requiredReversals":{{configured}}}""");

        Assert.Equal(expected, options.RequiredReversals);
        Assert.Contains("requiredReversals", options.Warning);
    }

    [Theory]
    [InlineData(10, 100)]
    [InlineData(60000, 5000)]
    public void An_out_of_range_window_is_clamped_and_reported(int configured, int expected)
    {
        var options = SwitchGestureOptions.Parse($$"""{"enabled":true,"windowMs":{{configured}}}""");

        Assert.Equal(expected, options.WindowMs);
        Assert.Contains("windowMs", options.Warning);
    }

    [Fact]
    public void Saved_settings_load_back_unchanged()
    {
        var path = Path.Combine(Path.GetTempPath(), $"blehid-gesture-{Guid.NewGuid():N}.json");
        var options = SwitchGestureOptions.ForTest(true, 150, 5, 1200);
        try
        {
            Assert.True(options.TrySave(path, out var error));
            Assert.Null(error);

            var reloaded = SwitchGestureOptions.Load(path);
            Assert.True(reloaded.Enabled);
            Assert.Equal(150, reloaded.MinStrokePx);
            Assert.Equal(5, reloaded.RequiredReversals);
            Assert.Equal(1200, reloaded.WindowMs);
            Assert.Null(reloaded.Warning);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Toggling_keeps_the_tuning_values(bool enabled)
    {
        var options = SwitchGestureOptions.ForTest(!enabled, 150, 5, 1200);

        var toggled = options.WithEnabled(enabled);

        Assert.Equal(enabled, toggled.Enabled);
        Assert.Equal(150, toggled.MinStrokePx);
        Assert.Equal(5, toggled.RequiredReversals);
        Assert.Equal(1200, toggled.WindowMs);
    }

    [Fact]
    public async Task A_toggle_written_to_disk_reaches_a_running_watcher()
    {
        var path = Path.Combine(Path.GetTempPath(), $"blehid-gesture-{Guid.NewGuid():N}.json");
        SwitchGestureOptions.ForTest(false, 80, 3, 800).TrySave(path, out _);
        try
        {
            using var watcher = new SwitchGestureWatcher(path);
            Assert.False(watcher.Current.Enabled);

            var seen = new TaskCompletionSource<SwitchGestureOptions>();
            watcher.Changed += options => seen.TrySetResult(options);

            SwitchGestureOptions.ForTest(true, 80, 3, 800).TrySave(path, out _);

            var finished = await Task.WhenAny(seen.Task, Task.Delay(TimeSpan.FromSeconds(10)));
            Assert.True(ReferenceEquals(finished, seen.Task), "the watcher never reported the change");
            Assert.True((await seen.Task).Enabled);
            Assert.True(watcher.Current.Enabled);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
