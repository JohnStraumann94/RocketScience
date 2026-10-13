using System.Text;
using Propulsion.Core;

namespace Propulsion.App.Models;

/// <summary>
/// Chapters 11 and 12 samples: solid propellant rocket motors.
/// </summary>
public static partial class SampleCatalog
{
    // ====================================================================================
    // CHAPTERS 11 and 12 - Solid Propellant Rocket Motors
    // ====================================================================================

    /// <summary>
    /// Equilibrium chamber pressure and the stability criterion n &lt; 1.
    /// </summary>
    private static CodeSample SolidMotorEquilibriumPressure() => new()
    {
        Id = "ch11-equilibrium-pressure",
        Chapter = "Chapter 11",
        Title = "Solid Motor Equilibrium Pressure and Stability",
        Summary = "Why the burn rate exponent n must be less than 1, or the motor explodes.",

        Theory =
            """
            A solid motor has no valves, no pumps and no throttle. Chamber pressure is set by a
            self-balancing equilibrium between two competing processes.

            GAS GENERATION - the propellant surface burns at a rate that increases with
            pressure, following Saint Robert's (Vieille's) law:

                r = a * p^n

            The mass generated per second is the burning area times the burn rate times the
            propellant density:

                m_dot_generated = A_b * rho_p * a * p^n

            GAS DISCHARGE - the choked throat passes mass in proportion to pressure:

                m_dot_discharged = p * A_t / c*

            Equilibrium is where these are equal. Solving for p:

                p = [ (A_b / A_t) * rho_p * a * c* ] ^ (1/(1-n))

            The bracketed group contains the KLEMMUNG, K = A_b / A_t, the ratio of burning
            surface area to throat area. K is the master design variable of a solid motor.

            THE STABILITY CRITERION. Look at the exponent 1/(1-n).

                If n < 1: the exponent is positive and finite, and the equilibrium is STABLE.
                A pressure rise increases generation as p^n but increases discharge as p^1.
                Since n < 1, discharge grows faster, and the pressure is pushed back down. The
                motor self-regulates.

                If n >= 1: generation grows as fast as or faster than discharge. Any small
                pressure rise generates more gas, which raises pressure further, which
                generates more gas. The pressure runs away and the motor detonates.

            This is not a theoretical worry. It is why propellant chemists work hard to keep
            n in the 0.2-0.5 range, and it is why a cracked grain is a catastrophic defect: a
            crack exposes new burning area, K jumps, and the pressure jumps with it - amplified
            by the 1/(1-n) exponent.

            That amplification is worth seeing explicitly. At n = 0.35 the exponent is
            1/(1-0.35) = 1.538, so a 10% increase in burning area produces a 1.1^1.538 = 15.8%
            increase in chamber pressure. Solid motors are exquisitely sensitive to grain
            geometry, and that is precisely why they cannot be throttled or shut down.
            """,

        CodeSnippet =
            """
            // Saint Robert's law: burn rate increases with pressure as r = a * p^n.
            double burnRate = SolidMotor.BurnRate(a, chamberPressure, n);

            // Klemmung K = burning area / throat area - the master design variable.
            double klemmung = SolidMotor.Klemmung(burningArea, throatArea);

            // The self-balancing equilibrium pressure.
            double pressure = SolidMotor.EquilibriumChamberPressure(
                burningSurfaceArea:    burningArea,
                throatArea:            throatArea,
                propellantDensity:     rho,
                characteristicVelocity: cStar,
                burnRateCoefficient:   a,
                pressureExponent:      n);

            // The stability criterion: n must be strictly less than 1.
            bool stable = SolidMotor.IsStable(n);
            """,

        Parameters =
        [
            new SampleParameter("ab", "Burning surface area", 2.5, "m^2", "Total exposed burning surface."),
            new SampleParameter("at", "Throat area", 60.0, "cm^2", "Nozzle throat."),
            new SampleParameter("rho", "Propellant density", 1770.0, "kg/m^3", "1750-1850 for APCP."),
            new SampleParameter("cstar", "Characteristic velocity", 1550.0, "m/s", "1500-1600 for APCP."),
            new SampleParameter("a", "Burn rate coefficient a", 5.0e-5, "SI",
                "In SI units where r is m/s and p is Pa."),
            new SampleParameter("n", "Pressure exponent n", 0.35, "",
                "MUST be below 1. Typical 0.2-0.5. Try 0.95 and 1.05 to see the cliff.")
        ],

        Run = inputs =>
        {
            double ab = inputs["ab"];
            double at = inputs["at"] * 1.0e-4;
            double rho = inputs["rho"];
            double cStar = inputs["cstar"];
            double a = inputs["a"];
            double n = inputs["n"];

            StringBuilder report = new();

            report.AppendLine(Heading("STABILITY CHECK - do this first, always"));
            report.AppendLine(Line("Pressure exponent n", n, "", "N4"));
            report.AppendLine(Line("Stable?", SolidMotor.IsStable(n) ? "YES" : "NO"));
            report.AppendLine();

            if (!SolidMotor.IsStable(n))
            {
                report.AppendLine("  *** UNSTABLE MOTOR - THIS DESIGN CANNOT WORK ***");
                report.AppendLine();
                report.AppendLine("  With n >= 1 the mass generation rate grows at least as fast with");
                report.AppendLine("  pressure as the discharge rate does. There is no equilibrium to");
                report.AppendLine("  settle at. Any perturbation - and there are always perturbations -");
                report.AppendLine("  drives the pressure up without bound until the case ruptures.");
                report.AppendLine();
                report.AppendLine("  The equilibrium equation itself confirms this: the exponent");
                report.AppendLine("  1/(1-n) is negative or undefined here, so the formula has no");
                report.AppendLine("  physically meaningful solution. No amount of case strength fixes");
                report.AppendLine("  this; the propellant formulation must be changed.");
                return report.ToString();
            }

            double klemmung = SolidMotor.Klemmung(ab, at);
            double pressure = SolidMotor.EquilibriumChamberPressure(ab, at, rho, cStar, a, n);
            double burnRate = SolidMotor.BurnRate(a, pressure, n);
            double exponent = 1.0 / (1.0 - n);

            // Verify the balance explicitly - generation must equal discharge.
            double generated = ab * rho * burnRate;
            double discharged = pressure * at / cStar;

            report.AppendLine(Heading("EQUILIBRIUM"));
            report.AppendLine(Line("Burning surface area Ab", ab, "m^2", "N4"));
            report.AppendLine(Line("Throat area At", at * 1.0e4, "cm^2", "N3"));
            report.AppendLine(Line("Klemmung K = Ab / At", klemmung, "", "N2"));
            report.AppendLine(Line("Sensitivity exponent 1/(1-n)", exponent, "", "N4"));
            report.AppendLine(Line("Equilibrium pressure", Constants.PaToBar(pressure), "bar", "N3"));
            report.AppendLine(Line("Equilibrium pressure", Constants.PaToPsi(pressure), "psi", "N1"));
            report.AppendLine(Line("Burn rate at that pressure", burnRate * 1000.0, "mm/s", "N4"));
            report.AppendLine();

            report.AppendLine(Heading("MASS BALANCE VERIFICATION"));
            report.AppendLine(Line("Generated  Ab * rho * r", generated, "kg/s", "N5"));
            report.AppendLine(Line("Discharged  p * At / c*", discharged, "kg/s", "N5"));
            report.AppendLine(Line("Residual", Math.Abs(generated - discharged), "kg/s", "E3"));
            report.AppendLine("  These must match. That balance IS the equilibrium condition, and");
            report.AppendLine("  the equilibrium pressure formula is simply its algebraic solution.");
            report.AppendLine();

            report.AppendLine(Heading("SENSITIVITY TO BURNING AREA - the cracked grain problem"));
            report.AppendLine("   Ab change   New Ab(m2)   Pressure(bar)   Pressure change");
            report.AppendLine("  " + new string('-', 64));
            foreach (double factor in new[] { 0.90, 0.95, 1.00, 1.05, 1.10, 1.25, 1.50, 2.00 })
            {
                double newAb = ab * factor;
                double newPressure = SolidMotor.EquilibriumChamberPressure(
                    newAb, at, rho, cStar, a, n);
                report.AppendLine(string.Format(
                    "  {0,10:P0}   {1,10:F4}   {2,13:F3}   {3,15:P2}",
                    factor - 1.0, newAb, Constants.PaToBar(newPressure),
                    newPressure / pressure - 1.0));
            }
            report.AppendLine();
            report.AppendLine(Line("Superlinearity check  1.1 ^ (1/(1-n))",
                Math.Pow(1.1, exponent), "", "N5"));
            report.AppendLine("  A 10% area increase produces MORE than a 10% pressure increase, by");
            report.AppendLine("  exactly that factor. A crack in the grain exposes new surface, K");
            report.AppendLine("  jumps, and the pressure jumps harder. This is the dominant failure");
            report.AppendLine("  mode for solid motors and is why grains are X-rayed before flight.");
            report.AppendLine();

            report.AppendLine(Heading("SENSITIVITY TO n - approaching the cliff"));
            report.AppendLine("     n     1/(1-n)   Pressure(bar)   dP for +10% area");
            report.AppendLine("  " + new string('-', 58));
            foreach (double testN in new[] { 0.20, 0.35, 0.50, 0.65, 0.80, 0.90, 0.95, 0.98 })
            {
                double p = SolidMotor.EquilibriumChamberPressure(ab, at, rho, cStar, a, testN);
                double pUp = SolidMotor.EquilibriumChamberPressure(ab * 1.1, at, rho, cStar, a, testN);
                report.AppendLine(string.Format(
                    "  {0,5:F2}   {1,8:F3}   {2,13:F2}   {3,16:P1}",
                    testN, 1.0 / (1.0 - testN), Constants.PaToBar(p), pUp / p - 1.0));
            }
            report.AppendLine();
            report.AppendLine("  As n approaches 1 the sensitivity exponent diverges. The motor becomes");
            report.AppendLine("  progressively less forgiving of any manufacturing variation, then");
            report.AppendLine("  fails outright at n = 1. Propellant formulations are deliberately");
            report.AppendLine("  designed to keep n well below 0.5.");
            report.AppendLine();

            report.AppendLine(Heading("KLEMMUNG SWEEP - throat area as the design knob"));
            report.AppendLine("   At(cm2)   Klemmung K   Pressure(bar)   Burn rate(mm/s)");
            report.AppendLine("  " + new string('-', 62));
            foreach (double factor in new[] { 0.6, 0.8, 1.0, 1.2, 1.5, 2.0 })
            {
                double testAt = at * factor;
                double p = SolidMotor.EquilibriumChamberPressure(ab, testAt, rho, cStar, a, n);
                report.AppendLine(string.Format(
                    "  {0,8:F2}   {1,10:F2}   {2,13:F3}   {3,16:F4}",
                    testAt * 1.0e4,
                    SolidMotor.Klemmung(ab, testAt),
                    Constants.PaToBar(p),
                    SolidMotor.BurnRate(a, p, n) * 1000.0));
            }
            report.AppendLine();
            report.AppendLine("  A SMALLER throat gives a HIGHER Klemmung and a HIGHER pressure. Throat");
            report.AppendLine("  erosion during a burn therefore lowers pressure over time - a real");
            report.AppendLine("  effect in long-burning motors with graphite throats.");
            report.AppendLine();

            report.AppendLine(Heading("TEMPERATURE SENSITIVITY"));
            report.AppendLine("  Solid propellant burn rate depends on the grain's bulk temperature.");
            report.AppendLine("  A motor soaked cold on a winter pad burns more slowly at lower");
            report.AppendLine("  pressure for longer; the same motor soaked hot burns faster and");
            report.AppendLine("  harder. Total impulse is nearly unchanged, but peak pressure and");
            report.AppendLine("  burn time both move, which drives the case design and the vehicle's");
            report.AppendLine("  guidance. Typical sensitivity is 0.1-0.5% pressure change per kelvin,");
            report.AppendLine("  which over a -40 to +60 C qualification range is a very large spread.");
            report.AppendLine();
            report.AppendLine("  Worked example using SolidMotor.TemperatureSensitivity:");
            double pHot = Constants.BarToPa(Constants.PaToBar(pressure) * 1.20);
            double pCold = Constants.BarToPa(Constants.PaToBar(pressure) * 0.85);
            report.AppendLine(Line("  Pressure at +60 C", Constants.PaToBar(pHot), "bar", "N3"));
            report.AppendLine(Line("  Pressure at -40 C", Constants.PaToBar(pCold), "bar", "N3"));
            report.AppendLine(Line("  Sensitivity pi_K",
                100.0 * SolidMotor.TemperatureSensitivity(pHot, pCold, 100.0), "%/K", "N4"));

            return report.ToString();
        }
    };

    /// <summary>
    /// BATES grain thrust-time curve, integrated web by web.
    /// </summary>
    private static CodeSample BatesGrainThrustCurve() => new()
    {
        Id = "ch12-bates-grain",
        Chapter = "Chapter 12",
        Title = "BATES Grain Thrust-Time Curve",
        Summary = "Grain geometry chooses the thrust profile before any propellant is mixed.",

        Theory =
            """
            In a solid motor you cannot throttle. The thrust-versus-time curve is decided
            entirely by the GRAIN GEOMETRY, at the moment the propellant is cast. Getting it
            right is the whole art of solid motor design.

            Three classes of profile:

            PROGRESSIVE - burning area increases with time, so pressure and thrust rise. A
            simple cylindrical core burning outwards is progressive: the core circumference
            grows as it burns.

            NEUTRAL - burning area stays roughly constant, giving flat thrust. This is usually
            what you want, because it minimises the peak pressure for a given total impulse and
            therefore minimises case mass.

            REGRESSIVE - burning area decreases, so thrust falls off. An end-burner is extreme
            regressive-to-neutral with a very long, low-thrust burn.

            The BATES grain (Ballistic Test and Evaluation System) is a classic. It is a
            cylindrical segment with a central circular perforation, with the outer surface
            inhibited (bonded to the case) and the two ENDS left exposed. As the web burns:

                - the core diameter grows, so the inner cylindrical area INCREASES;
                - the grain length shrinks as the ends burn back, so that area DECREASES;
                - the annular end faces grow in area as the core opens out.

            These effects partly cancel. With the length-to-diameter ratio chosen correctly -
            around L/D of 2 to 3 for a single segment - the total burning area stays nearly
            constant over most of the burn. That is a neutral profile achieved by pure geometry.

            More complex profiles use star, wagon-wheel, dog-bone or finocyl cross-sections.
            The Space Shuttle SRB used an eleven-point star in its forward segment specifically
            to give high initial thrust off the pad, then a deliberate thrust reduction around
            max-Q, then a recovery - all set by the cross-section shape varying along the
            length of the motor.

            The simulation below marches the web thickness forward in time. At each step it
            recomputes the burning area from the geometry, solves the equilibrium pressure for
            that area, gets the burn rate from Saint Robert's law, and advances the web. It is
            the same self-consistent loop a real internal ballistics code runs, just simpler.
            """,

        CodeSnippet =
            """
            double web = 0.0;
            double time = 0.0;

            while (web < maximumWeb)
            {
                // Geometry -> burning area at this instant.
                double burningArea = SolidMotor.BatesGrainBurningArea(
                    outerDiameter, initialCoreDiameter, grainLength, web, endsInhibited: false);

                // Area -> equilibrium pressure (the internal ballistics loop).
                double pressure = SolidMotor.EquilibriumChamberPressure(
                    burningArea, throatArea, rho, cStar, a, n);

                // Pressure -> burn rate -> advance the web.
                double burnRate = SolidMotor.BurnRate(a, pressure, n);

                double thrust = thrustCoefficient * pressure * throatArea;

                web  += burnRate * dt;
                time += dt;
            }
            """,

        Parameters =
        [
            new SampleParameter("od", "Grain outer diameter", 200.0, "mm", "Case inner diameter."),
            new SampleParameter("id", "Initial core diameter", 70.0, "mm", "The central perforation."),
            new SampleParameter("len", "Grain length", 500.0, "mm", "L/D near 2-3 gives a neutral burn."),
            new SampleParameter("at", "Throat area", 12.0, "cm^2", "Sets the operating pressure."),
            new SampleParameter("rho", "Propellant density", 1770.0, "kg/m^3", "APCP."),
            new SampleParameter("cstar", "Characteristic velocity", 1550.0, "m/s", "APCP."),
            new SampleParameter("a", "Burn rate coefficient a", 5.0e-5, "SI", "SI units."),
            new SampleParameter("n", "Pressure exponent n", 0.35, "", "Must be below 1."),
            new SampleParameter("cf", "Thrust coefficient C_F", 1.55, "", "From the nozzle area ratio."),
            new SampleParameter("dt", "Time step", 0.02, "s", "Integration step.")
        ],

        Run = inputs =>
        {
            double od = inputs["od"] / 1000.0;
            double id = inputs["id"] / 1000.0;
            double length = inputs["len"] / 1000.0;
            double at = inputs["at"] * 1.0e-4;
            double rho = inputs["rho"];
            double cStar = inputs["cstar"];
            double a = inputs["a"];
            double n = inputs["n"];
            double cf = inputs["cf"];
            double dt = Math.Clamp(inputs["dt"], 0.001, 0.5);

            if (id >= od)
            {
                throw new ArgumentException(
                    "The core diameter must be smaller than the outer diameter - otherwise " +
                    "there is no propellant, only a tube.");
            }

            if (!SolidMotor.IsStable(n))
            {
                throw new ArgumentException(
                    $"The pressure exponent n = {n:F3} is not below 1, so no stable equilibrium " +
                    "pressure exists and this motor would run away. See the equilibrium pressure " +
                    "sample for why.");
            }

            // The grain burns out when the web between the core and the outer wall is consumed.
            double maximumWeb = (od - id) / 2.0;

            double propellantMass = SolidMotor.BatesGrainMass(od, id, length, rho);

            // Case volume for the loading fraction, treating the case bore as the grain OD.
            double caseVolume = NozzleGeometry.AreaFromDiameter(od) * length;
            double propellantVolume = propellantMass / rho;
            double loading = SolidMotor.VolumetricLoadingFraction(propellantVolume, caseVolume);

            StringBuilder report = new();

            report.AppendLine(Heading("GRAIN GEOMETRY"));
            report.AppendLine(Line("Outer diameter", od * 1000.0, "mm", "N2"));
            report.AppendLine(Line("Initial core diameter", id * 1000.0, "mm", "N2"));
            report.AppendLine(Line("Grain length", length * 1000.0, "mm", "N2"));
            report.AppendLine(Line("Length / outer diameter", length / od, "", "N3"));
            report.AppendLine(Line("Web thickness", maximumWeb * 1000.0, "mm", "N2"));
            report.AppendLine(Line("Propellant mass", propellantMass, "kg", "N4"));
            report.AppendLine(Line("Propellant volume", propellantVolume * 1.0e6, "cm^3", "N1"));
            report.AppendLine(Line("Volumetric loading fraction", loading, "", "P2"));
            report.AppendLine();

            // ---- Internal ballistics march -------------------------------------------------
            List<(double Time, double Web, double Area, double Pressure, double Thrust, double Rate)>
                history = [];

            double web = 0.0;
            double time = 0.0;
            double totalImpulse = 0.0;
            double burntMass = 0.0;

            // Guard against a pathological input producing an unbounded loop.
            const int maxSteps = 200000;
            int steps = 0;

            while (web < maximumWeb && steps++ < maxSteps)
            {
                double burningArea = SolidMotor.BatesGrainBurningArea(od, id, length, web);

                if (burningArea <= 0.0)
                {
                    break;
                }

                double pressure = SolidMotor.EquilibriumChamberPressure(
                    burningArea, at, rho, cStar, a, n);
                double burnRate = SolidMotor.BurnRate(a, pressure, n);
                double thrust = cf * pressure * at;

                history.Add((time, web, burningArea, pressure, thrust, burnRate));

                totalImpulse += thrust * dt;
                burntMass += burningArea * rho * burnRate * dt;

                web += burnRate * dt;
                time += dt;
            }

            if (history.Count == 0)
            {
                throw new InvalidOperationException(
                    "The grain produced no burning area at all - check the geometry inputs.");
            }

            report.AppendLine(Heading("THRUST-TIME CURVE"));
            report.AppendLine("    t(s)   Web(mm)   Ab(cm2)   p(bar)   Thrust(N)   r(mm/s)");
            report.AppendLine("  " + new string('-', 64));

            int stride = Math.Max(1, history.Count / 18);
            for (int i = 0; i < history.Count; i += stride)
            {
                var h = history[i];
                report.AppendLine(string.Format(
                    "  {0,6:F2}   {1,7:F2}   {2,7:F1}   {3,6:F2}   {4,9:F1}   {5,8:F3}",
                    h.Time, h.Web * 1000.0, h.Area * 1.0e4,
                    Constants.PaToBar(h.Pressure), h.Thrust, h.Rate * 1000.0));
            }

            var final = history[^1];
            report.AppendLine(string.Format(
                "  {0,6:F2}   {1,7:F2}   {2,7:F1}   {3,6:F2}   {4,9:F1}   {5,8:F3}   <- burnout",
                final.Time, final.Web * 1000.0, final.Area * 1.0e4,
                Constants.PaToBar(final.Pressure), final.Thrust, final.Rate * 1000.0));
            report.AppendLine();

            double maxThrust = history.Max(h => h.Thrust);
            double minThrust = history.Min(h => h.Thrust);
            double meanThrust = history.Average(h => h.Thrust);
            double maxPressure = history.Max(h => h.Pressure);
            double burnTime = final.Time + dt;

            report.AppendLine(Heading("PERFORMANCE SUMMARY"));
            report.AppendLine(Line("Burn time", burnTime, "s", "N3"));
            report.AppendLine(Line("Maximum thrust", maxThrust, "N", "N2"));
            report.AppendLine(Line("Minimum thrust", minThrust, "N", "N2"));
            report.AppendLine(Line("Mean thrust", meanThrust, "N", "N2"));
            report.AppendLine(Line("Maximum chamber pressure",
                Constants.PaToBar(maxPressure), "bar", "N3"));
            report.AppendLine(Line("Total impulse", totalImpulse, "N.s", "N2"));
            report.AppendLine(Line("Propellant consumed (integrated)", burntMass, "kg", "N4"));
            report.AppendLine(Line("Propellant in the grain", propellantMass, "kg", "N4"));
            report.AppendLine(Line("Mass balance error",
                100.0 * Math.Abs(burntMass - propellantMass) / propellantMass, "%", "N3"));
            report.AppendLine(Line("Delivered specific impulse",
                Performance.SpecificImpulseFromTotalImpulse(totalImpulse, propellantMass), "s", "N2"));
            report.AppendLine();
            report.AppendLine("  The mass balance error is the accumulated truncation error of the");
            report.AppendLine("  explicit time march. Reduce the time step to shrink it.");
            report.AppendLine();

            report.AppendLine(Heading("PROFILE CLASSIFICATION"));
            double neutrality = maxThrust / minThrust;
            report.AppendLine(Line("Peak / minimum thrust", neutrality, "", "N4"));
            report.AppendLine(Line("Peak / mean thrust", maxThrust / meanThrust, "", "N4"));
            report.AppendLine(Line("Thrust at 10% of burn",
                history[(int)(history.Count * 0.10)].Thrust, "N", "N2"));
            report.AppendLine(Line("Thrust at 50% of burn",
                history[(int)(history.Count * 0.50)].Thrust, "N", "N2"));
            report.AppendLine(Line("Thrust at 90% of burn",
                history[(int)(history.Count * 0.90)].Thrust, "N", "N2"));
            report.AppendLine();

            double early = history[(int)(history.Count * 0.10)].Thrust;
            double late = history[(int)(history.Count * 0.90)].Thrust;

            report.AppendLine((late / early) switch
            {
                > 1.10 => "  PROGRESSIVE: thrust rises through the burn. The growing core area is\n" +
                          "  outrunning the shrinking end faces. Peak pressure occurs late, which\n" +
                          "  sizes the case for the highest load at the worst moment.",
                < 0.90 => "  REGRESSIVE: thrust falls through the burn. The ends are burning back\n" +
                          "  faster than the core is opening out. Good for a high initial kick,\n" +
                          "  but the case is sized by a peak that only occurs briefly.",
                _ => "  NEUTRAL: thrust is nearly constant. This is usually what you want, because\n" +
                     "  it gives the lowest peak pressure for a given total impulse, and case mass\n" +
                     "  scales with peak pressure. The L/D ratio you chose is doing its job."
            });
            report.AppendLine();

            report.AppendLine(Heading("HOW TO CHANGE THE PROFILE"));
            report.AppendLine("  - Increase L/D  -> the end faces matter less, core growth dominates,");
            report.AppendLine("                     the burn becomes more PROGRESSIVE.");
            report.AppendLine("  - Decrease L/D  -> the ends dominate, the burn becomes REGRESSIVE.");
            report.AppendLine("  - Inhibit the ends -> pure core burning, strongly PROGRESSIVE.");
            report.AppendLine("  - Star or finocyl cross-section -> a designer-chosen area history,");
            report.AppendLine("                     which is how genuinely neutral or dual-thrust");
            report.AppendLine("                     profiles are actually produced in flight motors.");
            report.AppendLine();
            report.AppendLine("  Remember that none of this can be changed after casting. A solid");
            report.AppendLine("  motor's entire thrust programme is frozen into its geometry.");

            return report.ToString();
        }
    };
}
