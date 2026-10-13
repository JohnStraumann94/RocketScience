namespace Astro.Core;

/// <summary>
/// The complete free-flight solution for a ballistic missile trajectory.
/// </summary>
/// <param name="QParameter">Q = v_bo^2 r_bo / mu — the dimensionless "energy" parameter at burnout.</param>
/// <param name="Eccentricity">e — eccentricity of the free-flight ellipse.</param>
/// <param name="SemiMajorAxisKm">a — semi-major axis of the free-flight ellipse [km].</param>
/// <param name="FreeFlightRangeAngleRad">Psi — the geocentric sweep angle from burnout to re-entry [rad].</param>
/// <param name="SurfaceRangeKm">Ground range covered during free flight [km].</param>
/// <param name="MaximumAltitudeKm">Apogee altitude above the surface [km].</param>
/// <param name="TimeOfFlightSec">Free-flight duration from burnout to re-entry [s].</param>
public readonly record struct BallisticTrajectory(
    double QParameter,
    double Eccentricity,
    double SemiMajorAxisKm,
    double FreeFlightRangeAngleRad,
    double SurfaceRangeKm,
    double MaximumAltitudeKm,
    double TimeOfFlightSec)
{
    /// <summary>Psi expressed in degrees, for display.</summary>
    public double FreeFlightRangeAngleDeg => FreeFlightRangeAngleRad * Constants.RadToDeg;

    /// <summary>Free-flight duration in minutes, for display.</summary>
    public double TimeOfFlightMinutes => TimeOfFlightSec / 60.0;
}

/// <summary>
/// Chapter 6 — <b>Ballistic Missile Trajectories</b>.
/// </summary>
/// <remarks>
/// <para>
/// An ICBM flight has three phases: a powered <b>boost</b> phase, a <b>free-flight</b> phase
/// that is pure two-body Keplerian motion, and a <b>re-entry</b> phase dominated by drag.
/// Only the middle phase is analytic, and it is by far the longest, so the standard treatment
/// is to characterise everything by the state at <i>burnout</i>: the radius r_bo, the speed
/// v_bo and the flight-path angle phi_bo.
/// </para>
/// <para>
/// The key insight is that a ballistic missile trajectory is simply an <b>elliptical orbit
/// whose perigee happens to lie inside the Earth</b>. All the machinery of Chapters 1–4
/// applies unchanged; the only new element is the "free-flight range equation" that relates
/// the burnout conditions to the ground range achieved.
/// </para>
/// </remarks>
public static class BallisticMissile
{
    /// <summary>
    /// Computes the complete free-flight trajectory from burnout conditions.
    /// </summary>
    /// <param name="burnoutRadiusKm">r_bo — geocentric radius at burnout [km] (Earth radius + burnout altitude).</param>
    /// <param name="burnoutSpeedKmPerSec">v_bo — inertial speed at burnout [km/s].</param>
    /// <param name="burnoutFlightPathAngleRad">phi_bo — flight-path angle at burnout [rad], measured from the local horizontal.</param>
    /// <param name="mu">Gravitational parameter [km^3/s^2].</param>
    /// <param name="planetRadiusKm">Radius of the planet's surface [km], used for the ground-range conversion.</param>
    /// <remarks>
    /// <para>
    /// <b>The free-flight range equation</b> (BMW eq. 6.2-4) gives the sweep angle Psi
    /// directly from the burnout state:
    /// </para>
    /// <code>
    ///     cos(Psi/2) = (1 − Q cos^2 phi) / sqrt(1 + Q(Q − 2) cos^2 phi)
    /// </code>
    /// <para>
    /// where Q = v_bo^2 r_bo / mu. Note that Q = 1 with phi = 0 is a circular orbit and Q = 2
    /// is escape speed, so a missile always has Q &lt; 2 — otherwise it is a spacecraft.
    /// </para>
    /// <para>
    /// A subtlety worth understanding: this assumes burnout and re-entry occur at the
    /// <i>same</i> radius, which makes the trajectory symmetric about apogee. That is the
    /// standard textbook idealisation and is accurate to a few percent for real systems.
    /// </para>
    /// </remarks>
    public static BallisticTrajectory Compute(
        double burnoutRadiusKm,
        double burnoutSpeedKmPerSec,
        double burnoutFlightPathAngleRad,
        double mu = Constants.MuEarth,
        double planetRadiusKm = Constants.EarthRadiusKm)
    {
        Guard.Positive(burnoutRadiusKm, nameof(burnoutRadiusKm));
        Guard.Positive(burnoutSpeedKmPerSec, nameof(burnoutSpeedKmPerSec));

        // Q is the ratio of twice the kinetic energy to the potential energy magnitude.
        double q = burnoutSpeedKmPerSec * burnoutSpeedKmPerSec * burnoutRadiusKm / mu;

        if (q >= 2.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(burnoutSpeedKmPerSec),
                $"Q = {q:F4} >= 2 means the vehicle has reached escape speed — this is no longer a ballistic trajectory.");
        }

        double cosPhi = Math.Cos(burnoutFlightPathAngleRad);
        double cosPhiSquared = cosPhi * cosPhi;

        // Eccentricity of the free-flight ellipse (BMW eq. 6.2-3).
        double e = Math.Sqrt(1.0 + q * (q - 2.0) * cosPhiSquared);

        // Semi-major axis from the energy.
        double energy = TwoBody.SpecificEnergy(burnoutRadiusKm, burnoutSpeedKmPerSec, mu);
        double a = -mu / (2.0 * energy);

        // ---- Free-flight range equation ----
        double numerator = 1.0 - q * cosPhiSquared;
        double denominator = Math.Sqrt(1.0 + q * (q - 2.0) * cosPhiSquared);
        double halfPsi = Angles.SafeAcos(numerator / denominator);
        double psi = 2.0 * halfPsi;

        // Ground range is the sweep angle times the planet's radius (great-circle arc).
        double surfaceRange = psi * planetRadiusKm;

        // Apogee of the ellipse, converted to an altitude above the surface.
        double apogeeRadius = a * (1.0 + e);
        double maximumAltitude = apogeeRadius - planetRadiusKm;

        // ---- Time of free flight ----
        // True anomaly at burnout (BMW eq. 6.3-1). The trajectory is symmetric about apogee,
        // so re-entry occurs at nu = 2*pi - nu_bo and the flight time is the difference in
        // mean anomaly divided by the mean motion.
        double nuBurnout = Math.Atan2(
            q * cosPhi * Math.Sin(burnoutFlightPathAngleRad),
            q * cosPhiSquared - 1.0);
        nuBurnout = Angles.Normalize(nuBurnout);

        double nuReentry = Constants.TwoPi - nuBurnout;

        double meanAnomalyBurnout = KeplerSolver.TrueToMeanAnomaly(nuBurnout, e);
        double meanAnomalyReentry = KeplerSolver.TrueToMeanAnomaly(nuReentry, e);
        double meanMotion = TwoBody.MeanMotion(a, mu);

        double timeOfFlight = (meanAnomalyReentry - meanAnomalyBurnout) / meanMotion;
        if (timeOfFlight < 0.0)
        {
            // Wrap-around safety net: add one full period if the anomalies straddled 2*pi.
            timeOfFlight += Constants.TwoPi / meanMotion;
        }

        return new BallisticTrajectory(q, e, a, psi, surfaceRange, maximumAltitude, timeOfFlight);
    }

    /// <summary>
    /// The burnout flight-path angle that maximises ground range for a given burnout speed:
    /// phi_optimal = (pi − Psi) / 4.
    /// </summary>
    /// <param name="freeFlightRangeAngleRad">Psi — the desired free-flight range angle [rad].</param>
    /// <returns>The optimal flight-path angle [rad].</returns>
    /// <remarks>
    /// <para>
    /// A beautifully simple result. For a short-range shot (Psi → 0) it reduces to 45 degrees —
    /// the familiar flat-Earth artillery answer. As the range grows towards half the globe
    /// (Psi → pi) the optimal angle falls towards zero, i.e. you fire almost horizontally and
    /// let the ellipse do the work.
    /// </para>
    /// <para>
    /// This is also the origin of the "high road / low road" pairing: for any range shorter
    /// than the maximum, two flight-path angles produce the same range — one lofted and one
    /// depressed — bracketing the optimum.
    /// </para>
    /// </remarks>
    public static double OptimalFlightPathAngle(double freeFlightRangeAngleRad)
        => (Math.PI - freeFlightRangeAngleRad) / 4.0;

    /// <summary>
    /// The minimum burnout speed capable of achieving a given free-flight range angle, flown
    /// at the optimal flight-path angle [km/s].
    /// </summary>
    /// <param name="freeFlightRangeAngleRad">Psi — required range angle [rad].</param>
    /// <param name="burnoutRadiusKm">r_bo — burnout radius [km].</param>
    /// <param name="mu">Gravitational parameter [km^3/s^2].</param>
    /// <remarks>
    /// Obtained by substituting the optimal angle into the range equation and solving for Q:
    /// <code>    Q_min = 2 sin(Psi/2) / (1 + sin(Psi/2))</code>
    /// then v = sqrt(Q mu / r).
    /// </remarks>
    public static double MinimumBurnoutSpeed(
        double freeFlightRangeAngleRad,
        double burnoutRadiusKm,
        double mu = Constants.MuEarth)
    {
        Guard.Positive(burnoutRadiusKm, nameof(burnoutRadiusKm));

        double sinHalfPsi = Math.Sin(freeFlightRangeAngleRad / 2.0);
        double qMin = 2.0 * sinHalfPsi / (1.0 + sinHalfPsi);

        return Math.Sqrt(qMin * mu / burnoutRadiusKm);
    }
}
