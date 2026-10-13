namespace Propulsion.Core;

/// <summary>
/// The fundamental performance definitions of Chapter 2 of Sutton: thrust, effective exhaust
/// velocity, specific impulse, total impulse, propellant mass fraction and mass ratio.
/// </summary>
/// <remarks>
/// <para>
/// Everything in this class follows from a single momentum balance. A rocket throws mass
/// backwards; the reaction is thrust. If the nozzle exit pressure does not happen to equal the
/// ambient pressure, there is an additional pressure force acting on the exit plane. Hence the
/// two-term thrust equation
/// </para>
/// <code>
///     F = mdot * v_e + (p_e - p_a) * A_e
///           momentum      pressure
/// </code>
/// <para>
/// The whole point of <i>effective</i> exhaust velocity is to hide that second term: define
/// c = F / mdot and the thrust equation collapses back to the tidy F = mdot * c. That is why c,
/// and not the actual gas velocity v_e, is what appears in the rocket equation.
/// </para>
/// </remarks>
public static class Performance
{
    /// <summary>
    /// Computes thrust from the full two-term thrust equation.
    /// </summary>
    /// <param name="massFlowRate">Propellant mass flow rate mdot, kg/s.</param>
    /// <param name="exitVelocity">Nozzle exit gas velocity v_e, m/s.</param>
    /// <param name="exitPressure">Static pressure at the nozzle exit plane p_e, Pa.</param>
    /// <param name="ambientPressure">Ambient (back) pressure p_a, Pa. Zero in vacuum.</param>
    /// <param name="exitArea">Nozzle exit plane area A_e, m^2.</param>
    /// <returns>Thrust in newtons.</returns>
    /// <remarks>
    /// <para>
    /// Note the sign of the pressure term. When p_e &gt; p_a the nozzle is <i>under-expanded</i>:
    /// the gas is still pushing outward at the exit plane and the term adds thrust. When
    /// p_e &lt; p_a it is <i>over-expanded</i>, ambient pressure pushes back in, and the term
    /// subtracts. This is precisely why the same engine produces noticeably more thrust in
    /// vacuum than at sea level, and why a nozzle sized for vacuum cannot simply be lit at sea
    /// level - over-expansion can drive flow separation and destroy the nozzle.
    /// </para>
    /// </remarks>
    public static double Thrust(
        double massFlowRate,
        double exitVelocity,
        double exitPressure,
        double ambientPressure,
        double exitArea)
    {
        Guard.Positive(massFlowRate, nameof(massFlowRate));
        Guard.Positive(exitVelocity, nameof(exitVelocity));
        Guard.NonNegative(exitPressure, nameof(exitPressure));
        Guard.NonNegative(ambientPressure, nameof(ambientPressure));
        Guard.Positive(exitArea, nameof(exitArea));

        return (massFlowRate * exitVelocity) + ((exitPressure - ambientPressure) * exitArea);
    }

    /// <summary>
    /// Effective exhaust velocity, c = F / mdot.
    /// </summary>
    /// <param name="thrust">Measured or computed thrust F, N.</param>
    /// <param name="massFlowRate">Propellant mass flow rate mdot, kg/s.</param>
    /// <returns>Effective exhaust velocity in m/s.</returns>
    /// <remarks>
    /// This is the single most useful number for an engine, because it is exactly what the
    /// rocket equation consumes. It absorbs the pressure-thrust term, so it depends on altitude
    /// even though the true gas velocity does not change much.
    /// </remarks>
    public static double EffectiveExhaustVelocity(double thrust, double massFlowRate)
    {
        Guard.Positive(thrust, nameof(thrust));
        Guard.Positive(massFlowRate, nameof(massFlowRate));

        return thrust / massFlowRate;
    }

    /// <summary>
    /// Specific impulse in seconds, Isp = c / g0.
    /// </summary>
    /// <param name="effectiveExhaustVelocity">Effective exhaust velocity c, m/s.</param>
    /// <returns>Specific impulse in seconds.</returns>
    /// <remarks>
    /// <para>
    /// Isp is nothing more than c divided by the defined constant
    /// <see cref="Constants.StandardGravity"/>. It carries no extra information whatsoever.
    /// It survives because it happens to have the same numerical value whether you work in SI
    /// or US customary units, which mattered enormously before 1970 and is now merely
    /// convenient tradition.
    /// </para>
    /// <para>
    /// Physically it answers: how many seconds can one kilogram of propellant produce one
    /// kilogram-force of thrust? Higher is better. LOX/RP-1 sits near 300 s at sea level,
    /// LOX/LH2 near 450 s in vacuum, and an ion thruster can exceed 3000 s.
    /// </para>
    /// </remarks>
    public static double SpecificImpulse(double effectiveExhaustVelocity)
    {
        Guard.Positive(effectiveExhaustVelocity, nameof(effectiveExhaustVelocity));

        return effectiveExhaustVelocity / Constants.StandardGravity;
    }

    /// <summary>
    /// Effective exhaust velocity from specific impulse, c = Isp * g0.
    /// </summary>
    /// <param name="specificImpulseSeconds">Specific impulse Isp, s.</param>
    /// <returns>Effective exhaust velocity in m/s.</returns>
    public static double ExhaustVelocityFromIsp(double specificImpulseSeconds)
    {
        Guard.Positive(specificImpulseSeconds, nameof(specificImpulseSeconds));

        return specificImpulseSeconds * Constants.StandardGravity;
    }

    /// <summary>
    /// Total impulse for a constant thrust burn, It = F * t.
    /// </summary>
    /// <param name="thrust">Thrust F, N.</param>
    /// <param name="burnTimeSeconds">Burn duration t, s.</param>
    /// <returns>Total impulse in N*s.</returns>
    /// <remarks>
    /// Total impulse is the area under the thrust-time curve and is the figure of merit for a
    /// solid motor, where you cannot throttle: it is the entire momentum budget the motor will
    /// ever deliver. Amateur and commercial motor classes (A, B, C, ... M) are defined by
    /// doubling bands of total impulse.
    /// </remarks>
    public static double TotalImpulse(double thrust, double burnTimeSeconds)
    {
        Guard.Positive(thrust, nameof(thrust));
        Guard.Positive(burnTimeSeconds, nameof(burnTimeSeconds));

        return thrust * burnTimeSeconds;
    }

    /// <summary>
    /// Specific impulse computed from total impulse and propellant weight.
    /// </summary>
    /// <param name="totalImpulse">Total impulse It, N*s.</param>
    /// <param name="propellantMass">Total propellant mass consumed, kg.</param>
    /// <returns>Specific impulse in seconds.</returns>
    /// <remarks>
    /// This is the definition used to reduce static-fire test data: integrate the load cell,
    /// weigh the propellant before and after, divide. It automatically averages over startup
    /// and tail-off transients, which is why a measured Isp is always a little below the
    /// steady-state value.
    /// </remarks>
    public static double SpecificImpulseFromTotalImpulse(double totalImpulse, double propellantMass)
    {
        Guard.Positive(totalImpulse, nameof(totalImpulse));
        Guard.Positive(propellantMass, nameof(propellantMass));

        return totalImpulse / (propellantMass * Constants.StandardGravity);
    }

    /// <summary>
    /// Mass flow rate required to produce a given thrust at a given effective exhaust velocity.
    /// </summary>
    /// <param name="thrust">Required thrust F, N.</param>
    /// <param name="effectiveExhaustVelocity">Effective exhaust velocity c, m/s.</param>
    /// <returns>Required propellant mass flow rate in kg/s.</returns>
    public static double MassFlowRate(double thrust, double effectiveExhaustVelocity)
    {
        Guard.Positive(thrust, nameof(thrust));
        Guard.Positive(effectiveExhaustVelocity, nameof(effectiveExhaustVelocity));

        return thrust / effectiveExhaustVelocity;
    }

    /// <summary>
    /// Propellant mass fraction, zeta = m_p / m_0.
    /// </summary>
    /// <param name="propellantMass">Usable propellant mass m_p, kg.</param>
    /// <param name="initialMass">Total initial (wet) mass m_0, kg.</param>
    /// <returns>The fraction of the initial mass that is propellant, between 0 and 1.</returns>
    /// <remarks>
    /// A launch vehicle stage is typically 0.85 to 0.95 propellant by mass. That is a startling
    /// number: the structure, engines, avionics and payload together account for well under a
    /// fifth of what sits on the pad. It is the direct consequence of the exponential in the
    /// rocket equation, and it is why rockets are built like eggshells.
    /// </remarks>
    public static double PropellantMassFraction(double propellantMass, double initialMass)
    {
        Guard.Positive(propellantMass, nameof(propellantMass));
        Guard.Positive(initialMass, nameof(initialMass));

        if (propellantMass > initialMass)
        {
            throw new ArgumentOutOfRangeException(
                nameof(propellantMass),
                propellantMass,
                "Propellant mass cannot exceed the initial mass of the vehicle.");
        }

        return propellantMass / initialMass;
    }

    /// <summary>
    /// Mass ratio, MR = m_f / m_0 (final over initial).
    /// </summary>
    /// <param name="initialMass">Initial (wet) mass m_0, kg.</param>
    /// <param name="finalMass">Final (burnout) mass m_f, kg.</param>
    /// <returns>The dimensionless mass ratio, between 0 and 1.</returns>
    /// <remarks>
    /// Sutton defines MR as final/initial, so it is a number less than one, and relates to the
    /// propellant mass fraction by MR = 1 - zeta. Be careful when reading other sources: many
    /// define the mass ratio the other way up, as initial/final, which is greater than one.
    /// The library is internally consistent - the rocket equation here takes the two masses
    /// separately so the convention never has a chance to bite.
    /// </remarks>
    public static double MassRatio(double initialMass, double finalMass)
    {
        Guard.Positive(initialMass, nameof(initialMass));
        Guard.Positive(finalMass, nameof(finalMass));

        if (finalMass > initialMass)
        {
            throw new ArgumentOutOfRangeException(
                nameof(finalMass), finalMass, "Final mass cannot exceed initial mass.");
        }

        return finalMass / initialMass;
    }

    /// <summary>
    /// Thrust-to-weight ratio of a vehicle at Earth's surface.
    /// </summary>
    /// <param name="thrust">Thrust F, N.</param>
    /// <param name="mass">Vehicle mass, kg.</param>
    /// <returns>The dimensionless thrust-to-weight ratio.</returns>
    /// <remarks>
    /// A vertically launched vehicle needs T/W greater than 1 simply to leave the pad, and in
    /// practice 1.2 to 1.5 at liftoff: too low and gravity losses become ruinous, too high and
    /// the vehicle hits maximum aerodynamic pressure at excessive speed. An upper stage
    /// igniting in vacuum has no such constraint and often has T/W well below 1.
    /// </remarks>
    public static double ThrustToWeightRatio(double thrust, double mass)
    {
        Guard.Positive(thrust, nameof(thrust));
        Guard.Positive(mass, nameof(mass));

        return thrust / (mass * Constants.StandardGravity);
    }
}
