using Propulsion.App.Models;

namespace Propulsion.App.Views;

/// <summary>
/// Code-behind for the trajectory visualisation window: owns the frame timer that turns a
/// static list of trajectory points into a moving picture.
/// </summary>
/// <remarks>
/// <para>
/// <b>How the "video" works.</b> There is no video file and no media codec involved. An
/// <see cref="IDispatcherTimer"/> fires about sixty times a second on the UI thread; each tick
/// advances a playback clock, converts that clock into a fractional position along the path,
/// writes it to <see cref="TrajectoryDrawable.Progress"/> and calls
/// <c>GraphicsView.Invalidate()</c>. MAUI responds by asking the drawable to repaint, and the
/// eye does the rest. This is precisely how a game loop works, and it means the animation is
/// resolution independent, instantly scrubbable, and costs the project no new dependencies.
/// </para>
/// <para>
/// <b>Playback time versus mission time.</b> These are deliberately different. Playback always
/// takes <see cref="Trajectory.PlaybackSeconds"/> of wall-clock time at 1x regardless of how
/// long the mission actually lasts, so a two-minute ascent and a three-day transfer are both
/// watchable. The clock in the corner always reports true mission elapsed time, and the
/// compression ratio is printed next to it so the viewer is never misled.
/// </para>
/// </remarks>
public partial class TrajectoryPage : ContentPage
{
    /// <summary>Speed multipliers offered in the picker.</summary>
    private static readonly double[] SpeedOptions = [0.25, 0.5, 1.0, 2.0, 4.0, 8.0];

    private readonly Trajectory _trajectory;
    private readonly TrajectoryDrawable _drawable;
    private readonly IDispatcherTimer _timer;

    /// <summary>Playback position in seconds of animation time (not mission time).</summary>
    private double _playbackClock;

    /// <summary>Current speed multiplier, chosen from <see cref="SpeedOptions"/>.</summary>
    private double _speed = 1.0;

    /// <summary>True while the timer should be advancing the clock.</summary>
    private bool _isPlaying = true;

    /// <summary>
    /// Guards against the feedback loop where the timer moves the slider, the slider raises
    /// ValueChanged, and the handler moves the clock that the timer just set.
    /// </summary>
    private bool _updatingScrubber;

    /// <summary>Timestamp of the previous tick, used to measure the real frame interval.</summary>
    private DateTime _lastTick = DateTime.UtcNow;

    /// <summary>Creates and starts the visualisation window for one trajectory.</summary>
    /// <param name="trajectory">The computed flight path to animate.</param>
    public TrajectoryPage(Trajectory trajectory)
    {
        InitializeComponent();

        _trajectory = trajectory;
        _drawable = new TrajectoryDrawable(trajectory);

        Surface.Drawable = _drawable;

        TitleLabel.Text = trajectory.Title;
        SubtitleLabel.Text = trajectory.Subtitle;
        NotesLabel.Text = trajectory.Notes;

        // Populate the speed picker from the single source of truth above, so adding a speed
        // means editing one array.
        foreach (double option in SpeedOptions)
        {
            SpeedPicker.Items.Add($"{option:0.##}x");
        }

        SpeedPicker.SelectedIndex = Array.IndexOf(SpeedOptions, 1.0);

        // 16 ms is about 60 frames per second. The timer is a DISPATCHER timer, so every tick
        // already runs on the UI thread and may touch controls directly.
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.Tick += OnTick;

        UpdateClockLabel();
    }

    /// <summary>Starts the animation when the window is shown.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();

        _lastTick = DateTime.UtcNow;
        _timer.Start();
    }

    /// <summary>
    /// Stops the animation when the window closes.
    /// </summary>
    /// <remarks>
    /// Essential, not merely tidy: a dispatcher timer holds a strong reference to this page
    /// through its Tick handler, so a timer left running would keep the closed window and its
    /// entire trajectory alive for the lifetime of the process. Opening a dozen visualisations
    /// would then leak a dozen windows' worth of sample arrays.
    /// </remarks>
    protected override void OnDisappearing()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;

        base.OnDisappearing();
    }

    /// <summary>Advances the playback clock by the real elapsed time and repaints.</summary>
    /// <param name="sender">The timer.</param>
    /// <param name="e">Unused.</param>
    /// <remarks>
    /// The elapsed interval is measured rather than assumed to be exactly 16 ms, so the
    /// animation runs at the same real-world rate on a machine that cannot keep up with the
    /// requested frame rate. Dropping frames is acceptable; running in slow motion is not.
    /// </remarks>
    private void OnTick(object? sender, EventArgs e)
    {
        DateTime now = DateTime.UtcNow;
        double deltaSeconds = (now - _lastTick).TotalSeconds;
        _lastTick = now;

        if (!_isPlaying)
        {
            return;
        }

        _playbackClock += deltaSeconds * _speed;

        double total = Math.Max(0.1, _trajectory.PlaybackSeconds);

        if (_playbackClock >= total)
        {
            // Hold on the final frame rather than looping. The end state (burnout conditions,
            // impact point, arrival) is usually the whole point, and a loop would snatch it
            // away after a second. Restart is one click.
            _playbackClock = total;
            _isPlaying = false;
            PlayPauseButton.Text = "Replay";
        }

        SetProgress(_playbackClock / total, updateSlider: true);
    }

    /// <summary>
    /// Applies a new playback position to the drawable, the slider and the clock label.
    /// </summary>
    /// <param name="progress">Fractional position along the path, 0 to 1.</param>
    /// <param name="updateSlider">
    /// False when the change originated from the slider itself, to avoid re-entrancy.
    /// </param>
    private void SetProgress(double progress, bool updateSlider)
    {
        progress = Math.Clamp(progress, 0.0, 1.0);

        _drawable.Progress = progress;

        if (updateSlider)
        {
            _updatingScrubber = true;
            ScrubSlider.Value = progress;
            _updatingScrubber = false;
        }

        UpdateClockLabel();

        // The only call in the whole class that actually produces a frame.
        Surface.Invalidate();
    }

    /// <summary>Refreshes the mission clock and the time-compression readout.</summary>
    private void UpdateClockLabel()
    {
        double missionSeconds = _trajectory.Points.Count == 0
            ? 0.0
            : _trajectory.Points[_drawable.CurrentIndex].Time;

        // How much faster than real life the viewer is watching. A 165 s ascent compressed
        // into 12 s of playback at 2x is running at about 27x real time.
        double compression = _trajectory.PlaybackSeconds <= 0.0
            ? 1.0
            : _trajectory.Duration / _trajectory.PlaybackSeconds * _speed;

        TimeSpan span = TimeSpan.FromSeconds(missionSeconds);

        string clock = span.TotalHours >= 1.0
            ? $"T+{(int)span.TotalHours}:{span.Minutes:D2}:{span.Seconds:D2}"
            : $"T+{span.Minutes:D2}:{span.Seconds:D2}";

        ClockLabel.Text = $"{clock}  ({compression:0.#}x real)";
    }

    /// <summary>Toggles between playing and paused, restarting from zero if already finished.</summary>
    /// <param name="sender">The button.</param>
    /// <param name="e">Unused.</param>
    private void OnPlayPauseClicked(object? sender, EventArgs e)
    {
        // If the clock is sitting at the end, the natural meaning of pressing the button is
        // "play it again" rather than "resume from the last frame".
        if (_playbackClock >= _trajectory.PlaybackSeconds)
        {
            _playbackClock = 0.0;
        }

        _isPlaying = !_isPlaying;
        PlayPauseButton.Text = _isPlaying ? "Pause" : "Play";
        _lastTick = DateTime.UtcNow;
    }

    /// <summary>Rewinds to liftoff and plays.</summary>
    /// <param name="sender">The button.</param>
    /// <param name="e">Unused.</param>
    private void OnRestartClicked(object? sender, EventArgs e)
    {
        _playbackClock = 0.0;
        _isPlaying = true;
        PlayPauseButton.Text = "Pause";
        _lastTick = DateTime.UtcNow;

        SetProgress(0.0, updateSlider: true);
    }

    /// <summary>Shows or hides the dashed preview of the path still to come.</summary>
    /// <param name="sender">The button.</param>
    /// <param name="e">Unused.</param>
    /// <remarks>
    /// Worth having both ways: the ghost path makes the shape of the whole trajectory clear
    /// immediately, while hiding it makes the flight feel genuinely unfolding.
    /// </remarks>
    private void OnTogglePathClicked(object? sender, EventArgs e)
    {
        _drawable.ShowFuturePath = !_drawable.ShowFuturePath;

        PathToggleButton.Text = _drawable.ShowFuturePath
            ? "Hide path ahead"
            : "Show path ahead";

        Surface.Invalidate();
    }

    /// <summary>Applies the selected speed multiplier.</summary>
    /// <param name="sender">The picker.</param>
    /// <param name="e">Unused.</param>
    private void OnSpeedChanged(object? sender, EventArgs e)
    {
        int index = SpeedPicker.SelectedIndex;

        if (index >= 0 && index < SpeedOptions.Length)
        {
            _speed = SpeedOptions[index];
            UpdateClockLabel();
        }
    }

    /// <summary>Pauses playback as soon as the user grabs the scrub bar.</summary>
    /// <param name="sender">The slider.</param>
    /// <param name="e">Unused.</param>
    private void OnScrubDragStarted(object? sender, EventArgs e)
    {
        _isPlaying = false;
        PlayPauseButton.Text = "Play";
    }

    /// <summary>Seeks to the scrubbed position.</summary>
    /// <param name="sender">The slider.</param>
    /// <param name="e">Carries the new value.</param>
    private void OnScrubValueChanged(object? sender, ValueChangedEventArgs e)
    {
        if (_updatingScrubber)
        {
            // This change came from the timer, not the user; acting on it would fight the
            // animation for control of the clock.
            return;
        }

        _playbackClock = e.NewValue * _trajectory.PlaybackSeconds;

        SetProgress(e.NewValue, updateSlider: false);
    }
}
