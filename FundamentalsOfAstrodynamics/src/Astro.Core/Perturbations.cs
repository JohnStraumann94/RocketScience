namespace Astro.Core;

/// <summary>
/// Chapter 9 — <b>Perturbations</b>: the secular (steadily accumulating) effects of the
/// Earth's oblateness on an orbit.
/// </summary>
/// <remarks>
/// <para>
/// Everything up to this point assumed a perfectly spherical central body. The Earth is not
/// spherical: centrifugal flattening makes the equatorial radius about 21 km larger than the
/// polar one. That equatorial bulge exerts a torque on an inclined orbit, and the orbit
/// responds like a gyroscope — it precesses.
/// </para>
/// <para>
/// The bulge is captured by the coefficient <b>J2</b> ≈ 1.0826e-3. Every other gravitational
/// harmonic is at least a thousand times smaller, so J2 alone accounts for the overwhelming
/// majority of the deviation from Keplerian motion in low Earth orbit.
/// </para>
/// <para>
/// J2 produces two <b>secular</b> effects — drifts that accumulate without bound rather than
/// oscillating — on the node and on perigee. Both are calculated below. Far from being a
/// nuisance, they are exploited: sun-synchronous orbits and Molniya orbits are both direct
/// engineering applications of these two formulas.
/// </para>
/// </remarks>
public static class Perturbations
{
    /// <summary>
    /// <b>Nodal regression</b>: the secular drift rate of the right ascension of the
    /// ascending node, dOmega/dt [rad/s].
    /// </summary>
    /// <param name="semiMajorAxisKm">a — semi-major axis [km].</param>
    /// <param name="eccentricity">e — eccentricity.</param>
    /// <param name="inclinationRad">i — inclination [rad].</param>
    /// <param name="j2">The oblateness coefficient (defaults to Earth's).</param>
    /// <param name="bodyRadiusKm">Equatorial radius of the central body [km].</param>
    /// <param name="mu">Gravitational parameter [km^3/s^2].</param>
    /// <remarks>
    /// <code>
    ///     dOmega/dt = −(3/2) n J2 (R_e / p)^2 cos(i)
    /// </code>
    /// <para>with p = a(1 − e^2) and n the mean motion. Note the consequences of cos(i):</para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <b>Prograde orbit (i &lt; 90 deg)</b>: cos i &gt; 0, so the node drifts <i>westward</i>
    ///     (negative). A typical LEO regresses several degrees per day.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Polar orbit (i = 90 deg)</b>: cos i = 0, so the node is stationary — the orbit
    ///     plane stays fixed in inertial space.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Retrograde orbit (i &gt; 90 deg)</b>: cos i &lt; 0, so the node drifts eastward.
    ///     Tune the inclination and the drift exactly matches the Earth's motion around the
    ///     Sun — that is a <see cref="SunSynchronousInclination">sun-synchronous</see> orbit.
    ///   </description></item>
    /// </list>
    /// </remarks>
    public static double NodalRegressionRate(
        double semiMajorAxisKm,
        double eccentricity,
        double inclinationRad,
        double j2 = Constants.J2Earth,
        double bodyRadiusKm = Constants.EarthRadiusKm,
        double mu = Constants.MuEarth)
    {
        Guard.Positive(semiMajorAxisKm, nameof(semiMajorAxisKm));
        Guard.InRange(eccentricity, 0.0, 1.0 - 1e-12, nameof(eccentricity));

        double n = TwoBody.MeanMotion(semiMajorAxisKm, mu);
        double p = semiMajorAxisKm * (1.0 - eccentricity * eccentricity);
        double factor = bodyRadiusKm / p;

        return -1.5 * n * j2 * factor * factor * Math.Cos(inclinationRad);
    }

    /// <summary>
    /// <b>Apsidal rotation</b>: the secular drift rate of the argument of perigee,
    /// domega/dt [rad/s].
    /// </summary>
    /// <remarks>
    /// <code>
    ///     domega/dt = (3/4) n J2 (R_e / p)^2 (5 cos^2(i) − 1)
    /// </code>
    /// <para>
    /// The bracket changes sign at cos^2(i) = 1/5, i.e. i = 63.435 degrees (or its retrograde
    /// mirror 116.565 degrees). At that <b>critical inclination</b> perigee does not move at
    /// all. Russian Molniya communications satellites fly at exactly 63.4 degrees so that
    /// their apogee — where the highly eccentric orbit lingers for hours — stays parked over
    /// the northern hemisphere for the life of the satellite.
    /// </para>
    /// </remarks>
    public static double ApsidalRotationRate(
        double semiMajorAxisKm,
        double eccentricity,
        double inclinationRad,
        double j2 = Constants.J2Earth,
        double bodyRadiusKm = Constants.EarthRadiusKm,
        double mu = Constants.MuEarth)
    {
        Guard.Positive(semiMajorAxisKm, nameof(semiMajorAxisKm));
        Guard.InRange(eccentricity, 0.0, 1.0 - 1e-12, nameof(eccentricity));

        double n = TwoBody.MeanMotion(semiMajorAxisKm, mu);
        double p = semiMajorAxisKm * (1.0 - eccentricity * eccentricity);
        double factor = bodyRadiusKm / p;
        double cosI = Math.Cos(inclinationRad);

        return 0.75 * n * j2 * factor * factor * (5.0 * cosI * cosI - 1.0);
    }

    /// <summary>
    /// The <b>critical inclination</b> at which apsidal rotation vanishes:
    /// i = arccos(sqrt(1/5)) ≈ 63.4349 degrees [rad].
    /// </summary>
    public static double CriticalInclinationRad => Math.Acos(Math.Sqrt(0.2));

    /// <summary>
    /// The inclination required for a <b>sun-synchronous</b> orbit [rad].
    /// </summary>
    /// <param name="semiMajorAxisKm">a — semi-major axis [km].</param>
    /// <param name="eccentricity">e — eccentricity.</param>
    /// <param name="j2">Oblateness coefficient.</param>
    /// <param name="bodyRadiusKm">Equatorial radius [km].</param>
    /// <param name="mu">Gravitational parameter [km^3/s^2].</param>
    /// <returns>The required inclination [rad], which is always retrograde (&gt; 90 degrees).</returns>
    /// <remarks>
    /// <para>
    /// A sun-synchronous orbit is one whose node precesses eastward at exactly the rate the
    /// Earth orbits the Sun — 360 degrees per tropical year, or 1.99106e-7 rad/s. The orbital
    /// plane then keeps a constant angle to the Sun, so the satellite crosses the equator at
    /// the same local solar time on every pass. Every Earth-observation and weather satellite
    /// relies on this, because consistent lighting makes images comparable over time.
    /// </para>
    /// <para>
    /// Setting the nodal regression formula equal to that target rate and solving for i gives
    /// the expression implemented below. For a typical 800 km circular orbit the answer is
    /// about 98.6 degrees.
    /// </para>
    /// </remarks>
    public static double SunSynchronousInclination(
        double semiMajorAxisKm,
        double eccentricity = 0.0,
        double j2 = Constants.J2Earth,
        double bodyRadiusKm = Constants.EarthRadiusKm,
        double mu = Constants.MuEarth)
    {
        Guard.Positive(semiMajorAxisKm, nameof(semiMajorAxisKm));

        // Required precession rate: one revolution per tropical year (365.2422 days).
        const double tropicalYearSeconds = 365.2422 * Constants.SolarDaySeconds;
        double requiredRate = Constants.TwoPi / tropicalYearSeconds;

        double n = TwoBody.MeanMotion(semiMajorAxisKm, mu);
        double p = semiMajorAxisKm * (1.0 - eccentricity * eccentricity);
        double factor = bodyRadiusKm / p;

        // requiredRate = -1.5 * n * J2 * (Re/p)^2 * cos(i)  =>  solve for cos(i).
        double cosI = -requiredRate / (1.5 * n * j2 * factor * factor);

        if (Math.Abs(cosI) > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(semiMajorAxisKm),
                $"No sun-synchronous inclination exists for a = {semiMajorAxisKm:F1} km; " +
                "the orbit is too high for J2 to supply the required precession rate.");
        }

        return Math.Acos(cosI);
    }

    /// <summary>
    /// Propagates the node and perigee of an element set forward in time under secular J2
    /// drift only, leaving a, e and i unchanged.
    /// </summary>
    /// <param name="elements">Initial osculating elements.</param>
    /// <param name="deltaTimeSec">Elapsed time [s].</param>
    /// <param name="j2">Oblateness coefficient.</param>
    /// <param name="bodyRadiusKm">Equatorial radius [km].</param>
    /// <param name="mu">Gravitational parameter [km^3/s^2].</param>
    /// <returns>Elements with Omega, omega and nu all advanced.</returns>
    /// <remarks>
    /// This is the classic "J2 mean element" propagator. It is correct only for the secular
    /// terms: the short-period oscillations (of order J2 times the orbit radius, so tens of
    /// kilometres) are deliberately ignored, which is exactly the approximation that makes
    /// two-line element sets useful.
    /// </remarks>
    public static OrbitalElements PropagateWithJ2(
        OrbitalElements elements,
        double deltaTimeSec,
        double j2 = Constants.J2Earth,
        double bodyRadiusKm = Constants.EarthRadiusKm,
        double mu = Constants.MuEarth)
    {
        double raanRate = NodalRegressionRate(
            elements.SemiMajorAxisKm, elements.Eccentricity, elements.InclinationRad, j2, bodyRadiusKm, mu);

        double argPerigeeRate = ApsidalRotationRate(
            elements.SemiMajorAxisKm, elements.Eccentricity, elements.InclinationRad, j2, bodyRadiusKm, mu);

        // Advance the in-plane position with the ordinary Keplerian propagator ...
        OrbitalElements advanced = KeplerSolver.Propagate(elements, deltaTimeSec, mu);

        // ... then rotate the orbit plane and the line of apsides on top of it.
        return advanced with
        {
            RaanRad = Angles.Normalize(elements.RaanRad + raanRate * deltaTimeSec),
            ArgumentOfPeriapsisRad = Angles.Normalize(elements.ArgumentOfPeriapsisRad + argPerigeeRate * deltaTimeSec)
        };
    }
}
