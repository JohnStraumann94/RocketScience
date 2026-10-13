namespace Propulsion.Core;

/// <summary>
/// Thermochemistry of the combustion gas: specific heats, speed of sound, isentropic stagnation
/// relations and a simple adiabatic flame temperature estimate. Sutton Chapter 5.
/// </summary>
/// <remarks>
/// Sutton's Chapter 5 is really about chemical equilibrium, which in practice means running a
/// Gibbs free-energy minimisation code such as NASA CEA. That is well outside the scope of a
/// teaching library, so what is provided here is the thermodynamic scaffolding around it: the
/// relations that convert between the gas properties an equilibrium code would hand you and the
/// quantities the nozzle theory in <see cref="NozzleTheory"/> needs.
/// </remarks>
public static class Thermochemistry
{
    /// <summary>
    /// Specific heat at constant pressure, from gamma and the specific gas constant.
    /// </summary>
    /// <param name="gamma">Ratio of specific heats.</param>
    /// <param name="specificGasConstant">Specific gas constant R, J/(kg*K).</param>
    /// <returns>c_p in J/(kg*K).</returns>
    /// <remarks>
    /// From cp - cv = R and gamma = cp/cv it follows that cp = gamma*R/(gamma-1). Note how large
    /// this becomes as gamma approaches 1: a gas with many active internal degrees of freedom
    /// stores a great deal of energy without getting hot, which is exactly why low-gamma
    /// combustion products expand so productively through a nozzle.
    /// </remarks>
    public static double SpecificHeatConstantPressure(double gamma, double specificGasConstant)
    {
        Guard.Gamma(gamma);
        Guard.Positive(specificGasConstant, nameof(specificGasConstant));

        return gamma * specificGasConstant / (gamma - 1.0);
    }

    /// <summary>
    /// Specific heat at constant volume, from gamma and the specific gas constant.
    /// </summary>
    /// <param name="gamma">Ratio of specific heats.</param>
    /// <param name="specificGasConstant">Specific gas constant R, J/(kg*K).</param>
    /// <returns>c_v in J/(kg*K).</returns>
    public static double SpecificHeatConstantVolume(double gamma, double specificGasConstant)
    {
        Guard.Gamma(gamma);
        Guard.Positive(specificGasConstant, nameof(specificGasConstant));

        return specificGasConstant / (gamma - 1.0);
    }

    /// <summary>
    /// Local speed of sound in the combustion gas.
    /// </summary>
    /// <param name="gamma">Ratio of specific heats.</param>
    /// <param name="specificGasConstant">Specific gas constant R, J/(kg*K).</param>
    /// <param name="temperature">Static temperature, K.</param>
    /// <returns>Speed of sound in m/s.</returns>
    /// <remarks>
    /// a = sqrt(gamma * R * T). In a hot, light rocket exhaust this is enormous - well over
    /// 1000 m/s in the chamber, against 340 m/s in cool air. That matters because the exhaust
    /// velocity is only a few times the local speed of sound, so exit Mach numbers of 3-5 are
    /// typical even though the jet is moving at several kilometres per second.
    /// </remarks>
    public static double SpeedOfSound(double gamma, double specificGasConstant, double temperature)
    {
        Guard.Gamma(gamma);
        Guard.Positive(specificGasConstant, nameof(specificGasConstant));
        Guard.Positive(temperature, nameof(temperature));

        return Math.Sqrt(gamma * specificGasConstant * temperature);
    }

    /// <summary>
    /// Ratio of stagnation to static temperature at a given Mach number.
    /// </summary>
    /// <param name="gamma">Ratio of specific heats.</param>
    /// <param name="machNumber">Mach number.</param>
    /// <returns>T0 / T.</returns>
    public static double StagnationTemperatureRatio(double gamma, double machNumber)
    {
        Guard.Gamma(gamma);
        Guard.NonNegative(machNumber, nameof(machNumber));

        return 1.0 + ((gamma - 1.0) / 2.0 * machNumber * machNumber);
    }

    /// <summary>
    /// Ratio of stagnation to static pressure at a given Mach number.
    /// </summary>
    /// <param name="gamma">Ratio of specific heats.</param>
    /// <param name="machNumber">Mach number.</param>
    /// <returns>p0 / p.</returns>
    /// <remarks>
    /// The isentropic exponent gamma/(gamma-1) makes this rise very steeply with Mach number:
    /// at M = 4 with gamma = 1.2 the stagnation pressure is more than 150 times the static
    /// pressure. It is that reservoir of stagnation pressure in the chamber that the nozzle
    /// spends to accelerate the gas.
    /// </remarks>
    public static double StagnationPressureRatio(double gamma, double machNumber)
    {
        Guard.Gamma(gamma);
        Guard.NonNegative(machNumber, nameof(machNumber));

        return Math.Pow(StagnationTemperatureRatio(gamma, machNumber), gamma / (gamma - 1.0));
    }

    /// <summary>
    /// Static temperature at a station in the nozzle with a given Mach number.
    /// </summary>
    /// <param name="gamma">Ratio of specific heats.</param>
    /// <param name="chamberTemperature">Chamber (stagnation) temperature, K.</param>
    /// <param name="machNumber">Local Mach number.</param>
    /// <returns>Local static temperature in K.</returns>
    /// <remarks>
    /// The gas cools dramatically as it accelerates - a 3500 K chamber can drop below 1200 K at
    /// the exit of a large nozzle. That is why the nozzle extension can be radiatively cooled
    /// while the throat needs aggressive regenerative cooling, and why exhaust plumes of some
    /// propellants condense visibly once the static temperature falls far enough.
    /// </remarks>
    public static double StaticTemperature(
        double gamma,
        double chamberTemperature,
        double machNumber)
    {
        Guard.Positive(chamberTemperature, nameof(chamberTemperature));

        return chamberTemperature / StagnationTemperatureRatio(gamma, machNumber);
    }

    /// <summary>
    /// A first-order adiabatic flame temperature estimate.
    /// </summary>
    /// <param name="heatOfReaction">
    /// Heat released per kilogram of propellant mixture, J/kg.
    /// </param>
    /// <param name="specificHeat">Mean specific heat of the products, J/(kg*K).</param>
    /// <param name="initialTemperature">Initial propellant temperature, K.</param>
    /// <returns>Estimated adiabatic flame temperature in K.</returns>
    /// <remarks>
    /// <para>
    /// T_flame = T_initial + Q / cp, from an energy balance that assumes all the released heat
    /// goes into raising the product temperature and none is lost to the walls.
    /// </para>
    /// <para>
    /// This runs <i>high</i>, often by several hundred kelvin, and the reason is instructive: at
    /// rocket chamber temperatures a significant fraction of the products dissociate. H2O breaks
    /// into OH and H, CO2 into CO and O. Dissociation absorbs energy and caps the temperature,
    /// which is why real flame temperatures plateau near 3600 K almost regardless of how
    /// energetic the propellant is. Some of that energy is recovered further down the nozzle as
    /// the gas cools and the radicals recombine - which is precisely the effect that frozen-flow
    /// nozzle theory ignores and equilibrium-flow theory captures.
    /// </para>
    /// </remarks>
    public static double AdiabaticFlameTemperature(
        double heatOfReaction,
        double specificHeat,
        double initialTemperature = 298.15)
    {
        Guard.Positive(heatOfReaction, nameof(heatOfReaction));
        Guard.Positive(specificHeat, nameof(specificHeat));
        Guard.Positive(initialTemperature, nameof(initialTemperature));

        return initialTemperature + (heatOfReaction / specificHeat);
    }

    /// <summary>
    /// Gas density from the ideal gas law.
    /// </summary>
    /// <param name="pressure">Static pressure, Pa.</param>
    /// <param name="specificGasConstant">Specific gas constant R, J/(kg*K).</param>
    /// <param name="temperature">Static temperature, K.</param>
    /// <returns>Density in kg/m^3.</returns>
    public static double Density(double pressure, double specificGasConstant, double temperature)
    {
        Guard.Positive(pressure, nameof(pressure));
        Guard.Positive(specificGasConstant, nameof(specificGasConstant));
        Guard.Positive(temperature, nameof(temperature));

        return pressure / (specificGasConstant * temperature);
    }
}
