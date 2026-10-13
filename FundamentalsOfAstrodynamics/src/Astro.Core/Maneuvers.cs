namespace Astro.Core;

/// <summary>
/// The result of a multi-burn orbital transfer.
/// </summary>
/// <param name="BurnsKmPerSec">The individual impulsive velocity changes, in execution order [km/s].</param>
/// <param name="TransferTimeSec">Total coast time from the first burn to the last [s].</param>
/// <param name="Description">A human-readable narrative of what the manoeuvre does.</param>
public readonly record struct TransferResult(
    IReadOnlyList<double> BurnsKmPerSec,
    double TransferTimeSec,
    string Description)
{
    /// <summary>
    /// Total delta-v — the sum of the <i>magnitudes</i> of the individual burns [km/s].
    /// </summary>
    /// <remarks>
    /// Magnitudes, not a vector sum: every burn costs propellant regardless of direction,
    /// and it is this scalar total that feeds the rocket equation.
    /// </remarks>
    public double TotalDeltaVKmPerSec => BurnsKmPerSec.Sum(Math.Abs);

    /// <summary>Transfer time expressed in hours, for readability.</summary>
    public double TransferTimeHours => TransferTimeSec / 3600.0;
}

/// <summary>
/// Chapter 3 — <b>Basic Orbital Manoeuvres</b>. Impulsive (instantaneous) burns that move a
/// spacecraft between orbits.
/// </summary>
/// <remarks>
/// <para>
/// <b>The impulsive approximation.</b> Every routine here assumes the engine fires for an
/// interval so short that the spacecraft's position does not change during the burn — only
/// its velocity does. Real chemical burns last minutes rather than instants, which incurs a
/// "gravity loss" of a few percent, but the impulsive model is accurate enough for mission
/// design and is what makes closed-form answers possible at all.
/// </para>
/// <para>
/// <b>Why delta-v is the currency of spaceflight.</b> Delta-v is independent of the vehicle:
/// it is a property of the trajectory. Convert it to propellant mass only at the end, via
/// Tsiolkovsky's rocket equation (the subject of the companion Rocket Propulsion Elements
/// project).
/// </para>
/// </remarks>
public static class Maneuvers
{
    /// <summary>
    /// The <b>Hohmann transfer</b>: the classic two-burn transfer between coplanar circular
    /// orbits, using an ellipse tangent to both.
    /// </summary>
    /// <param name="initialRadiusKm">r1 — radius of the starting circular orbit [km].</param>
    /// <param name="finalRadiusKm">r2 — radius of the target circular orbit [km].</param>
    /// <param name="mu">Gravitational parameter [km^3/s^2].</param>
    /// <returns>The two burns and the transfer (half-period) coast time.</returns>
    /// <remarks>
    /// <para>
    /// The transfer ellipse has its periapsis on the inner circle and its apoapsis on the
    /// outer one, so its semi-major axis is simply a_t = (r1 + r2)/2. Both burns are purely
    /// tangential — no direction change at all — which is exactly why it is efficient.
    /// </para>
    /// <code>
    ///     dv1 = v_transfer_periapsis − v_circular(r1)     (raise apoapsis to r2)
    ///     dv2 = v_circular(r2) − v_transfer_apoapsis      (circularise at r2)
    ///     t   = T_transfer / 2 = pi sqrt(a_t^3 / mu)
    /// </code>
    /// <para>
    /// <b>Optimality caveat.</b> Hohmann is minimum-delta-v for two impulses only when the
    /// radius ratio r2/r1 is below about 11.94. Above that the three-burn
    /// <see cref="BiEllipticTransfer"/> wins — see the discussion there.
    /// </para>
    /// <para>
    /// The routine works equally well for lowering an orbit (r2 &lt; r1); both burns simply
    /// come out negative, i.e. retrograde.
    /// </para>
    /// </remarks>
    public static TransferResult HohmannTransfer(
        double initialRadiusKm,
        double finalRadiusKm,
        double mu = Constants.MuEarth)
    {
        Guard.Positive(initialRadiusKm, nameof(initialRadiusKm));
        Guard.Positive(finalRadiusKm, nameof(finalRadiusKm));

        // Semi-major axis of the transfer ellipse: it spans from r1 to r2.
        double aTransfer = (initialRadiusKm + finalRadiusKm) / 2.0;

        // Circular speeds on the two parking orbits.
        double v1Circular = TwoBody.CircularSpeed(initialRadiusKm, mu);
        double v2Circular = TwoBody.CircularSpeed(finalRadiusKm, mu);

        // Speeds on the transfer ellipse at each end, from vis-viva.
        double vTransferAtR1 = TwoBody.VisViva(initialRadiusKm, aTransfer, mu);
        double vTransferAtR2 = TwoBody.VisViva(finalRadiusKm, aTransfer, mu);

        double deltaV1 = vTransferAtR1 - v1Circular;   // departure burn
        double deltaV2 = v2Circular - vTransferAtR2;   // arrival / circularisation burn

        // Half the period of the transfer ellipse: you fly exactly one half revolution.
        double transferTime = Math.PI * Math.Sqrt(Math.Pow(aTransfer, 3) / mu);

        string direction = finalRadiusKm > initialRadiusKm ? "raising" : "lowering";
        string description =
            $"Hohmann transfer {direction} the orbit from r = {initialRadiusKm:F1} km to r = {finalRadiusKm:F1} km. " +
            $"Transfer ellipse: a = {aTransfer:F1} km, e = {Math.Abs(finalRadiusKm - initialRadiusKm) / (finalRadiusKm + initialRadiusKm):F5}.";

        return new TransferResult([deltaV1, deltaV2], transferTime, description);
    }

    /// <summary>
    /// The <b>bi-elliptic transfer</b>: a three-burn transfer that first flings the vehicle
    /// out to a high intermediate apoapsis r_b, changes the orbit cheaply out there, then
    /// drops back down to the target radius.
    /// </summary>
    /// <param name="initialRadiusKm">r1 — starting circular radius [km].</param>
    /// <param name="intermediateRadiusKm">r_b — intermediate apoapsis radius [km]; must exceed both r1 and r2.</param>
    /// <param name="finalRadiusKm">r2 — target circular radius [km].</param>
    /// <param name="mu">Gravitational parameter [km^3/s^2].</param>
    /// <remarks>
    /// <para>
    /// Counter-intuitively, going <i>further</i> can cost <i>less</i>. The reason is the
    /// Oberth effect running in reverse: at a very high apoapsis the orbital speed is tiny,
    /// so the burn that changes the orbit's shape and/or plane is correspondingly cheap.
    /// The penalty is time — a bi-elliptic transfer can take many times longer than Hohmann.
    /// </para>
    /// <para>
    /// Rules of thumb for the radius ratio R = r2/r1:
    /// R &lt; 11.94 ⇒ Hohmann always wins; R &gt; 15.58 ⇒ bi-elliptic always wins (for a
    /// sufficiently large r_b); in between it depends on the chosen r_b.
    /// </para>
    /// </remarks>
    public static TransferResult BiEllipticTransfer(
        double initialRadiusKm,
        double intermediateRadiusKm,
        double finalRadiusKm,
        double mu = Constants.MuEarth)
    {
        Guard.Positive(initialRadiusKm, nameof(initialRadiusKm));
        Guard.Positive(intermediateRadiusKm, nameof(intermediateRadiusKm));
        Guard.Positive(finalRadiusKm, nameof(finalRadiusKm));

        if (intermediateRadiusKm <= Math.Max(initialRadiusKm, finalRadiusKm))
        {
            throw new ArgumentOutOfRangeException(
                nameof(intermediateRadiusKm),
                "The intermediate apoapsis must be larger than both the initial and the final radius, " +
                "otherwise the manoeuvre is not bi-elliptic.");
        }

        // First transfer ellipse: periapsis r1, apoapsis r_b.
        double a1 = (initialRadiusKm + intermediateRadiusKm) / 2.0;

        // Second transfer ellipse: apoapsis r_b, periapsis r2.
        double a2 = (intermediateRadiusKm + finalRadiusKm) / 2.0;

        // Burn 1 at r1: leave the circular orbit onto the first ellipse.
        double deltaV1 = TwoBody.VisViva(initialRadiusKm, a1, mu) - TwoBody.CircularSpeed(initialRadiusKm, mu);

        // Burn 2 at r_b: this is the cheap one, done where the vehicle is moving slowest.
        double deltaV2 = TwoBody.VisViva(intermediateRadiusKm, a2, mu) - TwoBody.VisViva(intermediateRadiusKm, a1, mu);

        // Burn 3 at r2: circularise.
        double deltaV3 = TwoBody.CircularSpeed(finalRadiusKm, mu) - TwoBody.VisViva(finalRadiusKm, a2, mu);

        // Two half-ellipses, so half of each period.
        double transferTime =
            Math.PI * Math.Sqrt(Math.Pow(a1, 3) / mu) +
            Math.PI * Math.Sqrt(Math.Pow(a2, 3) / mu);

        string description =
            $"Bi-elliptic transfer r1 = {initialRadiusKm:F1} km -> rb = {intermediateRadiusKm:F1} km -> r2 = {finalRadiusKm:F1} km. " +
            $"Radius ratio r2/r1 = {finalRadiusKm / initialRadiusKm:F2}.";

        return new TransferResult([deltaV1, deltaV2, deltaV3], transferTime, description);
    }

    /// <summary>
    /// A <b>simple plane change</b>: rotate the orbital plane by <paramref name="planeChangeRad"/>
    /// without altering the orbit's size or shape.
    /// </summary>
    /// <param name="orbitalSpeedKmPerSec">v — the speed at the burn point [km/s].</param>
    /// <param name="planeChangeRad">The angle between the old and new orbital planes [rad].</param>
    /// <returns>The required delta-v [km/s].</returns>
    /// <remarks>
    /// <para>
    /// Since the speed is unchanged, the velocity vectors before and after form an isosceles
    /// triangle, giving the deceptively simple
    /// </para>
    /// <code>    dv = 2 v sin(di / 2)</code>
    /// <para>
    /// <b>This is brutally expensive.</b> In LEO (v ≈ 7.7 km/s) even a 30-degree plane change
    /// costs about 4 km/s — more than the entire Hohmann transfer to geostationary orbit.
    /// The engineering response is always the same: perform plane changes where v is small,
    /// i.e. at apoapsis, or fold them into another burn with
    /// <see cref="CombinedManeuver"/>, or avoid them entirely by launching into the right
    /// plane in the first place.
    /// </para>
    /// </remarks>
    public static double SimplePlaneChange(double orbitalSpeedKmPerSec, double planeChangeRad)
    {
        Guard.Positive(orbitalSpeedKmPerSec, nameof(orbitalSpeedKmPerSec));
        return 2.0 * orbitalSpeedKmPerSec * Math.Sin(Math.Abs(planeChangeRad) / 2.0);
    }

    /// <summary>
    /// A <b>combined manoeuvre</b>: change speed and plane in a single burn, using the law of
    /// cosines on the velocity triangle.
    /// </summary>
    /// <param name="initialSpeedKmPerSec">v_i — speed before the burn [km/s].</param>
    /// <param name="finalSpeedKmPerSec">v_f — desired speed after the burn [km/s].</param>
    /// <param name="planeChangeRad">Angle between the two velocity vectors [rad].</param>
    /// <returns>The required delta-v [km/s].</returns>
    /// <remarks>
    /// <code>    dv = sqrt( v_i^2 + v_f^2 − 2 v_i v_f cos(di) )</code>
    /// <para>
    /// Always cheaper than doing the two operations separately, because vector addition
    /// beats scalar addition. This is precisely how a geostationary transfer orbit's apogee
    /// kick motor works: it circularises AND removes the launch-site inclination in one burn.
    /// </para>
    /// </remarks>
    public static double CombinedManeuver(
        double initialSpeedKmPerSec,
        double finalSpeedKmPerSec,
        double planeChangeRad)
    {
        Guard.Positive(initialSpeedKmPerSec, nameof(initialSpeedKmPerSec));
        Guard.Positive(finalSpeedKmPerSec, nameof(finalSpeedKmPerSec));

        double dv2 = initialSpeedKmPerSec * initialSpeedKmPerSec
                   + finalSpeedKmPerSec * finalSpeedKmPerSec
                   - 2.0 * initialSpeedKmPerSec * finalSpeedKmPerSec * Math.Cos(planeChangeRad);

        // Rounding can drive a near-zero result microscopically negative.
        return Math.Sqrt(Math.Max(dv2, 0.0));
    }

    /// <summary>
    /// A <b>phasing manoeuvre</b>: change a spacecraft's position along its own circular
    /// orbit (to catch up with, or drop back to, a target) without changing the orbit
    /// permanently.
    /// </summary>
    /// <param name="orbitRadiusKm">Radius of the circular orbit [km].</param>
    /// <param name="phaseAngleRad">
    /// The angle to make up [rad]. Positive means the chaser must <i>gain</i> on a target
    /// that is ahead of it.
    /// </param>
    /// <param name="numberOfRevolutions">How many revolutions of the phasing orbit to spread the change over.</param>
    /// <param name="mu">Gravitational parameter [km^3/s^2].</param>
    /// <remarks>
    /// <para>
    /// The trick is the great paradox of orbital mechanics: <b>to catch up, slow down.</b>
    /// Braking drops you into a smaller, faster-period orbit; after one lap you arrive back
    /// at the same point earlier than the target, having gained on it. Two burns of equal
    /// magnitude are needed — one to enter the phasing orbit and one to leave it.
    /// </para>
    /// </remarks>
    public static TransferResult PhasingManeuver(
        double orbitRadiusKm,
        double phaseAngleRad,
        int numberOfRevolutions = 1,
        double mu = Constants.MuEarth)
    {
        Guard.Positive(orbitRadiusKm, nameof(orbitRadiusKm));
        if (numberOfRevolutions < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(numberOfRevolutions), numberOfRevolutions, "At least one revolution is required.");
        }

        double period = TwoBody.Period(orbitRadiusKm, mu);

        // To gain a lead angle of phi, the phasing orbit's period must be SHORTER by the
        // time the target needs to cover phi: dT = phi / n, spread over k revolutions.
        double meanMotion = TwoBody.MeanMotion(orbitRadiusKm, mu);
        double phasingPeriod = period - Angles.NormalizeSigned(phaseAngleRad) / (meanMotion * numberOfRevolutions);

        if (phasingPeriod <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(phaseAngleRad),
                "The requested phase change is too large for the given number of revolutions.");
        }

        // Invert Kepler's third law to get the phasing orbit's semi-major axis.
        double aPhasing = Math.Cbrt(mu * Math.Pow(phasingPeriod / Constants.TwoPi, 2));

        // The burn point stays at r = orbitRadius, so vis-viva gives the new speed there.
        double vCircular = TwoBody.CircularSpeed(orbitRadiusKm, mu);
        double vPhasing = TwoBody.VisViva(orbitRadiusKm, aPhasing, mu);

        double deltaV = vPhasing - vCircular;   // burn 1: enter phasing orbit
        // Burn 2 is exactly the reverse, restoring the original circular orbit.

        string description =
            $"Phasing manoeuvre to change position by {Angles.ToDegrees(phaseAngleRad):F2} deg " +
            $"over {numberOfRevolutions} revolution(s). Phasing orbit a = {aPhasing:F1} km " +
            $"(period {phasingPeriod / 60.0:F2} min vs {period / 60.0:F2} min).";

        return new TransferResult([deltaV, -deltaV], phasingPeriod * numberOfRevolutions, description);
    }

    /// <summary>
    /// The break-even radius ratio at which the bi-elliptic transfer starts to beat Hohmann
    /// (for an infinite intermediate radius): approximately 11.93876.
    /// </summary>
    /// <remarks>
    /// Included as a named constant because it is one of those numbers that appears in every
    /// textbook without explanation. It is the root of a transcendental equation obtained by
    /// equating the two total-delta-v expressions in the limit r_b → infinity.
    /// </remarks>
    public const double BiEllipticBreakEvenRatio = 11.93876;
}
