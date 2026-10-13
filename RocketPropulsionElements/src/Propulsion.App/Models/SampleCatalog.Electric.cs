using System.Text;
using Propulsion.Core;

namespace Propulsion.App.Models;

/// <summary>
/// Chapters 17 and 19 samples: electric propulsion.
/// </summary>
public static partial class SampleCatalog
{
    // ====================================================================================
    // CHAPTERS 17 and 19 - Electric Propulsion
    // ====================================================================================

    /// <summary>
    /// The power balance that governs every electric thruster.
    /// </summary>
    private static CodeSample ElectricThrusterPowerBalance() => new()
    {
        Id = "ch19-electric-power",
        Chapter = "Chapter 19",
        Title = "Electric Thruster Power Balance",
        Summary = "Why high Isp costs power, and why electric thrusters are so weak.",

        Theory =
            """
            A chemical rocket carries its energy in the propellant itself, so exhaust velocity
            is capped by the chemistry - about 4500 m/s at best. An electric thruster separates
            the two: the propellant supplies only mass, and the ENERGY comes from an external
            power source. That removes the chemical ceiling entirely, and Isp values of
            2000-10000 s become possible.

            But energy conservation immediately imposes a different, harsher constraint. The
            kinetic power carried away in the jet is:

                P_jet = 0.5 * F * c

            and the electrical power required is that divided by the thruster efficiency:

                P_elec = F * c / (2 * eta)

            Rearranged to give thrust from available power:

                F = 2 * eta * P / c

            THRUST IS INVERSELY PROPORTIONAL TO EXHAUST VELOCITY at fixed power. This is the
            central trade of electric propulsion, and it is unavoidable. Doubling your Isp
            halves your thrust for the same power. You cannot have both.

            The numbers are sobering. A 5 kW Hall thruster at 1600 s Isp produces roughly 0.3 N
            of thrust - about the weight of an apple held in your hand. A gridded ion engine at
            3000 s produces about 0.15 N from the same 5 kW. Meanwhile a Merlin engine produces
            850000 N.

            So electric propulsion cannot launch anything, cannot land anything, and cannot
            perform a time-critical manoeuvre. What it CAN do is run for months or years. Dawn
            accumulated more than 11 km/s of delta-v with three ion thrusters - more than any
            spacecraft before it - by firing almost continuously for over five years. It visited
            two separate main-belt bodies and entered orbit around each, which no chemical
            spacecraft of that mass could have done.

            The other cost is the POWER PLANT. Solar arrays or a reactor have mass, characterised
            by the SPECIFIC POWER alpha in watts per kilogram. Modern deployable solar arrays
            reach 100-200 W/kg at 1 AU, degrading with the inverse square of solar distance -
            which is why outer-planet electric missions need nuclear power. That power plant
            mass is the hidden price of high Isp, and it is what makes the optimisation in the
            next sample non-trivial.
            """,

        CodeSnippet =
            """
            // Kinetic power in the jet: P_jet = 0.5 * F * c.
            double jetPower = ElectricPropulsion.JetPower(thrust, exhaustVelocity);

            // Electrical power required, after thruster efficiency losses.
            double electricalPower = ElectricPropulsion.RequiredElectricalPower(
                thrust, exhaustVelocity, thrusterEfficiency);

            // The inverse: thrust available from a given power budget.
            double availableThrust = ElectricPropulsion.ThrustFromPower(
                electricalPower, exhaustVelocity, thrusterEfficiency);

            // Power plant mass from the specific power alpha (W/kg).
            double powerPlantMass = ElectricPropulsion.PowerPlantMass(electricalPower, specificPower);
            """,

        Parameters =
        [
            new SampleParameter("thrust", "Required thrust", 0.25, "N",
                "Electric thrusters produce fractions of a newton."),
            new SampleParameter("isp", "Specific impulse", 1800.0, "s",
                "Hall ~1600-2000, gridded ion ~3000-4500, VASIMR up to 5000+."),
            new SampleParameter("eta", "Thruster efficiency", 0.55, "",
                "0.45-0.60 for Hall, 0.60-0.75 for gridded ion."),
            new SampleParameter("alpha", "Power plant specific power", 120.0, "W/kg",
                "100-200 for modern solar arrays at 1 AU."),
            new SampleParameter("hours", "Firing duration", 8000.0, "h",
                "Electric missions fire for thousands of hours.")
        ],

        Run = inputs =>
        {
            double thrust = inputs["thrust"];
            double isp = inputs["isp"];
            double eta = Math.Clamp(inputs["eta"], 0.01, 1.0);
            double alpha = inputs["alpha"];
            double hours = inputs["hours"];

            double c = Performance.ExhaustVelocityFromIsp(isp);
            double jetPower = ElectricPropulsion.JetPower(thrust, c);
            double electricalPower = ElectricPropulsion.RequiredElectricalPower(thrust, c, eta);
            double powerPlantMass = ElectricPropulsion.PowerPlantMass(electricalPower, alpha);

            double massFlow = Performance.MassFlowRate(thrust, c);
            double seconds = hours * 3600.0;
            double propellantUsed = massFlow * seconds;
            double totalImpulse = Performance.TotalImpulse(thrust, seconds);

            StringBuilder report = new();

            report.AppendLine(Heading("THRUSTER"));
            report.AppendLine(Line("Thrust", thrust, "N", "N5"));
            report.AppendLine(Line("Thrust", thrust * 1000.0, "mN", "N2"));
            report.AppendLine(Line("Specific impulse", isp, "s", "N1"));
            report.AppendLine(Line("Exhaust velocity", c, "m/s", "N1"));
            report.AppendLine(Line("Exhaust velocity", c / 1000.0, "km/s", "N3"));
            report.AppendLine(Line("Mass flow rate", massFlow * 1.0e6, "mg/s", "N3"));
            report.AppendLine();

            report.AppendLine(Heading("POWER BALANCE"));
            report.AppendLine(Line("Jet (kinetic) power  0.5*F*c", jetPower, "W", "N2"));
            report.AppendLine(Line("Thruster efficiency", eta, "", "P2"));
            report.AppendLine(Line("Required electrical power", electricalPower, "W", "N2"));
            report.AppendLine(Line("Required electrical power", electricalPower / 1000.0, "kW", "N4"));
            report.AppendLine(Line("Power dissipated as heat", electricalPower - jetPower, "W", "N2"));
            report.AppendLine();
            report.AppendLine(Line("Thrust per kilowatt", 1000.0 * thrust / electricalPower, "N/kW", "N5"));
            report.AppendLine(Line("Thrust per kilowatt",
                1.0e6 * thrust / electricalPower, "mN/kW", "N2"));
            report.AppendLine("  A Hall thruster manages roughly 60-70 mN/kW; a gridded ion engine");
            report.AppendLine("  about 30-40 mN/kW, trading thrust for its much higher Isp.");
            report.AppendLine();

            report.AppendLine(Heading("POWER PLANT"));
            report.AppendLine(Line("Specific power alpha", alpha, "W/kg", "N2"));
            report.AppendLine(Line("Power plant mass", powerPlantMass, "kg", "N3"));
            report.AppendLine(Line("Power plant mass per newton", powerPlantMass / thrust, "kg/N", "N1"));
            report.AppendLine();
            report.AppendLine("  Compare that last figure against a chemical engine, where the thrust-");
            report.AppendLine("  to-weight ratio of the engine alone is 60-150. Electric propulsion is");
            report.AppendLine("  four to five ORDERS OF MAGNITUDE worse on thrust per unit dry mass.");
            report.AppendLine();

            report.AppendLine(Heading($"OVER {hours:N0} HOURS OF FIRING"));
            report.AppendLine(Line("Firing duration", seconds / 86400.0, "days", "N2"));
            report.AppendLine(Line("Propellant consumed", propellantUsed, "kg", "N4"));
            report.AppendLine(Line("Total impulse", totalImpulse, "N.s", "N1"));
            report.AppendLine(Line("Energy consumed", electricalPower * seconds / 3.6e9, "MWh", "N4"));
            report.AppendLine();
            report.AppendLine("  Delta-v achievable for a given spacecraft dry mass:");
            report.AppendLine("     Dry mass(kg)   Delta-v(m/s)");
            report.AppendLine("  " + new string('-', 36));
            foreach (double dryMass in new[] { 200.0, 500.0, 1000.0, 2000.0, 5000.0 })
            {
                double dv = RocketEquation.DeltaV(c, dryMass + propellantUsed, dryMass);
                report.AppendLine($"   {dryMass,12:F0}   {dv,12:F1}");
            }
            report.AppendLine();

            report.AppendLine(Heading("THE ISP-versus-THRUST TRADE AT FIXED POWER"));
            report.AppendLine($"  Holding electrical power at {electricalPower / 1000.0:F3} kW:");
            report.AppendLine();
            report.AppendLine("    Isp(s)    c(km/s)   Thrust(mN)   m_dot(mg/s)   Accel of 1000 kg");
            report.AppendLine("  " + new string('-', 72));
            foreach (double testIsp in new[] { 800.0, 1200.0, 1600.0, 2000.0, 3000.0, 4000.0, 6000.0, 10000.0 })
            {
                double testC = Performance.ExhaustVelocityFromIsp(testIsp);
                double testThrust = ElectricPropulsion.ThrustFromPower(electricalPower, testC, eta);
                double testFlow = Performance.MassFlowRate(testThrust, testC);
                report.AppendLine(string.Format(
                    "  {0,8:F0}  {1,9:F2}  {2,11:F3}  {3,12:F4}  {4,14:E3} m/s^2",
                    testIsp, testC / 1000.0, testThrust * 1000.0,
                    testFlow * 1.0e6, testThrust / 1000.0));
            }
            report.AppendLine();
            report.AppendLine("  Thrust falls as exactly 1/c. Doubling the Isp halves the thrust. There");
            report.AppendLine("  is no way around this - it is energy conservation, not an engineering");
            report.AppendLine("  limitation that better hardware will fix.");
            report.AppendLine();

            report.AppendLine(Heading("CHEMICAL COMPARISON"));
            report.AppendLine(Line("This thruster", thrust, "N", "N4"));
            report.AppendLine(Line("A satellite hydrazine thruster", 22.0, "N", "N1"));
            report.AppendLine(Line("An RL10 upper stage engine", 110000.0, "N", "N0"));
            report.AppendLine(Line("A Merlin 1D first stage engine", 845000.0, "N", "N0"));
            report.AppendLine(Line("An F-1 (Saturn V) engine", 6770000.0, "N", "N0"));
            report.AppendLine();
            report.AppendLine("  Electric propulsion never competes on thrust and never will. It");
            report.AppendLine("  competes on TOTAL IMPULSE PER KILOGRAM OF PROPELLANT, delivered");
            report.AppendLine("  patiently over months. For station-keeping, orbit raising and deep-");
            report.AppendLine("  space cruise, patience is exactly the resource that is cheap.");

            return report.ToString();
        }
    };

    /// <summary>
    /// Optimum exhaust velocity and the full mission mass breakdown.
    /// </summary>
    private static CodeSample ElectricMissionOptimisation() => new()
    {
        Id = "ch19-electric-mission",
        Chapter = "Chapter 19",
        Title = "Electric Mission Optimisation",
        Summary = "There is an optimum Isp, and it depends on how long you have.",

        Theory =
            """
            Higher Isp is not automatically better for an electric mission, and understanding
            why is the key insight of Chapter 19.

            Two masses compete:

            PROPELLANT MASS falls as Isp rises. That is just the rocket equation - a higher
            exhaust velocity means less propellant for the same delta-v. This term pushes Isp up.

            POWER PLANT MASS rises as Isp rises. To deliver a given total impulse in a given
            TIME at a higher exhaust velocity, you need more thrust-power, because
            P = F*c/(2*eta) and the required thrust for a fixed manoeuvre time is fixed by the
            momentum you must deliver. More power means a bigger, heavier solar array or
            reactor. This term pushes Isp down.

            The sum of the two has a minimum, and that minimum defines the OPTIMUM EXHAUST
            VELOCITY:

                c_opt = sqrt( 2 * alpha * eta * t_burn )

            where alpha is the power plant specific power in W/kg, eta the thruster efficiency
            and t_burn the available firing time.

            Read the consequences carefully, because they are counter-intuitive:

            1. c_opt scales as the SQUARE ROOT OF BURN TIME. A mission that can afford to fire
               for four times as long should use twice the exhaust velocity. If you are in a
               hurry, you should use a LOWER Isp - the opposite of most people's instinct.

            2. c_opt scales as the square root of alpha. Better power technology justifies
               higher Isp. This is why nuclear-electric concepts assume much higher Isp than
               solar-electric ones: a reactor's alpha does not fall off with distance from
               the Sun.

            3. c_opt does not depend on the delta-v or the payload mass at all. It is a
               property of the PROPULSION SYSTEM and the SCHEDULE, not of the destination.

            The mass breakdown below solves the coupled system - propellant depends on mass
            ratio, mass ratio depends on initial mass, initial mass depends on power plant
            mass, and power plant mass depends on thrust, which depends on the total mass - by
            fixed-point iteration. It converges quickly because the coupling is weak, but the
            residual is why the unit tests for this function use a looser tolerance than
            elsewhere in the library.
            """,

        CodeSnippet =
            """
            // The optimum exhaust velocity for this power technology and this schedule.
            double cOptimum = ElectricPropulsion.OptimumExhaustVelocity(
                deltaV:             deltaV,
                burnTimeSeconds:    burnTime,
                specificPower:      alpha,
                thrusterEfficiency: eta);

            // The coupled mass breakdown, solved by fixed-point iteration.
            (double propellantMass, double powerPlantMass, double thrust,
             double electricalPower, double initialMass) =
                ElectricPropulsion.MissionMassBreakdown(
                    payloadMass:        payload,
                    deltaV:             deltaV,
                    exhaustVelocity:    c,
                    burnTimeSeconds:    burnTime,
                    specificPower:      alpha,
                    thrusterEfficiency: eta);
            """,

        Parameters =
        [
            new SampleParameter("payload", "Payload mass", 500.0, "kg",
                "The useful spacecraft: instruments, comms, structure."),
            new SampleParameter("dv", "Mission delta-v", 6000.0, "m/s",
                "LEO to GEO by spiral is about 4700 m/s; deep space missions 5000-15000."),
            new SampleParameter("days", "Available firing time", 300.0, "days",
                "The schedule constraint. Change this and watch the optimum Isp move."),
            new SampleParameter("alpha", "Power plant specific power", 120.0, "W/kg",
                "100-200 for solar arrays at 1 AU; lower further out."),
            new SampleParameter("eta", "Thruster efficiency", 0.60, "",
                "0.45-0.75 depending on thruster type."),
            new SampleParameter("isp", "Isp to evaluate (0 = use the optimum)", 0.0, "s",
                "Enter 0 to use the computed optimum, or force a specific value.")
        ],

        Run = inputs =>
        {
            double payload = inputs["payload"];
            double deltaV = inputs["dv"];
            double burnTime = inputs["days"] * 86400.0;
            double alpha = inputs["alpha"];
            double eta = Math.Clamp(inputs["eta"], 0.01, 1.0);
            double forcedIsp = inputs["isp"];

            double cOptimum = ElectricPropulsion.OptimumExhaustVelocity(deltaV, burnTime, alpha, eta);
            double ispOptimum = Performance.SpecificImpulse(cOptimum);

            // Use the optimum unless the user forced a value.
            double c = forcedIsp > 0.0 ? Performance.ExhaustVelocityFromIsp(forcedIsp) : cOptimum;
            double isp = Performance.SpecificImpulse(c);

            (double propellantMass, double powerPlantMass, double thrust,
             double electricalPower, double initialMass) =
                ElectricPropulsion.MissionMassBreakdown(payload, deltaV, c, burnTime, alpha, eta);

            StringBuilder report = new();

            report.AppendLine(Heading("MISSION"));
            report.AppendLine(Line("Payload mass", payload, "kg", "N2"));
            report.AppendLine(Line("Mission delta-v", deltaV, "m/s", "N1"));
            report.AppendLine(Line("Available firing time", burnTime / 86400.0, "days", "N2"));
            report.AppendLine(Line("Available firing time", burnTime / 3600.0, "hours", "N1"));
            report.AppendLine(Line("Power plant specific power", alpha, "W/kg", "N2"));
            report.AppendLine(Line("Thruster efficiency", eta, "", "P2"));
            report.AppendLine();

            report.AppendLine(Heading("OPTIMUM EXHAUST VELOCITY"));
            report.AppendLine(Line("c_opt = sqrt(2*alpha*eta*t)", cOptimum, "m/s", "N2"));
            report.AppendLine(Line("Optimum specific impulse", ispOptimum, "s", "N2"));
            report.AppendLine(Line("Specific impulse in use", isp, "s", "N2"));
            report.AppendLine(forcedIsp > 0.0
                ? "  (you forced this value - compare the mass breakdown against the optimum)"
                : "  (using the computed optimum)");
            report.AppendLine();

            report.AppendLine(Heading("MASS BREAKDOWN"));
            report.AppendLine(Line("Payload", payload, "kg", "N3"));
            report.AppendLine(Line("Propellant", propellantMass, "kg", "N3"));
            report.AppendLine(Line("Power plant", powerPlantMass, "kg", "N3"));
            report.AppendLine("  " + new string('=', LabelWidth + 12));
            report.AppendLine(Line("Initial mass", initialMass, "kg", "N3"));
            report.AppendLine(Line("Check  sum of the three parts",
                payload + propellantMass + powerPlantMass, "kg", "N3"));
            report.AppendLine();
            report.AppendLine(Line("Payload fraction", payload / initialMass, "", "P3"));
            report.AppendLine(Line("Propellant fraction", propellantMass / initialMass, "", "P3"));
            report.AppendLine(Line("Power plant fraction", powerPlantMass / initialMass, "", "P3"));
            report.AppendLine();
            report.AppendLine("  At the true optimum the propellant and power plant masses are");
            report.AppendLine("  comparable to one another. That is the signature of a balanced");
            report.AppendLine("  design - if one dominates, the Isp is on the wrong side of optimal.");
            report.AppendLine();

            report.AppendLine(Heading("PROPULSION SYSTEM"));
            report.AppendLine(Line("Required thrust", thrust * 1000.0, "mN", "N4"));
            report.AppendLine(Line("Required electrical power", electricalPower, "W", "N2"));
            report.AppendLine(Line("Required electrical power", electricalPower / 1000.0, "kW", "N4"));
            report.AppendLine(Line("Initial acceleration",
                thrust / initialMass, "m/s^2", "E3"));
            report.AppendLine(Line("Initial acceleration in g",
                thrust / initialMass / Constants.StandardGravity, "g", "E3"));
            report.AppendLine(Line("Mass flow rate",
                Performance.MassFlowRate(thrust, c) * 1.0e6, "mg/s", "N4"));
            report.AppendLine(Line("Consistency  m_dot * t_burn",
                Performance.MassFlowRate(thrust, c) * burnTime, "kg", "N3"));
            report.AppendLine("  That last figure should reproduce the propellant mass above.");
            report.AppendLine();

            report.AppendLine(Heading("ISP SWEEP - finding the minimum by brute force"));
            report.AppendLine("    Isp(s)   Propellant(kg)   Power plant(kg)   Initial mass(kg)   Power(kW)");
            report.AppendLine("  " + new string('-', 84));

            double bestIsp = 0.0;
            double bestMass = double.MaxValue;

            for (double testIsp = Math.Max(300.0, ispOptimum * 0.25);
                 testIsp <= ispOptimum * 2.6;
                 testIsp += ispOptimum * 0.15)
            {
                double testC = Performance.ExhaustVelocityFromIsp(testIsp);
                (double mp, double mpp, _, double pw, double m0) =
                    ElectricPropulsion.MissionMassBreakdown(
                        payload, deltaV, testC, burnTime, alpha, eta);

                bool isBest = m0 < bestMass;
                if (isBest)
                {
                    bestMass = m0;
                    bestIsp = testIsp;
                }

                report.AppendLine(string.Format(
                    "  {0,8:F0}   {1,14:F2}   {2,15:F2}   {3,16:F2}   {4,9:F3} {5}",
                    testIsp, mp, mpp, m0, pw / 1000.0, isBest ? "<--" : ""));
            }

            report.AppendLine();
            report.AppendLine(Line("Lowest initial mass found at Isp", bestIsp, "s", "N1"));
            report.AppendLine(Line("Analytical optimum Isp", ispOptimum, "s", "N1"));
            report.AppendLine("  The brute-force scan and the closed-form optimum should agree to");
            report.AppendLine("  within the resolution of the sweep. Notice the shape of the curve:");
            report.AppendLine("  propellant falls monotonically with Isp while the power plant grows,");
            report.AppendLine("  and the SUM has a genuine interior minimum.");
            report.AppendLine();

            report.AppendLine(Heading("THE SQUARE-ROOT-OF-TIME LAW"));
            report.AppendLine("   Burn time(days)   c_opt(m/s)   Optimum Isp(s)   Ratio to 300 d");
            report.AppendLine("  " + new string('-', 68));
            double reference = ElectricPropulsion.OptimumExhaustVelocity(
                deltaV, 300.0 * 86400.0, alpha, eta);
            foreach (double days in new[] { 30.0, 75.0, 150.0, 300.0, 600.0, 1200.0, 2400.0 })
            {
                double cOpt = ElectricPropulsion.OptimumExhaustVelocity(
                    deltaV, days * 86400.0, alpha, eta);
                report.AppendLine(string.Format(
                    "  {0,16:F0}   {1,10:F1}   {2,14:F1}   {3,14:F4}",
                    days, cOpt, Performance.SpecificImpulse(cOpt), cOpt / reference));
            }
            report.AppendLine();
            report.AppendLine("  Each quadrupling of burn time doubles the optimum exhaust velocity -");
            report.AppendLine("  the ratio column shows exactly that. The practical consequence is");
            report.AppendLine("  counter-intuitive but important: if a mission is in a HURRY, it should");
            report.AppendLine("  use a LOWER specific impulse, not a higher one.");
            report.AppendLine();

            report.AppendLine(Heading("AZURE PLACEHOLDER"));
            report.AppendLine("  A full trajectory optimisation - low-thrust spiral shaping, eclipse");
            report.AppendLine("  modelling, solar array degradation, thruster throttle scheduling -");
            report.AppendLine("  is a large parametric sweep and is the natural candidate for cloud");
            report.AppendLine("  offload. See Services/AzurePlaceholderService.cs, where");
            report.AppendLine("  SubmitVehicleOptimisationAsync is stubbed for exactly this purpose.");
            report.AppendLine("  It currently returns null because no Azure resources are provisioned.");

            return report.ToString();
        }
    };
}
