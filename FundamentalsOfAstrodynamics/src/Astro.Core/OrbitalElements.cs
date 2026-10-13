namespace Astro.Core;

/// <summary>
/// Classification of an orbit by the shape of its conic section.
/// </summary>
public enum OrbitType
{
    /// <summary>e = 0. A circle; energy is minimal for the given angular momentum.</summary>
    Circular,

    /// <summary>0 &lt; e &lt; 1. A closed, repeating orbit.</summary>
    Elliptical,

    /// <summary>e = 1. The exact escape trajectory; a is infinite.</summary>
    Parabolic,

    /// <summary>e &gt; 1. An escape trajectory with energy to spare.</summary>
    Hyperbolic
}

/// <summary>
/// The six <b>classical (Keplerian) orbital elements</b> plus a few convenience
/// quantities. This is Chapter 2 of BMW: the description of an orbit as a fixed geometric
/// shape in space rather than as an instantaneous position and velocity.
/// </summary>
/// <remarks>
/// <para>
/// A state vector (r, v) is six numbers that change every instant. The classical elements
/// are also six numbers, but five of them are <i>constant</i> under two-body motion and
/// only the sixth (the anomaly) advances with time. That is why every catalogue of
/// satellites, from TLEs to planetary ephemerides, is published in elements.
/// </para>
/// <para>The six, and what each one does:</para>
/// <list type="table">
///   <listheader><term>Element</term><description>Role</description></listheader>
///   <item><term>a — semi-major axis</term><description>SIZE of the orbit (and hence energy and period)</description></item>
///   <item><term>e — eccentricity</term><description>SHAPE: how far from circular</description></item>
///   <item><term>i — inclination</term><description>TILT of the orbit plane from the equator</description></item>
///   <item><term>Omega — right ascension of the ascending node (RAAN)</term><description>SWIVEL of that plane about the polar axis</description></item>
///   <item><term>omega — argument of periapsis</term><description>ROTATION of the ellipse within its own plane</description></item>
///   <item><term>nu — true anomaly</term><description>WHERE the satellite is right now along the ellipse</description></item>
/// </list>
/// <para>
/// <b>All angles in this type are stored in radians.</b> Degree-valued twins are exposed as
/// <c>...Deg</c> properties purely for display in the UI.
/// </para>
/// </remarks>
/// <param name="SemiMajorAxisKm">a — semi-major axis [km]; negative for hyperbolas, infinite for parabolas.</param>
/// <param name="Eccentricity">e — eccentricity [dimensionless, &gt;= 0].</param>
/// <param name="InclinationRad">i — inclination [rad], always in [0, pi].</param>
/// <param name="RaanRad">Omega — right ascension of the ascending node [rad], in [0, 2pi).</param>
/// <param name="ArgumentOfPeriapsisRad">omega — argument of periapsis [rad], in [0, 2pi).</param>
/// <param name="TrueAnomalyRad">nu — true anomaly [rad], in [0, 2pi).</param>
public readonly record struct OrbitalElements(
    double SemiMajorAxisKm,
    double Eccentricity,
    double InclinationRad,
    double RaanRad,
    double ArgumentOfPeriapsisRad,
    double TrueAnomalyRad)
{
    /// <summary>i — inclination in degrees, for display.</summary>
    public double InclinationDeg => InclinationRad * Constants.RadToDeg;

    /// <summary>Omega — RAAN in degrees, for display.</summary>
    public double RaanDeg => RaanRad * Constants.RadToDeg;

    /// <summary>omega — argument of periapsis in degrees, for display.</summary>
    public double ArgumentOfPeriapsisDeg => ArgumentOfPeriapsisRad * Constants.RadToDeg;

    /// <summary>nu — true anomaly in degrees, for display.</summary>
    public double TrueAnomalyDeg => TrueAnomalyRad * Constants.RadToDeg;

    /// <summary>
    /// The conic type implied by the eccentricity. The tolerances are deliberately loose
    /// (1e-8 for circular, 1e-10 for parabolic) because a "perfectly" circular or parabolic
    /// orbit never survives floating-point arithmetic.
    /// </summary>
    public OrbitType Type => Eccentricity switch
    {
        < 1e-8 => OrbitType.Circular,
        < 1.0 - 1e-10 => OrbitType.Elliptical,
        <= 1.0 + 1e-10 => OrbitType.Parabolic,
        _ => OrbitType.Hyperbolic
    };

    /// <summary>
    /// p — semi-latus rectum [km], the radius at nu = 90 degrees. Computed as a(1 − e^2),
    /// which stays valid (and positive) for hyperbolas because a is then negative.
    /// </summary>
    public double SemiLatusRectumKm => SemiMajorAxisKm * (1.0 - Eccentricity * Eccentricity);

    /// <summary>r_p — periapsis radius [km].</summary>
    public double PeriapsisRadiusKm => SemiMajorAxisKm * (1.0 - Eccentricity);

    /// <summary>
    /// r_a — apoapsis radius [km], or <see cref="double.PositiveInfinity"/> for an open orbit
    /// that never comes back.
    /// </summary>
    public double ApoapsisRadiusKm => Eccentricity < 1.0
        ? SemiMajorAxisKm * (1.0 + Eccentricity)
        : double.PositiveInfinity;

    /// <summary>
    /// Creates a copy of these elements with the true anomaly replaced — useful when
    /// sweeping a satellite around a fixed orbit for plotting.
    /// </summary>
    /// <param name="trueAnomalyRad">The new true anomaly [rad].</param>
    public OrbitalElements AtTrueAnomaly(double trueAnomalyRad)
        => this with { TrueAnomalyRad = Angles.Normalize(trueAnomalyRad) };

    /// <summary>A multi-line, human-readable dump of all six elements plus derived geometry.</summary>
    public override string ToString() =>
        $"""
         a  (semi-major axis)      = {SemiMajorAxisKm,14:F3} km
         e  (eccentricity)         = {Eccentricity,14:F7}
         i  (inclination)          = {InclinationDeg,14:F5} deg
         Om (RAAN)                 = {RaanDeg,14:F5} deg
         w  (arg. of periapsis)    = {ArgumentOfPeriapsisDeg,14:F5} deg
         nu (true anomaly)         = {TrueAnomalyDeg,14:F5} deg
         --------------------------------------------------
         p  (semi-latus rectum)    = {SemiLatusRectumKm,14:F3} km
         rp (periapsis radius)     = {PeriapsisRadiusKm,14:F3} km
         ra (apoapsis radius)      = {ApoapsisRadiusKm,14:F3} km
         type                      = {Type}
         """;
}
