namespace Propulsion.Core;

/// <summary>
/// One sample from a two-dimensional powered-ascent trajectory.
/// </summary>
/// <param name="Time">Time since liftoff, s.</param>
/// <param name="Downrange">Horizontal distance travelled over the ground, m.</param>
/// <param name="Altitude">Altitude above the launch site, m.</param>
/// <param name="Velocity">Speed along the flight path, m/s.</param>
/// <param name="FlightPathAngle">
/// Angle of the velocity vector above the local horizontal, degrees. 90 is straight up,
/// 0 is horizontal, negative is descending.
/// </param>
/// <param name="Mass">Instantaneous vehicle mass, kg.</param>
/// <param name="Thrust">Instantaneous thrust at the current ambient pressure, N.</param>
/// <param name="Drag">Instantaneous aerodynamic drag force, N.</param>
/// <param name="Acceleration">Net acceleration along the flight path, m/s^2.</param>
/// <param name="DynamicPressure">Dynamic pressure q = 0.5 * rho * v^2, Pa.</param>
/// <param name="Powered">True while the engine is burning, false during the coast.</param>
public readonly record struct GravityTurnSample(
    double Time,
    double Downrange,
    double Altitude,
    double Velocity,
    double FlightPathAngle,
    double Mass,
    double Thrust,
    double Drag,
    double Acceleration,
    double DynamicPressure,
    bool Powered);

/// <summary>
/// The result of a two-dimensional gravity-turn ascent integration.
/// </summary>
/// <param name="Samples">The full trajectory history, one entry per integration step.</param>
/// <param name="BurnoutTime">Time at which the propellant was exhausted, s.</param>
/// <param name="BurnoutAltitude">Altitude at burnout, m.</param>
/// <param name="BurnoutDownrange">Downrange distance at burnout, m.</param>
/// <param name="BurnoutVelocity">Speed at burnout, m/s.</param>
/// <param name="BurnoutFlightPathAngle">Flight path angle at burnout, degrees.</param>
/// <param name="IdealDeltaV">Rocket-equation delta-v, ignoring all losses, m/s.</param>
/// <param name="GravityLoss">Velocity lost to gravity over the powered phase, m/s.</param>
/// <param name="DragLoss">Velocity lost to aerodynamic drag over the powered phase, m/s.</param>
/// <param name="MaxDynamicPressure">Peak dynamic pressure encountered, Pa.</param>
/// <param name="MaxDynamicPressureTime">Time at which max-Q occurred, s.</param>
/// <param name="MaxDynamicPressureIndex">Index into <paramref name="Samples"/> of the max-Q point.</param>
/// <param name="BurnoutIndex">Index into <paramref name="Samples"/> of the burnout point.</param>
/// <param name="ApogeeAltitude">Highest altitude reached, including the coast, m.</param>
/// <param name="ApogeeTime">Time of apogee, s.</param>
/// <param name="ApogeeIndex">Index into <paramref name="Samples"/> of the apogee point.</param>
public readonly record struct GravityTurnResult(
    IReadOnlyList<GravityTurnSample> Samples,
    double BurnoutTime,
    double BurnoutAltitude,
    double BurnoutDownrange,
    double BurnoutVelocity,
    double BurnoutFlightPathAngle,
    double IdealDeltaV,
    double GravityLoss,
    double DragLoss,
    double MaxDynamicPressure,
    double MaxDynamicPressureTime,
    int MaxDynamicPressureIndex,
    int BurnoutIndex,
    double ApogeeAltitude,
    double ApogeeTime,
    int ApogeeIndex)
{
    /// <summary>Produces a readable multi-line summary of the ascent.</summary>
    /// <returns>A formatted, aligned report.</returns>
    public override string ToString()
    {
        System.Text.StringBuilder report = new();

        report.AppendLine($"Burnout time            = {BurnoutTime,12:F2} s");
        report.AppendLine($"Burnout altitude        = {BurnoutAltitude / 1000.0,12:F3} km");
        report.AppendLine($"Burnout downrange       = {BurnoutDownrange / 1000.0,12:F3} km");
        report.AppendLine($"Burnout speed           = {BurnoutVelocity,12:F2} m/s");
        report.AppendLine($"Burnout flight path ang = {BurnoutFlightPathAngle,12:F2} deg");
        report.AppendLine();
        report.AppendLine($"Ideal delta-v           = {IdealDeltaV,12:F2} m/s");
        report.AppendLine($"Gravity loss            = {GravityLoss,12:F2} m/s");
        report.AppendLine($"Drag loss               = {DragLoss,12:F2} m/s");
        report.AppendLine();
        report.AppendLine($"Max dynamic pressure    = {MaxDynamicPressure / 1000.0,12:F3} kPa");
        report.AppendLine($"Time of max-Q           = {MaxDynamicPressureTime,12:F2} s");
        report.AppendLine();
        report.AppendLine($"Apogee altitude         = {ApogeeAltitude / 1000.0,12:F3} km");
        report.AppendLine($"Time of apogee          = {ApogeeTime,12:F2} s");

        return report.ToString();
    }
}

/// <summary>
/// Two-dimensional gravity-turn ascent integration. Sutton Chapter 4.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FlightPerformance.VerticalAscent"/> answers "how much velocity do the losses eat?"
/// but it flies straight up, which no real launch vehicle does. This class integrates the
/// genuine two-dimensional problem and therefore produces an actual <i>flight path</i> - a
/// curve through altitude and downrange distance that can be plotted and animated.
/// </para>
/// <para>
/// <b>What a gravity turn is.</b> A launch vehicle cannot fly straight up: it would arrive at
/// altitude with all its velocity pointing the wrong way, and orbit requires horizontal speed.
/// Nor can it simply steer, because turning with aerodynamic lift at max-Q would tear the
/// vehicle apart. The solution is elegant: shortly after liftoff the vehicle performs a small
/// PITCH-OVER MANOEUVRE, tipping a few degrees away from vertical. From then on it holds zero
/// angle of attack - it points exactly along its own velocity vector - and lets GRAVITY do all
/// the steering. Because a component of gravity now acts perpendicular to the velocity, the
/// flight path angle decreases continuously and smoothly until the vehicle is flying nearly
/// horizontally at burnout. No side loads, no lift, no bending moments.
/// </para>
/// <para>
/// <b>The equations of motion.</b> With no lift and no angle of attack, in a plane, and writing
/// gamma for the flight path angle above the local horizontal:
/// </para>
/// <code>
///     dv/dt     = (T - D) / m  -  g * sin(gamma)
///     dgamma/dt = -( g / v  -  v / (R + h) ) * cos(gamma)
///     dh/dt     = v * sin(gamma)
///     dx/dt     = v * cos(gamma) * R / (R + h)
/// </code>
/// <para>
/// The <c>v / (R + h)</c> term in the second equation is the centrifugal relief from flying
/// around a curved planet. When <c>v^2 / (R + h)</c> equals <c>g</c> the term vanishes, gamma
/// stops changing, and the vehicle is in orbit - the equation contains the definition of
/// orbital velocity as a special case, which is rather satisfying.
/// </para>
/// <para>
/// <b>Numerical method.</b> Classical fourth-order Runge-Kutta with a fixed step. RK4 is used
/// rather than the explicit Euler step of <see cref="FlightPerformance.VerticalAscent"/> because
/// the flight path angle equation is stiff at low speed - the <c>g / v</c> term is large just
/// after pitch-over - and Euler visibly drifts there. The atmosphere is the same exponential
/// model used elsewhere in the library.
/// </para>
/// <para>
/// <b>Limitations, stated plainly.</b> This is a point-mass model in a plane. There is no
/// Earth rotation, no lift, no wind, no attitude dynamics, no staging, no throttling and no
/// steering authority beyond the initial kick. It reproduces the shape and the loss budget of a
/// real ascent well, and it is completely transparent, but it is not a flight-design tool.
/// </para>
/// </remarks>
public static class GravityTurn
{
    /// <summary>
    /// Integrates a two-dimensional gravity-turn ascent followed by an optional ballistic coast.
    /// </summary>
    /// <param name="initialMass">Gross liftoff mass m0, kg.</param>
    /// <param name="propellantMass">Usable propellant mass, kg. Must be less than <paramref name="initialMass"/>.</param>
    /// <param name="vacuumThrust">Vacuum thrust, N. Assumed constant while burning.</param>
    /// <param name="specificImpulseSeconds">Vacuum specific impulse, s.</param>
    /// <param name="exitArea">Nozzle exit area, m^2. Used for the back-pressure correction.</param>
    /// <param name="exitPressure">Nozzle exit pressure, Pa.</param>
    /// <param name="dragCoefficient">Drag coefficient Cd, dimensionless.</param>
    /// <param name="referenceArea">Frontal reference area, m^2.</param>
    /// <param name="pitchOverTime">
    /// Time after liftoff at which the pitch-over kick is applied, s. The vehicle flies
    /// vertically until this moment.
    /// </param>
    /// <param name="pitchOverAngleDegrees">
    /// Size of the pitch-over kick, degrees away from vertical. Real vehicles use 1 to 5 degrees.
    /// Too small and the vehicle never turns; too large and it pitches over too aggressively and
    /// flies into the ground.
    /// </param>
    /// <param name="coastSeconds">
    /// How long to continue integrating unpowered after burnout, s. Zero stops at burnout.
    /// </param>
    /// <param name="timeStep">Integration step, s. 0.05 to 0.2 is a sensible range.</param>
    /// <returns>The full trajectory and its summary statistics.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the propellant mass is not less than the initial mass, or when the vehicle
    /// has insufficient thrust to leave the pad.
    /// </exception>
    public static GravityTurnResult Ascend(
        double initialMass,
        double propellantMass,
        double vacuumThrust,
        double specificImpulseSeconds,
        double exitArea,
        double exitPressure,
        double dragCoefficient,
        double referenceArea,
        double pitchOverTime,
        double pitchOverAngleDegrees,
        double coastSeconds = 0.0,
        double timeStep = 0.1)
    {
        Guard.Positive(initialMass, nameof(initialMass));
        Guard.Positive(propellantMass, nameof(propellantMass));
        Guard.Positive(vacuumThrust, nameof(vacuumThrust));
        Guard.Positive(specificImpulseSeconds, nameof(specificImpulseSeconds));
        Guard.NonNegative(exitArea, nameof(exitArea));
        Guard.NonNegative(exitPressure, nameof(exitPressure));
        Guard.NonNegative(dragCoefficient, nameof(dragCoefficient));
        Guard.Positive(referenceArea, nameof(referenceArea));
        Guard.NonNegative(pitchOverTime, nameof(pitchOverTime));
        Guard.InRange(pitchOverAngleDegrees, 0.0, 45.0, nameof(pitchOverAngleDegrees));
        Guard.NonNegative(coastSeconds, nameof(coastSeconds));
        Guard.InRange(timeStep, 1.0e-3, 5.0, nameof(timeStep));

        if (propellantMass >= initialMass)
        {
            throw new ArgumentException(
                "The propellant mass must be less than the initial mass - the vehicle needs " +
                "some structure and payload left at burnout.",
                nameof(propellantMass));
        }

        double exhaustVelocity = Performance.ExhaustVelocityFromIsp(specificImpulseSeconds);
        double massFlowRate = vacuumThrust / exhaustVelocity;
        double burnTime = propellantMass / massFlowRate;
        double finalMass = initialMass - propellantMass;

        // Sanity check on the pad: sea-level thrust must exceed weight or nothing happens.
        double seaLevelThrust = vacuumThrust - Constants.SeaLevelPressure * exitArea;
        if (seaLevelThrust <= initialMass * Constants.StandardGravity)
        {
            throw new ArgumentException(
                $"Sea-level thrust ({seaLevelThrust / 1000.0:F1} kN) does not exceed the vehicle " +
                $"weight ({initialMass * Constants.StandardGravity / 1000.0:F1} kN), so the vehicle " +
                "cannot leave the pad. Increase the thrust or reduce the mass.",
                nameof(vacuumThrust));
        }

        List<GravityTurnSample> samples = [];

        // ---- Integration state ---------------------------------------------------------
        // v     speed along the flight path, m/s
        // gamma flight path angle above local horizontal, RADIANS (90 deg = straight up)
        // h     altitude, m
        // x     downrange distance, m
        //
        // The vehicle starts at rest pointing straight up. A tiny non-zero initial speed is
        // used because dgamma/dt contains g/v, which is singular at v = 0. Physically the
        // vehicle is vertical and not turning at that instant anyway, and the gravity-turn
        // equation is only switched on after the pitch-over kick, so this has no effect on
        // the answer - it merely keeps the arithmetic finite.
        double v = 0.1;
        double gamma = Math.PI / 2.0;
        double h = 0.0;
        double x = 0.0;
        double t = 0.0;

        bool pitchedOver = false;

        double gravityLoss = 0.0;
        double dragLoss = 0.0;

        double totalTime = burnTime + coastSeconds;
        int maximumSteps = (int)(totalTime / timeStep) + 10;

        for (int step = 0; step <= maximumSteps; step++)
        {
            bool powered = t < burnTime;
            double mass = powered ? initialMass - massFlowRate * t : finalMass;

            double ambientPressure = NozzleLosses.AmbientPressureAtAltitude(h);
            double density = FlightPerformance.DensityAtAltitude(h);

            double thrust = powered
                ? vacuumThrust - ambientPressure * exitArea
                : 0.0;

            double dynamicPressure = FlightPerformance.DynamicPressure(density, v);
            double drag = FlightPerformance.Drag(dragCoefficient, referenceArea, density, v);
            double gravity = LocalGravity(h);

            double acceleration = (thrust - drag) / mass - gravity * Math.Sin(gamma);

            samples.Add(new GravityTurnSample(
                Time: t,
                Downrange: x,
                Altitude: h,
                Velocity: v,
                FlightPathAngle: gamma * Constants.RadToDeg,
                Mass: mass,
                Thrust: thrust,
                Drag: drag,
                Acceleration: acceleration,
                DynamicPressure: dynamicPressure,
                Powered: powered));

            // Stop as soon as the vehicle comes back down to the ground.
            if (h < 0.0 && step > 0)
            {
                break;
            }

            if (t >= totalTime)
            {
                break;
            }

            // Accumulate the loss integrals over the POWERED phase only, which is the
            // convention Sutton uses when quoting a launch vehicle's loss budget.
            if (powered)
            {
                gravityLoss += gravity * Math.Sin(gamma) * timeStep;
                dragLoss += drag / mass * timeStep;
            }

            // ---- The pitch-over kick ---------------------------------------------------
            // Applied once, as an instantaneous change in flight path angle. Real vehicles
            // achieve it by gimballing the engines for a few seconds; the aerodynamic and
            // attitude dynamics of that manoeuvre are well outside a point-mass model, and
            // the resulting trajectory is indistinguishable at this level of fidelity.
            if (!pitchedOver && t >= pitchOverTime)
            {
                gamma -= pitchOverAngleDegrees * Constants.DegToRad;
                pitchedOver = true;
            }

            // ---- RK4 step --------------------------------------------------------------
            (double dv1, double dg1, double dh1, double dx1) = Derivatives(t, v, gamma, h);
            (double dv2, double dg2, double dh2, double dx2) = Derivatives(
                t + timeStep / 2.0, v + dv1 * timeStep / 2.0, gamma + dg1 * timeStep / 2.0, h + dh1 * timeStep / 2.0);
            (double dv3, double dg3, double dh3, double dx3) = Derivatives(
                t + timeStep / 2.0, v + dv2 * timeStep / 2.0, gamma + dg2 * timeStep / 2.0, h + dh2 * timeStep / 2.0);
            (double dv4, double dg4, double dh4, double dx4) = Derivatives(
                t + timeStep, v + dv3 * timeStep, gamma + dg3 * timeStep, h + dh3 * timeStep);

            v += timeStep / 6.0 * (dv1 + 2.0 * dv2 + 2.0 * dv3 + dv4);
            gamma += timeStep / 6.0 * (dg1 + 2.0 * dg2 + 2.0 * dg3 + dg4);
            h += timeStep / 6.0 * (dh1 + 2.0 * dh2 + 2.0 * dh3 + dh4);
            x += timeStep / 6.0 * (dx1 + 2.0 * dx2 + 2.0 * dx3 + dx4);
            t += timeStep;

            // Speed must stay positive for the gamma equation to remain finite. A vehicle
            // that has genuinely stopped is not going anywhere useful anyway.
            if (v < 0.1)
            {
                v = 0.1;
            }
        }

        // ---- Summary statistics --------------------------------------------------------
        int burnoutIndex = 0;
        for (int i = 0; i < samples.Count; i++)
        {
            if (samples[i].Powered)
            {
                burnoutIndex = i;
            }
        }

        int maxQIndex = 0;
        int apogeeIndex = 0;
        for (int i = 0; i < samples.Count; i++)
        {
            if (samples[i].DynamicPressure > samples[maxQIndex].DynamicPressure)
            {
                maxQIndex = i;
            }

            if (samples[i].Altitude > samples[apogeeIndex].Altitude)
            {
                apogeeIndex = i;
            }
        }

        GravityTurnSample burnout = samples[burnoutIndex];

        return new GravityTurnResult(
            Samples: samples,
            BurnoutTime: burnout.Time,
            BurnoutAltitude: burnout.Altitude,
            BurnoutDownrange: burnout.Downrange,
            BurnoutVelocity: burnout.Velocity,
            BurnoutFlightPathAngle: burnout.FlightPathAngle,
            IdealDeltaV: RocketEquation.DeltaV(exhaustVelocity, initialMass, finalMass),
            GravityLoss: gravityLoss,
            DragLoss: dragLoss,
            MaxDynamicPressure: samples[maxQIndex].DynamicPressure,
            MaxDynamicPressureTime: samples[maxQIndex].Time,
            MaxDynamicPressureIndex: maxQIndex,
            BurnoutIndex: burnoutIndex,
            ApogeeAltitude: samples[apogeeIndex].Altitude,
            ApogeeTime: samples[apogeeIndex].Time,
            ApogeeIndex: apogeeIndex);

        // ---- Local function: the right-hand side of the equations of motion -------------
        (double DvDt, double DgammaDt, double DhDt, double DxDt) Derivatives(
            double time, double speed, double angle, double altitude)
        {
            // Guard the state the integrator hands us; intermediate RK stages can stray
            // slightly below the ground or below the minimum speed.
            if (altitude < 0.0)
            {
                altitude = 0.0;
            }

            if (speed < 0.1)
            {
                speed = 0.1;
            }

            bool burning = time < burnTime;
            double m = burning ? initialMass - massFlowRate * time : finalMass;
            double pAmbient = NozzleLosses.AmbientPressureAtAltitude(altitude);
            double rho = FlightPerformance.DensityAtAltitude(altitude);

            double f = burning ? vacuumThrust - pAmbient * exitArea : 0.0;
            double d = FlightPerformance.Drag(dragCoefficient, referenceArea, rho, speed);
            double g = LocalGravity(altitude);
            double r = Constants.EarthRadius + altitude;

            double dvdt = (f - d) / m - g * Math.Sin(angle);

            // The gravity-turn steering law: no lift, no angle of attack, gravity alone
            // rotates the velocity vector. The v/r term is centrifugal relief.
            double dgammadt = -(g / speed - speed / r) * Math.Cos(angle);

            double dhdt = speed * Math.Sin(angle);

            // Downrange is measured along the surface, hence the R/(R+h) projection.
            double dxdt = speed * Math.Cos(angle) * Constants.EarthRadius / r;

            return (dvdt, dgammadt, dhdt, dxdt);
        }
    }

    /// <summary>
    /// Gravitational acceleration at altitude, using the inverse-square law.
    /// </summary>
    /// <param name="altitudeMetres">Altitude above the surface, m.</param>
    /// <returns>Local gravitational acceleration, m/s^2.</returns>
    /// <remarks>
    /// At 100 km gravity is still 97% of its sea-level value. The common misconception that
    /// orbit is "where gravity stops" is exactly backwards: orbit is where you are moving
    /// sideways fast enough that gravity's pull curves your path around the planet instead of
    /// into it.
    /// </remarks>
    public static double LocalGravity(double altitudeMetres)
    {
        double r = Constants.EarthRadius + Math.Max(0.0, altitudeMetres);

        return Constants.EarthMu / (r * r);
    }
}
