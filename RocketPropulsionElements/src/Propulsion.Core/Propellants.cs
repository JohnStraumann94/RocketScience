namespace Propulsion.Core;

/// <summary>
/// A propellant combination and its representative combustion properties.
/// </summary>
/// <param name="Name">Display name, e.g. "LOX / RP-1".</param>
/// <param name="Oxidizer">Oxidizer name.</param>
/// <param name="Fuel">Fuel name.</param>
/// <param name="OptimumMixtureRatio">
/// Oxidizer-to-fuel mass ratio typically flown. Note that this is usually <i>fuel-rich</i>
/// relative to the stoichiometric value - see the remarks on <see cref="Propellants"/>.
/// </param>
/// <param name="ChamberTemperature">Representative chamber flame temperature, K.</param>
/// <param name="MolecularMass">Mean exhaust molecular mass, kg/kmol.</param>
/// <param name="Gamma">Representative ratio of specific heats for the expansion.</param>
/// <param name="OxidizerDensity">Oxidizer density at storage conditions, kg/m^3.</param>
/// <param name="FuelDensity">Fuel density at storage conditions, kg/m^3.</param>
/// <param name="Notes">Practical engineering remarks.</param>
public readonly record struct PropellantCombination(
    string Name,
    string Oxidizer,
    string Fuel,
    double OptimumMixtureRatio,
    double ChamberTemperature,
    double MolecularMass,
    double Gamma,
    double OxidizerDensity,
    double FuelDensity,
    string Notes)
{
    /// <summary>
    /// Bulk density of the loaded propellant, kg/m^3.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The mass-weighted harmonic mean of the two densities at the flown mixture ratio. This is
    /// the number that decides how big the tanks have to be, and therefore how much the tanks,
    /// insulation and interstage weigh.
    /// </para>
    /// <para>
    /// It is the whole reason kerosene has not been displaced by hydrogen at the bottom of
    /// launch vehicles. LOX/LH2 has a far better Isp, but a bulk density around 360 kg/m^3
    /// against roughly 1030 kg/m^3 for LOX/RP-1 - nearly three times the tank volume for the
    /// same propellant mass, on the stage where tank mass and drag hurt most.
    /// </para>
    /// </remarks>
    public double BulkDensity
    {
        get
        {
            // Take 1 kg of fuel; then there are r kg of oxidizer with it.
            double oxidizerMass = OptimumMixtureRatio;
            const double fuelMass = 1.0;

            double totalVolume = (oxidizerMass / OxidizerDensity) + (fuelMass / FuelDensity);

            return (oxidizerMass + fuelMass) / totalVolume;
        }
    }

    /// <summary>
    /// Ideal vacuum specific impulse at a specified expansion ratio.
    /// </summary>
    /// <param name="chamberPressure">Chamber pressure, Pa.</param>
    /// <param name="areaRatio">Expansion area ratio.</param>
    /// <returns>Ideal vacuum Isp in seconds.</returns>
    /// <remarks>
    /// Computed from first principles via <see cref="EngineAnalysis"/> rather than quoted from
    /// a table, so it responds correctly to changes in chamber pressure and expansion ratio.
    /// Because the model is frozen-equilibrium ideal flow, it runs a few percent optimistic
    /// against real engines.
    /// </remarks>
    public double IdealVacuumIsp(double chamberPressure, double areaRatio)
    {
        EngineOperatingPoint point = EngineAnalysis.Analyse(
            Gamma, ChamberTemperature, MolecularMass, chamberPressure, areaRatio,
            throatArea: 0.01, ambientPressure: 0.0);

        return point.SpecificImpulse;
    }

    /// <summary>
    /// Density specific impulse, Isp multiplied by bulk density.
    /// </summary>
    /// <param name="chamberPressure">Chamber pressure, Pa.</param>
    /// <param name="areaRatio">Expansion area ratio.</param>
    /// <returns>Density impulse in s*kg/m^3.</returns>
    /// <remarks>
    /// The correct figure of merit whenever tank volume, rather than propellant mass, is the
    /// binding constraint - which is the case for a first stage, for a missile that must fit in
    /// a silo, and for anything volume-limited. Ranking propellants by density impulse instead
    /// of plain Isp reorders the table dramatically and puts dense storables back near the top.
    /// </remarks>
    public double DensityImpulse(double chamberPressure, double areaRatio)
        => IdealVacuumIsp(chamberPressure, areaRatio) * BulkDensity;
}

/// <summary>
/// A small reference catalogue of the propellant combinations discussed in Sutton
/// Chapters 5, 7 and 12.
/// </summary>
/// <remarks>
/// <para>
/// The values here are representative rather than authoritative. Real chamber temperature,
/// molecular mass and gamma all depend on mixture ratio, chamber pressure and how far chemical
/// equilibrium is allowed to shift during expansion; producing them properly requires a
/// chemical-equilibrium code such as NASA CEA. They are correct to within a few percent, which
/// is exactly the accuracy the ideal-nozzle theory in this library deserves.
/// </para>
/// <para>
/// One pattern is worth pointing out because it looks like an error and is not: every optimum
/// mixture ratio listed is <i>fuel-rich</i> compared with the stoichiometric value. LOX/LH2 is
/// stoichiometric at 8.0 but is flown near 5.5-6.0. The reason is that exhaust velocity depends
/// on sqrt(T/M), not on T alone. Running fuel-rich leaves unburned light species - free H2 - in
/// the exhaust, and the fall in mean molecular mass more than repays the drop in flame
/// temperature. It also, conveniently, keeps the chamber cooler.
/// </para>
/// </remarks>
public static class Propellants
{
    /// <summary>Liquid oxygen and liquid hydrogen - the highest-performing chemical pair.</summary>
    public static readonly PropellantCombination LoxLh2 = new(
        Name: "LOX / LH2",
        Oxidizer: "Liquid oxygen",
        Fuel: "Liquid hydrogen",
        OptimumMixtureRatio: 5.5,
        ChamberTemperature: 3250.0,
        MolecularMass: 10.0,
        Gamma: 1.24,
        OxidizerDensity: 1141.0,
        FuelDensity: 71.0,
        Notes: "Best Isp of any practical chemical pair (450+ s in vacuum), driven almost "
             + "entirely by the very low exhaust molecular mass. Penalties: hydrogen is "
             + "cryogenic at 20 K, extremely bulky, leaks through almost anything, and "
             + "embrittles many metals. Used where Isp dominates - upper stages, and the "
             + "Shuttle/SLS core.");

    /// <summary>Liquid oxygen and refined kerosene - the classic dense first-stage pair.</summary>
    public static readonly PropellantCombination LoxRp1 = new(
        Name: "LOX / RP-1",
        Oxidizer: "Liquid oxygen",
        Fuel: "RP-1 kerosene",
        OptimumMixtureRatio: 2.56,
        ChamberTemperature: 3670.0,
        MolecularMass: 21.9,
        Gamma: 1.24,
        OxidizerDensity: 1141.0,
        FuelDensity: 810.0,
        Notes: "The workhorse first-stage combination: F-1, RD-180, Merlin. Isp around 300 s "
             + "at sea level and 340 s in vacuum, but roughly three times the bulk density of "
             + "LOX/LH2, so tanks are small and light. Main headache is coking - kerosene "
             + "cracks and deposits carbon in regenerative cooling passages.");

    /// <summary>Liquid oxygen and liquid methane - the modern reusable choice.</summary>
    public static readonly PropellantCombination LoxMethane = new(
        Name: "LOX / CH4",
        Oxidizer: "Liquid oxygen",
        Fuel: "Liquid methane",
        OptimumMixtureRatio: 3.4,
        ChamberTemperature: 3500.0,
        MolecularMass: 20.0,
        Gamma: 1.24,
        OxidizerDensity: 1141.0,
        FuelDensity: 423.0,
        Notes: "A deliberate compromise: Isp better than kerosene, density far better than "
             + "hydrogen, and both propellants are cryogenic at similar temperatures which "
             + "simplifies tank design. Burns clean, so no coking, which matters enormously "
             + "for rapid reuse. Also producible from Martian CO2 via Sabatier. Raptor, "
             + "BE-4, Prometheus.");

    /// <summary>Nitrogen tetroxide and hydrazine derivatives - storable and hypergolic.</summary>
    public static readonly PropellantCombination N2O4Udmh = new(
        Name: "N2O4 / UDMH",
        Oxidizer: "Nitrogen tetroxide",
        Fuel: "Unsymmetrical dimethylhydrazine",
        OptimumMixtureRatio: 2.6,
        ChamberTemperature: 3200.0,
        MolecularMass: 21.0,
        Gamma: 1.25,
        OxidizerDensity: 1450.0,
        FuelDensity: 793.0,
        Notes: "Hypergolic - ignites on contact, so no ignition system and unlimited restarts. "
             + "Storable for years at room temperature, which is why it flew the Apollo "
             + "ascent engine and still runs most spacecraft. Isp only ~320 s vacuum, and "
             + "both propellants are severely toxic and carcinogenic; ground handling "
             + "dominates the cost.");

    /// <summary>Hydrogen peroxide and kerosene - a low-toxicity storable pair.</summary>
    public static readonly PropellantCombination PeroxideKerosene = new(
        Name: "H2O2 / Kerosene",
        Oxidizer: "High-test hydrogen peroxide (98%)",
        Fuel: "Kerosene",
        OptimumMixtureRatio: 7.5,
        ChamberTemperature: 2950.0,
        MolecularMass: 22.0,
        Gamma: 1.22,
        OxidizerDensity: 1440.0,
        FuelDensity: 810.0,
        Notes: "Dense, storable and comparatively benign - the decomposition products are "
             + "steam and oxygen. Isp is modest at around 310 s vacuum. HTP decomposes "
             + "catalytically, which makes excellent monopropellant gas generators, but it "
             + "is fussy about material compatibility and contamination.");

    /// <summary>Ammonium perchlorate composite propellant - the standard solid.</summary>
    public static readonly PropellantCombination ApcpSolid = new(
        Name: "APCP (solid)",
        Oxidizer: "Ammonium perchlorate",
        Fuel: "HTPB binder + aluminium",
        OptimumMixtureRatio: 4.5,
        ChamberTemperature: 3400.0,
        MolecularMass: 26.0,
        Gamma: 1.18,
        OxidizerDensity: 1950.0,
        FuelDensity: 1500.0,
        Notes: "Not a mixture ratio you can vary in flight - it is cast into the grain. "
             + "Isp around 265 s vacuum, but bulk density near 1800 kg/m^3 gives outstanding "
             + "density impulse, which is why boosters and missiles use it. Aluminium raises "
             + "the flame temperature but the condensed alumina causes a two-phase flow loss "
             + "in the nozzle. Once lit, it cannot be throttled or shut down.");

    /// <summary>Every combination in the catalogue.</summary>
    /// <returns>A read-only list of all catalogued propellant combinations.</returns>
    public static IReadOnlyList<PropellantCombination> All =>
    [
        LoxLh2,
        LoxRp1,
        LoxMethane,
        N2O4Udmh,
        PeroxideKerosene,
        ApcpSolid
    ];

    /// <summary>
    /// Oxidizer and fuel mass flow rates for a given total flow and mixture ratio.
    /// </summary>
    /// <param name="totalMassFlowRate">Total propellant mass flow rate, kg/s.</param>
    /// <param name="mixtureRatio">Oxidizer-to-fuel mass ratio r = mdot_ox / mdot_fuel.</param>
    /// <returns>The oxidizer and fuel flow rates in kg/s.</returns>
    /// <remarks>
    /// From mdot = mdot_ox + mdot_f and r = mdot_ox / mdot_f it follows immediately that
    /// mdot_f = mdot / (1 + r). These two numbers size the turbopumps, the injector orifices
    /// and the tanks, so they are usually the first thing computed after the total flow.
    /// </remarks>
    public static (double OxidizerFlow, double FuelFlow) SplitFlow(
        double totalMassFlowRate,
        double mixtureRatio)
    {
        Guard.Positive(totalMassFlowRate, nameof(totalMassFlowRate));
        Guard.Positive(mixtureRatio, nameof(mixtureRatio));

        double fuelFlow = totalMassFlowRate / (1.0 + mixtureRatio);
        double oxidizerFlow = totalMassFlowRate - fuelFlow;

        return (oxidizerFlow, fuelFlow);
    }

    /// <summary>
    /// Tank volumes required for a given propellant load.
    /// </summary>
    /// <param name="propellantMass">Total usable propellant mass, kg.</param>
    /// <param name="combination">The propellant combination.</param>
    /// <param name="ullageFraction">
    /// Extra volume as a fraction of the propellant volume, allowing for ullage gas, thermal
    /// expansion and residuals. 0.05 (5%) is a reasonable preliminary figure.
    /// </param>
    /// <returns>The oxidizer and fuel tank volumes in m^3.</returns>
    public static (double OxidizerVolume, double FuelVolume) TankVolumes(
        double propellantMass,
        PropellantCombination combination,
        double ullageFraction = 0.05)
    {
        Guard.Positive(propellantMass, nameof(propellantMass));
        Guard.InRange(ullageFraction, 0.0, 0.5, nameof(ullageFraction));

        (double oxidizerMass, double fuelMass) =
            SplitFlow(propellantMass, combination.OptimumMixtureRatio);

        double factor = 1.0 + ullageFraction;

        return (oxidizerMass / combination.OxidizerDensity * factor,
                fuelMass / combination.FuelDensity * factor);
    }
}
