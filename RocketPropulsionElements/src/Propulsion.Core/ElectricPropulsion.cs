namespace Propulsion.Core;

/// <summary>
/// Electric propulsion: the power-limited regime, where thrust is bought with electrical power
/// rather than with chemical energy stored in the propellant. Sutton Chapters 17 and 19.
/// </summary>
/// <remarks>
/// <para>
/// Chemical rockets are <i>energy limited</i>: the propellant carries its own energy, so exhaust
/// velocity is capped by chemistry at roughly 4.5 km/s and thrust is essentially free once you
/// have the propellant. Electric thrusters are <i>power limited</i>: exhaust velocity can be
/// anything you like, but every extra metre per second must be paid for with electrical power
/// from a solar array or reactor that you also have to carry.
/// </para>
/// <para>
/// That single difference reverses the design logic completely, and it is what produces the
/// optimum exhaust velocity in <see cref="OptimumExhaustVelocity"/>. There is no chemical
/// analogue: for a chemical engine, more Isp is always better.
/// </para>
/// </remarks>
public static class ElectricPropulsion
{
    /// <summary>
    /// Jet (kinetic) power in the exhaust beam.
    /// </summary>
    /// <param name="thrust">Thrust, N.</param>
    /// <param name="exhaustVelocity">Exhaust velocity, m/s.</param>
    /// <returns>Jet power in watts.</returns>
    /// <remarks>
    /// <para>
    /// P_jet = 0.5 * F * v_e, since P = 0.5 * mdot * v_e^2 and F = mdot * v_e.
    /// </para>
    /// <para>
    /// The factor of v_e is the tyranny of electric propulsion. Thrust per watt is 2/v_e, so a
    /// 3000 s ion thruster produces only about 68 micronewtons per watt. A kilowatt-class
    /// thruster therefore makes roughly 68 mN - about the weight of a few sheets of paper - and
    /// must run for months to accomplish anything. It does, however, run for months, and that
    /// is the trade.
    /// </para>
    /// </remarks>
    public static double JetPower(double thrust, double exhaustVelocity)
    {
        Guard.Positive(thrust, nameof(thrust));
        Guard.Positive(exhaustVelocity, nameof(exhaustVelocity));

        return 0.5 * thrust * exhaustVelocity;
    }

    /// <summary>
    /// Electrical input power required for a given thrust and exhaust velocity.
    /// </summary>
    /// <param name="thrust">Thrust, N.</param>
    /// <param name="exhaustVelocity">Exhaust velocity, m/s.</param>
    /// <param name="thrusterEfficiency">
    /// Overall efficiency: jet power divided by electrical input. Typically 0.4-0.5 for a Hall
    /// thruster and 0.6-0.8 for a gridded ion engine.
    /// </param>
    /// <returns>Required electrical power in watts.</returns>
    public static double RequiredElectricalPower(
        double thrust,
        double exhaustVelocity,
        double thrusterEfficiency)
    {
        Guard.InRange(thrusterEfficiency, 0.01, 1.0, nameof(thrusterEfficiency));

        return JetPower(thrust, exhaustVelocity) / thrusterEfficiency;
    }

    /// <summary>
    /// Thrust produced by a thruster at a given input power and exhaust velocity.
    /// </summary>
    /// <param name="electricalPower">Electrical input power, W.</param>
    /// <param name="exhaustVelocity">Exhaust velocity, m/s.</param>
    /// <param name="thrusterEfficiency">Overall thruster efficiency.</param>
    /// <returns>Thrust in newtons.</returns>
    public static double ThrustFromPower(
        double electricalPower,
        double exhaustVelocity,
        double thrusterEfficiency)
    {
        Guard.Positive(electricalPower, nameof(electricalPower));
        Guard.Positive(exhaustVelocity, nameof(exhaustVelocity));
        Guard.InRange(thrusterEfficiency, 0.01, 1.0, nameof(thrusterEfficiency));

        return 2.0 * thrusterEfficiency * electricalPower / exhaustVelocity;
    }

    /// <summary>
    /// The exhaust velocity that maximises payload for a power-limited mission.
    /// </summary>
    /// <param name="deltaV">Mission velocity change, m/s.</param>
    /// <param name="burnTimeSeconds">Available thrusting time, s.</param>
    /// <param name="specificPower">
    /// Specific power of the power plant, W/kg - array, conditioning and radiators included.
    /// Around 100 W/kg for current solar-electric systems.
    /// </param>
    /// <param name="thrusterEfficiency">Overall thruster efficiency.</param>
    /// <returns>The optimum exhaust velocity in m/s.</returns>
    /// <remarks>
    /// <para>
    /// The trade is a genuine optimum with a real peak, not a monotonic curve:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Raising v_e reduces propellant mass - the rocket equation improves.</description></item>
    ///   <item><description>But raising v_e also raises the power needed for a given thrust, and hence the mass of the power plant.</description></item>
    /// </list>
    /// <para>
    /// Balancing the two gives the characteristic velocity of the power plant,
    /// v_opt = sqrt(2 * eta * alpha * t), where alpha is specific power and t is thrusting time.
    /// </para>
    /// <para>
    /// The dependence on <i>time</i> is the interesting part, and it is why deep-space missions
    /// use much higher Isp than station-keeping missions. Give the thruster years to work and
    /// the optimum climbs; demand the manoeuvre in weeks and it falls. It is also why improving
    /// solar arrays (raising alpha) pushes optimum Isp up, quite independently of any
    /// improvement to the thruster itself.
    /// </para>
    /// </remarks>
    public static double OptimumExhaustVelocity(
        double deltaV,
        double burnTimeSeconds,
        double specificPower,
        double thrusterEfficiency)
    {
        Guard.Positive(deltaV, nameof(deltaV));
        Guard.Positive(burnTimeSeconds, nameof(burnTimeSeconds));
        Guard.Positive(specificPower, nameof(specificPower));
        Guard.InRange(thrusterEfficiency, 0.01, 1.0, nameof(thrusterEfficiency));

        return Math.Sqrt(2.0 * thrusterEfficiency * specificPower * burnTimeSeconds);
    }

    /// <summary>
    /// Mass of the power plant needed to supply a given electrical power.
    /// </summary>
    /// <param name="electricalPower">Required electrical power, W.</param>
    /// <param name="specificPower">Power plant specific power, W/kg.</param>
    /// <returns>Power plant mass in kg.</returns>
    /// <remarks>
    /// This is the mass that chemical propulsion simply does not have to carry, and it is the
    /// reason electric propulsion is useless for launch and superb for deep space. A 100 kW
    /// system at 100 W/kg weighs a tonne before any propellant or structure.
    /// </remarks>
    public static double PowerPlantMass(double electricalPower, double specificPower)
    {
        Guard.Positive(electricalPower, nameof(electricalPower));
        Guard.Positive(specificPower, nameof(specificPower));

        return electricalPower / specificPower;
    }

    /// <summary>
    /// Total mission mass breakdown for a power-limited electric propulsion mission.
    /// </summary>
    /// <param name="payloadMass">Payload mass, kg.</param>
    /// <param name="deltaV">Mission velocity change, m/s.</param>
    /// <param name="exhaustVelocity">Chosen exhaust velocity, m/s.</param>
    /// <param name="burnTimeSeconds">Thrusting time, s.</param>
    /// <param name="specificPower">Power plant specific power, W/kg.</param>
    /// <param name="thrusterEfficiency">Overall thruster efficiency.</param>
    /// <returns>
    /// The propellant mass, power plant mass, required thrust, required electrical power and
    /// total initial mass.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The calculation is circular in principle - propellant mass depends on the initial mass,
    /// which depends on the power plant mass, which depends on the thrust, which depends on the
    /// initial mass again. It is resolved here by fixed-point iteration, which converges in a
    /// handful of passes because the coupling is weak.
    /// </para>
    /// <para>
    /// The average thrust is estimated as F = m_avg * dv / t, using the mean of the initial and
    /// final masses. That is exact for constant acceleration and a good approximation here
    /// because electric missions have small mass ratios - the whole point is that they barely
    /// consume any propellant.
    /// </para>
    /// </remarks>
    public static (double PropellantMass, double PowerPlantMass, double Thrust,
                   double ElectricalPower, double InitialMass) MissionMassBreakdown(
        double payloadMass,
        double deltaV,
        double exhaustVelocity,
        double burnTimeSeconds,
        double specificPower,
        double thrusterEfficiency)
    {
        Guard.Positive(payloadMass, nameof(payloadMass));
        Guard.Positive(deltaV, nameof(deltaV));
        Guard.Positive(exhaustVelocity, nameof(exhaustVelocity));
        Guard.Positive(burnTimeSeconds, nameof(burnTimeSeconds));
        Guard.Positive(specificPower, nameof(specificPower));
        Guard.InRange(thrusterEfficiency, 0.01, 1.0, nameof(thrusterEfficiency));

        double massRatio = Math.Exp(deltaV / exhaustVelocity);

        // Seed the iteration with the payload alone as the dry mass, then let the power plant
        // mass grow into the answer.
        double powerPlantMass = 0.0;
        double propellantMass = 0.0;
        double initialMass = payloadMass;
        double thrust = 0.0;
        double electricalPower = 0.0;

        for (int iteration = 0; iteration < 100; iteration++)
        {
            double dryMass = payloadMass + powerPlantMass;

            initialMass = dryMass * massRatio;
            propellantMass = initialMass - dryMass;

            double averageMass = 0.5 * (initialMass + dryMass);
            thrust = averageMass * deltaV / burnTimeSeconds;

            electricalPower = RequiredElectricalPower(thrust, exhaustVelocity, thrusterEfficiency);

            double updatedPowerPlantMass = PowerPlantMass(electricalPower, specificPower);

            // Converged once the power plant mass stops moving by more than a gram.
            if (Math.Abs(updatedPowerPlantMass - powerPlantMass) < 1e-3)
            {
                powerPlantMass = updatedPowerPlantMass;
                break;
            }

            powerPlantMass = updatedPowerPlantMass;
        }

        return (propellantMass, powerPlantMass, thrust, electricalPower, initialMass);
    }
}
