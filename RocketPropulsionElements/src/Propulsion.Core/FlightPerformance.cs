namespace Propulsion.Core;

/// <summary>
/// A single sample from a vertical-ascent trajectory integration.
/// </summary>
/// <param name="Time">Time since liftoff, s.</param>
/// <param name="Altitude">Altitude above the launch site, m.</param>
/// <param name="Velocity">Vertical velocity, m/s.</param>
/// <param name="Mass">Instantaneous vehicle mass, kg.</param>
/// <param name="Acceleration">Net acceleration, m/s^2.</param>
/// <param name="DynamicPressure">Dynamic pressure q = 0.5 * rho * v^2, Pa.</param>
public readonly record struct AscentSample(
    double Time,
    double Altitude,
    double Velocity,
    double Mass,
    double Acceleration,
    double DynamicPressure);

/// <summary>
/// The result of a vertical-ascent integration, including the loss accounting.
/// </summary>
/// <param name="Samples">The trajectory history.</param>
/// <param name="BurnoutTime">Time at which propellant ran out, s.</param>
/// <param name="BurnoutAltitude">Altitude at burnout, m.</param>
/// <param name="BurnoutVelocity">Velocity actually achieved at burnout, m/s.</param>
/// <param name="IdealDeltaV">Rocket-equation delta-v, ignoring all losses, m/s.</param>
/// <param name="GravityLoss">Velocity lost to gravity, m/s.</param>
/// <param name="DragLoss">Velocity lost to aerodynamic drag, m/s.</param>
/// <param name="MaxDynamicPressure">Peak dynamic pressure encountered, Pa.</param>
/// <param name="MaxDynamicPressureTime">Time at which max-Q occurred, s.</param>
public readonly record struct AscentResult(
    IReadOnlyList<AscentSample> Samples,
    double BurnoutTime,
    double BurnoutAltitude,
    double BurnoutVelocity,
    double IdealDeltaV,
    double GravityLoss,
    double DragLoss,
    double MaxDynamicPressure,
    double MaxDynamicPressureTime);

/// <summary>
/// Flight performance: what actually happens once a real vehicle has to fight gravity and the
/// atmosphere. Sutton Chapter 4.
/// </summary>
/// <remarks>
/// The rocket equation gives an <i>ideal</i> delta-v. A launch vehicle never sees it, because
/// during the burn some of the acceleration is spent simply holding the vehicle up against
/// gravity, and some is spent pushing air out of the way. The gap is typically 1.5-2.0 km/s to
/// low Earth orbit, and it is the reason launchers are sized for ~9.4 km/s when orbital velocity
/// is only ~7.8 km/s.
/// </remarks>
public static class FlightPerformance
{
    /// <summary>
    /// Gravity loss for a burn at constant flight-path angle.
    /// </summary>
    /// <param name="burnTimeSeconds">Duration of the burn, s.</param>
    /// <param name="flightPathAngleDegrees">
    /// Angle of the velocity vector above the local horizontal, degrees. 90 is straight up.
    /// </param>
    /// <returns>Velocity lost to gravity, m/s.</returns>
    /// <remarks>
    /// <para>
    /// dv_gravity = g0 * t * sin(theta). The physical picture is simple: while the engine burns,
    /// gravity is continuously subtracting from the vehicle's vertical velocity, and only the
    /// vertical component of the flight path is affected.
    /// </para>
    /// <para>
    /// Two design consequences fall out immediately. First, <i>burn fast</i>: the loss is
    /// proportional to time, so high thrust-to-weight cuts it. Second, <i>turn early</i>: at
    /// theta = 0, flying horizontally, the gravity loss vanishes entirely. That is precisely
    /// what a gravity turn is for, and why a launcher pitches over within seconds of clearing
    /// the tower rather than climbing vertically. An engine with T/W just above 1 barely
    /// accelerates at all while burning propellant at full rate, which is the worst possible
    /// place to be.
    /// </para>
    /// </remarks>
    public static double GravityLoss(double burnTimeSeconds, double flightPathAngleDegrees)
    {
        Guard.NonNegative(burnTimeSeconds, nameof(burnTimeSeconds));
        Guard.InRange(flightPathAngleDegrees, -90.0, 90.0, nameof(flightPathAngleDegrees));

        double angleRadians = flightPathAngleDegrees * Math.PI / 180.0;

        return Constants.StandardGravity * burnTimeSeconds * Math.Sin(angleRadians);
    }

    /// <summary>
    /// Aerodynamic drag force.
    /// </summary>
    /// <param name="dragCoefficient">Drag coefficient C_D, dimensionless.</param>
    /// <param name="referenceArea">Reference (frontal) area, m^2.</param>
    /// <param name="density">Air density, kg/m^3.</param>
    /// <param name="velocity">Speed, m/s.</param>
    /// <returns>Drag force in N.</returns>
    public static double Drag(
        double dragCoefficient,
        double referenceArea,
        double density,
        double velocity)
    {
        Guard.NonNegative(dragCoefficient, nameof(dragCoefficient));
        Guard.Positive(referenceArea, nameof(referenceArea));
        Guard.NonNegative(density, nameof(density));

        return 0.5 * dragCoefficient * referenceArea * density * velocity * velocity;
    }

    /// <summary>
    /// Dynamic pressure q = 0.5 * rho * v^2.
    /// </summary>
    /// <param name="density">Air density, kg/m^3.</param>
    /// <param name="velocity">Speed, m/s.</param>
    /// <returns>Dynamic pressure in Pa.</returns>
    /// <remarks>
    /// The single most important structural load number during ascent. Density falls
    /// exponentially while velocity rises, so their product peaks - "max-Q" - typically around
    /// 11-14 km and 30-80 s into flight. Vehicles routinely throttle down through it, which is
    /// the origin of the call "throttling up" heard after max-Q has passed.
    /// </remarks>
    public static double DynamicPressure(double density, double velocity)
    {
        Guard.NonNegative(density, nameof(density));

        return 0.5 * density * velocity * velocity;
    }

    /// <summary>
    /// Air density from a simple exponential atmosphere.
    /// </summary>
    /// <param name="altitudeMetres">Altitude above sea level, m.</param>
    /// <returns>Density in kg/m^3.</returns>
    public static double DensityAtAltitude(double altitudeMetres)
    {
        Guard.NonNegative(altitudeMetres, nameof(altitudeMetres));

        return Constants.SeaLevelDensity
               * Math.Exp(-altitudeMetres / Constants.AtmosphericScaleHeight);
    }

    /// <summary>
    /// Circular orbital velocity at a given altitude above Earth.
    /// </summary>
    /// <param name="altitudeMetres">Altitude above the surface, m.</param>
    /// <returns>Orbital speed in m/s.</returns>
    /// <remarks>
    /// v = sqrt(mu / r). At 200 km this is 7784 m/s. Comparing it with the ideal delta-v a
    /// vehicle can produce is the fastest sanity check on any launcher design.
    /// </remarks>
    public static double CircularOrbitVelocity(double altitudeMetres)
    {
        Guard.NonNegative(altitudeMetres, nameof(altitudeMetres));

        return Math.Sqrt(Constants.EarthMu / (Constants.EarthRadius + altitudeMetres));
    }

    /// <summary>
    /// Integrates a purely vertical ascent, accounting for gravity, drag and the falling
    /// ambient pressure.
    /// </summary>
    /// <param name="initialMass">Liftoff mass, kg.</param>
    /// <param name="propellantMass">Propellant available, kg.</param>
    /// <param name="vacuumThrust">Thrust in vacuum, N.</param>
    /// <param name="specificImpulseSeconds">Vacuum specific impulse, s.</param>
    /// <param name="exitArea">Nozzle exit area, m^2, used for the pressure-thrust correction.</param>
    /// <param name="exitPressure">Nozzle exit static pressure, Pa.</param>
    /// <param name="dragCoefficient">Drag coefficient C_D.</param>
    /// <param name="referenceArea">Frontal reference area, m^2.</param>
    /// <param name="timeStep">Integration time step, s.</param>
    /// <returns>The full trajectory and the loss accounting.</returns>
    /// <remarks>
    /// <para>
    /// A straight vertical climb is not how anything is actually flown - it maximises gravity
    /// loss and reaches no useful orbit - but it is the classic teaching case because every
    /// term is visible and nothing is hidden inside a guidance algorithm. Comparing the
    /// integrated burnout velocity with the ideal rocket-equation delta-v shows exactly where
    /// the missing kilometres per second went.
    /// </para>
    /// <para>
    /// The integration is explicit Euler. That is deliberate: it is transparent, it matches how
    /// the equations are written in the textbook, and with a small time step it is entirely
    /// adequate for demonstrating loss magnitudes. It is not appropriate for real trajectory
    /// design, where a Runge-Kutta scheme and a proper atmosphere model belong.
    /// </para>
    /// </remarks>
    public static AscentResult VerticalAscent(
        double initialMass,
        double propellantMass,
        double vacuumThrust,
        double specificImpulseSeconds,
        double exitArea,
        double exitPressure,
        double dragCoefficient,
        double referenceArea,
        double timeStep = 0.1)
    {
        Guard.Positive(initialMass, nameof(initialMass));
        Guard.Positive(propellantMass, nameof(propellantMass));
        Guard.Positive(vacuumThrust, nameof(vacuumThrust));
        Guard.Positive(specificImpulseSeconds, nameof(specificImpulseSeconds));
        Guard.Positive(exitArea, nameof(exitArea));
        Guard.NonNegative(exitPressure, nameof(exitPressure));
        Guard.NonNegative(dragCoefficient, nameof(dragCoefficient));
        Guard.Positive(referenceArea, nameof(referenceArea));
        Guard.InRange(timeStep, 1e-4, 5.0, nameof(timeStep));

        if (propellantMass >= initialMass)
        {
            throw new ArgumentOutOfRangeException(
                nameof(propellantMass),
                propellantMass,
                "Propellant mass must be less than the total liftoff mass.");
        }

        // Vacuum exhaust velocity fixes the mass flow; mdot is constant for a fixed-throttle
        // engine because the throat is choked.
        double vacuumExhaustVelocity = specificImpulseSeconds * Constants.StandardGravity;
        double massFlowRate = vacuumThrust / vacuumExhaustVelocity;
        double burnTime = propellantMass / massFlowRate;

        List<AscentSample> samples = [];

        double time = 0.0;
        double altitude = 0.0;
        double velocity = 0.0;
        double mass = initialMass;

        double gravityLoss = 0.0;
        double dragLoss = 0.0;
        double maxDynamicPressure = 0.0;
        double maxDynamicPressureTime = 0.0;

        while (time < burnTime)
        {
            double step = Math.Min(timeStep, burnTime - time);

            double ambientPressure = NozzleLosses.AmbientPressureAtAltitude(altitude);
            double density = DensityAtAltitude(altitude);

            // Thrust rises as the vehicle climbs, because the pressure term stops fighting it.
            double thrust = vacuumThrust - (ambientPressure * exitArea);

            double dragForce = Drag(dragCoefficient, referenceArea, density, velocity);
            double dynamicPressure = DynamicPressure(density, velocity);

            if (dynamicPressure > maxDynamicPressure)
            {
                maxDynamicPressure = dynamicPressure;
                maxDynamicPressureTime = time;
            }

            // Newton's second law for a vertical climb: thrust up, drag and weight down.
            double acceleration = (thrust - dragForce) / mass - Constants.StandardGravity;

            samples.Add(new AscentSample(
                time, altitude, velocity, mass, acceleration, dynamicPressure));

            // Accumulate the losses as velocity increments never delivered to the vehicle.
            // This is exactly the bookkeeping that turns ideal delta-v into achieved velocity.
            gravityLoss += Constants.StandardGravity * step;
            dragLoss += dragForce / mass * step;

            velocity += acceleration * step;
            altitude += velocity * step;
            mass -= massFlowRate * step;
            time += step;

            // A vehicle with T/W below 1 will be driven backwards through the pad by this
            // integration. Clamp it so the result reports a failed launch rather than
            // nonsense negative altitudes.
            if (altitude < 0.0)
            {
                altitude = 0.0;
                velocity = 0.0;
            }
        }

        double idealDeltaV = RocketEquation.DeltaV(
            vacuumExhaustVelocity, initialMass, initialMass - propellantMass);

        return new AscentResult(
            Samples: samples,
            BurnoutTime: burnTime,
            BurnoutAltitude: altitude,
            BurnoutVelocity: velocity,
            IdealDeltaV: idealDeltaV,
            GravityLoss: gravityLoss,
            DragLoss: dragLoss,
            MaxDynamicPressure: maxDynamicPressure,
            MaxDynamicPressureTime: maxDynamicPressureTime);
    }
}
