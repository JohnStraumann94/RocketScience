namespace Astro.App.Models;

/// <summary>
/// A single point on an animated trajectory.
/// </summary>
/// <param name="Time">
/// Mission elapsed time at this point, in seconds. Used to drive playback, so it must increase
/// monotonically along the path.
/// </param>
/// <param name="X">Horizontal world coordinate, in whatever unit <see cref="Trajectory.XAxisLabel"/> declares.</param>
/// <param name="Y">Vertical world coordinate, in whatever unit <see cref="Trajectory.YAxisLabel"/> declares.</param>
/// <param name="Heading">
/// Direction the vehicle is pointing, in degrees measured counter-clockwise from the +X axis.
/// The renderer rotates the rocket glyph by this angle. For a zero-angle-of-attack ascent this
/// is simply the flight path angle.
/// </param>
/// <param name="Powered">
/// True while the engine is burning. The renderer draws an exhaust plume only when this is set,
/// which makes the transition to coasting flight immediately obvious on screen.
/// </param>
/// <param name="Channels">
/// Telemetry values for this instant, in the same order as <see cref="Trajectory.Channels"/>.
/// Kept as a flat array rather than a dictionary because a trajectory can easily contain several
/// thousand points and this is read once per rendered frame.
/// </param>
public readonly record struct TrajectoryPoint(
    double Time,
    double X,
    double Y,
    double Heading,
    bool Powered,
    double[] Channels);

/// <summary>
/// Describes one telemetry channel shown in the heads-up display during playback.
/// </summary>
/// <param name="Name">Short label, e.g. "Speed".</param>
/// <param name="Units">Unit string appended to the value, e.g. "m/s". May be empty.</param>
/// <param name="Format">A .NET numeric format string, e.g. "F1".</param>
public sealed record TrajectoryChannel(string Name, string Units, string Format = "F1");

/// <summary>
/// A labelled event pinned to a specific point on the path, such as max-Q or burnout.
/// </summary>
/// <param name="Label">The text drawn next to the marker.</param>
/// <param name="PointIndex">Index into <see cref="Trajectory.Points"/>.</param>
/// <param name="ColorHex">Marker colour as an ARGB or RGB hex string, e.g. "#FFB347".</param>
public sealed record TrajectoryMarker(string Label, int PointIndex, string ColorHex = "#FFB347");

/// <summary>
/// A circular body drawn behind the path - a planet, a moon, or the curved ground line.
/// </summary>
/// <param name="Name">Label drawn at the centre of the body. May be empty.</param>
/// <param name="CentreX">Centre in world X units.</param>
/// <param name="CentreY">Centre in world Y units.</param>
/// <param name="Radius">Radius in world units.</param>
/// <param name="FillHex">Fill colour hex string.</param>
/// <param name="StrokeHex">Outline colour hex string.</param>
public sealed record TrajectoryBody(
    string Name,
    double CentreX,
    double CentreY,
    double Radius,
    string FillHex = "#16324A",
    string StrokeHex = "#3E7CB1");

/// <summary>
/// A complete, renderable and animatable flight path produced by a code sample.
/// </summary>
/// <remarks>
/// <para>
/// This type is the contract between the physics and the picture. A sample computes real
/// numbers using <c>Astro.Core</c>, packages them here in display units, and the
/// visualisation window knows how to draw and animate <i>any</i> instance without knowing
/// anything about rockets. That separation is why one animation window can serve a powered
/// ascent, a ballistic arc and an orbit transfer equally well.
/// </para>
/// <para>
/// Everything is in DISPLAY units by the time it reaches here - kilometres rather than metres,
/// for instance. Converting inside the sample keeps the axis labels honest and the renderer
/// completely unit-agnostic.
/// </para>
/// </remarks>
public sealed class Trajectory
{
    /// <summary>Title shown at the top of the visualisation window.</summary>
    public required string Title { get; init; }

    /// <summary>A one-line description of what is being flown, shown under the title.</summary>
    public required string Subtitle { get; init; }

    /// <summary>Horizontal axis label including units, e.g. "Downrange distance (km)".</summary>
    public required string XAxisLabel { get; init; }

    /// <summary>Vertical axis label including units, e.g. "Altitude (km)".</summary>
    public required string YAxisLabel { get; init; }

    /// <summary>The path itself, in time order.</summary>
    public required IReadOnlyList<TrajectoryPoint> Points { get; init; }

    /// <summary>Telemetry channel definitions matching <see cref="TrajectoryPoint.Channels"/>.</summary>
    public required IReadOnlyList<TrajectoryChannel> Channels { get; init; }

    /// <summary>Labelled events along the path. May be empty.</summary>
    public IReadOnlyList<TrajectoryMarker> Markers { get; init; } = [];

    /// <summary>Circular bodies drawn behind the path. May be empty.</summary>
    public IReadOnlyList<TrajectoryBody> Bodies { get; init; } = [];

    /// <summary>
    /// True when the X and Y axes must use the same scale.
    /// </summary>
    /// <remarks>
    /// This matters enormously. An orbit plotted with independent axis scales is an ellipse of
    /// the wrong shape and is actively misleading, so orbital and ballistic plots set this.
    /// A launch profile, on the other hand, spans hundreds of kilometres downrange against
    /// tens of kilometres of altitude; forcing equal axes would squash it into an invisible
    /// sliver, so those plots leave it false and label the axes clearly instead.
    /// </remarks>
    public bool EqualAspect { get; init; }

    /// <summary>
    /// How many seconds of wall-clock time one complete playback should take at 1x speed.
    /// </summary>
    /// <remarks>
    /// This is deliberately NOT the mission duration. A three-day lunar transfer and a
    /// two-minute ascent should both take about twelve seconds to watch; the on-screen clock
    /// reports the real mission time, and the speed multiplier is shown so nobody is misled.
    /// </remarks>
    public double PlaybackSeconds { get; init; } = 12.0;

    /// <summary>
    /// Optional explanatory text shown in the footer of the visualisation window - the place to
    /// record modelling assumptions so the picture cannot be over-interpreted.
    /// </summary>
    public string Notes { get; init; } = string.Empty;

    /// <summary>Label for the heads-up-display row that reports the vehicle's state.</summary>
    /// <remarks>
    /// Configurable because the same renderer serves two very different domains. A launch
    /// vehicle's interesting binary state is whether the engine is lit; a spacecraft on a
    /// transfer orbit is coasting the entire time and what matters is which phase of the
    /// mission it is in. Rather than hard-code rocket vocabulary into the drawing code, the
    /// sample supplies whatever words are true for it.
    /// </remarks>
    public string StateLabel { get; init; } = "Engine";

    /// <summary>Text shown when <see cref="TrajectoryPoint.Powered"/> is true.</summary>
    public string PoweredText { get; init; } = "BURNING";

    /// <summary>Text shown when <see cref="TrajectoryPoint.Powered"/> is false.</summary>
    public string CoastingText { get; init; } = "COAST";

    /// <summary>Mission elapsed time at the final point, in seconds.</summary>
    public double Duration => Points.Count == 0 ? 0.0 : Points[^1].Time;
}
