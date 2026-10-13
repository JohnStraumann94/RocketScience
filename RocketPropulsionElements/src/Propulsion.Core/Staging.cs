namespace Propulsion.Core;

/// <summary>
/// One stage of a multi-stage vehicle.
/// </summary>
/// <param name="Name">A label for reporting, e.g. "Stage 1".</param>
/// <param name="PropellantMass">Usable propellant mass, kg.</param>
/// <param name="StructuralMass">
/// Inert mass of this stage: tanks, engines, plumbing, interstage - everything that is jettisoned
/// with the stage. Excludes the payload and all stages above.
/// </param>
/// <param name="SpecificImpulse">Effective specific impulse over the stage's burn, s.</param>
public readonly record struct Stage(
    string Name,
    double PropellantMass,
    double StructuralMass,
    double SpecificImpulse)
{
    /// <summary>Total mass of the stage itself, propellant plus structure, kg.</summary>
    public double TotalMass => PropellantMass + StructuralMass;

    /// <summary>
    /// Structural coefficient epsilon = m_structure / (m_structure + m_propellant).
    /// </summary>
    /// <remarks>
    /// Note carefully that this excludes the payload. It measures how efficiently the stage
    /// itself is built, independent of what it is carrying, which is what makes it comparable
    /// between vehicles. Good values are 0.05-0.12 for a liquid stage, and worse for a solid,
    /// whose thick pressure-vessel case is unavoidably heavy.
    /// </remarks>
    public double StructuralCoefficient => StructuralMass / TotalMass;

    /// <summary>Effective exhaust velocity of this stage, m/s.</summary>
    public double EffectiveExhaustVelocity =>
        SpecificImpulse * Constants.StandardGravity;
}

/// <summary>
/// The computed performance of a single stage in the context of the whole stack.
/// </summary>
/// <param name="Stage">The stage this result describes.</param>
/// <param name="IgnitionMass">Total vehicle mass when this stage ignites, kg.</param>
/// <param name="BurnoutMass">Total vehicle mass at this stage's burnout, kg.</param>
/// <param name="MassRatio">Ignition mass divided by burnout mass.</param>
/// <param name="DeltaV">Ideal velocity change contributed by this stage, m/s.</param>
public readonly record struct StageResult(
    Stage Stage,
    double IgnitionMass,
    double BurnoutMass,
    double MassRatio,
    double DeltaV);

/// <summary>
/// The performance of a complete multi-stage vehicle.
/// </summary>
/// <param name="Stages">Per-stage results, in firing order.</param>
/// <param name="PayloadMass">Payload mass carried to the final burnout, kg.</param>
/// <param name="GrossLiftoffMass">Total mass on the pad, kg.</param>
/// <param name="TotalDeltaV">Sum of all stage contributions, m/s.</param>
/// <param name="PayloadFraction">Payload divided by gross liftoff mass.</param>
public readonly record struct VehicleResult(
    IReadOnlyList<StageResult> Stages,
    double PayloadMass,
    double GrossLiftoffMass,
    double TotalDeltaV,
    double PayloadFraction)
{
    /// <summary>Produces a readable per-stage report.</summary>
    /// <returns>A formatted, aligned summary suitable for direct display.</returns>
    public override string ToString()
    {
        System.Text.StringBuilder report = new();

        report.AppendLine($"Gross liftoff mass      = {GrossLiftoffMass,12:F1} kg");
        report.AppendLine($"Payload                 = {PayloadMass,12:F1} kg");
        report.AppendLine($"Payload fraction        = {PayloadFraction * 100.0,12:F3} %");
        report.AppendLine($"Total ideal delta-v     = {TotalDeltaV,12:F1} m/s");
        report.AppendLine();

        foreach (StageResult stage in Stages)
        {
            report.AppendLine($"--- {stage.Stage.Name} ---");
            report.AppendLine($"  Isp                   = {stage.Stage.SpecificImpulse,10:F1} s");
            report.AppendLine($"  Propellant            = {stage.Stage.PropellantMass,10:F1} kg");
            report.AppendLine($"  Structure             = {stage.Stage.StructuralMass,10:F1} kg");
            report.AppendLine($"  Structural coeff.     = {stage.Stage.StructuralCoefficient,10:F4}");
            report.AppendLine($"  Ignition mass         = {stage.IgnitionMass,10:F1} kg");
            report.AppendLine($"  Burnout mass          = {stage.BurnoutMass,10:F1} kg");
            report.AppendLine($"  Mass ratio            = {stage.MassRatio,10:F4}");
            report.AppendLine($"  Delta-v               = {stage.DeltaV,10:F1} m/s");
            report.AppendLine();
        }

        return report.ToString();
    }
}

/// <summary>
/// Multi-stage vehicle analysis and optimal staging. Sutton Chapter 4.
/// </summary>
/// <remarks>
/// <para>
/// Staging exists because of one inequality. A single stage can deliver at most
/// -c*ln(epsilon) of velocity change even with zero payload, and for realistic hardware that
/// falls short of orbit. Throwing away spent structure resets the burden: each subsequent stage
/// only has to accelerate what is left.
/// </para>
/// <para>
/// The delta-v contributions of the stages simply add, but the mass ratios multiply, and that
/// asymmetry is exactly what makes staging so effective.
/// </para>
/// </remarks>
public static class Staging
{
    /// <summary>
    /// Analyses a complete multi-stage vehicle.
    /// </summary>
    /// <param name="stages">Stages in firing order: first to burn is first in the list.</param>
    /// <param name="payloadMass">Payload mass, kg.</param>
    /// <returns>The full vehicle performance breakdown.</returns>
    /// <remarks>
    /// <para>
    /// The bookkeeping runs backwards up the stack. A given stage must lift itself, the payload,
    /// and every stage above it that has not yet been discarded. So the ignition mass for stage
    /// <c>i</c> is the payload plus the total mass of stages <c>i</c> through <c>n</c>.
    /// </para>
    /// <para>
    /// At burnout, the stage's propellant is gone but its structure is still attached - the
    /// structure is only jettisoned at separation, which happens after burnout. Getting this
    /// one detail wrong is the most common error in staging calculations and it flatters the
    /// answer, because it credits the stage with a better mass ratio than it earned.
    /// </para>
    /// </remarks>
    public static VehicleResult AnalyseVehicle(IReadOnlyList<Stage> stages, double payloadMass)
    {
        ArgumentNullException.ThrowIfNull(stages);
        Guard.Positive(payloadMass, nameof(payloadMass));

        if (stages.Count == 0)
        {
            throw new ArgumentException("At least one stage is required.", nameof(stages));
        }

        List<StageResult> results = new(stages.Count);

        // Mass carried above the stage currently being considered. Starts as just the payload
        // and grows as we walk downwards through the stack.
        double massAbove = payloadMass;

        // Walk from the top stage downwards so that "mass above" is known before it is needed.
        List<double> ignitionMasses = new(new double[stages.Count]);

        for (int i = stages.Count - 1; i >= 0; i--)
        {
            ignitionMasses[i] = massAbove + stages[i].TotalMass;
            massAbove = ignitionMasses[i];
        }

        double totalDeltaV = 0.0;

        for (int i = 0; i < stages.Count; i++)
        {
            Stage stage = stages[i];

            double ignitionMass = ignitionMasses[i];

            // Structure is still attached at burnout - only the propellant has gone.
            double burnoutMass = ignitionMass - stage.PropellantMass;

            double massRatio = ignitionMass / burnoutMass;
            double deltaV = stage.EffectiveExhaustVelocity * Math.Log(massRatio);

            totalDeltaV += deltaV;

            results.Add(new StageResult(stage, ignitionMass, burnoutMass, massRatio, deltaV));
        }

        double grossLiftoffMass = ignitionMasses[0];

        return new VehicleResult(
            Stages: results,
            PayloadMass: payloadMass,
            GrossLiftoffMass: grossLiftoffMass,
            TotalDeltaV: totalDeltaV,
            PayloadFraction: payloadMass / grossLiftoffMass);
    }

    /// <summary>
    /// Overall payload fraction of an ideal n-stage vehicle in which every stage is identical
    /// in exhaust velocity and structural coefficient.
    /// </summary>
    /// <param name="totalDeltaV">Total required velocity change, m/s.</param>
    /// <param name="effectiveExhaustVelocity">Effective exhaust velocity of every stage, m/s.</param>
    /// <param name="structuralCoefficient">Structural coefficient of every stage.</param>
    /// <param name="stageCount">Number of stages.</param>
    /// <returns>Payload mass as a fraction of gross liftoff mass.</returns>
    /// <remarks>
    /// <para>
    /// For identical stages the optimum is to split the delta-v equally between them, so each
    /// stage contributes dv/n and the overall payload fraction is the per-stage fraction raised
    /// to the n-th power:
    /// </para>
    /// <code>
    ///     lambda_total = ((e^(-dv/(n*c)) - eps) / (1 - eps)) ^ n
    /// </code>
    /// <para>
    /// Sweeping n shows exactly why real vehicles have two or three stages and never seven. The
    /// gain from one stage to two is enormous, from two to three worthwhile, from three to four
    /// marginal, and beyond that the extra separation events, interstages and engines cost more
    /// than the mathematics returns. The equal-split assumption is also only exactly optimal
    /// when the stages truly are identical; when exhaust velocities differ, the higher-Isp stage
    /// should take a larger share.
    /// </para>
    /// </remarks>
    public static double IdealPayloadFraction(
        double totalDeltaV,
        double effectiveExhaustVelocity,
        double structuralCoefficient,
        int stageCount)
    {
        Guard.NonNegative(totalDeltaV, nameof(totalDeltaV));
        Guard.Positive(effectiveExhaustVelocity, nameof(effectiveExhaustVelocity));
        Guard.InRange(structuralCoefficient, 0.0, 0.999, nameof(structuralCoefficient));

        if (stageCount < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stageCount), stageCount, "A vehicle needs at least one stage.");
        }

        double perStageDeltaV = totalDeltaV / stageCount;

        double perStageFraction = RocketEquation.PayloadFraction(
            perStageDeltaV, effectiveExhaustVelocity, structuralCoefficient);

        // A stage that cannot carry any payload makes the whole vehicle impossible, and
        // Math.Pow(0, n) would report exactly that, but returning early is clearer.
        if (perStageFraction <= 0.0)
        {
            return 0.0;
        }

        return Math.Pow(perStageFraction, stageCount);
    }

    /// <summary>
    /// Finds the stage count that maximises payload fraction for identical stages.
    /// </summary>
    /// <param name="totalDeltaV">Total required velocity change, m/s.</param>
    /// <param name="effectiveExhaustVelocity">Effective exhaust velocity, m/s.</param>
    /// <param name="structuralCoefficient">Structural coefficient.</param>
    /// <param name="maximumStages">Highest stage count to consider.</param>
    /// <returns>
    /// The stage count giving the largest payload fraction, and that fraction.
    /// </returns>
    /// <remarks>
    /// A brute-force sweep, because the search space is a handful of integers and an exhaustive
    /// scan is both faster to run and impossible to get wrong. Note that this ignores the real
    /// cost of staging - each separation is an additional single-point failure and adds
    /// hardware the idealised model does not charge for - so the mathematical optimum usually
    /// sits one stage above what anyone actually builds.
    /// </remarks>
    public static (int StageCount, double PayloadFraction) OptimumStageCount(
        double totalDeltaV,
        double effectiveExhaustVelocity,
        double structuralCoefficient,
        int maximumStages = 6)
    {
        if (maximumStages < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumStages), maximumStages, "Must consider at least one stage.");
        }

        int bestCount = 1;
        double bestFraction = -1.0;

        for (int count = 1; count <= maximumStages; count++)
        {
            double fraction = IdealPayloadFraction(
                totalDeltaV, effectiveExhaustVelocity, structuralCoefficient, count);

            if (fraction > bestFraction)
            {
                bestFraction = fraction;
                bestCount = count;
            }
        }

        return (bestCount, bestFraction);
    }
}
