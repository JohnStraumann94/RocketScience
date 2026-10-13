namespace Astro.Core;

/// <summary>
/// Chapter 1 — <b>Two-Body Orbital Mechanics</b>. The constants of the motion and the
/// geometry of the conic-section orbit.
/// </summary>
/// <remarks>
/// <para>
/// Everything in this class flows from the two-body equation of motion, obtained by
/// applying Newton's law of universal gravitation to two point masses and neglecting all
/// other forces:
/// </para>
/// <code>
///     r'' + (mu / r^3) * r = 0
/// </code>
/// <para>
/// Two conserved quantities fall straight out of that differential equation and they are
/// the backbone of the whole subject:
/// </para>
/// <list type="number">
///   <item><description>
///     <b>Specific angular momentum</b> h = r x v (a vector). Constant ⇒ the motion is
///     confined to a fixed plane.
///   </description></item>
///   <item><description>
///     <b>Specific mechanical energy</b> eps = v^2/2 − mu/r (a scalar). Constant ⇒ speed
///     is determined by radius alone, which is the vis-viva equation.
///   </description></item>
/// </list>
/// <para>
/// "Specific" simply means "per unit mass of the satellite" — which is why the satellite's
/// own mass never appears. A feather and a space station in the same orbit move identically.
/// </para>
/// </remarks>
public static class TwoBody
{
    /// <summary>
    /// Specific mechanical energy, eps = v^2/2 − mu/r  [km^2/s^2].
    /// </summary>
    /// <param name="radiusKm">Distance from the centre of the central body, r [km].</param>
    /// <param name="speedKmPerSec">Inertial speed, v [km/s].</param>
    /// <param name="mu">Gravitational parameter of the central body [km^3/s^2].</param>
    /// <returns>
    /// The specific orbital energy. Its <i>sign</i> classifies the orbit outright:
    /// negative ⇒ closed (circle or ellipse), zero ⇒ parabolic escape, positive ⇒ hyperbolic.
    /// </returns>
    public static double SpecificEnergy(double radiusKm, double speedKmPerSec, double mu = Constants.MuEarth)
    {
        Guard.Positive(radiusKm, nameof(radiusKm));
        Guard.Positive(mu, nameof(mu));

        // Kinetic term minus potential term. Note the potential is -mu/r, not -mu/r^2:
        // the inverse-square law applies to force, the inverse-first-power to energy.
        return (speedKmPerSec * speedKmPerSec) / 2.0 - mu / radiusKm;
    }

    /// <summary>
    /// Semi-major axis derived from the energy, a = −mu / (2 eps)  [km].
    /// </summary>
    /// <remarks>
    /// This is the inverse of <see cref="SpecificEnergy(double, double, double)"/> and shows
    /// that energy and size are two views of the same fact. For a hyperbola the result is
    /// negative, which is the standard sign convention (and makes the vis-viva equation work
    /// unchanged for all conic types).
    /// </remarks>
    public static double SemiMajorAxisFromEnergy(double specificEnergy, double mu = Constants.MuEarth)
    {
        if (Math.Abs(specificEnergy) < 1e-15)
        {
            // eps = 0 is the parabolic case: a is infinite, so there is no meaningful value.
            return double.PositiveInfinity;
        }

        return -mu / (2.0 * specificEnergy);
    }

    /// <summary>
    /// The <b>vis-viva</b> ("living force") equation: v = sqrt( mu (2/r − 1/a) )  [km/s].
    /// </summary>
    /// <param name="radiusKm">Current radius r [km].</param>
    /// <param name="semiMajorAxisKm">
    /// Semi-major axis a [km]. Positive for ellipses, negative for hyperbolas, and
    /// <see cref="double.PositiveInfinity"/> for a parabola (which correctly degenerates to
    /// the escape-speed formula because 1/a → 0).
    /// </param>
    /// <param name="mu">Gravitational parameter [km^3/s^2].</param>
    /// <returns>The speed at that radius [km/s].</returns>
    /// <remarks>
    /// Arguably the single most useful equation in astrodynamics. Every impulsive-manoeuvre
    /// calculation in <see cref="Maneuvers"/> is just vis-viva evaluated twice and subtracted.
    /// </remarks>
    public static double VisViva(double radiusKm, double semiMajorAxisKm, double mu = Constants.MuEarth)
    {
        Guard.Positive(radiusKm, nameof(radiusKm));
        Guard.Positive(mu, nameof(mu));

        double inverseA = double.IsInfinity(semiMajorAxisKm) ? 0.0 : 1.0 / semiMajorAxisKm;
        double vSquared = mu * (2.0 / radiusKm - inverseA);

        if (vSquared < 0.0)
        {
            // Physically impossible: the requested radius lies outside the orbit
            // (e.g. r greater than apoapsis on a bound ellipse).
            throw new ArgumentOutOfRangeException(
                nameof(radiusKm),
                $"Radius {radiusKm:F3} km is not reachable on an orbit with a = {semiMajorAxisKm:F3} km.");
        }

        return Math.Sqrt(vSquared);
    }

    /// <summary>
    /// Speed of a circular orbit, v_c = sqrt(mu / r)  [km/s].
    /// </summary>
    /// <remarks>A special case of vis-viva with r = a.</remarks>
    public static double CircularSpeed(double radiusKm, double mu = Constants.MuEarth)
    {
        Guard.Positive(radiusKm, nameof(radiusKm));
        return Math.Sqrt(mu / radiusKm);
    }

    /// <summary>
    /// Local escape (parabolic) speed, v_esc = sqrt(2 mu / r)  [km/s].
    /// </summary>
    /// <remarks>
    /// Exactly sqrt(2) times the circular speed at the same radius — a ratio worth
    /// memorising, since it means only a 41.4% speed increase separates "in orbit" from
    /// "gone forever".
    /// </remarks>
    public static double EscapeSpeed(double radiusKm, double mu = Constants.MuEarth)
    {
        Guard.Positive(radiusKm, nameof(radiusKm));
        return Math.Sqrt(2.0 * mu / radiusKm);
    }

    /// <summary>
    /// Orbital period from Kepler's third law, T = 2 pi sqrt(a^3 / mu)  [s].
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown for a non-positive semi-major axis: open (parabolic/hyperbolic) trajectories
    /// never repeat and so have no period.
    /// </exception>
    public static double Period(double semiMajorAxisKm, double mu = Constants.MuEarth)
    {
        if (semiMajorAxisKm <= 0.0 || double.IsInfinity(semiMajorAxisKm))
        {
            throw new ArgumentOutOfRangeException(
                nameof(semiMajorAxisKm),
                "Only closed (elliptical) orbits have a period; a must be finite and positive.");
        }

        return Constants.TwoPi * Math.Sqrt(Math.Pow(semiMajorAxisKm, 3) / mu);
    }

    /// <summary>
    /// Mean motion, n = sqrt(mu / a^3)  [rad/s] — the average angular rate around the orbit.
    /// </summary>
    public static double MeanMotion(double semiMajorAxisKm, double mu = Constants.MuEarth)
    {
        Guard.Positive(semiMajorAxisKm, nameof(semiMajorAxisKm));
        return Math.Sqrt(mu / Math.Pow(semiMajorAxisKm, 3));
    }

    /// <summary>
    /// The polar equation of a conic ("the orbit equation"): r = p / (1 + e cos(nu))  [km].
    /// </summary>
    /// <param name="semiLatusRectumKm">Semi-latus rectum p = h^2/mu = a(1 − e^2) [km].</param>
    /// <param name="eccentricity">Eccentricity e [dimensionless].</param>
    /// <param name="trueAnomalyRad">True anomaly nu, measured from periapsis [rad].</param>
    /// <remarks>
    /// This one equation contains all four conic sections. Kepler's first law is simply the
    /// statement that e &lt; 1 for the planets.
    /// </remarks>
    public static double RadiusAtTrueAnomaly(double semiLatusRectumKm, double eccentricity, double trueAnomalyRad)
    {
        Guard.Positive(semiLatusRectumKm, nameof(semiLatusRectumKm));

        double denominator = 1.0 + eccentricity * Math.Cos(trueAnomalyRad);
        if (denominator <= 0.0)
        {
            // Beyond the asymptote of an open orbit there is no physical point at this angle.
            throw new ArgumentOutOfRangeException(
                nameof(trueAnomalyRad),
                "True anomaly lies beyond the asymptote of this hyperbola/parabola — no orbit point exists there.");
        }

        return semiLatusRectumKm / denominator;
    }

    /// <summary>Periapsis radius, r_p = a(1 − e)  [km] — the closest approach.</summary>
    public static double PeriapsisRadius(double semiMajorAxisKm, double eccentricity)
        => semiMajorAxisKm * (1.0 - eccentricity);

    /// <summary>
    /// Apoapsis radius, r_a = a(1 + e)  [km] — the farthest point.
    /// Defined only for closed orbits (e &lt; 1).
    /// </summary>
    public static double ApoapsisRadius(double semiMajorAxisKm, double eccentricity)
    {
        if (eccentricity >= 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(eccentricity),
                "Open trajectories (e >= 1) have no apoapsis.");
        }

        return semiMajorAxisKm * (1.0 + eccentricity);
    }

    /// <summary>
    /// Flight-path angle gamma [rad] — the angle between the velocity vector and the local
    /// horizontal (the direction perpendicular to the radius).
    /// </summary>
    /// <remarks>
    /// tan(gamma) = e sin(nu) / (1 + e cos(nu)). It is zero at periapsis and apoapsis (where
    /// motion is purely horizontal), positive while climbing and negative while descending.
    /// </remarks>
    public static double FlightPathAngle(double eccentricity, double trueAnomalyRad)
        => Math.Atan2(
            eccentricity * Math.Sin(trueAnomalyRad),
            1.0 + eccentricity * Math.Cos(trueAnomalyRad));
}
