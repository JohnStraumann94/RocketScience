namespace Astro.Core;

/// <summary>
/// Chapter 7 — <b>Lunar Trajectories</b>, treated with the <b>patched-conic</b> method.
/// </summary>
/// <remarks>
/// <para>
/// The Earth–Moon–spacecraft system is a three-body problem, which has no closed-form
/// solution. The patched-conic approximation sidesteps this by dividing space into regions
/// where a single body dominates, solving a simple two-body problem in each, and stitching
/// ("patching") the answers together at the boundary:
/// </para>
/// <list type="number">
///   <item><description>
///     Outside the Moon's <b>sphere of influence</b> (SOI), ignore the Moon: the vehicle
///     flies a geocentric ellipse.
///   </description></item>
///   <item><description>
///     At the SOI boundary, subtract the Moon's orbital velocity to convert the state into
///     the Moon-centred frame.
///   </description></item>
///   <item><description>
///     Inside the SOI, ignore the Earth: the vehicle flies a selenocentric hyperbola.
///   </description></item>
/// </list>
/// <para>
/// The result is typically accurate to a fraction of a percent in delta-v — good enough for
/// mission design, after which a numerically integrated trajectory takes over. Every Apollo
/// mission was first sketched exactly this way.
/// </para>
/// </remarks>
public static class LunarTrajectory
{
    /// <summary>
    /// Radius of the <b>sphere of influence</b> of a smaller body orbiting a larger one:
    /// r_SOI = R (m / M)^(2/5).
    /// </summary>
    /// <param name="orbitalRadiusKm">R — distance between the two bodies [km].</param>
    /// <param name="smallerBodyMu">mu of the smaller body [km^3/s^2].</param>
    /// <param name="largerBodyMu">mu of the larger body [km^3/s^2].</param>
    /// <returns>The SOI radius [km].</returns>
    /// <remarks>
    /// <para>
    /// Laplace's definition: the surface on which the perturbing acceleration of the distant
    /// body, relative to the near body's own attraction, is equal in the two possible
    /// framings of the problem. The 2/5 exponent is what falls out of that comparison.
    /// </para>
    /// <para>
    /// Because mu is proportional to mass, the mass ratio can be replaced by the mu ratio
    /// directly — which is convenient, since mu values are far better determined than masses.
    /// </para>
    /// <para>
    /// For the Earth–Moon system this yields about 66,200 km, roughly one sixth of the
    /// Earth–Moon distance.
    /// </para>
    /// </remarks>
    public static double SphereOfInfluenceRadius(
        double orbitalRadiusKm,
        double smallerBodyMu,
        double largerBodyMu)
    {
        Guard.Positive(orbitalRadiusKm, nameof(orbitalRadiusKm));
        Guard.Positive(smallerBodyMu, nameof(smallerBodyMu));
        Guard.Positive(largerBodyMu, nameof(largerBodyMu));

        return orbitalRadiusKm * Math.Pow(smallerBodyMu / largerBodyMu, 0.4);
    }

    /// <summary>
    /// The radius of the Moon's sphere of influence [km] — approximately 66,183 km.
    /// </summary>
    public static double MoonSphereOfInfluenceKm => SphereOfInfluenceRadius(
        Constants.EarthMoonDistanceKm, Constants.MuMoon, Constants.MuEarth);

    /// <summary>
    /// The result of the geocentric (phase 1) leg of a patched-conic lunar transfer.
    /// </summary>
    /// <param name="InjectionSpeedKmPerSec">v0 — the speed required at trans-lunar injection [km/s].</param>
    /// <param name="InjectionDeltaVKmPerSec">The delta-v from a circular parking orbit [km/s].</param>
    /// <param name="TransferSemiMajorAxisKm">a — semi-major axis of the geocentric transfer ellipse [km].</param>
    /// <param name="TransferEccentricity">e — eccentricity of the transfer ellipse.</param>
    /// <param name="ArrivalSpeedKmPerSec">v1 — geocentric speed on reaching the arrival radius [km/s].</param>
    /// <param name="TimeOfFlightSec">Coast time from injection to arrival [s].</param>
    public readonly record struct GeocentricLeg(
        double InjectionSpeedKmPerSec,
        double InjectionDeltaVKmPerSec,
        double TransferSemiMajorAxisKm,
        double TransferEccentricity,
        double ArrivalSpeedKmPerSec,
        double TimeOfFlightSec)
    {
        /// <summary>Time of flight in days, the usual unit for lunar transfers.</summary>
        public double TimeOfFlightDays => TimeOfFlightSec / Constants.SolarDaySeconds;
    }

    /// <summary>
    /// Computes the geocentric leg of a simplified (co-planar, degenerate) patched-conic
    /// lunar transfer: a Hohmann-like ellipse from a circular parking orbit out to the
    /// Moon's SOI arrival radius.
    /// </summary>
    /// <param name="parkingOrbitRadiusKm">Radius of the circular Earth parking orbit [km].</param>
    /// <param name="arrivalRadiusKm">
    /// Geocentric radius at which the vehicle meets the Moon's SOI [km]. For the simplest
    /// model use the Earth–Moon distance minus the SOI radius.
    /// </param>
    /// <param name="mu">Earth's gravitational parameter [km^3/s^2].</param>
    /// <remarks>
    /// This deliberately assumes the arrival point lies on the transfer ellipse's line of
    /// apsides, i.e. a pure Hohmann geometry. Real lunar transfers arrive with a "sweep angle"
    /// lambda that must be chosen to match the Moon's motion; that refinement is what turns
    /// this into a targeting problem rather than a formula.
    /// </remarks>
    public static GeocentricLeg ComputeGeocentricLeg(
        double parkingOrbitRadiusKm,
        double arrivalRadiusKm,
        double mu = Constants.MuEarth)
    {
        Guard.Positive(parkingOrbitRadiusKm, nameof(parkingOrbitRadiusKm));
        Guard.Positive(arrivalRadiusKm, nameof(arrivalRadiusKm));

        if (arrivalRadiusKm <= parkingOrbitRadiusKm)
        {
            throw new ArgumentOutOfRangeException(
                nameof(arrivalRadiusKm), "The arrival radius must be greater than the parking-orbit radius.");
        }

        // Transfer ellipse with perigee at the parking orbit and apogee at the arrival radius.
        double aTransfer = (parkingOrbitRadiusKm + arrivalRadiusKm) / 2.0;
        double eTransfer = (arrivalRadiusKm - parkingOrbitRadiusKm) / (arrivalRadiusKm + parkingOrbitRadiusKm);

        double injectionSpeed = TwoBody.VisViva(parkingOrbitRadiusKm, aTransfer, mu);
        double parkingSpeed = TwoBody.CircularSpeed(parkingOrbitRadiusKm, mu);
        double injectionDeltaV = injectionSpeed - parkingSpeed;

        double arrivalSpeed = TwoBody.VisViva(arrivalRadiusKm, aTransfer, mu);

        // Half the period, since we fly perigee to apogee.
        double timeOfFlight = Math.PI * Math.Sqrt(Math.Pow(aTransfer, 3) / mu);

        return new GeocentricLeg(
            injectionSpeed, injectionDeltaV, aTransfer, eTransfer, arrivalSpeed, timeOfFlight);
    }

    /// <summary>
    /// The delta-v required to brake from a hyperbolic arrival at the Moon into a circular
    /// lunar orbit ("lunar orbit insertion", LOI) [km/s].
    /// </summary>
    /// <param name="hyperbolicExcessSpeedKmPerSec">
    /// v_infinity — the selenocentric speed on entering the SOI [km/s].
    /// </param>
    /// <param name="periapsisRadiusKm">Radius of the target circular lunar orbit [km].</param>
    /// <param name="muMoon">The Moon's gravitational parameter [km^3/s^2].</param>
    /// <remarks>
    /// <para>
    /// Energy is conserved on the incoming hyperbola, so the speed at periapsis follows from
    /// vis-viva with v_infinity^2 = mu (−1/a):
    /// </para>
    /// <code>
    ///     v_periapsis = sqrt(v_inf^2 + 2 mu / r_p)
    ///     dv_LOI      = v_periapsis − v_circular(r_p)
    /// </code>
    /// <para>
    /// This is the <b>Oberth effect</b> in action, and it explains why every orbit-insertion
    /// burn is performed at periapsis: the deeper you dive before burning, the larger the
    /// v_periapsis, and the smaller the fraction of it you must shed.
    /// </para>
    /// </remarks>
    public static double LunarOrbitInsertionDeltaV(
        double hyperbolicExcessSpeedKmPerSec,
        double periapsisRadiusKm,
        double muMoon = Constants.MuMoon)
    {
        Guard.NonNegative(hyperbolicExcessSpeedKmPerSec, nameof(hyperbolicExcessSpeedKmPerSec));
        Guard.Positive(periapsisRadiusKm, nameof(periapsisRadiusKm));

        double vPeriapsis = Math.Sqrt(
            hyperbolicExcessSpeedKmPerSec * hyperbolicExcessSpeedKmPerSec + 2.0 * muMoon / periapsisRadiusKm);

        double vCircular = TwoBody.CircularSpeed(periapsisRadiusKm, muMoon);

        return vPeriapsis - vCircular;
    }
}
