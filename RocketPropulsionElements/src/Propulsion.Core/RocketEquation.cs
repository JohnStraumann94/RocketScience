namespace Propulsion.Core;

/// <summary>
/// The Tsiolkovsky rocket equation and its rearrangements, plus the burn-time and
/// mass-history relations that go with it. Sutton Chapter 4, "Flight Performance".
/// </summary>
/// <remarks>
/// <para>
/// The rocket equation falls straight out of conservation of momentum for a vehicle that
/// expels mass at constant effective exhaust velocity, with no external forces:
/// </para>
/// <code>
///     dv = c * ln(m_0 / m_f)
/// </code>
/// <para>
/// Two consequences dominate all vehicle design. First, dv depends only on the <i>ratio</i> of
/// the masses, not their absolute size - which is why a model rocket and an orbital launcher
/// have similar mass fractions. Second, the relationship is logarithmic in mass but linear in
/// c, so buying performance through propellant is subject to brutal diminishing returns while
/// buying it through a better engine is not. Doubling the mass ratio buys one extra factor of
/// ln(2)*c; doubling c doubles the dv outright.
/// </para>
/// </remarks>
public static class RocketEquation
{
    /// <summary>
    /// Ideal velocity change from initial and final mass.
    /// </summary>
    /// <param name="effectiveExhaustVelocity">Effective exhaust velocity c, m/s.</param>
    /// <param name="initialMass">Initial (wet) mass m_0, kg.</param>
    /// <param name="finalMass">Final (burnout) mass m_f, kg.</param>
    /// <returns>The ideal velocity change dv, m/s.</returns>
    /// <remarks>
    /// "Ideal" means gravity, drag and steering losses are all excluded. For a real launch to
    /// low Earth orbit those losses add roughly 1.5 to 2.0 km/s on top of the ~7.8 km/s orbital
    /// velocity, which is why launch vehicles are sized for around 9.3 to 9.5 km/s of ideal dv.
    /// See <see cref="FlightPerformance"/> for the loss terms.
    /// </remarks>
    public static double DeltaV(double effectiveExhaustVelocity, double initialMass, double finalMass)
    {
        Guard.Positive(effectiveExhaustVelocity, nameof(effectiveExhaustVelocity));
        Guard.Positive(initialMass, nameof(initialMass));
        Guard.Positive(finalMass, nameof(finalMass));

        if (finalMass > initialMass)
        {
            throw new ArgumentOutOfRangeException(
                nameof(finalMass),
                finalMass,
                "Final mass cannot exceed initial mass - the vehicle cannot gain propellant.");
        }

        return effectiveExhaustVelocity * Math.Log(initialMass / finalMass);
    }

    /// <summary>
    /// Ideal velocity change expressed in terms of specific impulse.
    /// </summary>
    /// <param name="specificImpulseSeconds">Specific impulse Isp, s.</param>
    /// <param name="initialMass">Initial (wet) mass m_0, kg.</param>
    /// <param name="finalMass">Final (burnout) mass m_f, kg.</param>
    /// <returns>The ideal velocity change dv, m/s.</returns>
    public static double DeltaVFromIsp(double specificImpulseSeconds, double initialMass, double finalMass)
        => DeltaV(Performance.ExhaustVelocityFromIsp(specificImpulseSeconds), initialMass, finalMass);

    /// <summary>
    /// The mass ratio m_0 / m_f required to achieve a given dv.
    /// </summary>
    /// <param name="deltaV">Required velocity change dv, m/s.</param>
    /// <param name="effectiveExhaustVelocity">Effective exhaust velocity c, m/s.</param>
    /// <returns>The required ratio of initial to final mass, a number of one or more.</returns>
    /// <remarks>
    /// This is the rocket equation solved the way a designer actually uses it: the mission
    /// hands you a dv, the engine hands you a c, and this tells you how much heavier the
    /// vehicle has to be at ignition than at burnout. Watch how fast it grows - at c = 3000 m/s,
    /// 9 km/s of dv demands a mass ratio of e^3 = 20, meaning 95% of the vehicle is propellant.
    /// </remarks>
    public static double RequiredMassRatio(double deltaV, double effectiveExhaustVelocity)
    {
        Guard.NonNegative(deltaV, nameof(deltaV));
        Guard.Positive(effectiveExhaustVelocity, nameof(effectiveExhaustVelocity));

        return Math.Exp(deltaV / effectiveExhaustVelocity);
    }

    /// <summary>
    /// The propellant mass required to give a known final mass a given dv.
    /// </summary>
    /// <param name="deltaV">Required velocity change dv, m/s.</param>
    /// <param name="effectiveExhaustVelocity">Effective exhaust velocity c, m/s.</param>
    /// <param name="finalMass">Burnout mass m_f (structure + payload), kg.</param>
    /// <returns>The propellant mass required, kg.</returns>
    /// <remarks>
    /// Derived by rearranging m_0 = m_f * exp(dv/c) and subtracting: m_p = m_f * (e^(dv/c) - 1).
    /// This is the form used to size a tank once the dry mass is known.
    /// </remarks>
    public static double RequiredPropellantMass(
        double deltaV,
        double effectiveExhaustVelocity,
        double finalMass)
    {
        Guard.Positive(finalMass, nameof(finalMass));

        return finalMass * (RequiredMassRatio(deltaV, effectiveExhaustVelocity) - 1.0);
    }

    /// <summary>
    /// The burnout mass reachable from a given initial mass and dv.
    /// </summary>
    /// <param name="deltaV">Velocity change dv, m/s.</param>
    /// <param name="effectiveExhaustVelocity">Effective exhaust velocity c, m/s.</param>
    /// <param name="initialMass">Initial (wet) mass m_0, kg.</param>
    /// <returns>The mass remaining at burnout, kg.</returns>
    public static double FinalMass(double deltaV, double effectiveExhaustVelocity, double initialMass)
    {
        Guard.Positive(initialMass, nameof(initialMass));

        return initialMass / RequiredMassRatio(deltaV, effectiveExhaustVelocity);
    }

    /// <summary>
    /// Burn time for a constant mass flow rate.
    /// </summary>
    /// <param name="propellantMass">Propellant mass to be consumed, kg.</param>
    /// <param name="massFlowRate">Mass flow rate mdot, kg/s.</param>
    /// <returns>Burn duration in seconds.</returns>
    public static double BurnTime(double propellantMass, double massFlowRate)
    {
        Guard.Positive(propellantMass, nameof(propellantMass));
        Guard.Positive(massFlowRate, nameof(massFlowRate));

        return propellantMass / massFlowRate;
    }

    /// <summary>
    /// Vehicle mass at a given time into a constant-mdot burn.
    /// </summary>
    /// <param name="initialMass">Initial (wet) mass m_0, kg.</param>
    /// <param name="massFlowRate">Mass flow rate mdot, kg/s.</param>
    /// <param name="timeSeconds">Elapsed time since ignition, s.</param>
    /// <returns>The instantaneous vehicle mass, kg.</returns>
    /// <remarks>
    /// Linear in time, because mdot is constant. This is what makes the acceleration of a
    /// constant-thrust rocket climb hyperbolically towards burnout - the numerator of F/m holds
    /// still while the denominator falls away. Shuttle throttled its main engines down at the
    /// end of ascent purely to keep this from exceeding 3 g.
    /// </remarks>
    public static double MassAtTime(double initialMass, double massFlowRate, double timeSeconds)
    {
        Guard.Positive(initialMass, nameof(initialMass));
        Guard.Positive(massFlowRate, nameof(massFlowRate));
        Guard.NonNegative(timeSeconds, nameof(timeSeconds));

        double mass = initialMass - (massFlowRate * timeSeconds);

        if (mass <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeSeconds),
                timeSeconds,
                "The vehicle has already consumed its entire mass by this time; " +
                "reduce the time or the flow rate.");
        }

        return mass;
    }

    /// <summary>
    /// Instantaneous acceleration during a constant-thrust burn, ignoring gravity and drag.
    /// </summary>
    /// <param name="thrust">Thrust F, N.</param>
    /// <param name="currentMass">Instantaneous vehicle mass, kg.</param>
    /// <returns>Acceleration in m/s^2.</returns>
    public static double Acceleration(double thrust, double currentMass)
    {
        Guard.Positive(thrust, nameof(thrust));
        Guard.Positive(currentMass, nameof(currentMass));

        return thrust / currentMass;
    }

    /// <summary>
    /// The payload fraction achievable for a given dv, exhaust velocity and structural
    /// coefficient.
    /// </summary>
    /// <param name="deltaV">Required velocity change dv, m/s.</param>
    /// <param name="effectiveExhaustVelocity">Effective exhaust velocity c, m/s.</param>
    /// <param name="structuralCoefficient">
    /// The structural coefficient epsilon = m_structure / (m_structure + m_propellant). A good
    /// launch stage manages 0.06 to 0.12.
    /// </param>
    /// <returns>
    /// Payload mass as a fraction of the stage's total initial mass. A negative result is
    /// returned as zero and means the mission is impossible with this stage - see remarks.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The derivation splits m_0 into payload, structure and propellant, then eliminates the
    /// propellant using the rocket equation. The result is
    /// </para>
    /// <code>
    ///     lambda = (e^(-dv/c) - epsilon) / (1 - epsilon)
    /// </code>
    /// <para>
    /// The term e^(-dv/c) is the fraction of the vehicle that survives to burnout. If the
    /// structure alone already weighs more than that, there is nothing left over for payload
    /// and the answer goes negative. That single inequality, dv &gt; -c*ln(epsilon), is the
    /// mathematical reason staging exists: no single stage of realistic construction can reach
    /// orbit with useful payload, so you throw the empty structure away and start again with a
    /// smaller epsilon burden.
    /// </para>
    /// </remarks>
    public static double PayloadFraction(
        double deltaV,
        double effectiveExhaustVelocity,
        double structuralCoefficient)
    {
        Guard.NonNegative(deltaV, nameof(deltaV));
        Guard.Positive(effectiveExhaustVelocity, nameof(effectiveExhaustVelocity));
        Guard.InRange(structuralCoefficient, 0.0, 0.999, nameof(structuralCoefficient));

        double survivingFraction = Math.Exp(-deltaV / effectiveExhaustVelocity);
        double payloadFraction = (survivingFraction - structuralCoefficient)
                                 / (1.0 - structuralCoefficient);

        // A negative payload fraction is physically meaningless; report it as zero and let the
        // caller notice that the mission is infeasible rather than propagating a negative mass.
        return Math.Max(payloadFraction, 0.0);
    }

    /// <summary>
    /// The maximum dv a single stage can deliver with zero payload.
    /// </summary>
    /// <param name="effectiveExhaustVelocity">Effective exhaust velocity c, m/s.</param>
    /// <param name="structuralCoefficient">Structural coefficient epsilon.</param>
    /// <returns>The limiting dv in m/s.</returns>
    /// <remarks>
    /// Setting the payload fraction to zero and solving gives dv_max = -c * ln(epsilon). With
    /// c = 3400 m/s and epsilon = 0.08 this is about 8.6 km/s - short of the roughly 9.4 km/s a
    /// launch actually needs, and that is with literally no payload at all.
    /// </remarks>
    public static double MaximumSingleStageDeltaV(
        double effectiveExhaustVelocity,
        double structuralCoefficient)
    {
        Guard.Positive(effectiveExhaustVelocity, nameof(effectiveExhaustVelocity));
        Guard.InRange(structuralCoefficient, 1e-6, 0.999, nameof(structuralCoefficient));

        return -effectiveExhaustVelocity * Math.Log(structuralCoefficient);
    }
}
