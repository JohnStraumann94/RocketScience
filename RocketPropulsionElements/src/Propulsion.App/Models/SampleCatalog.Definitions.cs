using System.Text;
using Propulsion.Core;

namespace Propulsion.App.Models;

/// <summary>
/// Chapter 2 samples: the definitions on which everything else in the book rests.
/// </summary>
public static partial class SampleCatalog
{
    // ====================================================================================
    // CHAPTER 2 - Definitions and Fundamentals
    // ====================================================================================

    /// <summary>
    /// Thrust, effective exhaust velocity and specific impulse from first principles.
    /// </summary>
    private static CodeSample ThrustAndSpecificImpulse() => new()
    {
        Id = "ch2-thrust-isp",
        Chapter = "Chapter 2",
        Title = "Thrust, Exhaust Velocity and Specific Impulse",
        Summary = "The two-term thrust equation and how Isp is defined from it.",

        Theory =
            """
            Sutton's thrust equation has two terms, and almost every misunderstanding in
            rocketry comes from forgetting the second one:

                F = m_dot * v2  +  (p2 - p3) * A2

            The FIRST term is momentum thrust: mass leaving at velocity v2 carries momentum
            away, and by Newton's third law that pushes the vehicle forward. This term is
            always positive and is usually 90% or more of the total.

            The SECOND term is pressure thrust. The gas at the nozzle exit plane is at
            pressure p2; the atmosphere outside pushes back at p3. The net force on the exit
            area A2 is (p2 - p3) * A2. Note the sign: it is POSITIVE in vacuum (p3 = 0) and
            can be NEGATIVE at sea level if the nozzle is over-expanded (p2 < p3). This is
            precisely why the same engine produces more thrust in space than on the pad, and
            why nozzles are a compromise.

            Effective exhaust velocity c rolls both terms into one number:

                c = F / m_dot

            so that F = m_dot * c always, whatever the ambient pressure. c is the honest
            performance figure. Specific impulse is just c divided by the standard gravity
            constant:

                Isp = c / g0        with g0 = 9.80665 m/s^2 exactly

            g0 here is NOT the local gravitational acceleration - it is a defined unit
            conversion constant. Isp is measured in seconds only because that number happens
            to be the same whether you work in SI or in pounds, which is why the convention
            survived. Physically, Isp is "how many seconds this engine could hold up one
            kilogram of its own propellant".
            """,

        CodeSnippet =
            """
            // Momentum thrust plus pressure thrust - Sutton Eq. 2-14.
            double thrust = Performance.Thrust(
                massFlowRate: mDot,
                exitVelocity: v2,
                exitPressure: p2,
                ambientPressure: p3,
                exitArea: a2);

            // Effective exhaust velocity folds the pressure term back in.
            double c = Performance.EffectiveExhaustVelocity(thrust, mDot);

            // Specific impulse is c divided by the DEFINED constant g0 = 9.80665.
            double isp = Performance.SpecificImpulse(c);
            """,

        Parameters =
        [
            new SampleParameter("mdot", "Propellant mass flow rate", 250.0, "kg/s",
                "Total flow of oxidiser plus fuel through the chamber."),
            new SampleParameter("v2", "Nozzle exit velocity", 2800.0, "m/s",
                "The actual gas velocity at the exit plane (not the same as c)."),
            new SampleParameter("p2", "Nozzle exit pressure", 70000.0, "Pa",
                "Static pressure of the gas at the exit plane."),
            new SampleParameter("a2", "Nozzle exit area", 1.5, "m^2",
                "Physical area of the nozzle exit plane."),
            new SampleParameter("p3", "Ambient pressure", 101325.0, "Pa",
                "101325 Pa at sea level, 0 in vacuum. Try both.")
        ],

        Run = inputs =>
        {
            double mDot = inputs["mdot"];
            double v2 = inputs["v2"];
            double p2 = inputs["p2"];
            double a2 = inputs["a2"];
            double p3 = inputs["p3"];

            // Split the calculation apart so the report can show each term separately -
            // seeing the pressure term change sign is the whole point of this sample.
            double momentumThrust = mDot * v2;
            double pressureThrust = (p2 - p3) * a2;

            double thrust = Performance.Thrust(mDot, v2, p2, p3, a2);
            double c = Performance.EffectiveExhaustVelocity(thrust, mDot);
            double isp = Performance.SpecificImpulse(c);

            // Recompute at vacuum for comparison, holding the engine identical.
            double vacuumThrust = Performance.Thrust(mDot, v2, p2, 0.0, a2);
            double vacuumIsp = Performance.SpecificImpulse(
                Performance.EffectiveExhaustVelocity(vacuumThrust, mDot));

            StringBuilder report = new();

            report.AppendLine(Heading("THRUST BREAKDOWN"));
            report.AppendLine(Line("Momentum thrust  m_dot * v2", momentumThrust / 1000.0, "kN", "N3"));
            report.AppendLine(Line("Pressure thrust  (p2-p3) * A2", pressureThrust / 1000.0, "kN", "N3"));
            report.AppendLine(Line("Total thrust F", thrust / 1000.0, "kN", "N3"));
            report.AppendLine(Line("Total thrust F", Constants.NewtonsToPoundsForce(thrust), "lbf", "N0"));
            report.AppendLine();

            report.AppendLine(Line("Pressure term as share of total",
                100.0 * pressureThrust / thrust, "%", "N2"));
            report.AppendLine(Line("Pressure term sign",
                pressureThrust > 0 ? "POSITIVE (under-expanded)"
                : pressureThrust < 0 ? "NEGATIVE (over-expanded)"
                : "ZERO (perfectly expanded)"));
            report.AppendLine();

            report.AppendLine(Heading("DERIVED PERFORMANCE"));
            report.AppendLine(Line("Exit velocity v2", v2, "m/s", "N1"));
            report.AppendLine(Line("Effective exhaust velocity c", c, "m/s", "N1"));
            report.AppendLine(Line("Specific impulse Isp", isp, "s", "N2"));
            report.AppendLine();

            report.AppendLine(Heading("THE SAME ENGINE IN VACUUM"));
            report.AppendLine(Line("Vacuum thrust", vacuumThrust / 1000.0, "kN", "N3"));
            report.AppendLine(Line("Vacuum Isp", vacuumIsp, "s", "N2"));
            report.AppendLine(Line("Thrust gained by leaving the atmosphere",
                (vacuumThrust - thrust) / 1000.0, "kN", "N3"));
            report.AppendLine();
            report.AppendLine("  The gain is exactly p3 * A2 - ambient pressure acting on the exit plane.");
            report.AppendLine(Line("  Check: p3 * A2", p3 * a2 / 1000.0, "kN", "N3"));

            return report.ToString();
        }
    };

    /// <summary>
    /// Total impulse, propellant mass fraction, mass ratio and thrust-to-weight.
    /// </summary>
    private static CodeSample TotalImpulseAndMassFractions() => new()
    {
        Id = "ch2-impulse-fractions",
        Chapter = "Chapter 2",
        Title = "Total Impulse and Mass Fractions",
        Summary = "The bookkeeping quantities used to compare whole vehicles.",

        Theory =
            """
            Thrust tells you how hard an engine pushes. Total impulse tells you how much
            pushing it can do in total:

                It = F * tb            (constant thrust)

            Total impulse is the integral of thrust over the burn, and it is the single number
            that most directly predicts what a stage can achieve. Dividing it by the propellant
            weight recovers specific impulse, which is why Isp is called "specific" - it is
            total impulse per unit weight of propellant:

                Isp = It / (mp * g0)

            The mass bookkeeping quantities matter just as much as the engine numbers:

                zeta = mp / m0                  propellant mass fraction
                MR   = m0 / mf                  mass ratio
                m0   = mf + mp                  initial = final + propellant

            These are related by MR = 1 / (1 - zeta). A propellant mass fraction of 0.90 - a
            very good launch stage - is a mass ratio of 10. Pushing zeta from 0.90 to 0.91
            raises MR from 10.0 to 11.1, and because the rocket equation is logarithmic, that
            single percentage point buys real delta-v. It is also brutally hard to get, which
            is why launch vehicle structures are as thin as they are.

            Thrust-to-weight ratio decides whether the vehicle moves at all:

                T/W = F / (m0 * g0)

            A first stage needs T/W above 1.0 just to leave the pad, and in practice around
            1.2 to 1.5 - below that, gravity losses during the slow initial climb eat the
            delta-v advantage. An upper stage or a spacecraft can happily run at 0.1 or less,
            and an ion thruster runs at 0.0001.
            """,

        CodeSnippet =
            """
            double totalImpulse   = Performance.TotalImpulse(thrust, burnTime);
            double isp            = Performance.SpecificImpulseFromTotalImpulse(totalImpulse, propellantMass);
            double massFlow       = Performance.MassFlowRate(thrust, Performance.ExhaustVelocityFromIsp(isp));
            double zeta           = Performance.PropellantMassFraction(propellantMass, initialMass);
            double massRatio      = Performance.MassRatio(initialMass, initialMass - propellantMass);
            double thrustToWeight = Performance.ThrustToWeightRatio(thrust, initialMass);
            """,

        Parameters =
        [
            new SampleParameter("thrust", "Thrust (assumed constant)", 1000.0, "kN",
                "Sea-level or vacuum thrust, whichever the burn is flown at."),
            new SampleParameter("burntime", "Burn time", 160.0, "s",
                "How long the engine fires."),
            new SampleParameter("m0", "Initial (gross) mass", 100000.0, "kg",
                "Everything on the pad: propellant, structure and payload."),
            new SampleParameter("mp", "Propellant mass", 88000.0, "kg",
                "Usable propellant only; residuals stay in the final mass.")
        ],

        Run = inputs =>
        {
            double thrust = inputs["thrust"] * 1000.0;   // kN entered, newtons used internally
            double burnTime = inputs["burntime"];
            double m0 = inputs["m0"];
            double mp = inputs["mp"];

            if (mp >= m0)
            {
                throw new ArgumentException(
                    "Propellant mass must be less than the initial mass - the vehicle needs " +
                    "some structure and payload left over at burnout.");
            }

            double mf = m0 - mp;

            double totalImpulse = Performance.TotalImpulse(thrust, burnTime);
            double isp = Performance.SpecificImpulseFromTotalImpulse(totalImpulse, mp);
            double c = Performance.ExhaustVelocityFromIsp(isp);
            double massFlow = Performance.MassFlowRate(thrust, c);

            double zeta = Performance.PropellantMassFraction(mp, m0);
            double massRatio = Performance.MassRatio(m0, mf);
            double thrustToWeight = Performance.ThrustToWeightRatio(thrust, m0);
            double burnoutThrustToWeight = Performance.ThrustToWeightRatio(thrust, mf);

            // Ideal delta-v follows immediately and is the reason anyone cares about the above.
            double deltaV = RocketEquation.DeltaV(c, m0, mf);

            StringBuilder report = new();

            report.AppendLine(Heading("IMPULSE"));
            report.AppendLine(Line("Total impulse It", totalImpulse / 1.0e6, "MN.s", "N3"));
            report.AppendLine(Line("Implied specific impulse", isp, "s", "N2"));
            report.AppendLine(Line("Implied effective exhaust velocity", c, "m/s", "N1"));
            report.AppendLine(Line("Implied mass flow rate", massFlow, "kg/s", "N2"));
            report.AppendLine(Line("Consistency check  m_dot * tb", massFlow * burnTime, "kg", "N1"));
            report.AppendLine("  (that check should reproduce the propellant mass you entered)");
            report.AppendLine();

            report.AppendLine(Heading("MASS BOOKKEEPING"));
            report.AppendLine(Line("Initial mass m0", m0, "kg", "N1"));
            report.AppendLine(Line("Propellant mass mp", mp, "kg", "N1"));
            report.AppendLine(Line("Final (burnout) mass mf", mf, "kg", "N1"));
            report.AppendLine(Line("Propellant mass fraction zeta", zeta, "", "N4"));
            report.AppendLine(Line("Mass ratio MR = mf/m0", massRatio, "", "N4"));
            report.AppendLine(Line("Check  1 - zeta", 1.0 - zeta, "", "N4"));
            report.AppendLine("  Sutton defines MR as FINAL over INITIAL, so it is less than one");
            report.AppendLine("  and equals 1 - zeta exactly. Many other sources use the reciprocal.");
            report.AppendLine(Line("Reciprocal  m0/mf", 1.0 / massRatio, "", "N4"));
            report.AppendLine();

            report.AppendLine(Heading("ACCELERATION"));
            report.AppendLine(Line("Thrust-to-weight at ignition", thrustToWeight, "", "N3"));
            report.AppendLine(Line("Thrust-to-weight at burnout", burnoutThrustToWeight, "", "N3"));
            report.AppendLine(Line("Initial acceleration",
                RocketEquation.Acceleration(thrust, m0), "m/s^2", "N3"));
            report.AppendLine(Line("Burnout acceleration",
                RocketEquation.Acceleration(thrust, mf), "m/s^2", "N3"));
            report.AppendLine(Line("Burnout acceleration in g",
                RocketEquation.Acceleration(thrust, mf) / Constants.StandardGravity, "g", "N2"));
            report.AppendLine();

            report.AppendLine(thrustToWeight > 1.0
                ? "  T/W > 1: this stage can lift itself off the pad."
                : "  T/W < 1: this stage CANNOT lift off - it must be an upper stage or an in-space burn.");
            report.AppendLine();

            report.AppendLine(Heading("RESULTING CAPABILITY"));
            report.AppendLine(Line("Ideal delta-v (no losses)", deltaV, "m/s", "N1"));

            return report.ToString();
        }
    };
}
