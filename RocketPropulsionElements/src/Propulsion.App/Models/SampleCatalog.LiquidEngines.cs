using System.Text;
using Propulsion.Core;

namespace Propulsion.App.Models;

/// <summary>
/// Chapters 6, 8, 9 and 10 samples: liquid propellant engine subsystems.
/// </summary>
public static partial class SampleCatalog
{
    // ====================================================================================
    // CHAPTERS 6, 8, 9 and 10 - Liquid Propellant Engines
    // ====================================================================================

    /// <summary>
    /// Injector orifice sizing from the incompressible orifice equation.
    /// </summary>
    private static CodeSample InjectorSizing() => new()
    {
        Id = "ch8-injector",
        Chapter = "Chapter 8",
        Title = "Injector Orifice Sizing",
        Summary = "The orifice equation, and why injector pressure drop is deliberately large.",

        Theory =
            """
            The injector is the single most important component in a liquid rocket engine. It
            meters the flow, atomises it, mixes oxidiser with fuel, and - more than any other
            part - determines whether the engine runs smoothly or destroys itself in a
            combustion instability. The F-1 engine's development was dominated by injector
            redesign; roughly 2000 full-scale tests were fired to find a baffled injector
            configuration that was stable.

            The flow through each orifice follows the incompressible orifice equation:

                m_dot = Cd * A * sqrt(2 * rho * delta_p)

            Cd is the discharge coefficient, an empirical factor covering the vena contracta
            and viscous losses. A sharp-edged hole gives about 0.6; a well-rounded entry gives
            0.75-0.85; a properly contoured injector element can approach 0.9.

            Inverting for design:

                A = m_dot / (Cd * sqrt(2 * rho * delta_p))

            Now the important design rule. The injector pressure drop is typically set at
            15-25% OF CHAMBER PRESSURE, and it is chosen deliberately large, not minimised.
            The reason is HYDRAULIC DECOUPLING. If the pressure drop were small, a pressure
            oscillation in the chamber would feed straight back into the feed lines, modulating
            the flow, which modulates the combustion, which reinforces the oscillation. That
            positive feedback loop is a combustion instability, and it can go from nothing to
            engine destruction in milliseconds.

            A large, stiff injector drop makes the flow rate nearly independent of chamber
            pressure - because m_dot goes as sqrt(delta_p), and if delta_p is large, a small
            chamber perturbation barely changes it. The cost is that every bar of injector drop
            has to be supplied by the pumps or the pressurisation system, which is real mass and
            real power. So the drop is a deliberate stability-versus-mass trade, not an
            inefficiency.

            Note the square-root scaling: to double the flow through a fixed orifice you must
            QUADRUPLE the pressure drop. That is why deep throttling of a fixed-geometry
            injector is so hard, and why throttleable engines use pintle injectors or variable
            geometry.
            """,

        CodeSnippet =
            """
            // Required total orifice area for one propellant circuit.
            double area = LiquidEngine.RequiredOrificeArea(
                massFlowRate:         circuitFlow,
                dischargeCoefficient: cd,
                propellantDensity:    density,
                pressureDrop:         deltaP);

            // How many holes of the chosen diameter that needs.
            int holes = LiquidEngine.OrificeCount(area, orificeDiameter);

            // Round-trip check: the sized orifices should pass the flow we asked for.
            double actualFlow = LiquidEngine.InjectorMassFlow(cd, area, density, deltaP);
            """,

        Parameters =
        [
            new SampleParameter("mdot", "Total propellant flow", 250.0, "kg/s", "Oxidiser plus fuel."),
            new SampleParameter("mr", "Mixture ratio", 2.3, "", "Splits the flow between the circuits."),
            new SampleParameter("rhoox", "Oxidiser density", 1141.0, "kg/m^3", "1141 for LOX."),
            new SampleParameter("rhofuel", "Fuel density", 810.0, "kg/m^3", "810 for RP-1."),
            new SampleParameter("p1", "Chamber pressure", 70.0, "bar", "Sets the target injector drop."),
            new SampleParameter("dpfrac", "Injector drop as fraction of p1", 0.20, "",
                "0.15-0.25 typical. Below 0.10 invites instability."),
            new SampleParameter("cd", "Discharge coefficient", 0.75, "",
                "0.6 sharp-edged, 0.75-0.85 rounded."),
            new SampleParameter("dox", "Oxidiser orifice diameter", 1.5, "mm", "Typical 0.5-2.5 mm."),
            new SampleParameter("dfuel", "Fuel orifice diameter", 1.2, "mm", "Usually smaller than the oxidiser holes.")
        ],

        Run = inputs =>
        {
            double totalFlow = inputs["mdot"];
            double mixtureRatio = inputs["mr"];
            double rhoOx = inputs["rhoox"];
            double rhoFuel = inputs["rhofuel"];
            double p1 = Constants.BarToPa(inputs["p1"]);
            double dropFraction = inputs["dpfrac"];
            double cd = inputs["cd"];
            double dOx = inputs["dox"] / 1000.0;
            double dFuel = inputs["dfuel"] / 1000.0;

            double deltaP = dropFraction * p1;

            (double oxFlow, double fuelFlow) = Propellants.SplitFlow(totalFlow, mixtureRatio);

            double oxArea = LiquidEngine.RequiredOrificeArea(oxFlow, cd, rhoOx, deltaP);
            double fuelArea = LiquidEngine.RequiredOrificeArea(fuelFlow, cd, rhoFuel, deltaP);

            int oxHoles = LiquidEngine.OrificeCount(oxArea, dOx);
            int fuelHoles = LiquidEngine.OrificeCount(fuelArea, dFuel);

            // Round-trip verification - the whole point of having both directions in the library.
            double oxCheck = LiquidEngine.InjectorMassFlow(cd, oxArea, rhoOx, deltaP);
            double fuelCheck = LiquidEngine.InjectorMassFlow(cd, fuelArea, rhoFuel, deltaP);

            // Injection velocities, which drive atomisation quality.
            double oxVelocity = oxFlow / (rhoOx * oxArea);
            double fuelVelocity = fuelFlow / (rhoFuel * fuelArea);

            StringBuilder report = new();

            report.AppendLine(Heading("DESIGN POINT"));
            report.AppendLine(Line("Chamber pressure", Constants.PaToBar(p1), "bar", "N3"));
            report.AppendLine(Line("Injector pressure drop", Constants.PaToBar(deltaP), "bar", "N3"));
            report.AppendLine(Line("Drop as fraction of p1", dropFraction, "", "P2"));
            report.AppendLine(Line("Discharge coefficient", cd, "", "N3"));
            report.AppendLine();

            report.AppendLine(dropFraction switch
            {
                < 0.10 => "  WARNING: this drop is below 10% of chamber pressure. The injector is\n" +
                          "  hydraulically soft and the feed system is coupled to the chamber. This\n" +
                          "  configuration is a strong candidate for combustion instability.",
                < 0.15 => "  Marginal. Below 15% the injector provides limited decoupling; expect to\n" +
                          "  need acoustic cavities or baffles to guarantee stability.",
                <= 0.25 => "  Normal design range. Stiff enough to decouple the feed system from\n" +
                           "  chamber pressure oscillations without an excessive pump burden.",
                _ => "  High drop. Very stable, but the feed system must supply this pressure on\n" +
                     "  top of chamber pressure, costing pump power or tank mass."
            });
            report.AppendLine();

            report.AppendLine(Heading("OXIDISER CIRCUIT"));
            report.AppendLine(Line("Flow rate", oxFlow, "kg/s", "N3"));
            report.AppendLine(Line("Density", rhoOx, "kg/m^3", "N1"));
            report.AppendLine(Line("Required total orifice area", oxArea * 1.0e6, "mm^2", "N3"));
            report.AppendLine(Line("Orifice diameter", dOx * 1000.0, "mm", "N3"));
            report.AppendLine(Line("Area of one orifice",
                NozzleGeometry.AreaFromDiameter(dOx) * 1.0e6, "mm^2", "N4"));
            report.AppendLine(Line("Number of orifices", oxHoles.ToString()));
            report.AppendLine(Line("Injection velocity", oxVelocity, "m/s", "N2"));
            report.AppendLine(Line("Round-trip flow check", oxCheck, "kg/s", "N4"));
            report.AppendLine();

            report.AppendLine(Heading("FUEL CIRCUIT"));
            report.AppendLine(Line("Flow rate", fuelFlow, "kg/s", "N3"));
            report.AppendLine(Line("Density", rhoFuel, "kg/m^3", "N1"));
            report.AppendLine(Line("Required total orifice area", fuelArea * 1.0e6, "mm^2", "N3"));
            report.AppendLine(Line("Orifice diameter", dFuel * 1000.0, "mm", "N3"));
            report.AppendLine(Line("Area of one orifice",
                NozzleGeometry.AreaFromDiameter(dFuel) * 1.0e6, "mm^2", "N4"));
            report.AppendLine(Line("Number of orifices", fuelHoles.ToString()));
            report.AppendLine(Line("Injection velocity", fuelVelocity, "m/s", "N2"));
            report.AppendLine(Line("Round-trip flow check", fuelCheck, "kg/s", "N4"));
            report.AppendLine();

            report.AppendLine(Heading("ELEMENT COUNT"));
            report.AppendLine(Line("Total orifices", (oxHoles + fuelHoles).ToString()));
            report.AppendLine(Line("Momentum ratio (fuel / ox)",
                (fuelFlow * fuelVelocity) / (oxFlow * oxVelocity), "", "N4"));
            report.AppendLine("  Momentum ratio near 1 gives good impingement mixing in a");
            report.AppendLine("  like-on-like or unlike-doublet element pattern.");
            report.AppendLine();

            report.AppendLine(Heading("THE SQUARE-ROOT SCALING - throttling a fixed injector"));
            report.AppendLine("   Throttle   Required drop (bar)   Drop as % of full p1");
            report.AppendLine("  " + new string('-', 60));
            foreach (double throttle in new[] { 1.0, 0.8, 0.6, 0.4, 0.3, 0.2 })
            {
                // Flow goes as sqrt(dp), so dp goes as flow squared.
                double requiredDrop = deltaP * throttle * throttle;
                report.AppendLine(string.Format(
                    "  {0,8:P0}   {1,19:F3}   {2,20:P2}",
                    throttle, Constants.PaToBar(requiredDrop), requiredDrop / p1));
            }
            report.AppendLine();
            report.AppendLine("  At deep throttle the drop collapses as the SQUARE of the flow, so the");
            report.AppendLine("  injector loses its hydraulic stiffness exactly when you least want it");
            report.AppendLine("  to. That is why deeply throttleable engines use pintle injectors or");
            report.AppendLine("  other variable-area schemes rather than fixed orifices.");

            return report.ToString();
        }
    };

    /// <summary>
    /// The pressure budget from tank to chamber.
    /// </summary>
    private static CodeSample FeedSystemPressureBudget() => new()
    {
        Id = "ch6-feed-pressure",
        Chapter = "Chapter 6",
        Title = "Feed System Pressure Budget",
        Summary = "Working backwards from the chamber to the required tank pressure.",

        Theory =
            """
            Propellant only flows because something pushes it. The pressure at the source must
            exceed chamber pressure by the sum of everything in the way:

                p_tank = p_chamber + dp_injector + dp_lines_and_valves + dp_dynamic + dp_cooling

            Every one of those terms is real mass or real power.

            PRESSURE-FED SYSTEMS put that entire pressure in the tanks. The tanks then become
            pressure vessels, and tank mass scales as pressure times volume. That makes
            pressure feeding attractive only at low chamber pressure or small size - which is
            precisely where it is used: reaction control thrusters, small upper stages, landers
            and the Apollo Service Propulsion System. The great advantage is simplicity: no
            turbomachinery, no start transient to manage, very high reliability, and easy
            restart. Chamber pressures are typically limited to 10-25 bar.

            PUMP-FED SYSTEMS keep the tanks near-empty of pressure - just enough to prevent
            pump cavitation, typically 2-4 bar of net positive suction head - and let a
            turbopump supply the rest. The tanks become thin-walled and light, which is what
            makes chamber pressures of 100-300 bar possible. The price is a turbopump, which is
            one of the most highly stressed machines ever built: the Space Shuttle Main Engine
            high-pressure fuel turbopump extracted around 70000 horsepower from a package the
            size of a car engine, spinning at 35000 rpm.

            The cross-over is roughly at 20-30 bar chamber pressure or a few tonnes of
            propellant. Below that, the turbopump's fixed mass and complexity is not worth it;
            above it, the tank mass of pressure feeding becomes prohibitive.
            """,

        CodeSnippet =
            """
            // Sum the pressure budget from the chamber back to the tank.
            double tankPressure = LiquidEngine.RequiredTankPressure(
                chamberPressure:            p1,
                injectorPressureDrop:       injectorDrop,
                lineAndValvePressureDrop:   lineDrop,
                dynamicPressureAllowance:   dynamicAllowance);

            // Tank wall thickness from the thin-wall hoop stress formula: t = p*r / sigma.
            double wallThickness = tankPressure * tankRadius / allowableStress;
            """,

        Parameters =
        [
            new SampleParameter("p1", "Chamber pressure", 20.0, "bar",
                "Try 20 (pressure-fed) then 150 (pump-fed) to see the tank mass explode."),
            new SampleParameter("dpinj", "Injector pressure drop", 4.0, "bar",
                "Typically 15-25% of chamber pressure."),
            new SampleParameter("dpline", "Line, valve and cooling jacket drop", 3.0, "bar",
                "Plumbing losses plus the regenerative jacket."),
            new SampleParameter("dpdyn", "Dynamic and acceleration allowance", 1.0, "bar",
                "Covers flow acceleration and vehicle g-loading on the propellant column."),
            new SampleParameter("volume", "Total tank volume", 12.0, "m^3",
                "Drives tank mass directly."),
            new SampleParameter("tankr", "Tank radius", 1.2, "m",
                "Cylindrical tank radius."),
            new SampleParameter("sigma", "Allowable material stress", 350.0, "MPa",
                "About 350 MPa for aerospace aluminium with a safety factor applied."),
            new SampleParameter("rhomat", "Tank material density", 2800.0, "kg/m^3",
                "2800 for aluminium, 4500 for titanium, 1600 for composite.")
        ],

        Run = inputs =>
        {
            double p1 = Constants.BarToPa(inputs["p1"]);
            double injectorDrop = Constants.BarToPa(inputs["dpinj"]);
            double lineDrop = Constants.BarToPa(inputs["dpline"]);
            double dynamicDrop = Constants.BarToPa(inputs["dpdyn"]);
            double volume = inputs["volume"];
            double tankRadius = inputs["tankr"];
            double sigma = inputs["sigma"] * 1.0e6;
            double rhoMaterial = inputs["rhomat"];

            double tankPressure = LiquidEngine.RequiredTankPressure(
                p1, injectorDrop, lineDrop, dynamicDrop);

            StringBuilder report = new();

            report.AppendLine(Heading("PRESSURE BUDGET (chamber outwards)"));
            report.AppendLine(Line("Chamber pressure", Constants.PaToBar(p1), "bar", "N3"));
            report.AppendLine(Line("+ Injector drop", Constants.PaToBar(injectorDrop), "bar", "N3"));
            report.AppendLine(Line("+ Line, valve, jacket drop", Constants.PaToBar(lineDrop), "bar", "N3"));
            report.AppendLine(Line("+ Dynamic allowance", Constants.PaToBar(dynamicDrop), "bar", "N3"));
            report.AppendLine("  " + new string('=', LabelWidth + 12));
            report.AppendLine(Line("= Required source pressure", Constants.PaToBar(tankPressure), "bar", "N3"));
            report.AppendLine(Line("= Required source pressure", Constants.PaToPsi(tankPressure), "psi", "N1"));
            report.AppendLine();
            report.AppendLine(Line("Injector drop as % of p1", 100.0 * injectorDrop / p1, "%", "N2"));
            report.AppendLine(Line("Total overhead above chamber",
                100.0 * (tankPressure - p1) / p1, "%", "N2"));
            report.AppendLine();

            report.AppendLine(Heading("OPTION A - PRESSURE-FED"));
            report.AppendLine("  The tanks themselves must hold the full source pressure.");
            report.AppendLine();

            // Thin-wall cylinder: hoop stress sigma = p*r/t, so t = p*r/sigma.
            // Tank surface area approximated as a cylinder of the given volume and radius,
            // plus two hemispherical end caps.
            double cylinderLength = Math.Max(
                0.0, (volume - 4.0 / 3.0 * Math.PI * Math.Pow(tankRadius, 3)) / (Math.PI * tankRadius * tankRadius));
            double surfaceArea = 2.0 * Math.PI * tankRadius * cylinderLength
                                 + 4.0 * Math.PI * tankRadius * tankRadius;

            double wallThickness = tankPressure * tankRadius / sigma;
            double tankMass = surfaceArea * wallThickness * rhoMaterial;

            report.AppendLine(Line("Tank volume", volume, "m^3", "N3"));
            report.AppendLine(Line("Tank radius", tankRadius, "m", "N3"));
            report.AppendLine(Line("Cylindrical section length", cylinderLength, "m", "N3"));
            report.AppendLine(Line("Tank surface area", surfaceArea, "m^2", "N3"));
            report.AppendLine(Line("Required wall thickness", wallThickness * 1000.0, "mm", "N3"));
            report.AppendLine(Line("Tank structural mass", tankMass, "kg", "N1"));
            report.AppendLine();
            report.AppendLine("  Wall thickness is from the thin-wall hoop stress relation t = p*r/sigma.");
            report.AppendLine("  It scales LINEARLY with pressure, so tank mass does too.");
            report.AppendLine();

            report.AppendLine(Heading("OPTION B - PUMP-FED"));

            // Pumps only need enough tank pressure to avoid cavitation at the inlet.
            const double nsphPressure = 3.0e5;   // 3 bar of net positive suction head
            double pumpRise = tankPressure - nsphPressure;
            double pumpTankThickness = nsphPressure * tankRadius / sigma;
            double pumpTankMass = surfaceArea * pumpTankThickness * rhoMaterial;

            report.AppendLine(Line("Tank pressure needed (NPSH only)",
                Constants.PaToBar(nsphPressure), "bar", "N3"));
            report.AppendLine(Line("Pressure rise the pump must supply",
                Constants.PaToBar(pumpRise), "bar", "N3"));
            report.AppendLine(Line("Required wall thickness", pumpTankThickness * 1000.0, "mm", "N3"));
            report.AppendLine(Line("Tank structural mass", pumpTankMass, "kg", "N1"));
            report.AppendLine(Line("Tank mass saved", tankMass - pumpTankMass, "kg", "N1"));
            report.AppendLine(Line("Tank mass saving",
                100.0 * (1.0 - pumpTankMass / tankMass), "%", "N2"));
            report.AppendLine();
            report.AppendLine("  That saving is what must pay for the turbopump, its turbine, its gas");
            report.AppendLine("  generator or preburner, and the development cost of all of it.");
            report.AppendLine();

            report.AppendLine(Heading("VERDICT"));
            report.AppendLine(Constants.PaToBar(p1) switch
            {
                < 15.0 => "  At this low chamber pressure a pressure-fed system is almost certainly\n" +
                          "  the right answer. The tank mass penalty is modest and the reliability,\n" +
                          "  restartability and simplicity are worth far more.",
                < 30.0 => "  This is the cross-over region. Both architectures are defensible and the\n" +
                          "  decision will turn on propellant load, restart requirements, development\n" +
                          "  budget and reliability targets rather than on mass alone.",
                _ => "  At this chamber pressure a pressure-fed system is not viable - look at the\n" +
                     "  wall thickness and tank mass above. A turbopump is mandatory, and this is\n" +
                     "  exactly why every large launch vehicle engine is pump-fed."
            });

            return report.ToString();
        }
    };

    /// <summary>
    /// Turbopump power, head and specific speed.
    /// </summary>
    private static CodeSample TurbopumpSizing() => new()
    {
        Id = "ch10-turbopump",
        Chapter = "Chapter 10",
        Title = "Turbopump Power and Specific Speed",
        Summary = "How much power a pump needs, and why hydrogen pumps are enormous.",

        Theory =
            """
            The hydraulic power a pump must deliver is flow times pressure rise, divided by
            efficiency:

                P = m_dot * delta_p / (rho * eta)

            Look hard at the density in the denominator. For a GIVEN MASS FLOW and a GIVEN
            PRESSURE RISE, pump power is INVERSELY PROPORTIONAL TO DENSITY. Liquid hydrogen is
            71 kg/m^3 and RP-1 is 810 kg/m^3, a factor of 11.4. So a hydrogen pump moving the
            same mass to the same pressure needs 11.4 times the power of a kerosene pump.

            This is why the SSME's high-pressure fuel turbopump was such an extraordinary
            machine: about 52 MW (70000 hp) from a unit weighing around 350 kg, at 35000 rpm.
            It is also part of why hydrogen stages are expensive: it is not just the tanks, it
            is the turbomachinery too.

            PUMP HEAD expresses the same pressure rise as an equivalent column of the fluid:

                H = delta_p / (rho * g)

            Head is the natural variable for pump design because a given impeller geometry and
            tip speed produces a head that is nearly independent of the fluid. A hydrogen pump
            therefore needs a vastly greater head than a LOX pump for the same delta_p, which
            in practice means multiple stages - the SSME fuel pump has three.

            SPECIFIC SPEED is the dimensionless similarity parameter that selects the impeller
            type:

                Ns = omega * sqrt(Q) / (g*H)^0.75

            Low Ns (below about 0.6) calls for a radial centrifugal impeller: high head, low
            flow. High Ns (above about 2.5) calls for an axial impeller: high flow, low head.
            In between sits the mixed-flow design. Getting the specific speed wrong means an
            inefficient pump no matter how well it is made.

            CAVITATION is the other constraint. If the local static pressure at the impeller
            inlet drops below the propellant's vapour pressure, bubbles form and then collapse
            violently, eroding the metal and destroying the head rise. That is why tanks are
            pressurised to provide net positive suction head, and why an inducer - a low-head
            axial screw - is fitted ahead of the main impeller on essentially every rocket pump.
            """,

        CodeSnippet =
            """
            double power = LiquidEngine.PumpPower(
                massFlowRate:   mDot,
                pressureRise:   deltaP,
                density:        rho,
                pumpEfficiency: eta);

            double head = LiquidEngine.PumpHead(deltaP, rho);

            // Specific speed selects the impeller type. Q is VOLUME flow.
            double volumeFlow   = mDot / rho;
            double omega        = rpm * 2.0 * Math.PI / 60.0;
            double specificSpeed = LiquidEngine.PumpSpecificSpeed(omega, volumeFlow, head);
            """,

        Parameters =
        [
            new SampleParameter("mdot", "Mass flow rate", 70.0, "kg/s", "Flow through this one pump."),
            new SampleParameter("dp", "Pressure rise", 200.0, "bar", "Discharge minus inlet pressure."),
            new SampleParameter("rho", "Propellant density", 810.0, "kg/m^3",
                "810 RP-1, 1141 LOX, 71 LH2, 423 LCH4. Try 71 and watch the power."),
            new SampleParameter("eta", "Pump efficiency", 0.75, "", "0.65-0.80 for rocket pumps."),
            new SampleParameter("rpm", "Rotational speed", 25000.0, "rpm", "Rocket pumps run 10000-40000 rpm.")
        ],

        Run = inputs =>
        {
            double mDot = inputs["mdot"];
            double deltaP = Constants.BarToPa(inputs["dp"]);
            double rho = inputs["rho"];
            double eta = Math.Clamp(inputs["eta"], 0.05, 1.0);
            double rpm = inputs["rpm"];

            double power = LiquidEngine.PumpPower(mDot, deltaP, rho, eta);
            double head = LiquidEngine.PumpHead(deltaP, rho);
            double volumeFlow = mDot / rho;
            double omega = rpm * 2.0 * Math.PI / 60.0;
            double specificSpeed = LiquidEngine.PumpSpecificSpeed(omega, volumeFlow, head);

            // Ideal (100% efficient) power, to show what the efficiency term costs.
            double idealPower = mDot * deltaP / rho;

            StringBuilder report = new();

            report.AppendLine(Heading("PUMP DUTY"));
            report.AppendLine(Line("Mass flow rate", mDot, "kg/s", "N3"));
            report.AppendLine(Line("Density", rho, "kg/m^3", "N1"));
            report.AppendLine(Line("Volume flow rate", volumeFlow * 1000.0, "litre/s", "N2"));
            report.AppendLine(Line("Pressure rise", Constants.PaToBar(deltaP), "bar", "N2"));
            report.AppendLine(Line("Pressure rise", Constants.PaToPsi(deltaP), "psi", "N0"));
            report.AppendLine(Line("Pump head", head, "m", "N1"));
            report.AppendLine();

            report.AppendLine(Heading("POWER"));
            report.AppendLine(Line("Ideal hydraulic power", idealPower / 1000.0, "kW", "N2"));
            report.AppendLine(Line("Pump efficiency", eta, "", "P2"));
            report.AppendLine(Line("Shaft power required", power / 1000.0, "kW", "N2"));
            report.AppendLine(Line("Shaft power required", power / 1000.0 / 0.7457, "hp", "N0"));
            report.AppendLine(Line("Power lost to inefficiency", (power - idealPower) / 1000.0, "kW", "N2"));
            report.AppendLine(Line("Torque at this speed", power / omega, "N.m", "N2"));
            report.AppendLine();

            report.AppendLine(Heading("IMPELLER SELECTION"));
            report.AppendLine(Line("Rotational speed", rpm, "rpm", "N0"));
            report.AppendLine(Line("Angular velocity", omega, "rad/s", "N1"));
            report.AppendLine(Line("Specific speed Ns", specificSpeed, "", "N4"));
            report.AppendLine();
            report.AppendLine(specificSpeed switch
            {
                < 0.3 => "  Very low specific speed. A single radial impeller will be inefficient;\n" +
                         "  consider multiple stages in series or a higher shaft speed.",
                < 0.6 => "  RADIAL (centrifugal) impeller. High head, low flow - the classic rocket\n" +
                         "  oxidiser pump configuration.",
                < 1.5 => "  MIXED-FLOW impeller. A good compromise between head and flow.",
                < 2.5 => "  MIXED-FLOW toward AXIAL. Consider an axial design.",
                _ => "  AXIAL impeller. High flow, low head per stage - typical of an inducer\n" +
                     "  or a low-head booster pump."
            });
            report.AppendLine();

            report.AppendLine(Heading("THE DENSITY PENALTY - same duty, different propellant"));
            report.AppendLine("  Propellant         rho(kg/m3)   Head(m)     Power(kW)   Power(hp)");
            report.AppendLine("  " + new string('-', 70));
            foreach ((string name, double density) in new[]
                     {
                         ("Liquid oxygen", 1141.0),
                         ("RP-1 kerosene", 810.0),
                         ("Liquid methane", 423.0),
                         ("Nitrogen tetroxide", 1440.0),
                         ("Liquid hydrogen", 71.0)
                     })
            {
                double p = LiquidEngine.PumpPower(mDot, deltaP, density, eta);
                double h = LiquidEngine.PumpHead(deltaP, density);
                report.AppendLine(string.Format(
                    "  {0,-18} {1,10:F0} {2,10:F0} {3,12:F1} {4,11:F0}",
                    name, density, h, p / 1000.0, p / 745.7));
            }
            report.AppendLine();
            report.AppendLine("  Same mass flow, same pressure rise, same efficiency - and the hydrogen");
            report.AppendLine("  pump needs sixteen times the power of the LOX pump. Power is inversely");
            report.AppendLine("  proportional to density, and hydrogen is astonishingly light.");
            report.AppendLine();

            report.AppendLine(Heading("CAVITATION"));
            report.AppendLine("  The inlet static pressure must stay above the propellant's vapour");
            report.AppendLine("  pressure or bubbles form, collapse, and erode the impeller while");
            report.AppendLine("  destroying the head rise. Two standard mitigations:");
            report.AppendLine("    1. Pressurise the tank to provide net positive suction head,");
            report.AppendLine("       typically 2-4 bar above the vapour pressure.");
            report.AppendLine("    2. Fit an inducer - a low-head axial screw ahead of the main");
            report.AppendLine("       impeller - which raises the inlet pressure enough for the");
            report.AppendLine("       impeller to work cavitation-free.");
            report.AppendLine("  Cryogenic propellants are especially awkward because they sit at their");
            report.AppendLine("  boiling point by definition, leaving almost no margin.");

            return report.ToString();
        }
    };

    /// <summary>
    /// Regenerative cooling heat balance.
    /// </summary>
    private static CodeSample RegenerativeCoolingBalance() => new()
    {
        Id = "ch9-cooling",
        Chapter = "Chapter 9",
        Title = "Regenerative Cooling Heat Balance",
        Summary = "Can the fuel absorb the heat before it reaches the injector?",

        Theory =
            """
            A rocket chamber runs at 3000-3600 K. Every structural metal melts far below that:
            copper at 1358 K, stainless steel around 1700 K, and even the best nickel
            superalloys around 1700 K. Something must remove the heat continuously, or the
            chamber lasts a few seconds.

            REGENERATIVE COOLING is the elegant answer. Route one of the propellants - almost
            always the fuel - through channels in the chamber and nozzle wall before injecting
            it. The heat is absorbed by the coolant rather than lost, and it is carried back
            into the chamber where it is burnt anyway. Thermodynamically it costs essentially
            nothing.

            The heat flux to the wall is convective:

                q = h * A * (T_gas - T_wall)

            The heat transfer coefficient h is brutal near the throat, where the gas velocity
            peaks and the boundary layer is thinnest. Throat heat fluxes of 20-80 MW/m^2 are
            normal - among the highest sustained heat fluxes in any engineering device
            anywhere. That is why chambers are made of high-conductivity copper alloys such as
            NARloy-Z or GRCop-42 rather than steel: the metal must move heat sideways to the
            coolant faster than the gas delivers it.

            The coolant's temperature rise follows from its heat capacity:

                delta_T = q / (m_dot_coolant * cp)

            The design constraints are then:
              - the coolant must not boil in the channels (film boiling collapses heat transfer
                and the wall burns through in milliseconds);
              - the coolant must not coke - RP-1 deposits carbon above roughly 600 K, which
                insulates the wall and progressively makes things worse;
              - the pressure drop through the channels must be affordable in the feed budget;
              - the wall must stay below its strength limit at operating pressure.

            When regenerative cooling is not enough, engines add FILM COOLING (a deliberately
            fuel-rich layer along the wall), ABLATIVE liners (which erode away by design), or
            RADIATION cooling for small vacuum thrusters with refractory nozzle extensions.
            """,

        CodeSnippet =
            """
            // Convective heat rate into the wall over the cooled area.
            double heatRate = LiquidEngine.ConvectiveHeatTransfer(
                heatTransferCoefficient: h,
                surfaceArea:             cooledArea,
                gasTemperature:          gasTemperature,
                wallTemperature:         wallTemperature);

            // How much the coolant warms up absorbing it.
            double temperatureRise = LiquidEngine.CoolantTemperatureRise(
                heatRate:         heatRate,
                coolantMassFlow:  coolantFlow,
                specificHeat:     coolantCp);
            """,

        Parameters =
        [
            new SampleParameter("h", "Heat transfer coefficient", 8000.0, "W/(m^2.K)",
                "5000-20000 in the chamber; much higher at the throat."),
            new SampleParameter("area", "Cooled surface area", 1.2, "m^2",
                "Chamber plus converging section plus part of the nozzle."),
            new SampleParameter("tgas", "Gas-side driving temperature", 3400.0, "K",
                "The adiabatic wall temperature, slightly below chamber stagnation."),
            new SampleParameter("twall", "Gas-side wall temperature", 800.0, "K",
                "Design limit for the wall material."),
            new SampleParameter("mdotcool", "Coolant mass flow", 60.0, "kg/s",
                "Usually the entire fuel flow."),
            new SampleParameter("cp", "Coolant specific heat", 2100.0, "J/(kg.K)",
                "2100 for RP-1, 14300 for LH2, 3500 for methane."),
            new SampleParameter("tin", "Coolant inlet temperature", 290.0, "K",
                "Tank temperature."),
            new SampleParameter("tlimit", "Coolant temperature limit", 600.0, "K",
                "Coking limit for RP-1; boiling limit for cryogens.")
        ],

        Run = inputs =>
        {
            double h = inputs["h"];
            double area = inputs["area"];
            double tGas = inputs["tgas"];
            double tWall = inputs["twall"];
            double coolantFlow = inputs["mdotcool"];
            double cp = inputs["cp"];
            double tIn = inputs["tin"];
            double tLimit = inputs["tlimit"];

            if (tWall >= tGas)
            {
                throw new ArgumentException(
                    "The wall temperature must be below the gas temperature - heat only " +
                    "flows down a temperature gradient.");
            }

            double heatRate = LiquidEngine.ConvectiveHeatTransfer(h, area, tGas, tWall);
            double heatFlux = heatRate / area;
            double temperatureRise = LiquidEngine.CoolantTemperatureRise(heatRate, coolantFlow, cp);
            double tOut = tIn + temperatureRise;

            StringBuilder report = new();

            report.AppendLine(Heading("HEAT INPUT"));
            report.AppendLine(Line("Heat transfer coefficient h", h, "W/(m^2.K)", "N1"));
            report.AppendLine(Line("Cooled surface area", area, "m^2", "N4"));
            report.AppendLine(Line("Gas temperature", tGas, "K", "N1"));
            report.AppendLine(Line("Wall temperature", tWall, "K", "N1"));
            report.AppendLine(Line("Temperature difference", tGas - tWall, "K", "N1"));
            report.AppendLine(Line("Total heat rate", heatRate / 1.0e6, "MW", "N4"));
            report.AppendLine(Line("Average heat flux", heatFlux / 1.0e6, "MW/m^2", "N4"));
            report.AppendLine();
            report.AppendLine((heatFlux / 1.0e6) switch
            {
                < 5.0 => "  Modest heat flux, typical of a chamber wall away from the throat.",
                < 20.0 => "  High but routine for a chamber. Copper alloy walls handle this.",
                < 50.0 => "  Very high - this is throat-region territory. High-conductivity copper\n" +
                          "  alloy with closely spaced milled channels is essential.",
                _ => "  Extreme. Among the highest sustained heat fluxes in engineering. Expect\n" +
                     "  to need copper alloy plus film cooling, and expect the throat to be the\n" +
                     "  life-limiting component of the whole engine."
            });
            report.AppendLine();

            report.AppendLine(Heading("COOLANT SIDE"));
            report.AppendLine(Line("Coolant mass flow", coolantFlow, "kg/s", "N3"));
            report.AppendLine(Line("Coolant specific heat", cp, "J/(kg.K)", "N1"));
            report.AppendLine(Line("Thermal capacity  m_dot * cp",
                coolantFlow * cp / 1000.0, "kW/K", "N3"));
            report.AppendLine(Line("Inlet temperature", tIn, "K", "N1"));
            report.AppendLine(Line("Temperature rise", temperatureRise, "K", "N2"));
            report.AppendLine(Line("Outlet temperature", tOut, "K", "N2"));
            report.AppendLine(Line("Temperature limit", tLimit, "K", "N1"));
            report.AppendLine(Line("Margin", tLimit - tOut, "K", "N2"));
            report.AppendLine();

            if (tOut > tLimit)
            {
                report.AppendLine("  *** COOLING IS INADEQUATE ***");
                report.AppendLine();
                report.AppendLine("  The coolant exceeds its temperature limit before reaching the");
                report.AppendLine("  injector. With RP-1 this means coking: carbon deposits on the");
                report.AppendLine("  channel wall, insulating it, which raises the wall temperature,");
                report.AppendLine("  which accelerates the coking. That runaway ends in a burn-through.");
                report.AppendLine();
                report.AppendLine("  Options, roughly in order of preference:");
                report.AppendLine("    - increase the coolant flow (raise the mixture ratio's fuel side);");
                report.AppendLine("    - add film cooling along the wall to cut the gas-side heat flux;");
                report.AppendLine("    - accept a higher wall temperature with a better material;");
                report.AppendLine("    - reduce the cooled area by shortening the chamber (lower L*);");
                report.AppendLine("    - switch to an ablative or radiation-cooled nozzle extension.");

                double requiredFlow = heatRate / (cp * (tLimit - tIn));
                report.AppendLine();
                report.AppendLine(Line("  Coolant flow that WOULD suffice", requiredFlow, "kg/s", "N3"));
                report.AppendLine(Line("  Additional flow needed", requiredFlow - coolantFlow, "kg/s", "N3"));
            }
            else
            {
                report.AppendLine("  Cooling is adequate with margin. The coolant absorbs the full heat");
                report.AppendLine("  load and arrives at the injector below its limit temperature.");
                report.AppendLine();
                double maxHeat = coolantFlow * cp * (tLimit - tIn);
                report.AppendLine(Line("  Maximum heat this flow can absorb", maxHeat / 1.0e6, "MW", "N4"));
                report.AppendLine(Line("  Utilisation", 100.0 * heatRate / maxHeat, "%", "N2"));
            }
            report.AppendLine();

            report.AppendLine(Heading("WHY HYDROGEN IS THE BEST COOLANT"));
            report.AppendLine("  Coolant        cp(J/kg.K)   Rise(K)   Outlet(K)");
            report.AppendLine("  " + new string('-', 50));
            foreach ((string name, double coolantCp) in new[]
                     {
                         ("Liquid hydrogen", 14300.0),
                         ("Liquid methane", 3500.0),
                         ("RP-1 kerosene", 2100.0),
                         ("Hydrazine", 3080.0)
                     })
            {
                double rise = LiquidEngine.CoolantTemperatureRise(heatRate, coolantFlow, coolantCp);
                report.AppendLine(string.Format(
                    "  {0,-14} {1,10:F0} {2,9:F1} {3,11:F1}",
                    name, coolantCp, rise, tIn + rise));
            }
            report.AppendLine();
            report.AppendLine("  Hydrogen's specific heat is seven times kerosene's, so for the same");
            report.AppendLine("  heat load it warms up seven times less. It also does not coke. That");
            report.AppendLine("  is a large part of why hydrogen engines can run such high chamber");
            report.AppendLine("  pressures and such long durations.");
            report.AppendLine();
            report.AppendLine("  Note also that the absorbed heat is NOT wasted: the warmed fuel is");
            report.AppendLine("  injected and burnt, so regenerative cooling is nearly free. On some");
            report.AppendLine("  expander-cycle engines the heat picked up in the jacket is the ONLY");
            report.AppendLine("  thing driving the turbopump - the RL10 works exactly this way.");

            return report.ToString();
        }
    };
}
