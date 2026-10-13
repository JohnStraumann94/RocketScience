using System.Text;
using Propulsion.Core;

namespace Propulsion.App.Models;

/// <summary>
/// Chapter 4 samples: flight performance, the rocket equation, staging and ascent.
/// </summary>
public static partial class SampleCatalog
{
    // ====================================================================================
    // CHAPTER 4 - Flight Performance
    // ====================================================================================

    /// <summary>
    /// The Tsiolkovsky rocket equation, forwards and backwards.
    /// </summary>
    private static CodeSample RocketEquationExplorer() => new()
    {
        Id = "ch4-rocket-equation",
        Chapter = "Chapter 4",
        Title = "The Rocket Equation",
        Summary = "Delta-v from mass ratio, and the inverse problem of sizing propellant.",

        Theory =
            """
            Tsiolkovsky, 1903. Integrate F = m * dv/dt with F = -c * dm/dt and you get:

                delta_v = c * ln(m0 / mf) = g0 * Isp * ln(m0 / mf)

            Everything about launch vehicle design follows from the shape of that logarithm.

            The consequence people underestimate: delta-v is LINEAR in exhaust velocity but
            only LOGARITHMIC in mass ratio. Doubling Isp doubles your delta-v. Doubling your
            mass ratio only adds c * ln(2) = 0.69c. To get twice the delta-v from mass ratio
            alone you must SQUARE the mass ratio - a vehicle with MR = 10 must become MR = 100,
            which means going from 90% propellant by mass to 99%. That is not an engineering
            challenge; it is impossible with a real structure and a real payload.

            This is why staging exists, and why chemical rockets stopped improving decades ago:
            we ran out of chemistry. LOX/LH2 at ~450 s vacuum Isp is essentially the ceiling
            for practical bipropellants.

            The inverse problem is the one you actually solve in design. Given a required
            delta-v and a final mass, the propellant needed is:

                mp = mf * (exp(delta_v / c) - 1)

            Note the exponential. Propellant mass grows exponentially with mission delta-v.
            Adding 1000 m/s to a mission that already needs 9000 m/s costs far more than adding
            the first 1000 m/s. This is why margin is expensive and why mission planners fight
            over tens of metres per second.
            """,

        CodeSnippet =
            """
            // Forward problem: what delta-v does this vehicle have?
            double deltaV = RocketEquation.DeltaVFromIsp(isp, initialMass, finalMass);

            // Inverse problem: how much propellant do I need for a required delta-v?
            double c              = Performance.ExhaustVelocityFromIsp(isp);
            double requiredMr     = RocketEquation.RequiredMassRatio(requiredDeltaV, c);
            double propellantMass = RocketEquation.RequiredPropellantMass(requiredDeltaV, c, finalMass);
            """,

        Parameters =
        [
            new SampleParameter("isp", "Specific impulse", 350.0, "s",
                "Solids ~250, storables ~320, LOX/RP-1 ~340, LOX/LH2 ~450."),
            new SampleParameter("m0", "Initial mass", 50000.0, "kg",
                "Gross mass at ignition."),
            new SampleParameter("mf", "Final (burnout) mass", 8000.0, "kg",
                "Structure plus payload plus residuals."),
            new SampleParameter("dvreq", "Required delta-v (inverse problem)", 9500.0, "m/s",
                "LEO needs ~9400 m/s including losses; GTO ~11800 m/s.")
        ],

        Run = inputs =>
        {
            double isp = inputs["isp"];
            double m0 = inputs["m0"];
            double mf = inputs["mf"];
            double requiredDeltaV = inputs["dvreq"];

            if (mf >= m0)
            {
                throw new ArgumentException(
                    "Final mass must be less than initial mass - a rocket has to throw " +
                    "something overboard to accelerate.");
            }

            double c = Performance.ExhaustVelocityFromIsp(isp);

            // NOTE ON CONVENTION: Propulsion.Core follows Sutton, who defines the mass ratio as
            // FINAL over INITIAL, so MR is less than one and equals 1 - zeta. The reciprocal
            // m0/mf, which many other texts call the mass ratio, is what appears inside the
            // logarithm of the rocket equation, so both are reported here to avoid confusion.
            double massRatio = Performance.MassRatio(m0, mf);
            double inverseMassRatio = 1.0 / massRatio;

            double deltaV = RocketEquation.DeltaV(c, m0, mf);
            double propellantCarried = m0 - mf;

            StringBuilder report = new();

            report.AppendLine(Heading("FORWARD PROBLEM - what can this vehicle do?"));
            report.AppendLine(Line("Specific impulse", isp, "s", "N2"));
            report.AppendLine(Line("Effective exhaust velocity c", c, "m/s", "N2"));
            report.AppendLine(Line("Initial mass m0", m0, "kg", "N1"));
            report.AppendLine(Line("Final mass mf", mf, "kg", "N1"));
            report.AppendLine(Line("Propellant carried", propellantCarried, "kg", "N1"));
            report.AppendLine(Line("Mass ratio MR = mf/m0 (Sutton)", massRatio, "", "N4"));
            report.AppendLine(Line("Reciprocal m0/mf", inverseMassRatio, "", "N4"));
            report.AppendLine(Line("ln(m0/mf)", Math.Log(inverseMassRatio), "", "N5"));
            report.AppendLine(Line("Ideal delta-v", deltaV, "m/s", "N2"));
            report.AppendLine();

            report.AppendLine(Heading("SENSITIVITY - the logarithm at work"));
            double dvIspUp = RocketEquation.DeltaVFromIsp(isp * 1.1, m0, mf);
            double dvMassUp = RocketEquation.DeltaV(c, m0, mf * 0.9);
            report.AppendLine(Line("+10% Isp buys", dvIspUp - deltaV, "m/s", "N2"));
            report.AppendLine(Line("-10% dry mass buys", dvMassUp - deltaV, "m/s", "N2"));
            report.AppendLine(Line("Doubling the mass ratio buys",
                RocketEquation.DeltaV(c, m0, m0 / (2.0 * inverseMassRatio)) - deltaV, "m/s", "N2"));
            report.AppendLine("  Doubling m0/mf always adds exactly c * ln(2), regardless of where");
            report.AppendLine("  you started. Doubling Isp doubles the whole answer. Not the same.");
            report.AppendLine(Line("  c * ln(2)", c * Math.Log(2.0), "m/s", "N2"));
            report.AppendLine();

            report.AppendLine(Heading("INVERSE PROBLEM - sizing for a requirement"));
            report.AppendLine(Line("Required delta-v", requiredDeltaV, "m/s", "N2"));
            double requiredMr = RocketEquation.RequiredMassRatio(requiredDeltaV, c);
            double requiredProp = RocketEquation.RequiredPropellantMass(requiredDeltaV, c, mf);
            report.AppendLine(Line("Required mass ratio m0/mf", requiredMr, "", "N4"));
            report.AppendLine(Line("Required propellant (same dry mass)", requiredProp, "kg", "N1"));
            report.AppendLine(Line("Required initial mass", mf + requiredProp, "kg", "N1"));
            report.AppendLine(Line("Propellant you actually carry", propellantCarried, "kg", "N1"));
            report.AppendLine(Line("Shortfall or surplus", propellantCarried - requiredProp, "kg", "N1"));
            report.AppendLine();
            report.AppendLine(deltaV >= requiredDeltaV
                ? "  This vehicle MEETS the requirement."
                : "  This vehicle FALLS SHORT of the requirement.");
            report.AppendLine();

            report.AppendLine(Heading("THE EXPONENTIAL COST OF MARGIN"));
            report.AppendLine("   Extra dv (m/s)   Extra propellant (kg)");
            report.AppendLine("  " + new string('-', 44));
            foreach (double extra in new[] { 100.0, 250.0, 500.0, 1000.0, 2000.0 })
            {
                double withMargin = RocketEquation.RequiredPropellantMass(
                    requiredDeltaV + extra, c, mf);
                report.AppendLine($"   {extra,12:F0}   {withMargin - requiredProp,20:F1}");
            }
            report.AppendLine();
            report.AppendLine("  Each row costs more than the one above it, for the same increment.");
            report.AppendLine("  That is the exponential, and it is why delta-v margin is fought over.");

            return report.ToString();
        }
    };

    /// <summary>
    /// Payload fraction and why single-stage-to-orbit is so marginal.
    /// </summary>
    private static CodeSample PayloadFractionAndTheSingleStageLimit() => new()
    {
        Id = "ch4-payload-fraction",
        Chapter = "Chapter 4",
        Title = "Payload Fraction and the Single-Stage Limit",
        Summary = "Why SSTO is possible on paper and brutal in practice.",

        Theory =
            """
            Define the structural coefficient as the fraction of a stage's non-payload mass
            that is structure rather than propellant:

                epsilon = m_structure / (m_structure + m_propellant)

            A very good launch stage achieves epsilon = 0.06 to 0.10. The Centaur upper stage,
            a famous outlier with its pressure-stabilised balloon tank, reached about 0.075
            despite carrying cryogenic hydrogen.

            For a single stage carrying a payload, the payload fraction works out as:

                lambda = [ exp(-dv/c) - epsilon ] / (1 - epsilon)

            Stare at the numerator. The exponential term shrinks as the mission gets harder.
            When exp(-dv/c) falls to epsilon, the payload fraction hits ZERO - the stage can
            only just accelerate its own structure, and there is nothing left over. That gives
            an absolute ceiling on what a single stage can do:

                dv_max = -c * ln(epsilon)

            For epsilon = 0.08 and c = 3400 m/s (a good LOX/RP-1 engine), dv_max is about
            8590 m/s. Orbit needs about 9400 m/s including gravity and drag losses. A
            kerosene SSTO is therefore not merely difficult - it is IMPOSSIBLE, with zero
            payload, at that structural coefficient.

            With LOX/LH2 at c = 4400 m/s, dv_max rises to about 11100 m/s, so SSTO becomes
            arithmetically possible. But the payload fraction at 9400 m/s is only a couple of
            percent, and hydrogen's low density means enormous, heavy tanks that push epsilon
            back up. Every SSTO programme has died on this cliff. The curve is not gentle: the
            payload fraction plunges toward zero as you approach dv_max, so a small mass growth
            during development wipes out the entire payload.
            """,

        CodeSnippet =
            """
            // Payload fraction for a single stage: lambda = (exp(-dv/c) - eps) / (1 - eps).
            double payloadFraction = RocketEquation.PayloadFraction(
                deltaV:                deltaV,
                effectiveExhaustVelocity: c,
                structuralCoefficient: epsilon);

            // The hard ceiling: dv_max = -c * ln(eps), where payload fraction reaches zero.
            double maxDeltaV = RocketEquation.MaximumSingleStageDeltaV(c, epsilon);
            """,

        Parameters =
        [
            new SampleParameter("isp", "Specific impulse", 340.0, "s",
                "Try 340 (LOX/RP-1) then 450 (LOX/LH2) to see SSTO become possible."),
            new SampleParameter("eps", "Structural coefficient epsilon", 0.08, "",
                "0.06 is superb, 0.10 is ordinary, 0.15 is heavy."),
            new SampleParameter("dv", "Mission delta-v", 9400.0, "m/s",
                "LEO including gravity and drag losses.")
        ],

        Run = inputs =>
        {
            double isp = inputs["isp"];
            double epsilon = inputs["eps"];
            double deltaV = inputs["dv"];

            if (epsilon <= 0.0 || epsilon >= 1.0)
            {
                throw new ArgumentException(
                    "The structural coefficient must be strictly between 0 and 1.");
            }

            double c = Performance.ExhaustVelocityFromIsp(isp);
            double maxDeltaV = RocketEquation.MaximumSingleStageDeltaV(c, epsilon);
            double payloadFraction = RocketEquation.PayloadFraction(deltaV, c, epsilon);

            StringBuilder report = new();

            report.AppendLine(Heading("THE STAGE"));
            report.AppendLine(Line("Specific impulse", isp, "s", "N2"));
            report.AppendLine(Line("Effective exhaust velocity c", c, "m/s", "N2"));
            report.AppendLine(Line("Structural coefficient epsilon", epsilon, "", "N4"));
            report.AppendLine();

            report.AppendLine(Heading("THE HARD CEILING"));
            report.AppendLine(Line("dv_max = -c * ln(epsilon)", maxDeltaV, "m/s", "N2"));
            report.AppendLine(Line("Requested mission delta-v", deltaV, "m/s", "N2"));
            report.AppendLine(Line("Margin against the ceiling", maxDeltaV - deltaV, "m/s", "N2"));
            report.AppendLine();

            if (payloadFraction <= 0.0)
            {
                report.AppendLine("  PAYLOAD FRACTION IS ZERO OR NEGATIVE.");
                report.AppendLine();
                report.AppendLine("  This single stage cannot reach the requested delta-v even with no");
                report.AppendLine("  payload at all. It would run out of propellant while still trying");
                report.AppendLine("  to accelerate its own empty structure. There is no amount of");
                report.AppendLine("  engineering cleverness that fixes this within a single stage - you");
                report.AppendLine("  must raise Isp, lower epsilon, or add a stage.");
            }
            else
            {
                report.AppendLine(Heading("PAYLOAD"));
                report.AppendLine(Line("Payload fraction lambda", payloadFraction, "", "N5"));
                report.AppendLine(Line("Payload fraction", 100.0 * payloadFraction, "%", "N3"));
                report.AppendLine(Line("Payload per 100 t gross mass",
                    100000.0 * payloadFraction, "kg", "N1"));
            }
            report.AppendLine();

            report.AppendLine(Heading("THE CLIFF - payload fraction versus mission delta-v"));
            report.AppendLine("    dv (m/s)   Payload fraction   Payload per 100 t");
            report.AppendLine("  " + new string('-', 52));
            for (double dv = 5000.0; dv <= Math.Max(deltaV, maxDeltaV) + 1000.0; dv += 500.0)
            {
                double lambda = RocketEquation.PayloadFraction(dv, c, epsilon);
                report.AppendLine(lambda > 0.0
                    ? $"   {dv,9:F0}   {lambda,16:P3}   {100000.0 * lambda,15:F0} kg"
                    : $"   {dv,9:F0}   {"IMPOSSIBLE",16}   {"-",15}");
            }
            report.AppendLine();
            report.AppendLine("  Notice how the payload collapses long before dv_max is reached.");
            report.AppendLine("  A vehicle designed near the cliff edge has no mass growth margin,");
            report.AppendLine("  and mass growth during development is the norm, not the exception.");
            report.AppendLine();

            report.AppendLine(Heading("WHAT WOULD FIX IT?"));
            report.AppendLine(Line("dv_max with epsilon reduced to 0.05",
                RocketEquation.MaximumSingleStageDeltaV(c, 0.05), "m/s", "N2"));
            report.AppendLine(Line("dv_max with LOX/LH2 (Isp 450 s)",
                RocketEquation.MaximumSingleStageDeltaV(
                    Performance.ExhaustVelocityFromIsp(450.0), epsilon), "m/s", "N2"));
            report.AppendLine(Line("dv_max with both improvements",
                RocketEquation.MaximumSingleStageDeltaV(
                    Performance.ExhaustVelocityFromIsp(450.0), 0.05), "m/s", "N2"));

            return report.ToString();
        }
    };

    /// <summary>
    /// Stage-by-stage analysis of a real multi-stage vehicle.
    /// </summary>
    private static CodeSample MultiStageVehicleAnalysis() => new()
    {
        Id = "ch4-multistage",
        Chapter = "Chapter 4",
        Title = "Multi-Stage Vehicle Analysis",
        Summary = "Stage-by-stage delta-v bookkeeping for a three-stage launcher.",

        Theory =
            """
            Staging works because you stop carrying empty tanks. The key insight is the
            accounting rule for what each stage must accelerate:

                Stage 1 accelerates: stage 1 + stage 2 + stage 3 + payload
                Stage 2 accelerates: stage 2 + stage 3 + payload
                Stage 3 accelerates: stage 3 + payload

            Each stage sees a much better mass ratio than a single stage of the same total
            propellant would, because the discarded structure never has to be accelerated
            again. And the delta-v contributions simply ADD:

                dv_total = sum over stages of c_i * ln(m0_i / mf_i)

            They add because the rocket equation is a logarithm of a ratio, and logarithms of
            products add. That is a mathematically deep reason for a very practical result.

            Two design heuristics fall out of this:

            1. Put your HIGHEST Isp engine on the LAST stage. That stage's delta-v is
               multiplied against the smallest mass, so improving it is cheapest, and its
               burn is in vacuum where high-expansion nozzles work.

            2. Diminishing returns set in quickly. Going from one stage to two is transformative.
               Two to three is a solid gain. Three to four is marginal, and four is almost never
               worth the extra interstages, separation events and failure modes. Every staging
               event is a chance for the mission to fail.

            The output below shows, for each stage, the ignition mass, burnout mass, mass ratio
            and delta-v contribution, and then the totals.
            """,

        CodeSnippet =
            """
            // Stages are listed FIRST TO LAST. The library handles the accumulated
            // upper-stage-plus-payload mass that each stage must accelerate.
            List<Stage> stages =
            [
                new Stage("Stage 1", propellantMass: mp1, structuralMass: ms1, specificImpulse: isp1),
                new Stage("Stage 2", propellantMass: mp2, structuralMass: ms2, specificImpulse: isp2),
                new Stage("Stage 3", propellantMass: mp3, structuralMass: ms3, specificImpulse: isp3)
            ];

            VehicleResult result = Staging.AnalyseVehicle(stages, payloadMass);
            string report = result.ToString();
            """,

        Parameters =
        [
            new SampleParameter("mp1", "Stage 1 propellant", 130000.0, "kg", "Booster propellant."),
            new SampleParameter("ms1", "Stage 1 structure", 12000.0, "kg", "Tanks, engines, interstage."),
            new SampleParameter("isp1", "Stage 1 Isp", 300.0, "s", "Sea-level-biased, low area ratio."),
            new SampleParameter("mp2", "Stage 2 propellant", 30000.0, "kg", "Second stage propellant."),
            new SampleParameter("ms2", "Stage 2 structure", 3000.0, "kg", "Second stage dry mass."),
            new SampleParameter("isp2", "Stage 2 Isp", 340.0, "s", "Vacuum-optimised."),
            new SampleParameter("mp3", "Stage 3 propellant", 6000.0, "kg", "Upper stage propellant."),
            new SampleParameter("ms3", "Stage 3 structure", 800.0, "kg", "Upper stage dry mass."),
            new SampleParameter("isp3", "Stage 3 Isp", 450.0, "s", "Highest Isp goes on the LAST stage."),
            new SampleParameter("payload", "Payload mass", 4000.0, "kg", "The actual point of the vehicle.")
        ],

        Run = inputs =>
        {
            List<Stage> stages =
            [
                new Stage("Stage 1", inputs["mp1"], inputs["ms1"], inputs["isp1"]),
                new Stage("Stage 2", inputs["mp2"], inputs["ms2"], inputs["isp2"]),
                new Stage("Stage 3", inputs["mp3"], inputs["ms3"], inputs["isp3"])
            ];

            double payload = inputs["payload"];

            VehicleResult result = Staging.AnalyseVehicle(stages, payload);

            StringBuilder report = new();
            report.AppendLine(result.ToString());

            report.AppendLine();
            report.AppendLine(Heading("PER-STAGE STRUCTURAL COEFFICIENTS"));
            foreach (Stage stage in stages)
            {
                report.AppendLine(Line(
                    $"{stage.Name} epsilon", stage.StructuralCoefficient, "", "N4"));
            }
            report.AppendLine();

            report.AppendLine(Heading("COMPARISON AGAINST A SINGLE STAGE"));

            // Build a hypothetical single stage with the SAME total propellant, the SAME total
            // structure and the average exhaust velocity, to isolate the benefit of staging.
            double totalPropellant = stages.Sum(s => s.PropellantMass);
            double totalStructure = stages.Sum(s => s.StructuralMass);
            double averageIsp = stages.Sum(s => s.SpecificImpulse * s.PropellantMass) / totalPropellant;
            double averageC = Performance.ExhaustVelocityFromIsp(averageIsp);

            double singleStageM0 = totalPropellant + totalStructure + payload;
            double singleStageMf = totalStructure + payload;
            double singleStageDeltaV = RocketEquation.DeltaV(averageC, singleStageM0, singleStageMf);

            report.AppendLine(Line("Total propellant", totalPropellant, "kg", "N1"));
            report.AppendLine(Line("Total structure", totalStructure, "kg", "N1"));
            report.AppendLine(Line("Propellant-weighted average Isp", averageIsp, "s", "N2"));
            report.AppendLine(Line("Single-stage delta-v with same mass", singleStageDeltaV, "m/s", "N2"));
            report.AppendLine(Line("Staged delta-v", result.TotalDeltaV, "m/s", "N2"));
            report.AppendLine(Line("Delta-v gained by staging",
                result.TotalDeltaV - singleStageDeltaV, "m/s", "N2"));
            report.AppendLine(Line("Improvement",
                100.0 * (result.TotalDeltaV / singleStageDeltaV - 1.0), "%", "N2"));
            report.AppendLine();
            report.AppendLine("  That gain came purely from not carrying dead tankage. No engine");
            report.AppendLine("  was improved and no mass was removed from the vehicle.");
            report.AppendLine();

            report.AppendLine(Heading("MISSION ASSESSMENT"));
            const double leoRequirement = 9400.0;
            report.AppendLine(Line("Typical LEO requirement", leoRequirement, "m/s", "N0"));
            report.AppendLine(Line("Margin", result.TotalDeltaV - leoRequirement, "m/s", "N2"));
            report.AppendLine(result.TotalDeltaV >= leoRequirement
                ? "  This vehicle reaches low Earth orbit with the stated payload."
                : "  This vehicle does NOT reach low Earth orbit with the stated payload.");

            return report.ToString();
        }
    };

    /// <summary>
    /// How the idealised payload fraction varies with stage count.
    /// </summary>
    private static CodeSample OptimumStageCountSweep() => new()
    {
        Id = "ch4-optimum-stages",
        Chapter = "Chapter 4",
        Title = "Optimum Number of Stages",
        Summary = "Diminishing returns - and what the idealised model leaves out.",

        Theory =
            """
            Assume n identical stages, each with the same structural coefficient epsilon and
            the same exhaust velocity c, splitting a total mission delta-v evenly. Each stage
            then provides dv/n, and the overall payload fraction is the per-stage payload
            fraction raised to the n-th power:

                lambda_total = { [exp(-dv/(n*c)) - epsilon] / (1 - epsilon) } ^ n

            Sweeping n produces the classic diminishing-returns curve. The improvement from
            n = 1 to n = 2 is enormous - it is often the difference between "impossible" and
            "routine". From 2 to 3 is a real but much smaller gain. From 3 to 4 is small, and
            beyond that the curve is nearly flat.

            IMPORTANT MODEL LIMITATION, and this is worth understanding rather than glossing
            over: in this idealised formula the payload fraction is MONOTONICALLY INCREASING in
            n. It never turns over, so a naive optimiser will always recommend as many stages
            as you let it. The formula charges nothing for a stage beyond its propellant-scaled
            structure.

            Reality charges plenty for each additional stage:
              - a separation system, and a separation event that can fail;
              - an interstage structure that is pure dead mass;
              - another engine, another set of avionics, another propulsion feed system;
              - the fixed mass of small components that does NOT scale down with the stage;
              - additional integration, test and qualification cost.

            Once those fixed per-stage costs are included, the curve does turn over, and it
            turns over at two or three stages. That is exactly what flown vehicles use. The
            idealised model tells you WHY staging helps; the fixed costs tell you where to stop.
            """,

        CodeSnippet =
            """
            // Idealised payload fraction for n identical stages sharing the mission delta-v.
            for (int n = 1; n <= maximumStages; n++)
            {
                double lambda = Staging.IdealPayloadFraction(
                    totalDeltaV:              deltaV,
                    effectiveExhaustVelocity: c,
                    structuralCoefficient:    epsilon,
                    stageCount:               n);
            }

            // The library's optimiser - which, in this idealised model, always saturates.
            (int stageCount, double payloadFraction) =
                Staging.OptimumStageCount(deltaV, c, epsilon, maximumStages);
            """,

        Parameters =
        [
            new SampleParameter("isp", "Specific impulse (all stages)", 340.0, "s",
                "Identical stages is the simplifying assumption."),
            new SampleParameter("eps", "Structural coefficient epsilon", 0.08, "",
                "Same for every stage in this idealisation."),
            new SampleParameter("dv", "Total mission delta-v", 9400.0, "m/s",
                "Split evenly between the stages."),
            new SampleParameter("nmax", "Maximum stages to consider", 8.0, "",
                "Integer. The sweep runs from 1 to this value.")
        ],

        Run = inputs =>
        {
            double isp = inputs["isp"];
            double epsilon = inputs["eps"];
            double deltaV = inputs["dv"];
            int maxStages = Math.Clamp(inputs.Integer("nmax"), 1, 12);

            double c = Performance.ExhaustVelocityFromIsp(isp);

            StringBuilder report = new();

            report.AppendLine(Heading("INPUTS"));
            report.AppendLine(Line("Effective exhaust velocity c", c, "m/s", "N2"));
            report.AppendLine(Line("Structural coefficient", epsilon, "", "N4"));
            report.AppendLine(Line("Total mission delta-v", deltaV, "m/s", "N2"));
            report.AppendLine(Line("Single-stage ceiling dv_max",
                RocketEquation.MaximumSingleStageDeltaV(c, epsilon), "m/s", "N2"));
            report.AppendLine();

            report.AppendLine(Heading("SWEEP"));
            report.AppendLine("   n   dv per stage   Payload fraction   Gain over n-1   Payload/100 t");
            report.AppendLine("  " + new string('-', 74));

            double previous = 0.0;
            for (int n = 1; n <= maxStages; n++)
            {
                double lambda = Staging.IdealPayloadFraction(deltaV, c, epsilon, n);
                double gain = n == 1 ? double.NaN : lambda - previous;

                report.AppendLine(string.Format(
                    "  {0,2}   {1,12:F1}   {2,16}   {3,13}   {4,12}",
                    n,
                    deltaV / n,
                    lambda > 0.0 ? lambda.ToString("P4") : "IMPOSSIBLE",
                    double.IsNaN(gain) ? "-" : gain.ToString("P4"),
                    lambda > 0.0 ? (100000.0 * lambda).ToString("F0") + " kg" : "-"));

                previous = lambda;
            }
            report.AppendLine();

            (int optimumCount, double optimumFraction) =
                Staging.OptimumStageCount(deltaV, c, epsilon, maxStages);

            report.AppendLine(Heading("WHAT THE OPTIMISER SAYS"));
            report.AppendLine(Line("Recommended stage count", optimumCount.ToString()));
            report.AppendLine(Line("Payload fraction there", optimumFraction, "", "N6"));
            report.AppendLine();

            if (optimumCount == maxStages)
            {
                report.AppendLine("  Note that the optimiser has SATURATED at the limit you gave it.");
                report.AppendLine("  That is not a bug: in this idealised model the payload fraction");
                report.AppendLine("  rises monotonically with stage count because a stage costs nothing");
                report.AppendLine("  beyond its propellant-scaled structure. Look at the 'gain' column");
                report.AppendLine("  above instead - the marginal benefit collapses after 2 or 3 stages,");
                report.AppendLine("  and in a real vehicle the fixed cost of an interstage, a separation");
                report.AppendLine("  system and an extra engine overwhelms that remaining gain.");
            }

            report.AppendLine();
            report.AppendLine(Heading("MARGINAL GAIN, EXPRESSED AS PAYLOAD PER 100 t"));
            double lambdaPrev = Staging.IdealPayloadFraction(deltaV, c, epsilon, 1);
            for (int n = 2; n <= maxStages; n++)
            {
                double lambdaNow = Staging.IdealPayloadFraction(deltaV, c, epsilon, n);
                report.AppendLine(Line($"Adding stage {n}",
                    100000.0 * (lambdaNow - lambdaPrev), "kg", "N1"));
                lambdaPrev = lambdaNow;
            }
            report.AppendLine();
            report.AppendLine("  If an interstage plus separation system plus an extra engine weighs");
            report.AppendLine("  more than the figure on a given row, that stage is not worth adding.");

            return report.ToString();
        }
    };

    /// <summary>
    /// Numerically integrated vertical ascent with gravity and drag losses.
    /// </summary>
    private static CodeSample VerticalAscentSimulation() => new()
    {
        Id = "ch4-vertical-ascent",
        Chapter = "Chapter 4",
        Title = "Vertical Ascent with Gravity and Drag Losses",
        Summary = "Numerically integrating the real equation of motion, and finding max-Q.",

        Theory =
            """
            The rocket equation gives IDEAL delta-v: the velocity change a vehicle would
            achieve in field-free space with no atmosphere. Real launch vehicles lose a
            substantial part of that. The full one-dimensional equation of motion is:

                m * dv/dt = F(h) - m*g - D(h,v)

            with three loss mechanisms hidden inside:

            GRAVITY LOSS - the vehicle spends part of its thrust simply holding itself up
            against gravity. For a purely vertical flight the loss is g * tb, the burn time
            multiplied by g. A 160-second vertical burn throws away 1570 m/s. This is why
            launch vehicles pitch over almost immediately into a gravity turn: the loss goes as
            g * sin(gamma) * t, so flying horizontally reduces it toward zero.

            DRAG LOSS - D = 0.5 * rho * v^2 * Cd * A, integrated over the flight. It is usually
            the smallest of the three at 100-200 m/s for a well-shaped vehicle, because by the
            time the vehicle is fast, the air is already thin.

            PRESSURE / BACK-PRESSURE LOSS - the engine produces less thrust in the atmosphere
            than in vacuum, by exactly p3 * A2. This is already handled inside the thrust term.

            MAX-Q is the peak of dynamic pressure q = 0.5 * rho * v^2. Early in flight rho is
            high but v is low; later v is high but rho has collapsed exponentially. The product
            peaks somewhere in between, typically 30-90 seconds in and around 10-14 km altitude.
            Max-Q sizes the vehicle's structural loads and is the moment engines are commonly
            throttled back ("throttling down for max-Q").

            The simulation below integrates forward in time with a simple explicit Euler step,
            using an exponential atmosphere. That is crude compared with a real trajectory tool,
            but it reproduces the physics faithfully and, crucially, you can see every term.
            """,

        CodeSnippet =
            """
            // Forward Euler integration of m*dv/dt = F - m*g - D, with an exponential atmosphere.
            AscentResult ascent = FlightPerformance.VerticalAscent(
                initialMass:           m0,
                propellantMass:        mp,
                vacuumThrust:          vacuumThrust,
                specificImpulseSeconds: isp,
                exitArea:              exitArea,
                exitPressure:          exitPressure,
                dragCoefficient:       cd,
                referenceArea:         frontalArea,
                timeStep:              0.05);

            // The result carries the full loss accounting and the max-Q point.
            double ideal   = ascent.IdealDeltaV;
            double gravity = ascent.GravityLoss;
            double drag    = ascent.DragLoss;
            """,

        Parameters =
        [
            new SampleParameter("m0", "Initial mass", 50000.0, "kg", "Gross lift-off mass."),
            new SampleParameter("mp", "Propellant mass", 42000.0, "kg", "Usable propellant."),
            new SampleParameter("thrust", "Vacuum thrust", 900.0, "kN", "Needs T/W > 1 to leave the pad."),
            new SampleParameter("isp", "Vacuum specific impulse", 310.0, "s", "Engine performance."),
            new SampleParameter("aexit", "Nozzle exit area", 0.8, "m^2", "Sets the back-pressure loss."),
            new SampleParameter("pexit", "Nozzle exit pressure", 0.5, "bar", "From the area ratio."),
            new SampleParameter("cd", "Drag coefficient", 0.3, "", "0.2-0.5 for a slender launcher."),
            new SampleParameter("aref", "Frontal reference area", 7.0, "m^2", "Cross-sectional area."),
            new SampleParameter("dt", "Integration time step", 0.05, "s", "Smaller is more accurate and slower.")
        ],

        Run = inputs =>
        {
            double m0 = inputs["m0"];
            double mp = inputs["mp"];
            double vacuumThrust = inputs["thrust"] * 1000.0;
            double isp = inputs["isp"];
            double exitArea = inputs["aexit"];
            double exitPressure = Constants.BarToPa(inputs["pexit"]);
            double cd = inputs["cd"];
            double refArea = inputs["aref"];
            double dt = Math.Clamp(inputs["dt"], 0.01, 1.0);

            if (mp >= m0)
            {
                throw new ArgumentException("Propellant mass must be less than initial mass.");
            }

            // Sea-level thrust is the vacuum figure minus ambient pressure on the exit plane.
            double seaLevelThrust = vacuumThrust - Constants.SeaLevelPressure * exitArea;
            double liftoffTw = Performance.ThrustToWeightRatio(seaLevelThrust, m0);

            if (liftoffTw <= 1.0)
            {
                throw new ArgumentException(
                    $"Sea-level thrust-to-weight is {liftoffTw:F3}, which is not greater than 1. " +
                    "This vehicle cannot leave the pad. Increase the thrust or reduce the mass.");
            }

            AscentResult ascent = FlightPerformance.VerticalAscent(
                m0, mp, vacuumThrust, isp, exitArea, exitPressure, cd, refArea, dt);

            StringBuilder report = new();

            report.AppendLine(Heading("VEHICLE"));
            report.AppendLine(Line("Gross lift-off mass", m0, "kg", "N1"));
            report.AppendLine(Line("Propellant mass", mp, "kg", "N1"));
            report.AppendLine(Line("Vacuum thrust", vacuumThrust / 1000.0, "kN", "N2"));
            report.AppendLine(Line("Sea-level thrust", seaLevelThrust / 1000.0, "kN", "N2"));
            report.AppendLine(Line("Lift-off thrust-to-weight", liftoffTw, "", "N3"));
            report.AppendLine();

            report.AppendLine(Heading("TRAJECTORY"));
            report.AppendLine("   t(s)   Alt(km)   Vel(m/s)   Mass(kg)   Accel(g)     q(kPa)");
            report.AppendLine("  " + new string('-', 62));

            // Print about 15 evenly spaced samples rather than every integration step.
            int sampleCount = ascent.Samples.Count;
            int stride = Math.Max(1, sampleCount / 15);
            for (int i = 0; i < sampleCount; i += stride)
            {
                AscentSample s = ascent.Samples[i];
                report.AppendLine(string.Format(
                    "  {0,5:F1}   {1,7:F3}   {2,8:F1}   {3,8:F0}   {4,8:F2}   {5,8:F2}",
                    s.Time,
                    s.Altitude / 1000.0,
                    s.Velocity,
                    s.Mass,
                    s.Acceleration / Constants.StandardGravity,
                    s.DynamicPressure / 1000.0));
            }

            // Always show the final state, even if the stride skipped it.
            AscentSample last = ascent.Samples[^1];
            report.AppendLine(string.Format(
                "  {0,5:F1}   {1,7:F3}   {2,8:F1}   {3,8:F0}   {4,8:F2}   {5,8:F2}   <- burnout",
                last.Time, last.Altitude / 1000.0, last.Velocity, last.Mass,
                last.Acceleration / Constants.StandardGravity, last.DynamicPressure / 1000.0));
            report.AppendLine();

            report.AppendLine(Heading("BURNOUT STATE"));
            report.AppendLine(Line("Burnout time", ascent.BurnoutTime, "s", "N2"));
            report.AppendLine(Line("Burnout altitude", ascent.BurnoutAltitude / 1000.0, "km", "N3"));
            report.AppendLine(Line("Burnout velocity", ascent.BurnoutVelocity, "m/s", "N2"));
            report.AppendLine();

            report.AppendLine(Heading("LOSS ACCOUNTING"));
            report.AppendLine(Line("Ideal delta-v (rocket equation)", ascent.IdealDeltaV, "m/s", "N2"));
            report.AppendLine(Line("Gravity loss", ascent.GravityLoss, "m/s", "N2"));
            report.AppendLine(Line("Drag loss", ascent.DragLoss, "m/s", "N2"));
            report.AppendLine(Line("Back-pressure and residual",
                ascent.IdealDeltaV - ascent.GravityLoss - ascent.DragLoss - ascent.BurnoutVelocity,
                "m/s", "N2"));
            report.AppendLine(Line("Velocity actually achieved", ascent.BurnoutVelocity, "m/s", "N2"));
            report.AppendLine(Line("Efficiency",
                100.0 * ascent.BurnoutVelocity / ascent.IdealDeltaV, "%", "N2"));
            report.AppendLine();
            report.AppendLine(Line("Gravity loss as g * tb",
                Constants.StandardGravity * ascent.BurnoutTime, "m/s", "N2"));
            report.AppendLine("  For a purely vertical flight the gravity loss is exactly g*tb.");
            report.AppendLine("  A gravity turn cuts it by flying the thrust vector away from vertical:");
            foreach (double angle in new[] { 90.0, 60.0, 45.0, 30.0, 10.0 })
            {
                report.AppendLine(Line($"  Loss at flight path angle {angle,4:F0} deg",
                    FlightPerformance.GravityLoss(ascent.BurnoutTime, angle), "m/s", "N2"));
            }
            report.AppendLine();

            report.AppendLine(Heading("MAXIMUM DYNAMIC PRESSURE (MAX-Q)"));
            report.AppendLine(Line("Max-Q", ascent.MaxDynamicPressure / 1000.0, "kPa", "N3"));
            report.AppendLine(Line("Time of max-Q", ascent.MaxDynamicPressureTime, "s", "N2"));

            // Find the altitude and velocity at max-Q for context.
            AscentSample maxQSample = ascent.Samples
                .OrderByDescending(s => s.DynamicPressure)
                .First();
            report.AppendLine(Line("Altitude at max-Q", maxQSample.Altitude / 1000.0, "km", "N3"));
            report.AppendLine(Line("Velocity at max-Q", maxQSample.Velocity, "m/s", "N2"));
            report.AppendLine(Line("Air density at max-Q",
                FlightPerformance.DensityAtAltitude(maxQSample.Altitude), "kg/m^3", "N5"));
            report.AppendLine();
            report.AppendLine("  Max-Q sizes the airframe's structural loads and is when real");
            report.AppendLine("  vehicles throttle down. It occurs where the rising v^2 term and");
            report.AppendLine("  the exponentially falling density term cross over.");
            report.AppendLine();

            report.AppendLine(Heading("ORBITAL CONTEXT"));
            report.AppendLine(Line("Circular orbit speed at 200 km",
                FlightPerformance.CircularOrbitVelocity(200000.0), "m/s", "N2"));
            report.AppendLine(Line("Still needed after this burn",
                FlightPerformance.CircularOrbitVelocity(200000.0) - ascent.BurnoutVelocity,
                "m/s", "N2"));

            return report.ToString();
        },

        // =============================================================================
        // TRAJECTORY VISUALISATION
        // =============================================================================
        //
        // This sample integrates real equations of motion, so it has a genuine flight path
        // to show. Note carefully what the horizontal axis is: TIME, not distance. This is a
        // purely VERTICAL ascent - downrange distance is identically zero throughout - so
        // plotting altitude against downrange would give a single vertical line and tell the
        // viewer nothing. Altitude against time is the honest picture of this particular
        // trajectory, and the rocket glyph is held pointing straight up because that is
        // exactly where the vehicle is pointing.
        //
        // For a flight path through actual SPACE, see "Gravity Turn Ascent Trajectory" in the
        // same chapter, which integrates the two-dimensional problem.
        BuildTrajectory = inputs =>
        {
            double m0 = inputs["m0"];
            double mp = inputs["mp"];
            double vacuumThrust = inputs["thrust"] * 1000.0;
            double isp = inputs["isp"];
            double exitArea = inputs["aexit"];
            double exitPressure = Constants.BarToPa(inputs["pexit"]);
            double cd = inputs["cd"];
            double refArea = inputs["aref"];
            double dt = Math.Clamp(inputs["dt"], 0.01, 1.0);

            AscentResult ascent = FlightPerformance.VerticalAscent(
                m0, mp, vacuumThrust, isp, exitArea, exitPressure, cd, refArea, dt);

            List<TrajectoryPoint> points = new(ascent.Samples.Count);

            foreach (AscentSample s in ascent.Samples)
            {
                points.Add(new TrajectoryPoint(
                    Time: s.Time,
                    X: s.Time,
                    Y: s.Altitude / 1000.0,
                    Heading: 90.0,
                    Powered: true,
                    Channels:
                    [
                        s.Altitude / 1000.0,
                        s.Velocity,
                        s.Mass,
                        s.Acceleration / Constants.StandardGravity,
                        s.DynamicPressure / 1000.0
                    ]));
            }

            // Locate max-Q by index so the marker sits exactly on a plotted point.
            int maxQIndex = 0;
            for (int i = 0; i < ascent.Samples.Count; i++)
            {
                if (ascent.Samples[i].DynamicPressure > ascent.Samples[maxQIndex].DynamicPressure)
                {
                    maxQIndex = i;
                }
            }

            return new Trajectory
            {
                Title = "Vertical Ascent Profile",
                Subtitle =
                    $"{m0:N0} kg vehicle, {vacuumThrust / 1000.0:N0} kN vacuum thrust, " +
                    $"Isp {isp:N0} s - flown straight up",
                XAxisLabel = "Time since lift-off (s)",
                YAxisLabel = "Altitude (km)",
                Points = points,
                EqualAspect = false,
                PlaybackSeconds = 11.0,
                Channels =
                [
                    new TrajectoryChannel("Altitude", "km", "F2"),
                    new TrajectoryChannel("Speed", "m/s", "F1"),
                    new TrajectoryChannel("Mass", "kg", "F0"),
                    new TrajectoryChannel("Accel", "g", "F2"),
                    new TrajectoryChannel("Dyn press", "kPa", "F2")
                ],
                Markers =
                [
                    new TrajectoryMarker("Lift-off", 0, "#7BD88F"),
                    new TrajectoryMarker("Max-Q", maxQIndex, "#FFB347"),
                    new TrajectoryMarker("Burnout", points.Count - 1, "#FF5C5C")
                ],
                Notes =
                    "Horizontal axis is TIME, not distance: this is a purely vertical flight, so " +
                    "downrange distance is zero at every instant. The engine burns for the whole " +
                    "of the plotted interval. Model: point mass, explicit Euler integration, " +
                    "exponential atmosphere, constant drag coefficient, no Earth rotation and no " +
                    "steering. See 'Gravity Turn Ascent Trajectory' for a path through real space."
            };
        }
    };
}
