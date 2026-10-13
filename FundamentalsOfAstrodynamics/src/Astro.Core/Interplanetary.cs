namespace Astro.Core;

/// <summary>
/// A complete interplanetary Hohmann transfer budget, from a low parking orbit at the
/// departure planet to a low parking orbit at the target planet.
/// </summary>
/// <param name="HeliocentricDepartureSpeedKmPerSec">Speed required on the heliocentric transfer ellipse at departure [km/s].</param>
/// <param name="DepartureVInfinityKmPerSec">v_infinity relative to the departure planet [km/s].</param>
/// <param name="DepartureC3">C3 = v_infinity^2 — the "characteristic energy" quoted by launch providers [km^2/s^2].</param>
/// <param name="InjectionDeltaVKmPerSec">Delta-v for trans-planetary injection from the departure parking orbit [km/s].</param>
/// <param name="ArrivalVInfinityKmPerSec">v_infinity relative to the arrival planet [km/s].</param>
/// <param name="CaptureDeltaVKmPerSec">Delta-v to capture into the arrival parking orbit [km/s].</param>
/// <param name="TransferTimeSec">Heliocentric coast time [s].</param>
/// <param name="DeparturefPhaseAngleRad">Required phase angle of the target planet at departure [rad].</param>
/// <param name="SynodicPeriodSec">Time between successive launch opportunities [s].</param>
public readonly record struct InterplanetaryTransfer(
    double HeliocentricDepartureSpeedKmPerSec,
    double DepartureVInfinityKmPerSec,
    double DepartureC3,
    double InjectionDeltaVKmPerSec,
    double ArrivalVInfinityKmPerSec,
    double CaptureDeltaVKmPerSec,
    double TransferTimeSec,
    double DeparturefPhaseAngleRad,
    double SynodicPeriodSec)
{
    /// <summary>Total mission delta-v (injection plus capture) [km/s].</summary>
    public double TotalDeltaVKmPerSec => InjectionDeltaVKmPerSec + CaptureDeltaVKmPerSec;

    /// <summary>Heliocentric transfer time in days.</summary>
    public double TransferTimeDays => TransferTimeSec / Constants.SolarDaySeconds;

    /// <summary>Required departure phase angle in degrees.</summary>
    public double DeparturePhaseAngleDeg => DeparturefPhaseAngleRad * Constants.RadToDeg;

    /// <summary>Synodic period in days — how long you wait for the next launch window.</summary>
    public double SynodicPeriodDays => SynodicPeriodSec / Constants.SolarDaySeconds;
}

/// <summary>
/// Chapter 8 — <b>Interplanetary Trajectories</b>. Patched conics applied on the scale of
/// the solar system.
/// </summary>
/// <remarks>
/// <para>
/// The structure mirrors the lunar case but with three conics instead of two:
/// </para>
/// <list type="number">
///   <item><description>A departure <b>hyperbola</b> relative to the launch planet.</description></item>
///   <item><description>A heliocentric <b>transfer ellipse</b> between the two planetary orbits.</description></item>
///   <item><description>An arrival <b>hyperbola</b> relative to the target planet.</description></item>
/// </list>
/// <para>
/// The planets' spheres of influence are so tiny compared with the astronomical unit
/// (Earth's SOI is 0.6% of 1 AU) that the patch points can be treated as coincident with the
/// planets themselves — a simplification that makes the whole calculation tractable by hand.
/// </para>
/// <para>
/// Every routine here assumes <b>coplanar circular</b> planetary orbits. Real trajectory
/// design replaces the Hohmann ellipse with a Lambert solve on a grid of departure and
/// arrival dates (a "porkchop plot"), but this model gets the delta-v budget right to within
/// a few percent and, crucially, explains <i>why</i> the numbers are what they are.
/// </para>
/// </remarks>
public static class Interplanetary
{
    /// <summary>
    /// <b>Synodic period</b>: the interval between successive identical alignments of two
    /// planets, and therefore the spacing of launch windows [s].
    /// </summary>
    /// <param name="period1Sec">Orbital period of the first planet [s].</param>
    /// <param name="period2Sec">Orbital period of the second planet [s].</param>
    /// <remarks>
    /// <code>    1 / T_syn = | 1/T1 − 1/T2 |</code>
    /// <para>
    /// For Earth and Mars this gives about 780 days — which is precisely why Mars missions
    /// launch in clusters roughly every 26 months and why missing a window is so expensive.
    /// </para>
    /// </remarks>
    public static double SynodicPeriod(double period1Sec, double period2Sec)
    {
        Guard.Positive(period1Sec, nameof(period1Sec));
        Guard.Positive(period2Sec, nameof(period2Sec));

        double difference = Math.Abs(1.0 / period1Sec - 1.0 / period2Sec);
        if (difference < 1e-20)
        {
            // Co-orbital bodies never change their relative geometry.
            return double.PositiveInfinity;
        }

        return 1.0 / difference;
    }

    /// <summary>
    /// <b>Hyperbolic excess speed</b> v_infinity: the speed the vehicle retains "at infinity"
    /// after climbing out of a planet's gravity well [km/s].
    /// </summary>
    /// <param name="periapsisSpeedKmPerSec">Speed at the departure hyperbola's periapsis [km/s].</param>
    /// <param name="periapsisRadiusKm">Radius of that periapsis [km].</param>
    /// <param name="mu">Gravitational parameter of the planet [km^3/s^2].</param>
    /// <remarks>
    /// From the energy equation, v_inf^2 = v_p^2 − 2 mu / r_p. Note that v_inf is <i>not</i>
    /// simply v_p minus the escape speed: energies add, speeds do not. That fact is exactly
    /// the Oberth effect, and it is why a burn deep in the gravity well buys far more
    /// v_infinity than the same burn performed far away.
    /// </remarks>
    public static double HyperbolicExcessSpeed(
        double periapsisSpeedKmPerSec,
        double periapsisRadiusKm,
        double mu)
    {
        Guard.Positive(periapsisRadiusKm, nameof(periapsisRadiusKm));

        double vInfSquared = periapsisSpeedKmPerSec * periapsisSpeedKmPerSec - 2.0 * mu / periapsisRadiusKm;
        if (vInfSquared < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(periapsisSpeedKmPerSec),
                "The vehicle is below escape speed at that radius, so it is on a closed orbit and has no v_infinity.");
        }

        return Math.Sqrt(vInfSquared);
    }

    /// <summary>
    /// The delta-v needed to depart a circular parking orbit onto a hyperbola with the
    /// required v_infinity [km/s].
    /// </summary>
    /// <param name="hyperbolicExcessSpeedKmPerSec">v_infinity required by the heliocentric transfer [km/s].</param>
    /// <param name="parkingOrbitRadiusKm">Radius of the circular parking orbit [km].</param>
    /// <param name="mu">Gravitational parameter of the departure planet [km^3/s^2].</param>
    /// <remarks>
    /// <code>    dv = sqrt(v_inf^2 + 2 mu / r_p) − sqrt(mu / r_p)</code>
    /// <para>
    /// The same expression, with the sign reversed, gives the capture burn at arrival.
    /// </para>
    /// </remarks>
    public static double InjectionDeltaV(
        double hyperbolicExcessSpeedKmPerSec,
        double parkingOrbitRadiusKm,
        double mu)
    {
        Guard.NonNegative(hyperbolicExcessSpeedKmPerSec, nameof(hyperbolicExcessSpeedKmPerSec));
        Guard.Positive(parkingOrbitRadiusKm, nameof(parkingOrbitRadiusKm));

        double vPeriapsis = Math.Sqrt(
            hyperbolicExcessSpeedKmPerSec * hyperbolicExcessSpeedKmPerSec + 2.0 * mu / parkingOrbitRadiusKm);

        return vPeriapsis - TwoBody.CircularSpeed(parkingOrbitRadiusKm, mu);
    }

    /// <summary>
    /// Computes a complete planet-to-planet Hohmann transfer budget.
    /// </summary>
    /// <param name="departureOrbitRadiusKm">Heliocentric radius of the departure planet's orbit [km].</param>
    /// <param name="arrivalOrbitRadiusKm">Heliocentric radius of the target planet's orbit [km].</param>
    /// <param name="departurePlanetMu">mu of the departure planet [km^3/s^2].</param>
    /// <param name="departureParkingRadiusKm">Radius of the parking orbit at departure [km].</param>
    /// <param name="arrivalPlanetMu">mu of the arrival planet [km^3/s^2].</param>
    /// <param name="arrivalParkingRadiusKm">Radius of the desired parking orbit at arrival [km].</param>
    /// <param name="muSun">mu of the Sun [km^3/s^2].</param>
    /// <remarks>
    /// <para>
    /// <b>The departure phase angle</b> is what makes an interplanetary launch window. By the
    /// time the spacecraft reaches the target's orbital radius, the target must be there too.
    /// Since the transfer takes half the period of the transfer ellipse, the target must lead
    /// the departure point by
    /// </para>
    /// <code>    phi = pi − n_target · t_transfer</code>
    /// <para>
    /// For Earth to Mars this works out at roughly 44 degrees — Mars must be 44 degrees ahead
    /// of Earth at launch.
    /// </para>
    /// </remarks>
    public static InterplanetaryTransfer ComputeHohmannTransfer(
        double departureOrbitRadiusKm,
        double arrivalOrbitRadiusKm,
        double departurePlanetMu,
        double departureParkingRadiusKm,
        double arrivalPlanetMu,
        double arrivalParkingRadiusKm,
        double muSun = Constants.MuSun)
    {
        Guard.Positive(departureOrbitRadiusKm, nameof(departureOrbitRadiusKm));
        Guard.Positive(arrivalOrbitRadiusKm, nameof(arrivalOrbitRadiusKm));

        // ---- Leg 2: the heliocentric transfer ellipse ----
        double aTransfer = (departureOrbitRadiusKm + arrivalOrbitRadiusKm) / 2.0;

        double vDepartPlanetHelio = TwoBody.CircularSpeed(departureOrbitRadiusKm, muSun);
        double vArrivePlanetHelio = TwoBody.CircularSpeed(arrivalOrbitRadiusKm, muSun);

        double vTransferAtDeparture = TwoBody.VisViva(departureOrbitRadiusKm, aTransfer, muSun);
        double vTransferAtArrival = TwoBody.VisViva(arrivalOrbitRadiusKm, aTransfer, muSun);

        // v_infinity is the *difference* between the spacecraft's heliocentric speed and the
        // planet's own heliocentric speed — i.e. the excess it must be given by the launcher.
        double vInfDeparture = Math.Abs(vTransferAtDeparture - vDepartPlanetHelio);
        double vInfArrival = Math.Abs(vArrivePlanetHelio - vTransferAtArrival);

        // ---- Legs 1 and 3: the planetocentric hyperbolas ----
        double injectionDeltaV = InjectionDeltaV(vInfDeparture, departureParkingRadiusKm, departurePlanetMu);
        double captureDeltaV = InjectionDeltaV(vInfArrival, arrivalParkingRadiusKm, arrivalPlanetMu);

        // ---- Timing ----
        double transferTime = Math.PI * Math.Sqrt(Math.Pow(aTransfer, 3) / muSun);

        double meanMotionTarget = TwoBody.MeanMotion(arrivalOrbitRadiusKm, muSun);
        double phaseAngle = Angles.NormalizeSigned(Math.PI - meanMotionTarget * transferTime);

        double synodicPeriod = SynodicPeriod(
            TwoBody.Period(departureOrbitRadiusKm, muSun),
            TwoBody.Period(arrivalOrbitRadiusKm, muSun));

        return new InterplanetaryTransfer(
            vTransferAtDeparture,
            vInfDeparture,
            vInfDeparture * vInfDeparture,
            injectionDeltaV,
            vInfArrival,
            captureDeltaV,
            transferTime,
            phaseAngle,
            synodicPeriod);
    }

    /// <summary>
    /// The turning angle of a hyperbolic flyby ("gravity assist"), delta [rad].
    /// </summary>
    /// <param name="eccentricity">Eccentricity of the flyby hyperbola (&gt; 1).</param>
    /// <remarks>
    /// <code>    sin(delta / 2) = 1 / e</code>
    /// <para>
    /// The flyby rotates the v_infinity vector by delta without changing its magnitude — in
    /// the planet's frame. In the <i>Sun's</i> frame that rotation adds vectorially to the
    /// planet's orbital velocity, which is how a spacecraft steals energy from a planet.
    /// Voyager 2 gained roughly 10 km/s at Jupiter this way.
    /// </para>
    /// </remarks>
    public static double FlybyTurningAngle(double eccentricity)
    {
        if (eccentricity <= 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(eccentricity), eccentricity, "A flyby requires a hyperbolic trajectory (e > 1).");
        }

        return 2.0 * Math.Asin(1.0 / eccentricity);
    }

    /// <summary>
    /// The maximum heliocentric speed change obtainable from a single gravity assist [km/s].
    /// </summary>
    /// <param name="hyperbolicExcessSpeedKmPerSec">v_infinity relative to the planet [km/s].</param>
    /// <param name="turningAngleRad">The achieved turning angle [rad].</param>
    /// <remarks>
    /// The v_infinity vector is rotated, so the change in the heliocentric velocity has
    /// magnitude 2 v_inf sin(delta/2) — structurally identical to the plane-change formula,
    /// except that here the manoeuvre is free.
    /// </remarks>
    public static double GravityAssistDeltaV(double hyperbolicExcessSpeedKmPerSec, double turningAngleRad)
        => 2.0 * hyperbolicExcessSpeedKmPerSec * Math.Sin(turningAngleRad / 2.0);
}
