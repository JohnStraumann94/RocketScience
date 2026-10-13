using System.Text;
using Propulsion.Core;

namespace Propulsion.App.Models;

/// <summary>
/// Chapter 3 samples: nozzle theory and thermodynamic relations - the heart of the book.
/// </summary>
public static partial class SampleCatalog
{
    // ====================================================================================
    // CHAPTER 3 - Nozzle Theory and Thermodynamic Relations
    // ====================================================================================

    /// <summary>
    /// The ideal exhaust velocity equation and the sensitivity of each of its inputs.
    /// </summary>
    private static CodeSample IdealExhaustVelocityExplorer() => new()
    {
        Id = "ch3-ideal-exhaust-velocity",
        Chapter = "Chapter 3",
        Title = "Ideal Exhaust Velocity",
        Summary = "Sutton Eq. 3-15 and what each term is really worth.",

        Theory =
            """
            The single most important equation in the book. For isentropic expansion of a
            perfect gas from a large chamber down to exit pressure p2:

                v2 = sqrt( (2 * gamma / (gamma - 1)) * (R' * T1 / M) * [1 - (p2/p1)^((gamma-1)/gamma)] )

            where R' = 8314.46 J/(kmol.K) is the universal gas constant, M is the molecular
            mass of the exhaust in kg/kmol, T1 the chamber temperature and p1 the chamber
            pressure.

            Read the three groups separately, because they behave very differently:

            1. T1 / M is the term that actually matters. Note it appears under a square root,
               so v2 scales as sqrt(T1/M). This is why hydrogen wins: LOX/LH2 burns COOLER
               (about 3000 K) than LOX/RP-1 (about 3600 K), yet its exhaust has M near 10
               kg/kmol instead of 22, and 3000/10 beats 3600/22 by a factor of 1.8 - a 35%
               velocity advantage. Chasing flame temperature is a trap; chasing low molecular
               mass is the real game.

            2. The pressure ratio term [1 - (p2/p1)^((gamma-1)/gamma)] saturates. Going from
               p1/p2 = 10 to 100 buys a lot. Going from 100 to 1000 buys much less, and going
               to infinity (perfect vacuum expansion) only adds a few more percent. This is
               why chamber pressures plateau around 100-300 bar: the returns diminish while
               the pump power, wall thickness and cooling problem do not.

            3. gamma enters twice and its net effect is weak - lower gamma is slightly better,
               which is another small point in favour of complex polyatomic exhaust species.

            The theoretical ceiling is the value at p2 = 0, the maximum possible exhaust
            velocity. No real nozzle reaches it because that would need an infinite exit area.
            """,

        CodeSnippet =
            """
            // Sutton Eq. 3-15 - the ideal (isentropic, frozen, one-dimensional) exhaust velocity.
            double v2 = NozzleTheory.IdealExhaustVelocity(
                gamma:              gamma,
                chamberTemperature: t1,
                molecularMass:      molarMass,
                chamberPressure:    p1,
                exitPressure:       p2);

            // Specific gas constant for this exhaust: R = R_universal / M.
            double r = NozzleTheory.SpecificGasConstant(molarMass);

            // The theoretical ceiling, obtained by expanding to a perfect vacuum.
            double vMax = Math.Sqrt(2.0 * gamma / (gamma - 1.0) * r * t1);
            """,

        Parameters =
        [
            new SampleParameter("gamma", "Specific heat ratio gamma", 1.24, "",
                "Typically 1.13 (LH2) to 1.30 (solids). Weak effect."),
            new SampleParameter("t1", "Chamber temperature T1", 3500.0, "K",
                "Adiabatic flame temperature of the propellant combination."),
            new SampleParameter("mm", "Exhaust molecular mass M", 22.0, "kg/kmol",
                "About 10 for LOX/LH2, 22-24 for hydrocarbons, 25-30 for solids."),
            new SampleParameter("p1", "Chamber pressure p1", 70.0, "bar",
                "Modern engines run 60-300 bar."),
            new SampleParameter("p2", "Nozzle exit pressure p2", 1.0, "bar",
                "Set this to the design ambient pressure for optimum expansion.")
        ],

        Run = inputs =>
        {
            double gamma = inputs["gamma"];
            double t1 = inputs["t1"];
            double mm = inputs["mm"];
            double p1 = Constants.BarToPa(inputs["p1"]);
            double p2 = Constants.BarToPa(inputs["p2"]);

            if (p2 >= p1)
            {
                throw new ArgumentException(
                    "Exit pressure must be below chamber pressure - the gas has to expand " +
                    "for the nozzle to do anything useful.");
            }

            double r = NozzleTheory.SpecificGasConstant(mm);
            double v2 = NozzleTheory.IdealExhaustVelocity(gamma, t1, mm, p1, p2);

            // Decompose the equation so the report can attribute the answer to its parts.
            double gammaGroup = 2.0 * gamma / (gamma - 1.0);
            double thermalGroup = r * t1;
            double pressureGroup = 1.0 - Math.Pow(p2 / p1, (gamma - 1.0) / gamma);

            // Expanding to zero pressure sets the pressure group to exactly 1.
            double vMax = Math.Sqrt(gammaGroup * thermalGroup);

            StringBuilder report = new();

            report.AppendLine(Heading("EQUATION 3-15 TERM BY TERM"));
            report.AppendLine(Line("Specific gas constant R = R'/M", r, "J/(kg.K)", "N2"));
            report.AppendLine(Line("Gamma group 2g/(g-1)", gammaGroup, "", "N4"));
            report.AppendLine(Line("Thermal group R*T1", thermalGroup, "J/kg", "N0"));
            report.AppendLine(Line("Pressure ratio p1/p2", p1 / p2, "", "N2"));
            report.AppendLine(Line("Pressure group [1-(p2/p1)^((g-1)/g)]", pressureGroup, "", "N5"));
            report.AppendLine();

            report.AppendLine(Heading("RESULT"));
            report.AppendLine(Line("Ideal exhaust velocity v2", v2, "m/s", "N1"));
            report.AppendLine(Line("Equivalent specific impulse",
                Performance.SpecificImpulse(v2), "s", "N2"));
            report.AppendLine("  (that Isp assumes perfect expansion, so p2 = p3 and c = v2)");
            report.AppendLine();

            report.AppendLine(Heading("HOW CLOSE TO THE CEILING?"));
            report.AppendLine(Line("Maximum possible v2 (p2 -> 0)", vMax, "m/s", "N1"));
            report.AppendLine(Line("Fraction of ceiling achieved", 100.0 * v2 / vMax, "%", "N2"));
            report.AppendLine(Line("Velocity still on the table", vMax - v2, "m/s", "N1"));
            report.AppendLine();

            report.AppendLine(Heading("SENSITIVITY - one variable at a time, 10% change"));
            report.AppendLine(Line("+10% chamber temperature",
                NozzleTheory.IdealExhaustVelocity(gamma, t1 * 1.1, mm, p1, p2) - v2, "m/s", "N1"));
            report.AppendLine(Line("-10% molecular mass",
                NozzleTheory.IdealExhaustVelocity(gamma, t1, mm * 0.9, p1, p2) - v2, "m/s", "N1"));
            report.AppendLine(Line("+10% chamber pressure",
                NozzleTheory.IdealExhaustVelocity(gamma, t1, mm, p1 * 1.1, p2) - v2, "m/s", "N1"));
            report.AppendLine(Line("-10% exit pressure",
                NozzleTheory.IdealExhaustVelocity(gamma, t1, mm, p1, p2 * 0.9) - v2, "m/s", "N1"));
            report.AppendLine();
            report.AppendLine("  Compare the first two lines: they are nearly equal in magnitude,");
            report.AppendLine("  which is the sqrt(T1/M) grouping showing itself. Then compare");
            report.AppendLine("  against the chamber pressure line to see how weak p1 really is.");

            return report.ToString();
        }
    };

    /// <summary>
    /// c* and C_F, and the identity c = c* x C_F that separates chamber from nozzle.
    /// </summary>
    private static CodeSample CharacteristicVelocityAndThrustCoefficient() => new()
    {
        Id = "ch3-cstar-cf",
        Chapter = "Chapter 3",
        Title = "Characteristic Velocity c* and Thrust Coefficient C_F",
        Summary = "The identity c = c* x C_F, and why engineers test them separately.",

        Theory =
            """
            Rocket performance factors cleanly into two independent pieces, and this is one of
            the most useful ideas in the whole subject.

            CHARACTERISTIC VELOCITY c* measures the combustion chamber alone:

                c* = sqrt(R * T1 / gamma) / Gamma        (Sutton Eq. 3-32)

            where Gamma is the Vandenkerckhove function

                Gamma = sqrt(gamma) * (2/(gamma+1))^((gamma+1)/(2*(gamma-1)))

            Note what is ABSENT from c*: any mention of the nozzle exit, the area ratio, or the
            ambient pressure. c* depends only on the propellant chemistry and how completely it
            burns. That makes it the standard figure of merit for combustion efficiency: fire a
            chamber with a plain sonic throat, measure p1, m_dot and A_t, compute
            c* = p1 * A_t / m_dot, and compare against theory. A c* efficiency of 95-99% is the
            normal range, and anything low points at the injector.

            THRUST COEFFICIENT C_F measures the nozzle alone:

                C_F = Gamma * sqrt( 2*gamma/(gamma-1) * [1-(p2/p1)^((gamma-1)/gamma)] )
                      + (p2-p3)/p1 * A2/At        (Sutton Eq. 3-30)

            C_F is dimensionless and lives between about 0.6 (no divergent section at all) and
            2.25 (infinite area ratio in vacuum). Its theoretical maximum, at infinite expansion
            in vacuum, is C_F,max = Gamma * sqrt(2*gamma/(gamma-1)).

            The two combine exactly:

                c = c* x C_F        and        F = C_F * p1 * At

            That second form is remarkable: thrust is fully determined by the thrust
            coefficient, the chamber pressure and the throat area. You never need to know the
            mass flow rate or the exhaust velocity to size an engine for thrust.
            """,

        CodeSnippet =
            """
            double capitalGamma = NozzleTheory.VandenkerckhoveFunction(gamma);

            // Chamber-only figure of merit.
            double cStar = NozzleTheory.CharacteristicVelocity(gamma, t1, molarMass);

            // Nozzle-only figure of merit, at this specific ambient pressure.
            double cf = NozzleTheory.ThrustCoefficient(gamma, p1, p2, p3, areaRatio);

            // The identity - these two agree to within floating-point round-off.
            double cFromIdentity = cStar * cf;
            double thrust        = cf * p1 * throatArea;
            """,

        Parameters =
        [
            new SampleParameter("gamma", "Specific heat ratio gamma", 1.24, "", "Exhaust gas property."),
            new SampleParameter("t1", "Chamber temperature T1", 3500.0, "K", "Adiabatic flame temperature."),
            new SampleParameter("mm", "Exhaust molecular mass M", 22.0, "kg/kmol", "Exhaust gas property."),
            new SampleParameter("p1", "Chamber pressure p1", 70.0, "bar", "Chamber stagnation pressure."),
            new SampleParameter("eps", "Area ratio A2/At", 16.0, "", "Nozzle expansion ratio."),
            new SampleParameter("at", "Throat area At", 200.0, "cm^2", "Sets the physical size of the engine."),
            new SampleParameter("p3", "Ambient pressure p3", 1.01325, "bar", "Use 0 for vacuum.")
        ],

        Run = inputs =>
        {
            double gamma = inputs["gamma"];
            double t1 = inputs["t1"];
            double mm = inputs["mm"];
            double p1 = Constants.BarToPa(inputs["p1"]);
            double eps = inputs["eps"];
            double at = inputs["at"] * 1.0e-4;           // cm^2 entered, m^2 used internally
            double p3 = Constants.BarToPa(inputs["p3"]);

            double capitalGamma = NozzleTheory.VandenkerckhoveFunction(gamma);
            double cStar = NozzleTheory.CharacteristicVelocity(gamma, t1, mm);

            // The exit pressure is fixed by the area ratio and gamma alone - it is a geometric
            // consequence, not something the designer sets independently.
            double p2 = NozzleTheory.ExitPressureFromAreaRatio(gamma, p1, eps);

            double cf = NozzleTheory.ThrustCoefficient(gamma, p1, p2, p3, eps);
            double cfMax = NozzleTheory.MaximumThrustCoefficient(gamma);
            double cfVacuum = NozzleTheory.ThrustCoefficient(gamma, p1, p2, 0.0, eps);

            double mDot = NozzleTheory.MassFlowRate(gamma, p1, t1, mm, at);
            double thrust = cf * p1 * at;

            // Two independent routes to c, which must agree - this is the regression identity
            // the unit tests assert on.
            double cFromIdentity = cStar * cf;
            double cFromThrust = Performance.EffectiveExhaustVelocity(thrust, mDot);

            StringBuilder report = new();

            report.AppendLine(Heading("CHAMBER: CHARACTERISTIC VELOCITY"));
            report.AppendLine(Line("Vandenkerckhove function Gamma", capitalGamma, "", "N5"));
            report.AppendLine(Line("Characteristic velocity c*", cStar, "m/s", "N2"));
            report.AppendLine(Line("Mass flow rate m_dot", mDot, "kg/s", "N3"));
            report.AppendLine(Line("Check  c* = p1*At/m_dot", p1 * at / mDot, "m/s", "N2"));
            report.AppendLine();

            report.AppendLine(Heading("NOZZLE: THRUST COEFFICIENT"));
            report.AppendLine(Line("Area ratio A2/At", eps, "", "N3"));
            report.AppendLine(Line("Resulting exit pressure p2", Constants.PaToBar(p2), "bar", "N5"));
            report.AppendLine(Line("Ambient pressure p3", Constants.PaToBar(p3), "bar", "N5"));
            report.AppendLine(Line("Thrust coefficient C_F", cf, "", "N5"));
            report.AppendLine(Line("Vacuum thrust coefficient", cfVacuum, "", "N5"));
            report.AppendLine(Line("Theoretical maximum C_F,max", cfMax, "", "N5"));
            report.AppendLine(Line("Nozzle quality  C_F / C_F,max", 100.0 * cf / cfMax, "%", "N2"));
            report.AppendLine();

            report.AppendLine(Heading("THE IDENTITY  c = c* x C_F"));
            report.AppendLine(Line("c from c* x C_F", cFromIdentity, "m/s", "N4"));
            report.AppendLine(Line("c from F / m_dot", cFromThrust, "m/s", "N4"));
            report.AppendLine(Line("Absolute difference",
                Math.Abs(cFromIdentity - cFromThrust), "m/s", "E3"));
            report.AppendLine("  The difference is pure floating-point round-off. These are the");
            report.AppendLine("  same quantity computed along two completely different paths.");
            report.AppendLine();

            report.AppendLine(Heading("THRUST FROM C_F ALONE"));
            report.AppendLine(Line("F = C_F * p1 * At", thrust / 1000.0, "kN", "N3"));
            report.AppendLine(Line("F = m_dot * c", mDot * cFromThrust / 1000.0, "kN", "N3"));
            report.AppendLine(Line("Specific impulse",
                Performance.SpecificImpulse(cFromThrust), "s", "N2"));

            return report.ToString();
        }
    };

    /// <summary>
    /// One-call complete engine operating point.
    /// </summary>
    private static CodeSample FullEngineAnalysis() => new()
    {
        Id = "ch3-full-engine",
        Chapter = "Chapter 3",
        Title = "Complete Engine Operating Point",
        Summary = "Every Chapter 3 quantity for one engine, in one call.",

        Theory =
            """
            This sample is the payoff for the previous two. Given the exhaust gas properties
            (gamma, T1, M), the chamber pressure, the area ratio, the throat area and the
            ambient pressure, every other quantity in Chapter 3 follows deterministically:

                Gamma        -> Vandenkerckhove function from gamma
                c*           -> from gamma, T1, M          (chamber only)
                m_dot        -> from p1, At and the choked-flow relation
                p2           -> from the area ratio, by inverting the area-Mach relation
                M2           -> exit Mach number from the pressure ratio
                C_F          -> from p1, p2, p3, area ratio
                F            -> C_F * p1 * At
                c, Isp       -> F / m_dot, then divide by g0
                expansion    -> compare p2 against p3
                separation   -> flag if p2 falls below roughly 0.4 * p3

            The one genuinely awkward step is inverting the area ratio to get exit pressure.
            The area-Mach relation cannot be solved in closed form for Mach given area, so the
            library brackets and bisects on pressure ratio until the computed area ratio
            matches the requested one. It is robust because the function is monotone.

            Watch the EXPANSION STATE and SEPARATION flags. A nozzle sized for vacuum, run at
            sea level, will be badly over-expanded; when p2 drops below roughly 0.35-0.4 * p3
            the boundary layer separates from the wall, the flow becomes unsteady and
            asymmetric, and the resulting side loads can destroy the nozzle. That failure mode
            is the reason first-stage nozzles are "too small" for vacuum.
            """,

        CodeSnippet =
            """
            // A single call produces the entire operating point as an immutable record.
            EngineOperatingPoint point = EngineAnalysis.Analyse(
                gamma:              gamma,
                chamberTemperature: t1,
                molecularMass:      molarMass,
                chamberPressure:    p1,
                areaRatio:          areaRatio,
                throatArea:         throatArea,
                ambientPressure:    p3);

            // The record's ToString() is already a formatted engineering report.
            string report = point.ToString();
            """,

        Parameters =
        [
            new SampleParameter("gamma", "Specific heat ratio gamma", 1.22, "", "Exhaust gas property."),
            new SampleParameter("t1", "Chamber temperature T1", 3600.0, "K", "Adiabatic flame temperature."),
            new SampleParameter("mm", "Exhaust molecular mass M", 21.5, "kg/kmol", "Exhaust gas property."),
            new SampleParameter("p1", "Chamber pressure p1", 100.0, "bar", "Chamber stagnation pressure."),
            new SampleParameter("eps", "Area ratio A2/At", 22.0, "", "Sea-level engines: 10-25. Vacuum: 40-200."),
            new SampleParameter("at", "Throat area At", 300.0, "cm^2", "Scales thrust linearly."),
            new SampleParameter("p3", "Ambient pressure p3", 1.01325, "bar", "Use 0 for vacuum.")
        ],

        Run = inputs =>
        {
            double gamma = inputs["gamma"];
            double t1 = inputs["t1"];
            double mm = inputs["mm"];
            double p1 = Constants.BarToPa(inputs["p1"]);
            double eps = inputs["eps"];
            double at = inputs["at"] * 1.0e-4;
            double p3 = Constants.BarToPa(inputs["p3"]);

            EngineOperatingPoint point =
                EngineAnalysis.Analyse(gamma, t1, mm, p1, eps, at, p3);

            StringBuilder report = new();
            report.AppendLine(point.ToString());

            // Add the design-intent commentary the record itself does not carry.
            report.AppendLine();
            report.AppendLine(Heading("INTERPRETATION"));

            double optimumEps = p3 > 0.0
                ? NozzleTheory.OptimumAreaRatio(gamma, p1, p3)
                : double.PositiveInfinity;

            report.AppendLine(point.Expansion switch
            {
                ExpansionState.Optimum =>
                    "  OPTIMUM: p2 = p3. This nozzle is perfectly matched to this altitude and\n" +
                    "  is producing the maximum thrust available from this chamber here.",
                ExpansionState.UnderExpanded =>
                    "  UNDER-EXPANDED: p2 > p3. The gas is still above ambient when it leaves,\n" +
                    "  so expansion continues wastefully outside the nozzle. A larger area ratio\n" +
                    "  would recover some of that. This is the normal state at high altitude.",
                _ =>
                    "  OVER-EXPANDED: p2 < p3. The nozzle expanded the gas below ambient, so the\n" +
                    "  pressure thrust term is NEGATIVE and is subtracting from performance.\n" +
                    "  This is the normal state for a vacuum-optimised nozzle at sea level."
            });

            if (point.FlowSeparationLikely)
            {
                report.AppendLine();
                report.AppendLine("  *** FLOW SEPARATION WARNING ***");
                report.AppendLine("  p2 has fallen below roughly 0.4 * p3. The boundary layer will detach");
                report.AppendLine("  from the nozzle wall, producing unsteady, asymmetric flow and large");
                report.AppendLine("  lateral side loads. Real nozzles are destroyed by this. Reduce the");
                report.AppendLine("  area ratio, raise the chamber pressure, or only run this engine at");
                report.AppendLine("  altitude.");
            }

            report.AppendLine();
            report.AppendLine(Line("Optimum area ratio at this altitude",
                double.IsPositiveInfinity(optimumEps) ? "infinite (vacuum)" : optimumEps.ToString("N3")));
            report.AppendLine(Line("Area ratio you specified", eps, "", "N3"));

            return report.ToString();
        }
    };

    /// <summary>
    /// Sweeps one fixed nozzle from sea level to vacuum.
    /// </summary>
    private static CodeSample AltitudeSweep() => new()
    {
        Id = "ch3-altitude-sweep",
        Chapter = "Chapter 3",
        Title = "Altitude Sweep - One Nozzle, Every Altitude",
        Summary = "Why a fixed nozzle is always a compromise.",

        Theory =
            """
            A nozzle is a fixed piece of metal. The atmosphere it flies through is not fixed.
            This sample holds the engine constant and walks it from the pad to vacuum, printing
            thrust, Isp and expansion state at each altitude.

            Three things happen simultaneously as altitude increases:

            1. The exit pressure p2 does NOT change. It is set purely by the area ratio and
               gamma. The gas inside the nozzle has no idea what altitude it is at.

            2. The ambient pressure p3 falls exponentially, roughly as
               p3 = 101325 * exp(-h / 7200) for the first few scale heights.

            3. Therefore the pressure thrust term (p2 - p3) * A2 climbs steadily, starting
               negative (or small) at sea level and reaching its maximum p2 * A2 in vacuum.

            The engine gains thrust and Isp continuously as it climbs, "for free". A typical
            first-stage engine gains 10-15% thrust between the pad and vacuum.

            The design compromise is the area ratio. Choose it too small and you leave vacuum
            performance on the table for the majority of the burn. Choose it too large and the
            nozzle over-expands at sea level, loses thrust when thrust matters most, and may
            separate and destroy itself. Real vehicles dodge this by using different engines on
            different stages - a sea-level first stage with a small area ratio and a vacuum
            upper stage with a huge one. Altitude-compensating designs (aerospikes, extendible
            nozzles, dual-bell) attack the same problem directly.
            """,

        CodeSnippet =
            """
            foreach (double altitude in altitudes)
            {
                // Exponential atmosphere - adequate for trajectory-level work.
                double p3 = NozzleLosses.AmbientPressureAtAltitude(altitude);

                // The engine is IDENTICAL every time; only p3 changes.
                EngineOperatingPoint point = EngineAnalysis.Analyse(
                    gamma, t1, molarMass, p1, areaRatio, throatArea, p3);

                Report(altitude, point.Thrust, point.SpecificImpulse, point.Expansion);
            }
            """,

        Parameters =
        [
            new SampleParameter("gamma", "Specific heat ratio gamma", 1.22, "", "Exhaust gas property."),
            new SampleParameter("t1", "Chamber temperature T1", 3600.0, "K", "Adiabatic flame temperature."),
            new SampleParameter("mm", "Exhaust molecular mass M", 21.5, "kg/kmol", "Exhaust gas property."),
            new SampleParameter("p1", "Chamber pressure p1", 100.0, "bar", "Chamber stagnation pressure."),
            new SampleParameter("eps", "Area ratio A2/At", 22.0, "", "Try 10 (sea level) then 60 (vacuum)."),
            new SampleParameter("at", "Throat area At", 300.0, "cm^2", "Scales thrust linearly."),
            new SampleParameter("hmax", "Maximum altitude", 60.0, "km", "Above ~50 km the atmosphere is negligible.")
        ],

        Run = inputs =>
        {
            double gamma = inputs["gamma"];
            double t1 = inputs["t1"];
            double mm = inputs["mm"];
            double p1 = Constants.BarToPa(inputs["p1"]);
            double eps = inputs["eps"];
            double at = inputs["at"] * 1.0e-4;
            double hMax = inputs["hmax"] * 1000.0;

            const int steps = 12;

            StringBuilder report = new();

            report.AppendLine("  Alt(km)   p3(bar)    Thrust(kN)   Isp(s)   Expansion       Sep?");
            report.AppendLine("  " + new string('-', 68));

            // Walk the atmosphere, then finish with a true vacuum row.
            for (int i = 0; i <= steps; i++)
            {
                double altitude = hMax * i / steps;
                double p3 = NozzleLosses.AmbientPressureAtAltitude(altitude);

                EngineOperatingPoint point =
                    EngineAnalysis.Analyse(gamma, t1, mm, p1, eps, at, p3);

                report.AppendLine(string.Format(
                    "  {0,6:F1}  {1,9:F5}  {2,11:F2}  {3,7:F1}   {4,-14}  {5}",
                    altitude / 1000.0,
                    Constants.PaToBar(p3),
                    point.Thrust / 1000.0,
                    point.SpecificImpulse,
                    point.Expansion,
                    point.FlowSeparationLikely ? "YES" : "-"));
            }

            EngineOperatingPoint seaLevel =
                EngineAnalysis.Analyse(gamma, t1, mm, p1, eps, at, Constants.SeaLevelPressure);
            EngineOperatingPoint vacuum =
                EngineAnalysis.Analyse(gamma, t1, mm, p1, eps, at, 0.0);

            report.AppendLine();
            report.AppendLine(Heading("SEA LEVEL versus VACUUM"));
            report.AppendLine(Line("Sea-level thrust", seaLevel.Thrust / 1000.0, "kN", "N2"));
            report.AppendLine(Line("Vacuum thrust", vacuum.Thrust / 1000.0, "kN", "N2"));
            report.AppendLine(Line("Thrust gain", 100.0 * (vacuum.Thrust / seaLevel.Thrust - 1.0), "%", "N2"));
            report.AppendLine(Line("Sea-level Isp", seaLevel.SpecificImpulse, "s", "N2"));
            report.AppendLine(Line("Vacuum Isp", vacuum.SpecificImpulse, "s", "N2"));
            report.AppendLine(Line("Isp gain", vacuum.SpecificImpulse - seaLevel.SpecificImpulse, "s", "N2"));
            report.AppendLine();
            report.AppendLine(Line("Fixed exit pressure p2",
                Constants.PaToBar(vacuum.ExitPressure), "bar", "N5"));
            report.AppendLine("  Note p2 is identical in both cases - only the atmosphere changed.");
            report.AppendLine();
            report.AppendLine(Line("Optimum area ratio at sea level",
                NozzleTheory.OptimumAreaRatio(gamma, p1, Constants.SeaLevelPressure), "", "N2"));
            report.AppendLine("  Compare that against the area ratio you specified to see which");
            report.AppendLine("  altitude this nozzle is really designed for.");

            return report.ToString();
        }
    };

    /// <summary>
    /// Sizing a nozzle and chamber for a required thrust.
    /// </summary>
    private static CodeSample NozzleSizingAndGeometry() => new()
    {
        Id = "ch3-nozzle-sizing",
        Chapter = "Chapter 3",
        Title = "Nozzle Sizing and Geometry",
        Summary = "From a thrust requirement to actual metal dimensions.",

        Theory =
            """
            Design runs backwards from analysis. You are given a required thrust and you must
            produce dimensions.

            Step 1 - the thrust coefficient follows from the gas properties and the chosen area
            ratio, and does not depend on size at all.

            Step 2 - invert F = C_F * p1 * At to get the throat area:

                At = F / (C_F * p1)

            This is the fundamental sizing equation. Everything scales from the throat.

            Step 3 - the exit area is A2 = eps * At, and diameters follow from D = sqrt(4A/pi).

            Step 4 - the chamber volume comes from the characteristic length L*:

                Vc = L* * At

            L* is an empirical residence-time parameter: it is the length of a cylinder of
            throat area that has the same volume as the chamber. Typical values are 0.8-1.0 m
            for LOX/LH2, 1.0-1.3 m for LOX/RP-1, and 0.6-0.9 m for storable hypergolics. Too
            small and the propellant leaves before it has finished burning (c* efficiency
            drops); too large and you are carrying dead mass and extra cooled wall area.

            Step 5 - nozzle length. A 15-degree half-angle cone is the classic reference, but
            it is long and heavy, and its divergence loss factor is

                lambda = (1 + cos(alpha)) / 2

            which is 0.983 at 15 degrees - a 1.7% velocity loss because the exhaust is not
            leaving axially. A bell contour turns the flow back to nearly axial and reaches
            98-99% of ideal at 60-80% of the conical length. That is why essentially every
            production engine uses a bell.
            """,

        CodeSnippet =
            """
            double cf = NozzleTheory.ThrustCoefficient(gamma, p1, p2, p3, areaRatio);

            // The fundamental sizing equation: At = F / (C_F * p1).
            double throatArea = NozzleGeometry.RequiredThroatArea(requiredThrust, cf, p1);
            double exitArea   = throatArea * areaRatio;

            double throatDiameter = NozzleGeometry.DiameterFromArea(throatArea);
            double exitDiameter   = NozzleGeometry.DiameterFromArea(exitArea);

            double chamberVolume  = NozzleGeometry.ChamberVolume(lStar, throatArea);
            double conicalLength  = NozzleGeometry.ConicalNozzleLength(throatDiameter / 2.0, areaRatio, 15.0);
            double bellLength     = NozzleGeometry.BellNozzleLength(throatDiameter / 2.0, areaRatio, 0.8);
            """,

        Parameters =
        [
            new SampleParameter("thrust", "Required thrust", 500.0, "kN", "The design requirement."),
            new SampleParameter("gamma", "Specific heat ratio gamma", 1.22, "", "Exhaust gas property."),
            new SampleParameter("t1", "Chamber temperature T1", 3600.0, "K", "Adiabatic flame temperature."),
            new SampleParameter("mm", "Exhaust molecular mass M", 21.5, "kg/kmol", "Exhaust gas property."),
            new SampleParameter("p1", "Chamber pressure p1", 100.0, "bar", "Higher p1 gives a smaller engine."),
            new SampleParameter("eps", "Area ratio A2/At", 22.0, "", "Chosen for the design altitude."),
            new SampleParameter("p3", "Design ambient pressure", 1.01325, "bar", "Altitude the thrust is quoted at."),
            new SampleParameter("lstar", "Characteristic length L*", 1.1, "m", "0.8-1.3 m depending on propellant."),
            new SampleParameter("halfangle", "Conical half-angle", 15.0, "deg", "The classic reference cone.")
        ],

        Run = inputs =>
        {
            double requiredThrust = inputs["thrust"] * 1000.0;
            double gamma = inputs["gamma"];
            double t1 = inputs["t1"];
            double mm = inputs["mm"];
            double p1 = Constants.BarToPa(inputs["p1"]);
            double eps = inputs["eps"];
            double p3 = Constants.BarToPa(inputs["p3"]);
            double lStar = inputs["lstar"];
            double halfAngle = inputs["halfangle"];

            // DesignForThrust does the inversion internally and returns the sized engine.
            EngineOperatingPoint point =
                EngineAnalysis.DesignForThrust(gamma, t1, mm, p1, eps, requiredThrust, p3);

            double throatRadius = point.ThroatDiameter / 2.0;

            double chamberVolume = NozzleGeometry.ChamberVolume(lStar, point.ThroatArea);
            double conicalLength = NozzleGeometry.ConicalNozzleLength(throatRadius, eps, halfAngle);
            double bellLength = NozzleGeometry.BellNozzleLength(throatRadius, eps, 0.8);
            double divergence = NozzleGeometry.ConicalDivergenceFactor(halfAngle);

            // A representative chamber diameter, using a contraction ratio typical of a
            // medium-sized engine. Contraction ratio falls as engines get bigger.
            double contractionRatio = 3.0;
            double chamberArea = contractionRatio * point.ThroatArea;
            double chamberDiameter = NozzleGeometry.DiameterFromArea(chamberArea);
            double chamberLength = chamberVolume / chamberArea;

            StringBuilder report = new();

            report.AppendLine(Heading("REQUIREMENT"));
            report.AppendLine(Line("Required thrust", requiredThrust / 1000.0, "kN", "N2"));
            report.AppendLine(Line("At ambient pressure", Constants.PaToBar(p3), "bar", "N5"));
            report.AppendLine(Line("Delivered thrust (check)", point.Thrust / 1000.0, "kN", "N2"));
            report.AppendLine();

            report.AppendLine(Heading("PERFORMANCE OF THE RESULTING ENGINE"));
            report.AppendLine(Line("Thrust coefficient C_F", point.ThrustCoefficient, "", "N5"));
            report.AppendLine(Line("Characteristic velocity c*", point.CharacteristicVelocity, "m/s", "N2"));
            report.AppendLine(Line("Specific impulse", point.SpecificImpulse, "s", "N2"));
            report.AppendLine(Line("Mass flow rate", point.MassFlowRate, "kg/s", "N3"));
            report.AppendLine(Line("Expansion state", point.Expansion.ToString()));
            report.AppendLine();

            report.AppendLine(Heading("THROAT AND EXIT"));
            report.AppendLine(Line("Throat area At", point.ThroatArea * 1.0e4, "cm^2", "N3"));
            report.AppendLine(Line("Throat diameter", point.ThroatDiameter * 1000.0, "mm", "N2"));
            report.AppendLine(Line("Exit area A2", point.ExitArea * 1.0e4, "cm^2", "N3"));
            report.AppendLine(Line("Exit diameter", point.ExitDiameter * 1000.0, "mm", "N2"));
            report.AppendLine();

            report.AppendLine(Heading("COMBUSTION CHAMBER"));
            report.AppendLine(Line("Characteristic length L*", lStar, "m", "N3"));
            report.AppendLine(Line("Chamber volume Vc", chamberVolume * 1.0e6, "cm^3", "N1"));
            report.AppendLine(Line("Assumed contraction ratio Ac/At", contractionRatio, "", "N2"));
            report.AppendLine(Line("Chamber diameter", chamberDiameter * 1000.0, "mm", "N2"));
            report.AppendLine(Line("Chamber length (cylindrical equivalent)", chamberLength * 1000.0, "mm", "N2"));
            report.AppendLine();

            report.AppendLine(Heading("DIVERGENT SECTION"));
            report.AppendLine(Line($"Conical length at {halfAngle:F1} deg",
                conicalLength * 1000.0, "mm", "N2"));
            report.AppendLine(Line("Conical divergence factor lambda", divergence, "", "N5"));
            report.AppendLine(Line("Divergence velocity loss", 100.0 * (1.0 - divergence), "%", "N3"));
            report.AppendLine(Line("80% bell length", bellLength * 1000.0, "mm", "N2"));
            report.AppendLine(Line("Length saved by using a bell",
                (conicalLength - bellLength) * 1000.0, "mm", "N2"));
            report.AppendLine(Line("Length saving",
                100.0 * (1.0 - bellLength / conicalLength), "%", "N2"));
            report.AppendLine();
            report.AppendLine("  A bell reaches 98-99% of ideal expansion at that reduced length,");
            report.AppendLine("  which is better than the cone AND shorter. Hence its universal use.");

            return report.ToString();
        }
    };

    /// <summary>
    /// The real-versus-ideal efficiency chain.
    /// </summary>
    private static CodeSample NozzleLossBudget() => new()
    {
        Id = "ch3-loss-budget",
        Chapter = "Chapter 3",
        Title = "Nozzle Loss Budget",
        Summary = "Where the difference between theoretical and delivered Isp goes.",

        Theory =
            """
            Ideal theory always over-predicts. A real engine typically delivers 92-96% of its
            theoretical specific impulse, and a design review needs to know exactly where the
            missing few percent went. Sutton groups the losses into a small number of
            multiplicative correction factors:

            DIVERGENCE LOSS - the exhaust does not leave purely axially. Only the axial
            component produces thrust. For a cone of half-angle alpha,
            lambda = (1 + cos alpha) / 2, giving 0.983 at 15 degrees. Good bells reach 0.985-0.995.

            FRICTION / BOUNDARY LAYER LOSS - the gas next to the wall is slowed by viscosity
            and heat transfer. Typically 0.5-1.5%, so a factor of 0.985-0.995. It gets worse
            for small engines because the boundary layer is a larger fraction of the throat.

            TWO-PHASE FLOW LOSS - only matters for metallised propellants. Aluminium oxide
            condenses into liquid droplets that cannot expand, cannot follow the gas
            acceleration, and lag the flow thermally as well. In an aluminised solid this can
            cost 2-5%, so a factor of 0.95-0.98. For clean gas-phase liquid propellants it is
            exactly 1.0.

            CHEMICAL / KINETIC LOSS - real expansion is neither perfectly frozen nor perfectly
            in equilibrium. Recombination reactions that would release energy do not have time
            to complete. Typically 0.5-2%.

            The factors multiply:

                eta_nozzle = lambda * eta_friction * eta_two_phase * eta_chemical

            Separately, C* EFFICIENCY captures how well the chamber burns:

                eta_cstar = c*_measured / c*_theoretical

            typically 0.95-0.99. Poor injector design shows up here and nowhere else.

            Delivered Isp = theoretical Isp * eta_cstar * eta_nozzle. Note that the two
            efficiencies are measured by completely different tests, which is exactly why the
            c* / C_F split from the earlier sample is so useful in practice.
            """,

        CodeSnippet =
            """
            // Divergence factor from the cone half-angle: lambda = (1 + cos a) / 2.
            double divergence = NozzleGeometry.ConicalDivergenceFactor(halfAngleDegrees);

            // The nozzle factors multiply together.
            double nozzleEfficiency = NozzleLosses.CombinedNozzleEfficiency(
                divergenceFactor: divergence,
                frictionFactor:   friction,
                twoPhaseFactor:   twoPhase,
                chemicalFactor:   chemical);

            // Chamber efficiency is measured separately and multiplies on top.
            double cStarEfficiency = NozzleLosses.CStarEfficiency(measuredCStar, theoreticalCStar);

            double deliveredIsp = theoreticalIsp * nozzleEfficiency * cStarEfficiency;
            """,

        Parameters =
        [
            new SampleParameter("isp", "Theoretical specific impulse", 350.0, "s",
                "The ideal value from Chapter 3 theory."),
            new SampleParameter("halfangle", "Divergence half-angle", 15.0, "deg",
                "Use the equivalent angle for a bell, typically 8-12 deg."),
            new SampleParameter("friction", "Friction / boundary layer factor", 0.99, "",
                "0.985-0.995 typical; lower for small engines."),
            new SampleParameter("twophase", "Two-phase flow factor", 1.0, "",
                "1.0 for clean gas; 0.95-0.98 for aluminised solids."),
            new SampleParameter("chemical", "Chemical / kinetic factor", 0.985, "",
                "0.98-0.995 typical."),
            new SampleParameter("cstarmeas", "Measured c*", 1780.0, "m/s",
                "From a hot-fire test: c* = p1 * At / m_dot."),
            new SampleParameter("cstartheo", "Theoretical c*", 1830.0, "m/s",
                "From the propellant thermochemistry.")
        ],

        Run = inputs =>
        {
            double theoreticalIsp = inputs["isp"];
            double halfAngle = inputs["halfangle"];
            double friction = inputs["friction"];
            double twoPhase = inputs["twophase"];
            double chemical = inputs["chemical"];
            double measuredCStar = inputs["cstarmeas"];
            double theoreticalCStar = inputs["cstartheo"];

            double divergence = NozzleGeometry.ConicalDivergenceFactor(halfAngle);
            double nozzleEfficiency = NozzleLosses.CombinedNozzleEfficiency(
                divergence, friction, twoPhase, chemical);
            double cStarEfficiency = NozzleLosses.CStarEfficiency(measuredCStar, theoreticalCStar);

            double overall = nozzleEfficiency * cStarEfficiency;
            double deliveredIsp = theoreticalIsp * overall;

            // Attribute the lost seconds to each cause. Because the factors multiply rather
            // than add, this attribution is approximate but is how loss budgets are presented.
            double lostTotal = theoreticalIsp - deliveredIsp;

            StringBuilder report = new();

            report.AppendLine(Heading("EFFICIENCY FACTORS"));
            report.AppendLine(Line($"Divergence lambda at {halfAngle:F1} deg", divergence, "", "N5"));
            report.AppendLine(Line("Friction / boundary layer", friction, "", "N5"));
            report.AppendLine(Line("Two-phase flow", twoPhase, "", "N5"));
            report.AppendLine(Line("Chemical / kinetic", chemical, "", "N5"));
            report.AppendLine(Line("Combined nozzle efficiency", nozzleEfficiency, "", "N5"));
            report.AppendLine();
            report.AppendLine(Line("Measured c*", measuredCStar, "m/s", "N2"));
            report.AppendLine(Line("Theoretical c*", theoreticalCStar, "m/s", "N2"));
            report.AppendLine(Line("c* efficiency", cStarEfficiency, "", "N5"));
            report.AppendLine();
            report.AppendLine(Line("Overall efficiency", overall, "", "N5"));
            report.AppendLine(Line("Overall efficiency", 100.0 * overall, "%", "N3"));
            report.AppendLine();

            report.AppendLine(Heading("SPECIFIC IMPULSE"));
            report.AppendLine(Line("Theoretical Isp", theoreticalIsp, "s", "N2"));
            report.AppendLine(Line("Delivered Isp", deliveredIsp, "s", "N2"));
            report.AppendLine(Line("Total loss", lostTotal, "s", "N2"));
            report.AppendLine();

            report.AppendLine(Heading("WHERE THE SECONDS WENT (approximate attribution)"));
            report.AppendLine(Line("Divergence", theoreticalIsp * (1.0 - divergence), "s", "N3"));
            report.AppendLine(Line("Friction / boundary layer", theoreticalIsp * (1.0 - friction), "s", "N3"));
            report.AppendLine(Line("Two-phase flow", theoreticalIsp * (1.0 - twoPhase), "s", "N3"));
            report.AppendLine(Line("Chemical / kinetic", theoreticalIsp * (1.0 - chemical), "s", "N3"));
            report.AppendLine(Line("Incomplete combustion (c*)",
                theoreticalIsp * (1.0 - cStarEfficiency), "s", "N3"));
            report.AppendLine();
            report.AppendLine("  These sum to slightly more than the total loss because the factors");
            report.AppendLine("  multiply rather than add; the cross terms are counted twice. For");
            report.AppendLine("  losses of a few percent the error in this attribution is small.");
            report.AppendLine();

            report.AppendLine(Heading("VERDICT"));
            report.AppendLine(overall switch
            {
                >= 0.96 => "  Excellent. This is flight-hardware territory.",
                >= 0.93 => "  Normal for a well-developed engine.",
                >= 0.90 => "  Acceptable for a development article; there is money left on the table.",
                _ => "  Poor. Something specific is wrong - check the c* efficiency first,\n" +
                     "  because a low c* points squarely at the injector or the chamber L*."
            });

            return report.ToString();
        }
    };
}
