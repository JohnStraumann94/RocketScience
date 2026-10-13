using System.Text;
using Propulsion.Core;

namespace Propulsion.App.Models;

/// <summary>
/// Chapters 5, 7 and 12 samples: thermochemistry and propellant selection.
/// </summary>
public static partial class SampleCatalog
{
    // ====================================================================================
    // CHAPTERS 5, 7 and 12 - Thermochemistry and Propellants
    // ====================================================================================

    /// <summary>
    /// Perfect-gas thermochemistry and isentropic stagnation relations.
    /// </summary>
    private static CodeSample ThermochemistryAndStagnationRelations() => new()
    {
        Id = "ch5-thermochemistry",
        Chapter = "Chapter 5",
        Title = "Thermochemistry and Isentropic Relations",
        Summary = "Specific heats, speed of sound, and conditions through the nozzle.",

        Theory =
            """
            Chapter 3's nozzle theory rests entirely on perfect-gas thermodynamics, and this
            sample exposes that foundation.

            For a perfect gas the specific heats are fixed by gamma and the specific gas
            constant R = R_universal / M:

                cp = gamma * R / (gamma - 1)
                cv = R / (gamma - 1)
                cp - cv = R          and          cp / cv = gamma

            The speed of sound depends only on the LOCAL temperature:

                a = sqrt(gamma * R * T)

            This matters more than it first appears. The gas cools dramatically as it expands
            down the nozzle, so the local speed of sound falls with it, which is why the Mach
            number can reach 4 or 5 at the exit even though the absolute velocity has only
            roughly tripled since the throat.

            The isentropic stagnation relations connect chamber (stagnation) conditions to
            conditions at any Mach number:

                T1/T = 1 + (gamma-1)/2 * M^2
                p1/p = [1 + (gamma-1)/2 * M^2]^(gamma/(gamma-1))

            At M = 1 - the throat, always - these reduce to constants that depend only on gamma:

                T_t/T1 = 2/(gamma+1)                      about 0.91 for gamma = 1.2
                p_t/p1 = [2/(gamma+1)]^(gamma/(gamma-1))  about 0.56 for gamma = 1.2

            So the throat always sits at roughly 91% of chamber temperature and 56% of chamber
            pressure, no matter what the engine is. That is a genuinely useful sanity check
            when reading someone else's engine data.

            The adiabatic flame temperature comes from balancing the heat released by the
            reaction against the heat capacity of the products:

                T_flame = T_initial + Q / cp

            Real CEA-class codes do this properly with equilibrium chemistry, dissociation and
            temperature-dependent cp. The simple form here is a first-order estimate only - it
            over-predicts, because at 3000 K a real exhaust is significantly dissociated and
            that dissociation absorbs energy.
            """,

        CodeSnippet =
            """
            double r  = NozzleTheory.SpecificGasConstant(molarMass);
            double cp = Thermochemistry.SpecificHeatConstantPressure(gamma, r);
            double cv = Thermochemistry.SpecificHeatConstantVolume(gamma, r);

            // Local speed of sound depends only on the LOCAL temperature.
            double a1 = Thermochemistry.SpeedOfSound(gamma, r, chamberTemperature);

            // Throat conditions: always at Mach 1, so they depend only on gamma.
            double throatTemperature = NozzleTheory.ThroatTemperature(gamma, chamberTemperature);
            double throatPressure    = NozzleTheory.ThroatPressure(gamma, chamberPressure);

            // Static conditions at an arbitrary Mach number.
            double staticT = Thermochemistry.StaticTemperature(gamma, chamberTemperature, mach);
            """,

        Parameters =
        [
            new SampleParameter("gamma", "Specific heat ratio gamma", 1.22, "", "Exhaust gas property."),
            new SampleParameter("mm", "Molecular mass M", 21.5, "kg/kmol", "Exhaust gas property."),
            new SampleParameter("t1", "Chamber temperature T1", 3600.0, "K", "Stagnation temperature."),
            new SampleParameter("p1", "Chamber pressure p1", 100.0, "bar", "Stagnation pressure."),
            new SampleParameter("mach", "Mach number of interest", 3.5, "", "Typical nozzle exit Mach."),
            new SampleParameter("q", "Heat of reaction per kg", 5.5e6, "J/kg", "For the flame temperature estimate.")
        ],

        Run = inputs =>
        {
            double gamma = inputs["gamma"];
            double mm = inputs["mm"];
            double t1 = inputs["t1"];
            double p1 = Constants.BarToPa(inputs["p1"]);
            double mach = inputs["mach"];
            double q = inputs["q"];

            double r = NozzleTheory.SpecificGasConstant(mm);
            double cp = Thermochemistry.SpecificHeatConstantPressure(gamma, r);
            double cv = Thermochemistry.SpecificHeatConstantVolume(gamma, r);

            StringBuilder report = new();

            report.AppendLine(Heading("GAS PROPERTIES"));
            report.AppendLine(Line("Universal gas constant R'", Constants.UniversalGasConstant, "J/(kmol.K)", "N4"));
            report.AppendLine(Line("Molecular mass M", mm, "kg/kmol", "N3"));
            report.AppendLine(Line("Specific gas constant R = R'/M", r, "J/(kg.K)", "N3"));
            report.AppendLine(Line("cp", cp, "J/(kg.K)", "N3"));
            report.AppendLine(Line("cv", cv, "J/(kg.K)", "N3"));
            report.AppendLine(Line("Check  cp - cv", cp - cv, "J/(kg.K)", "N3"));
            report.AppendLine(Line("Check  cp / cv", cp / cv, "", "N6"));
            report.AppendLine("  Those two checks must return R and gamma exactly.");
            report.AppendLine();

            report.AppendLine(Heading("CHAMBER (STAGNATION) CONDITIONS"));
            report.AppendLine(Line("Temperature T1", t1, "K", "N2"));
            report.AppendLine(Line("Pressure p1", Constants.PaToBar(p1), "bar", "N3"));
            report.AppendLine(Line("Density", Thermochemistry.Density(p1, r, t1), "kg/m^3", "N5"));
            report.AppendLine(Line("Speed of sound", Thermochemistry.SpeedOfSound(gamma, r, t1), "m/s", "N2"));
            report.AppendLine();

            double throatT = NozzleTheory.ThroatTemperature(gamma, t1);
            double throatP = NozzleTheory.ThroatPressure(gamma, p1);
            double throatA = Thermochemistry.SpeedOfSound(gamma, r, throatT);

            report.AppendLine(Heading("THROAT CONDITIONS (always Mach 1)"));
            report.AppendLine(Line("Tt / T1", throatT / t1, "", "N6"));
            report.AppendLine(Line("Check  2/(gamma+1)", 2.0 / (gamma + 1.0), "", "N6"));
            report.AppendLine(Line("Throat temperature Tt", throatT, "K", "N2"));
            report.AppendLine(Line("pt / p1", throatP / p1, "", "N6"));
            report.AppendLine(Line("Throat pressure pt", Constants.PaToBar(throatP), "bar", "N4"));
            report.AppendLine(Line("Throat velocity (= local sound speed)", throatA, "m/s", "N2"));
            report.AppendLine(Line("Throat density",
                Thermochemistry.Density(throatP, r, throatT), "kg/m^3", "N5"));
            report.AppendLine();

            report.AppendLine(Heading($"STATIC CONDITIONS AT MACH {mach:F2}"));
            double staticT = Thermochemistry.StaticTemperature(gamma, t1, mach);
            double tRatio = Thermochemistry.StagnationTemperatureRatio(gamma, mach);
            double pRatio = Thermochemistry.StagnationPressureRatio(gamma, mach);
            double staticP = p1 / pRatio;
            double localA = Thermochemistry.SpeedOfSound(gamma, r, staticT);

            report.AppendLine(Line("T1 / T", tRatio, "", "N5"));
            report.AppendLine(Line("Static temperature T", staticT, "K", "N2"));
            report.AppendLine(Line("p1 / p", pRatio, "", "N3"));
            report.AppendLine(Line("Static pressure p", Constants.PaToBar(staticP), "bar", "N6"));
            report.AppendLine(Line("Local speed of sound", localA, "m/s", "N2"));
            report.AppendLine(Line("Gas velocity  M * a", mach * localA, "m/s", "N2"));
            report.AppendLine(Line("Area ratio needed for this Mach",
                NozzleTheory.AreaRatioFromMach(gamma, mach), "", "N4"));
            report.AppendLine();
            report.AppendLine("  Compare the local sound speed here against the chamber value above.");
            report.AppendLine("  The gas has cooled so much that sound travels far more slowly, which");
            report.AppendLine("  is how the Mach number climbs so high on a modest velocity increase.");
            report.AppendLine();

            report.AppendLine(Heading("EXPANSION PROFILE THROUGH THE NOZZLE"));
            report.AppendLine("   Mach    T(K)     p(bar)      a(m/s)    v(m/s)    A/At");
            report.AppendLine("  " + new string('-', 62));
            foreach (double m in new[] { 0.1, 0.3, 0.5, 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5 })
            {
                double tm = Thermochemistry.StaticTemperature(gamma, t1, m);
                double pm = p1 / Thermochemistry.StagnationPressureRatio(gamma, m);
                double am = Thermochemistry.SpeedOfSound(gamma, r, tm);
                double areaRatio = NozzleTheory.AreaRatioFromMach(gamma, m);
                report.AppendLine(string.Format(
                    "  {0,5:F2}  {1,7:F1}  {2,9:F4}  {3,9:F1}  {4,8:F1}  {5,7:F3}",
                    m, tm, Constants.PaToBar(pm), am, m * am, areaRatio));
            }
            report.AppendLine();
            report.AppendLine("  A/At has a MINIMUM of 1.0 at Mach 1 and rises on both sides. That is");
            report.AppendLine("  the converging-diverging shape, and it is why the throat is a throat.");
            report.AppendLine();

            report.AppendLine(Heading("ADIABATIC FLAME TEMPERATURE ESTIMATE"));
            double flameT = Thermochemistry.AdiabaticFlameTemperature(q, cp);
            report.AppendLine(Line("Heat of reaction", q / 1.0e6, "MJ/kg", "N3"));
            report.AppendLine(Line("Estimated flame temperature", flameT, "K", "N1"));
            report.AppendLine(Line("Chamber temperature you entered", t1, "K", "N1"));
            report.AppendLine();
            report.AppendLine("  This simple estimate ignores dissociation. Above about 2500 K a real");
            report.AppendLine("  exhaust partially dissociates into H, OH, O and CO, which absorbs");
            report.AppendLine("  energy and caps the temperature well below this figure. Use a proper");
            report.AppendLine("  equilibrium code (NASA CEA) for anything real - see the Azure");
            report.AppendLine("  placeholder service, which is where a CEA wrapper would be called.");

            return report.ToString();
        }
    };

    /// <summary>
    /// Side-by-side comparison of the standard propellant combinations.
    /// </summary>
    private static CodeSample PropellantComparisonTable() => new()
    {
        Id = "ch7-propellant-comparison",
        Chapter = "Chapter 7",
        Title = "Propellant Comparison",
        Summary = "Isp versus density impulse - the trade that decides real vehicles.",

        Theory =
            """
            Specific impulse is not the only thing that matters, and treating it as such leads
            to bad vehicle designs.

            LOX/LH2 has the best Isp of any practical chemical combination, around 450 s in
            vacuum. But liquid hydrogen has a density of only 71 kg/m^3 - about one fourteenth
            the density of water. The bulk density of a LOX/LH2 stage at its optimum mixture
            ratio is around 350 kg/m^3, whereas LOX/RP-1 comes in near 1030 kg/m^3, three times
            denser. That means for the same propellant MASS, hydrogen tanks are three times
            larger. Larger tanks mean more structure, more insulation (LH2 boils at 20 K), more
            surface area, more drag, and a worse structural coefficient.

            The figure of merit that captures this is DENSITY IMPULSE:

                I_d = Isp * rho_bulk

            Density impulse tells you the impulse available per unit of TANK VOLUME rather than
            per unit of propellant weight. On this measure LOX/RP-1 comfortably beats LOX/LH2.

            Hence the near-universal design pattern for launch vehicles:
              - FIRST STAGE: dense propellant. It is volume-limited and drag-limited, it flies
                in the atmosphere, and it carries the entire upper vehicle. Density wins.
              - UPPER STAGES: high Isp. Mass ratio dominates there and the volume penalty is
                paid on a much smaller stage. Isp wins.

            Storable propellants (N2O4/UDMH, and the peroxide combinations) give up 20-30 s of
            Isp in exchange for something enormously valuable: they are liquid at room
            temperature and hypergolic, so they need no ignition system and can sit fuelled for
            years. Every deep-space probe and almost every satellite uses storables for exactly
            that reason. They are also toxic and carcinogenic, which is why newer designs are
            moving to LOX/methane and to monopropellants like LMP-103S.
            """,

        CodeSnippet =
            """
            foreach (PropellantCombination combo in Propellants.All)
            {
                double isp            = combo.IdealVacuumIsp(chamberPressure, areaRatio);
                double bulkDensity    = combo.BulkDensity;
                double densityImpulse = combo.DensityImpulse(chamberPressure, areaRatio);

                Report(combo.Name, combo.OptimumMixtureRatio, isp, bulkDensity, densityImpulse);
            }
            """,

        Parameters =
        [
            new SampleParameter("p1", "Chamber pressure", 100.0, "bar",
                "Applied identically to every combination for a fair comparison."),
            new SampleParameter("eps", "Area ratio", 40.0, "",
                "A vacuum-class expansion ratio."),
            new SampleParameter("mass", "Propellant load to size tanks for", 100000.0, "kg",
                "Used for the tank volume comparison at the bottom.")
        ],

        Run = inputs =>
        {
            double p1 = Constants.BarToPa(inputs["p1"]);
            double eps = inputs["eps"];
            double propellantMass = inputs["mass"];

            StringBuilder report = new();

            report.AppendLine(Heading($"AT {Constants.PaToBar(p1):F0} bar CHAMBER PRESSURE, AREA RATIO {eps:F0}"));
            report.AppendLine();
            report.AppendLine("  Combination            O/F    T1(K)   M     Isp(s)  rho(kg/m3)  Id(s.kg/m3)");
            report.AppendLine("  " + new string('-', 78));

            foreach (PropellantCombination combo in Propellants.All)
            {
                report.AppendLine(string.Format(
                    "  {0,-20} {1,5:F2} {2,8:F0} {3,6:F1} {4,8:F1} {5,10:F0} {6,12:F0}",
                    combo.Name,
                    combo.OptimumMixtureRatio,
                    combo.ChamberTemperature,
                    combo.MolecularMass,
                    combo.IdealVacuumIsp(p1, eps),
                    combo.BulkDensity,
                    combo.DensityImpulse(p1, eps)));
            }

            report.AppendLine();
            report.AppendLine("  Isp is computed from ideal frozen-flow theory with representative gas");
            report.AppendLine("  properties, so treat it as accurate to a few percent, not to a second.");
            report.AppendLine();

            // Rank by each figure of merit so the trade-off is unmissable.
            List<PropellantCombination> all = [.. Propellants.All];

            report.AppendLine(Heading("RANKED BY SPECIFIC IMPULSE"));
            int rank = 1;
            foreach (PropellantCombination combo in all.OrderByDescending(c => c.IdealVacuumIsp(p1, eps)))
            {
                report.AppendLine($"  {rank++}. {combo.Name,-22} {combo.IdealVacuumIsp(p1, eps),7:F1} s");
            }
            report.AppendLine();

            report.AppendLine(Heading("RANKED BY DENSITY IMPULSE"));
            rank = 1;
            foreach (PropellantCombination combo in all.OrderByDescending(c => c.DensityImpulse(p1, eps)))
            {
                report.AppendLine($"  {rank++}. {combo.Name,-22} {combo.DensityImpulse(p1, eps),9:F0} s.kg/m^3");
            }
            report.AppendLine();
            report.AppendLine("  Compare the two orderings. If they disagree - and they will - you are");
            report.AppendLine("  looking at the central propellant trade of launch vehicle design.");
            report.AppendLine();

            report.AppendLine(Heading($"TANK VOLUME FOR {propellantMass:N0} kg OF PROPELLANT"));
            report.AppendLine("  Combination            Ox vol(m3)  Fuel vol(m3)  Total(m3)  Sphere dia(m)");
            report.AppendLine("  " + new string('-', 74));
            foreach (PropellantCombination combo in Propellants.All)
            {
                (double oxVolume, double fuelVolume) = Propellants.TankVolumes(propellantMass, combo);
                double total = oxVolume + fuelVolume;

                // Equivalent single-sphere diameter, as a crude size intuition.
                double sphereDiameter = 2.0 * Math.Cbrt(3.0 * total / (4.0 * Math.PI));

                report.AppendLine(string.Format(
                    "  {0,-20} {1,11:F2} {2,13:F2} {3,10:F2} {4,14:F2}",
                    combo.Name, oxVolume, fuelVolume, total, sphereDiameter));
            }
            report.AppendLine();
            report.AppendLine("  Volumes include a 5% ullage allowance. Look at how much bigger the");
            report.AppendLine("  hydrogen tankage is for exactly the same propellant mass - that extra");
            report.AppendLine("  volume has to be built, insulated, pressurised and flown through the");
            report.AppendLine("  atmosphere, and it is what eats hydrogen's Isp advantage on a first stage.");
            report.AppendLine();

            report.AppendLine(Heading("NOTES"));
            foreach (PropellantCombination combo in Propellants.All)
            {
                report.AppendLine($"  {combo.Name}:");
                report.AppendLine($"      {combo.Oxidizer} / {combo.Fuel}");
                report.AppendLine($"      {combo.Notes}");
            }

            return report.ToString();
        }
    };

    /// <summary>
    /// Mixture ratio, flow split and tank sizing for one chosen combination.
    /// </summary>
    private static CodeSample TankVolumeAndMixtureRatio() => new()
    {
        Id = "ch7-mixture-ratio",
        Chapter = "Chapter 7",
        Title = "Mixture Ratio, Flow Split and Tank Sizing",
        Summary = "Turning a total flow rate into two separate propellant systems.",

        Theory =
            """
            The mixture ratio is defined as the mass flow of oxidiser divided by the mass flow
            of fuel:

                r = m_dot_ox / m_dot_fuel

            Given a total flow rate, the split follows from mass conservation:

                m_dot_ox   = m_dot_total * r / (1 + r)
                m_dot_fuel = m_dot_total * 1 / (1 + r)

            A crucial and often surprising point: the OPTIMUM mixture ratio for performance is
            NOT the stoichiometric ratio. Engines deliberately run FUEL RICH. For LOX/LH2 the
            stoichiometric ratio is 8:1 by mass, but flight engines run around 5.5:1 or even
            6:1. Running fuel rich does lower the flame temperature, which by itself would hurt
            performance - but it lowers the average molecular mass of the exhaust by more,
            leaving unburnt H2 (M = 2) in the products. Since exhaust velocity goes as
            sqrt(T/M), the M reduction wins.

            There are two useful side effects. First, a cooler chamber is dramatically easier to
            cool and is far kinder to the injector face and throat. Second, an oxidiser-rich
            environment at 3500 K attacks metals aggressively, so fuel-rich operation protects
            the hardware. Oxidiser-rich staged combustion, as used by the Russian RD-180 family,
            is metallurgically very hard for exactly this reason.

            Tank volumes follow from the split masses and the individual densities, plus an
            ullage allowance - typically 3-6% of the tank volume left as gas space for thermal
            expansion, pressurisation gas and propellant management.
            """,

        CodeSnippet =
            """
            // Split a total mass flow according to the mixture ratio r = ox / fuel.
            (double oxFlow, double fuelFlow) = Propellants.SplitFlow(totalFlow, mixtureRatio);

            // Tank volumes from the split masses, densities and an ullage allowance.
            (double oxVolume, double fuelVolume) =
                Propellants.TankVolumes(propellantMass, combination, ullageFraction: 0.05);
            """,

        Parameters =
        [
            new SampleParameter("choice", "Propellant (1=LOX/LH2 2=LOX/RP-1 3=LOX/CH4 4=N2O4/UDMH 5=H2O2/RP-1)",
                2.0, "", "Selects one entry from the Propellants catalogue."),
            new SampleParameter("mdot", "Total mass flow rate", 300.0, "kg/s",
                "Total of oxidiser plus fuel."),
            new SampleParameter("mr", "Mixture ratio r (0 = use the optimum)", 0.0, "",
                "Enter 0 to use the combination's optimum, or override it."),
            new SampleParameter("mass", "Total propellant mass", 100000.0, "kg",
                "Determines the tank sizes."),
            new SampleParameter("ullage", "Ullage fraction", 0.05, "",
                "Gas space left in each tank, typically 0.03-0.06."),
            new SampleParameter("p1", "Chamber pressure", 100.0, "bar", "For the Isp figures."),
            new SampleParameter("eps", "Area ratio", 25.0, "", "For the Isp figures.")
        ],

        Run = inputs =>
        {
            int choice = Math.Clamp(inputs.Integer("choice"), 1, Propellants.All.Count);
            PropellantCombination combo = Propellants.All[choice - 1];

            double totalFlow = inputs["mdot"];
            double enteredMr = inputs["mr"];
            double propellantMass = inputs["mass"];
            double ullage = Math.Clamp(inputs["ullage"], 0.0, 0.5);
            double p1 = Constants.BarToPa(inputs["p1"]);
            double eps = inputs["eps"];

            // Zero means "use the catalogued optimum", which is the common case.
            double mixtureRatio = enteredMr > 0.0 ? enteredMr : combo.OptimumMixtureRatio;

            (double oxFlow, double fuelFlow) = Propellants.SplitFlow(totalFlow, mixtureRatio);
            (double oxVolume, double fuelVolume) = Propellants.TankVolumes(propellantMass, combo, ullage);

            double oxMass = propellantMass * mixtureRatio / (1.0 + mixtureRatio);
            double fuelMass = propellantMass / (1.0 + mixtureRatio);

            StringBuilder report = new();

            report.AppendLine(Heading("PROPELLANT COMBINATION"));
            report.AppendLine(Line("Name", combo.Name));
            report.AppendLine(Line("Oxidiser", combo.Oxidizer));
            report.AppendLine(Line("Fuel", combo.Fuel));
            report.AppendLine(Line("Optimum mixture ratio", combo.OptimumMixtureRatio, "", "N3"));
            report.AppendLine(Line("Mixture ratio in use", mixtureRatio, "", "N3"));
            report.AppendLine(Line("Chamber temperature", combo.ChamberTemperature, "K", "N1"));
            report.AppendLine(Line("Exhaust molecular mass", combo.MolecularMass, "kg/kmol", "N2"));
            report.AppendLine(Line("Gamma", combo.Gamma, "", "N4"));
            report.AppendLine(Line("Vacuum Isp", combo.IdealVacuumIsp(p1, eps), "s", "N2"));
            report.AppendLine(Line("Bulk density", combo.BulkDensity, "kg/m^3", "N2"));
            report.AppendLine(Line("Density impulse", combo.DensityImpulse(p1, eps), "s.kg/m^3", "N1"));
            report.AppendLine();
            report.AppendLine($"  {combo.Notes}");
            report.AppendLine();

            report.AppendLine(Heading("FLOW SPLIT"));
            report.AppendLine(Line("Total mass flow", totalFlow, "kg/s", "N3"));
            report.AppendLine(Line("Oxidiser flow", oxFlow, "kg/s", "N3"));
            report.AppendLine(Line("Fuel flow", fuelFlow, "kg/s", "N3"));
            report.AppendLine(Line("Check  sum", oxFlow + fuelFlow, "kg/s", "N3"));
            report.AppendLine(Line("Check  ox / fuel", oxFlow / fuelFlow, "", "N4"));
            report.AppendLine(Line("Oxidiser volumetric flow",
                1000.0 * oxFlow / combo.OxidizerDensity, "litre/s", "N3"));
            report.AppendLine(Line("Fuel volumetric flow",
                1000.0 * fuelFlow / combo.FuelDensity, "litre/s", "N3"));
            report.AppendLine();

            report.AppendLine(Heading("PROPELLANT LOAD"));
            report.AppendLine(Line("Total propellant mass", propellantMass, "kg", "N1"));
            report.AppendLine(Line("Oxidiser mass", oxMass, "kg", "N1"));
            report.AppendLine(Line("Fuel mass", fuelMass, "kg", "N1"));
            report.AppendLine(Line("Burn time at this flow rate",
                RocketEquation.BurnTime(propellantMass, totalFlow), "s", "N2"));
            report.AppendLine();

            report.AppendLine(Heading($"TANK SIZING (ullage {ullage:P1})"));
            report.AppendLine(Line("Oxidiser density", combo.OxidizerDensity, "kg/m^3", "N1"));
            report.AppendLine(Line("Fuel density", combo.FuelDensity, "kg/m^3", "N1"));
            report.AppendLine(Line("Oxidiser tank volume", oxVolume, "m^3", "N3"));
            report.AppendLine(Line("Fuel tank volume", fuelVolume, "m^3", "N3"));
            report.AppendLine(Line("Total tank volume", oxVolume + fuelVolume, "m^3", "N3"));
            report.AppendLine(Line("Volume ratio fuel / oxidiser", fuelVolume / oxVolume, "", "N4"));
            report.AppendLine();

            // Express the tanks as cylinders of a plausible vehicle diameter, since that is
            // how a real stage layout is actually driven.
            const double vehicleDiameter = 3.7;
            double crossSection = NozzleGeometry.AreaFromDiameter(vehicleDiameter);
            report.AppendLine(Line($"Assumed vehicle diameter", vehicleDiameter, "m", "N2"));
            report.AppendLine(Line("Oxidiser tank length", oxVolume / crossSection, "m", "N2"));
            report.AppendLine(Line("Fuel tank length", fuelVolume / crossSection, "m", "N2"));
            report.AppendLine(Line("Combined tank length",
                (oxVolume + fuelVolume) / crossSection, "m", "N2"));
            report.AppendLine();

            report.AppendLine(Heading("MIXTURE RATIO SWEEP - flow split sensitivity"));
            report.AppendLine("     r     Ox flow    Fuel flow   Ox tank(m3)  Fuel tank(m3)");
            report.AppendLine("  " + new string('-', 62));
            for (double r = combo.OptimumMixtureRatio * 0.6;
                 r <= combo.OptimumMixtureRatio * 1.45;
                 r += combo.OptimumMixtureRatio * 0.1)
            {
                (double ox, double fu) = Propellants.SplitFlow(totalFlow, r);
                double oxM = propellantMass * r / (1.0 + r);
                double fuM = propellantMass / (1.0 + r);
                double oxV = oxM / combo.OxidizerDensity / (1.0 - ullage);
                double fuV = fuM / combo.FuelDensity / (1.0 - ullage);
                report.AppendLine(string.Format(
                    "  {0,5:F2}  {1,9:F2}  {2,11:F2}  {3,12:F2}  {4,13:F2}",
                    r, ox, fu, oxV, fuV));
            }
            report.AppendLine();
            report.AppendLine("  Real engines run FUEL RICH of stoichiometric. That trades a lower");
            report.AppendLine("  flame temperature for a lower exhaust molecular mass, and because");
            report.AppendLine("  exhaust velocity scales as sqrt(T/M), the molecular mass wins. It");
            report.AppendLine("  also keeps the chamber coolable and stops hot oxygen eating the wall.");

            return report.ToString();
        }
    };
}
