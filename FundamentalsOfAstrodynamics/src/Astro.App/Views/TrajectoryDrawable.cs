using Astro.App.Models;

namespace Astro.App.Views;

/// <summary>
/// Renders a <see cref="Trajectory"/> onto a MAUI <c>GraphicsView</c>, including the animated
/// vehicle glyph, the flown-so-far path, event markers and a telemetry heads-up display.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately written against <c>Microsoft.Maui.Graphics</c> only - no charting package, no
/// media file, no video encoder. The "video" the user sees is produced the same way any game
/// produces one: a timer advances <see cref="Progress"/>, the view is invalidated, and this
/// method redraws the whole scene from scratch at the new time. That keeps the app's dependency
/// list unchanged and makes every pixel on screen traceable to a line of code here.
/// </para>
/// <para>
/// <b>Coordinate systems.</b> There are two. WORLD coordinates are the physical values in the
/// trajectory (kilometres of altitude and downrange, say), with Y increasing upwards. SCREEN
/// coordinates are pixels, with Y increasing DOWNWARDS, which is why every mapping flips the
/// vertical axis. <see cref="ToScreenX"/> and <see cref="ToScreenY"/> are the only places that
/// conversion happens.
/// </para>
/// </remarks>
public sealed class TrajectoryDrawable : IDrawable
{
    // ---- Palette -----------------------------------------------------------------------
    // Matched to the main window's dark theme so the visualisation looks like part of the
    // same application rather than a chart pasted into it.
    private static readonly Color Background = Color.FromArgb("#0C1017");
    private static readonly Color PanelFill = Color.FromArgb("#141A24");
    private static readonly Color GridLine = Color.FromArgb("#1E2733");
    private static readonly Color AxisLine = Color.FromArgb("#3A4757");
    private static readonly Color AxisText = Color.FromArgb("#8A98A8");
    private static readonly Color PathGhost = Color.FromArgb("#2A3646");
    private static readonly Color PathPowered = Color.FromArgb("#FF8A3D");
    private static readonly Color PathCoasting = Color.FromArgb("#4FC3F7");
    private static readonly Color HudText = Color.FromArgb("#E6EDF3");
    private static readonly Color HudLabel = Color.FromArgb("#8A98A8");
    private static readonly Color Accent = Color.FromArgb("#7BD88F");

    /// <summary>Margins reserved for the axis labels and tick text, in pixels.</summary>
    private const float MarginLeft = 74f;
    private const float MarginRight = 24f;
    private const float MarginTop = 22f;
    private const float MarginBottom = 54f;

    private readonly Trajectory _trajectory;

    // World-space bounding box of everything that must be visible, computed once in the
    // constructor. Recomputing it per frame would make the view drift as the rocket moves,
    // which is disorienting; a fixed frame lets the eye track the vehicle against the scene.
    private readonly double _minX;
    private readonly double _maxX;
    private readonly double _minY;
    private readonly double _maxY;

    /// <summary>Creates a drawable for one trajectory.</summary>
    /// <param name="trajectory">The path to render. Must contain at least one point.</param>
    public TrajectoryDrawable(Trajectory trajectory)
    {
        _trajectory = trajectory;

        double minX = double.MaxValue, maxX = double.MinValue;
        double minY = double.MaxValue, maxY = double.MinValue;

        foreach (TrajectoryPoint point in trajectory.Points)
        {
            minX = Math.Min(minX, point.X);
            maxX = Math.Max(maxX, point.X);
            minY = Math.Min(minY, point.Y);
            maxY = Math.Max(maxY, point.Y);
        }

        // Bodies must fit in frame too, otherwise an orbit would be drawn around an Earth we
        // could only see a sliver of.
        foreach (TrajectoryBody body in trajectory.Bodies)
        {
            minX = Math.Min(minX, body.CentreX - body.Radius);
            maxX = Math.Max(maxX, body.CentreX + body.Radius);
            minY = Math.Min(minY, body.CentreY - body.Radius);
            maxY = Math.Max(maxY, body.CentreY + body.Radius);
        }

        if (minX > maxX)
        {
            // Defensive: an empty trajectory still has to produce a drawable frame.
            minX = 0.0;
            maxX = 1.0;
            minY = 0.0;
            maxY = 1.0;
        }

        // A degenerate axis (every value identical, e.g. a purely vertical ascent where
        // downrange is always zero) would give a zero-width window and divide by zero.
        if (maxX - minX < 1.0e-9)
        {
            double centre = (maxX + minX) / 2.0;
            minX = centre - 1.0;
            maxX = centre + 1.0;
        }

        if (maxY - minY < 1.0e-9)
        {
            double centre = (maxY + minY) / 2.0;
            minY = centre - 1.0;
            maxY = centre + 1.0;
        }

        // A 6% breathing space stops the path from touching the frame edge and gives the
        // marker labels somewhere to live.
        double padX = (maxX - minX) * 0.06;
        double padY = (maxY - minY) * 0.06;

        _minX = minX - padX;
        _maxX = maxX + padX;
        _minY = minY - padY;
        _maxY = maxY + padY;
    }

    /// <summary>
    /// Playback position as a fraction of the path, from 0 (liftoff) to 1 (final point).
    /// </summary>
    /// <remarks>
    /// The animation timer writes this and then invalidates the view. It is a plain property
    /// rather than a bindable one because it changes up to sixty times a second and binding
    /// machinery would be pure overhead.
    /// </remarks>
    public double Progress { get; set; }

    /// <summary>Whether to draw the full path faintly ahead of the vehicle.</summary>
    public bool ShowFuturePath { get; set; } = true;

    /// <summary>Whether to draw the telemetry heads-up display.</summary>
    public bool ShowTelemetry { get; set; } = true;

    /// <summary>The index into the trajectory currently being displayed.</summary>
    public int CurrentIndex
    {
        get
        {
            if (_trajectory.Points.Count == 0)
            {
                return 0;
            }

            int index = (int)Math.Round(Progress * (_trajectory.Points.Count - 1));

            return Math.Clamp(index, 0, _trajectory.Points.Count - 1);
        }
    }

    // Plot rectangle in screen pixels, recomputed each frame because the window is resizable.
    private float _plotLeft;
    private float _plotTop;
    private float _plotWidth;
    private float _plotHeight;

    // World units per pixel, per axis.
    private double _scaleX;
    private double _scaleY;

    /// <summary>Draws one complete frame of the animation.</summary>
    /// <param name="canvas">The canvas supplied by the <c>GraphicsView</c>.</param>
    /// <param name="dirtyRect">The region to paint; always the whole control here.</param>
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.Antialias = true;

        canvas.FillColor = Background;
        canvas.FillRectangle(dirtyRect);

        if (_trajectory.Points.Count == 0)
        {
            return;
        }

        // ---- Establish the plot rectangle and the world-to-screen scale ------------------
        _plotLeft = dirtyRect.Left + MarginLeft;
        _plotTop = dirtyRect.Top + MarginTop;
        _plotWidth = Math.Max(10f, dirtyRect.Width - MarginLeft - MarginRight);
        _plotHeight = Math.Max(10f, dirtyRect.Height - MarginTop - MarginBottom);

        _scaleX = _plotWidth / (_maxX - _minX);
        _scaleY = _plotHeight / (_maxY - _minY);

        if (_trajectory.EqualAspect)
        {
            // Use the more restrictive of the two scales for both axes, so circles are round
            // and ellipses have their true shape. The plot is then centred on the leftover
            // space rather than being pinned to a corner.
            double uniform = Math.Min(_scaleX, _scaleY);
            _scaleX = uniform;
            _scaleY = uniform;

            float usedWidth = (float)((_maxX - _minX) * uniform);
            float usedHeight = (float)((_maxY - _minY) * uniform);

            _plotLeft += (_plotWidth - usedWidth) / 2f;
            _plotTop += (_plotHeight - usedHeight) / 2f;
            _plotWidth = usedWidth;
            _plotHeight = usedHeight;
        }

        DrawGridAndAxes(canvas);
        DrawBodies(canvas);
        DrawPath(canvas);
        DrawMarkers(canvas);
        DrawVehicle(canvas);

        if (ShowTelemetry)
        {
            DrawTelemetry(canvas, dirtyRect);
        }
    }

    // ---- Coordinate mapping ------------------------------------------------------------

    /// <summary>Converts a world X value to a screen X pixel.</summary>
    /// <param name="worldX">World horizontal coordinate.</param>
    /// <returns>Pixel position.</returns>
    private float ToScreenX(double worldX) => (float)(_plotLeft + (worldX - _minX) * _scaleX);

    /// <summary>Converts a world Y value to a screen Y pixel, flipping the axis.</summary>
    /// <param name="worldY">World vertical coordinate.</param>
    /// <returns>Pixel position.</returns>
    private float ToScreenY(double worldY) =>
        (float)(_plotTop + _plotHeight - (worldY - _minY) * _scaleY);

    // ---- Scene elements ------------------------------------------------------------------

    /// <summary>Draws the grid, the axis lines, the tick labels and the axis titles.</summary>
    /// <param name="canvas">Target canvas.</param>
    private void DrawGridAndAxes(ICanvas canvas)
    {
        canvas.FontSize = 11;

        double stepX = NiceStep(_maxX - _minX);
        double stepY = NiceStep(_maxY - _minY);

        canvas.StrokeSize = 1;

        // Vertical grid lines and their labels along the bottom.
        for (double value = Math.Ceiling(_minX / stepX) * stepX; value <= _maxX; value += stepX)
        {
            float px = ToScreenX(value);

            canvas.StrokeColor = GridLine;
            canvas.DrawLine(px, _plotTop, px, _plotTop + _plotHeight);

            canvas.FontColor = AxisText;
            canvas.DrawString(
                FormatTick(value, stepX),
                px - 40f,
                _plotTop + _plotHeight + 6f,
                80f,
                14f,
                Microsoft.Maui.Graphics.HorizontalAlignment.Center,
                Microsoft.Maui.Graphics.VerticalAlignment.Top);
        }

        // Horizontal grid lines and their labels down the left.
        for (double value = Math.Ceiling(_minY / stepY) * stepY; value <= _maxY; value += stepY)
        {
            float py = ToScreenY(value);

            canvas.StrokeColor = GridLine;
            canvas.DrawLine(_plotLeft, py, _plotLeft + _plotWidth, py);

            canvas.FontColor = AxisText;
            canvas.DrawString(
                FormatTick(value, stepY),
                _plotLeft - 70f,
                py - 7f,
                64f,
                14f,
                Microsoft.Maui.Graphics.HorizontalAlignment.Right,
                Microsoft.Maui.Graphics.VerticalAlignment.Center);
        }

        // The plot frame.
        canvas.StrokeColor = AxisLine;
        canvas.StrokeSize = 1.5f;
        canvas.DrawRectangle(_plotLeft, _plotTop, _plotWidth, _plotHeight);

        // Axis titles.
        canvas.FontSize = 12;
        canvas.FontColor = AxisText;

        canvas.DrawString(
            _trajectory.XAxisLabel,
            _plotLeft,
            _plotTop + _plotHeight + 26f,
            _plotWidth,
            18f,
            Microsoft.Maui.Graphics.HorizontalAlignment.Center,
            Microsoft.Maui.Graphics.VerticalAlignment.Top);

        // The Y title is rotated to run up the left edge, which is why the canvas state is
        // saved: Rotate and Translate are cumulative transforms on the canvas itself.
        canvas.SaveState();
        canvas.Translate(14f, _plotTop + _plotHeight / 2f);
        canvas.Rotate(-90f);
        canvas.DrawString(
            _trajectory.YAxisLabel,
            -110f,
            -9f,
            220f,
            18f,
            Microsoft.Maui.Graphics.HorizontalAlignment.Center,
            Microsoft.Maui.Graphics.VerticalAlignment.Center);
        canvas.RestoreState();
    }

    /// <summary>Draws planets, moons or the curved ground line behind the path.</summary>
    /// <param name="canvas">Target canvas.</param>
    private void DrawBodies(ICanvas canvas)
    {
        foreach (TrajectoryBody body in _trajectory.Bodies)
        {
            float cx = ToScreenX(body.CentreX);
            float cy = ToScreenY(body.CentreY);

            // Equal-aspect plots have identical scales, so a single radius is correct. For
            // non-equal-aspect plots we take the X scale and accept an oval; in practice only
            // orbital plots use bodies, and those always set EqualAspect.
            float radius = (float)(body.Radius * _scaleX);

            canvas.FillColor = Color.FromArgb(body.FillHex);
            canvas.FillCircle(cx, cy, radius);

            canvas.StrokeColor = Color.FromArgb(body.StrokeHex);
            canvas.StrokeSize = 1.5f;
            canvas.DrawCircle(cx, cy, radius);

            if (!string.IsNullOrEmpty(body.Name))
            {
                canvas.FontSize = 12;
                canvas.FontColor = Color.FromArgb(body.StrokeHex);
                canvas.DrawString(
                    body.Name,
                    cx - 60f,
                    cy - 8f,
                    120f,
                    16f,
                    Microsoft.Maui.Graphics.HorizontalAlignment.Center,
                    Microsoft.Maui.Graphics.VerticalAlignment.Center);
            }
        }
    }

    /// <summary>
    /// Draws the ghosted full path and then the brightly coloured flown portion.
    /// </summary>
    /// <param name="canvas">Target canvas.</param>
    /// <remarks>
    /// The flown portion is drawn segment by segment rather than as one path so that each
    /// segment can take its colour from whether the engine was burning at that moment. The
    /// result is that the orange powered arc and the blue ballistic arc are separated visually
    /// with no extra annotation needed.
    /// </remarks>
    private void DrawPath(ICanvas canvas)
    {
        IReadOnlyList<TrajectoryPoint> points = _trajectory.Points;

        if (ShowFuturePath)
        {
            PathF ghost = new();
            ghost.MoveTo(ToScreenX(points[0].X), ToScreenY(points[0].Y));

            for (int i = 1; i < points.Count; i++)
            {
                ghost.LineTo(ToScreenX(points[i].X), ToScreenY(points[i].Y));
            }

            canvas.StrokeColor = PathGhost;
            canvas.StrokeSize = 2f;
            canvas.StrokeDashPattern = [4f, 4f];
            canvas.DrawPath(ghost);
            canvas.StrokeDashPattern = null;
        }

        int current = CurrentIndex;

        canvas.StrokeSize = 3f;
        canvas.StrokeLineCap = LineCap.Round;

        for (int i = 1; i <= current; i++)
        {
            canvas.StrokeColor = points[i].Powered ? PathPowered : PathCoasting;
            canvas.DrawLine(
                ToScreenX(points[i - 1].X),
                ToScreenY(points[i - 1].Y),
                ToScreenX(points[i].X),
                ToScreenY(points[i].Y));
        }
    }

    /// <summary>Draws event markers, but only once the vehicle has actually reached them.</summary>
    /// <param name="canvas">Target canvas.</param>
    private void DrawMarkers(ICanvas canvas)
    {
        int current = CurrentIndex;

        canvas.FontSize = 11;

        foreach (TrajectoryMarker marker in _trajectory.Markers)
        {
            if (marker.PointIndex > current || marker.PointIndex >= _trajectory.Points.Count)
            {
                // Revealing markers as they happen turns the plot into a narrative rather than
                // a spoiler.
                continue;
            }

            TrajectoryPoint point = _trajectory.Points[marker.PointIndex];
            float px = ToScreenX(point.X);
            float py = ToScreenY(point.Y);

            Color color = Color.FromArgb(marker.ColorHex);

            canvas.StrokeColor = color;
            canvas.StrokeSize = 1.5f;
            canvas.DrawCircle(px, py, 5f);

            canvas.FillColor = color;
            canvas.FillCircle(px, py, 2.5f);

            // A short leader line keeps the text clear of the path itself.
            canvas.DrawLine(px, py - 6f, px, py - 18f);

            canvas.FontColor = color;
            canvas.DrawString(
                marker.Label,
                px - 80f,
                py - 34f,
                160f,
                14f,
                Microsoft.Maui.Graphics.HorizontalAlignment.Center,
                Microsoft.Maui.Graphics.VerticalAlignment.Center);
        }
    }

    /// <summary>Draws the rocket glyph, rotated along the flight path, with its exhaust plume.</summary>
    /// <param name="canvas">Target canvas.</param>
    private void DrawVehicle(ICanvas canvas)
    {
        TrajectoryPoint point = _trajectory.Points[CurrentIndex];

        float px = ToScreenX(point.X);
        float py = ToScreenY(point.Y);

        canvas.SaveState();

        // Move the origin to the vehicle, then rotate. Screen Y points down while the heading
        // is measured in the conventional counter-clockwise sense, so the angle is negated.
        canvas.Translate(px, py);
        canvas.Rotate(-(float)point.Heading);

        // ---- Exhaust plume, drawn first so the body sits on top of it -------------------
        if (point.Powered)
        {
            // A cheap flicker keyed to the sample index makes the plume look alive without
            // needing a particle system or any per-frame state.
            float flicker = 1.0f + 0.25f * (float)Math.Sin(CurrentIndex * 0.9);

            PathF plume = new();
            plume.MoveTo(-7f, -4f);
            plume.LineTo(-7f, 4f);
            plume.LineTo(-7f - 22f * flicker, 0f);
            plume.Close();

            canvas.FillColor = Color.FromArgb("#FF6A00").WithAlpha(0.85f);
            canvas.FillPath(plume);

            PathF core = new();
            core.MoveTo(-7f, -2f);
            core.LineTo(-7f, 2f);
            core.LineTo(-7f - 13f * flicker, 0f);
            core.Close();

            canvas.FillColor = Color.FromArgb("#FFE082");
            canvas.FillPath(core);
        }

        // ---- Body: a simple pointed fuselage with two fins ------------------------------
        PathF hull = new();
        hull.MoveTo(14f, 0f);      // nose
        hull.LineTo(2f, -5f);
        hull.LineTo(-7f, -5f);
        hull.LineTo(-7f, 5f);
        hull.LineTo(2f, 5f);
        hull.Close();

        canvas.FillColor = Color.FromArgb("#E6EDF3");
        canvas.FillPath(hull);

        canvas.StrokeColor = Color.FromArgb("#9AA7B4");
        canvas.StrokeSize = 1f;
        canvas.DrawPath(hull);

        PathF fins = new();
        fins.MoveTo(-2f, -5f);
        fins.LineTo(-9f, -11f);
        fins.LineTo(-7f, -5f);
        fins.Close();
        fins.MoveTo(-2f, 5f);
        fins.LineTo(-9f, 11f);
        fins.LineTo(-7f, 5f);
        fins.Close();

        canvas.FillColor = Color.FromArgb("#FF8A3D");
        canvas.FillPath(fins);

        canvas.RestoreState();

        // A halo, drawn unrotated, makes the vehicle easy to find on a busy plot.
        canvas.StrokeColor = Accent.WithAlpha(0.35f);
        canvas.StrokeSize = 1.5f;
        canvas.DrawCircle(px, py, 19f);
    }

    /// <summary>Draws the translucent telemetry panel in the top-right corner.</summary>
    /// <param name="canvas">Target canvas.</param>
    /// <param name="bounds">The full control bounds.</param>
    private void DrawTelemetry(ICanvas canvas, RectF bounds)
    {
        TrajectoryPoint point = _trajectory.Points[CurrentIndex];

        const float panelWidth = 232f;
        float rowHeight = 19f;
        int rowCount = _trajectory.Channels.Count + 2;
        float panelHeight = 16f + rowCount * rowHeight;

        float left = bounds.Right - panelWidth - 34f;
        float top = bounds.Top + 34f;

        canvas.FillColor = PanelFill.WithAlpha(0.92f);
        canvas.StrokeColor = AxisLine;
        canvas.StrokeSize = 1f;
        canvas.FillRoundedRectangle(left, top, panelWidth, panelHeight, 8f);
        canvas.DrawRoundedRectangle(left, top, panelWidth, panelHeight, 8f);

        float y = top + 8f;

        // Mission elapsed time always comes first: it is the anchor for everything else.
        DrawRow(canvas, left, ref y, rowHeight, "T+", FormatTime(point.Time), string.Empty, HudText);

        // Vehicle state, colour-coded to match the path colours. The words come from the
        // trajectory itself so an orbital transfer can say "CRUISE" where a launcher says
        // "BURNING".
        DrawRow(
            canvas,
            left,
            ref y,
            rowHeight,
            _trajectory.StateLabel,
            point.Powered ? _trajectory.PoweredText : _trajectory.CoastingText,
            string.Empty,
            point.Powered ? PathPowered : PathCoasting);

        for (int i = 0; i < _trajectory.Channels.Count; i++)
        {
            TrajectoryChannel channel = _trajectory.Channels[i];

            double value = i < point.Channels.Length ? point.Channels[i] : 0.0;

            DrawRow(
                canvas,
                left,
                ref y,
                rowHeight,
                channel.Name,
                value.ToString(channel.Format),
                channel.Units,
                HudText);
        }
    }

    /// <summary>Draws one label/value row of the telemetry panel and advances the cursor.</summary>
    /// <param name="canvas">Target canvas.</param>
    /// <param name="left">Left edge of the panel.</param>
    /// <param name="y">Current vertical cursor; advanced by <paramref name="rowHeight"/>.</param>
    /// <param name="rowHeight">Row pitch in pixels.</param>
    /// <param name="label">Left-hand label.</param>
    /// <param name="value">Right-hand value.</param>
    /// <param name="units">Unit suffix, may be empty.</param>
    /// <param name="valueColor">Colour for the value text.</param>
    private static void DrawRow(
        ICanvas canvas,
        float left,
        ref float y,
        float rowHeight,
        string label,
        string value,
        string units,
        Color valueColor)
    {
        canvas.FontSize = 11;
        canvas.FontColor = HudLabel;
        canvas.DrawString(
            label,
            left + 12f,
            y,
            96f,
            rowHeight,
            Microsoft.Maui.Graphics.HorizontalAlignment.Left,
            Microsoft.Maui.Graphics.VerticalAlignment.Center);

        canvas.FontColor = valueColor;
        canvas.DrawString(
            string.IsNullOrEmpty(units) ? value : $"{value} {units}",
            left + 108f,
            y,
            112f,
            rowHeight,
            Microsoft.Maui.Graphics.HorizontalAlignment.Right,
            Microsoft.Maui.Graphics.VerticalAlignment.Center);

        y += rowHeight;
    }

    // ---- Formatting helpers ---------------------------------------------------------------

    /// <summary>
    /// Chooses a "nice" axis tick interval - 1, 2 or 5 times a power of ten - covering the
    /// given range in roughly six steps.
    /// </summary>
    /// <param name="range">The full span of the axis in world units.</param>
    /// <returns>The tick interval.</returns>
    /// <remarks>
    /// Without this, ticks land on values like 3.7194 and the axis becomes unreadable. The
    /// 1/2/5 sequence is the standard choice because those are the intervals humans can
    /// subdivide mentally.
    /// </remarks>
    private static double NiceStep(double range)
    {
        if (range <= 0.0 || !double.IsFinite(range))
        {
            return 1.0;
        }

        double rough = range / 6.0;
        double magnitude = Math.Pow(10.0, Math.Floor(Math.Log10(rough)));
        double normalised = rough / magnitude;

        double nice = normalised switch
        {
            <= 1.0 => 1.0,
            <= 2.0 => 2.0,
            <= 5.0 => 5.0,
            _ => 10.0
        };

        return nice * magnitude;
    }

    /// <summary>Formats an axis tick with just enough decimal places for the chosen step.</summary>
    /// <param name="value">The tick value.</param>
    /// <param name="step">The tick interval.</param>
    /// <returns>The formatted label.</returns>
    private static string FormatTick(double value, double step)
    {
        // Snap values that are within a millionth of a step to zero, so floating-point noise
        // never produces a "-0.0" or "1.9999999" label.
        if (Math.Abs(value) < step * 1.0e-6)
        {
            value = 0.0;
        }

        int decimals = step switch
        {
            >= 100.0 => 0,
            >= 10.0 => 0,
            >= 1.0 => 1,
            >= 0.1 => 2,
            >= 0.01 => 3,
            _ => 4
        };

        return value.ToString($"F{decimals}");
    }

    /// <summary>Formats mission elapsed time as h:mm:ss, dropping the hours when zero.</summary>
    /// <param name="seconds">Elapsed seconds.</param>
    /// <returns>A compact time string.</returns>
    private static string FormatTime(double seconds)
    {
        TimeSpan span = TimeSpan.FromSeconds(seconds);

        return span.TotalHours >= 1.0
            ? $"{(int)span.TotalHours}:{span.Minutes:D2}:{span.Seconds:D2}"
            : $"{span.Minutes:D2}:{span.Seconds:D2}.{span.Milliseconds / 100}";
    }
}
