namespace Propulsion.Core;

/// <summary>
/// Real-nozzle correction factors and off-design behaviour: the gap between ideal theory and
/// the number a test stand actually reports. Sutton Chapter 3, "Real Nozzles".
/// </summary>
public static class NozzleLosses
{
    /// <summary>
    /// Combined nozzle efficiency from its individual contributions.
    /// </summary>
    /// <param name="divergenceFactor">
    /// Divergence factor lambda from <see cref="NozzleGeometry.ConicalDivergenceFactor"/>,
    /// typically 0.98-0.99.
    /// </param>
    /// <param name="frictionFactor">
    /// Boundary-layer friction factor, typically 0.97-0.99. The wall drags on the gas and a
    /// subsonic boundary layer contributes no useful thrust.
    /// </param>
    /// <param name="twoPhaseFactor">
    /// Two-phase-flow factor. Exactly 1.0 for an all-gas exhaust. For a metallised solid
    /// propellant, condensed alumina particles lag the gas in both velocity and temperature and
    /// this drops to 0.94-0.98.
    /// </param>
    /// <param name="chemicalFactor">
    /// Kinetic (finite-rate chemistry) factor, typically 0.98-1.0. Real recombination in the
    /// nozzle releases energy that frozen-flow theory ignores, so this can slightly exceed the
    /// frozen prediction.
    /// </param>
    /// <returns>The combined nozzle efficiency, a value near but below 1.</returns>
    /// <remarks>
    /// The factors are treated as independent and simply multiplied. That is not rigorous - the
    /// mechanisms interact - but it is the standard first-order engineering practice and it is
    /// how preliminary design is done everywhere.
    /// </remarks>
    public static double CombinedNozzleEfficiency(
        double divergenceFactor,
        double frictionFactor,
        double twoPhaseFactor = 1.0,
        double chemicalFactor = 1.0)
    {
        Guard.InRange(divergenceFactor, 0.5, 1.0, nameof(divergenceFactor));
        Guard.InRange(frictionFactor, 0.5, 1.0, nameof(frictionFactor));
        Guard.InRange(twoPhaseFactor, 0.5, 1.0, nameof(twoPhaseFactor));
        Guard.InRange(chemicalFactor, 0.5, 1.05, nameof(chemicalFactor));

        return divergenceFactor * frictionFactor * twoPhaseFactor * chemicalFactor;
    }

    /// <summary>
    /// C-star efficiency: measured characteristic velocity divided by theoretical.
    /// </summary>
    /// <param name="measuredCharacteristicVelocity">Measured c*, m/s.</param>
    /// <param name="theoreticalCharacteristicVelocity">Theoretical c*, m/s.</param>
    /// <returns>The dimensionless efficiency, normally 0.92-0.99.</returns>
    /// <remarks>
    /// Because c* depends only on chamber conditions, this number is a direct report card on
    /// the injector: how completely and how uniformly it mixed the propellants. A poor value
    /// almost always means unmixed streaks or incomplete combustion, and the fix is in the
    /// injector, not the nozzle.
    /// </remarks>
    public static double CStarEfficiency(
        double measuredCharacteristicVelocity,
        double theoreticalCharacteristicVelocity)
    {
        Guard.Positive(measuredCharacteristicVelocity, nameof(measuredCharacteristicVelocity));
        Guard.Positive(theoreticalCharacteristicVelocity, nameof(theoreticalCharacteristicVelocity));

        return measuredCharacteristicVelocity / theoreticalCharacteristicVelocity;
    }

    /// <summary>
    /// Classifies a nozzle operating point as under-, over- or optimally expanded.
    /// </summary>
    /// <param name="exitPressure">Exit static pressure p2, Pa.</param>
    /// <param name="ambientPressure">Ambient pressure p3, Pa.</param>
    /// <param name="tolerance">
    /// Relative tolerance within which the two pressures count as matched. The default of 2%
    /// reflects the fact that thrust is stationary at the optimum, so small mismatches cost
    /// almost nothing.
    /// </param>
    /// <returns>The expansion state of the nozzle.</returns>
    public static ExpansionState ClassifyExpansion(
        double exitPressure,
        double ambientPressure,
        double tolerance = 0.02)
    {
        Guard.NonNegative(exitPressure, nameof(exitPressure));
        Guard.NonNegative(ambientPressure, nameof(ambientPressure));

        if (ambientPressure <= 0.0)
        {
            // In vacuum there is nothing to match, so any finite exit pressure is
            // under-expanded by definition.
            return ExpansionState.UnderExpanded;
        }

        double relativeDifference = (exitPressure - ambientPressure) / ambientPressure;

        if (Math.Abs(relativeDifference) <= tolerance)
        {
            return ExpansionState.Optimum;
        }

        return relativeDifference > 0.0
            ? ExpansionState.UnderExpanded
            : ExpansionState.OverExpanded;
    }

    /// <summary>
    /// Checks a nozzle against the Summerfield flow-separation criterion.
    /// </summary>
    /// <param name="exitPressure">Exit static pressure p2, Pa.</param>
    /// <param name="ambientPressure">Ambient pressure p3, Pa.</param>
    /// <returns>
    /// True when the exit pressure has fallen below roughly 0.4 times ambient, at which point
    /// the flow is expected to separate from the nozzle wall.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Summerfield's empirical rule says a supersonic jet cannot sustain more than about a
    /// 2.5:1 over-expansion before the boundary layer separates from the wall, after which
    /// ambient air floods back up the inside of the bell.
    /// </para>
    /// <para>
    /// This is not a curiosity - it is a destructive failure mode. The separation line is
    /// unstable and moves asymmetrically, generating enormous side loads that can tear a nozzle
    /// off its mounts. It is why an upper-stage engine with a huge area ratio cannot simply be
    /// test-fired at sea level without an altitude chamber, and why the Shuttle's SSMEs were
    /// deliberately given a much smaller area ratio than vacuum optimisation alone would
    /// suggest.
    /// </para>
    /// </remarks>
    public static bool IsFlowSeparationLikely(double exitPressure, double ambientPressure)
    {
        Guard.NonNegative(exitPressure, nameof(exitPressure));
        Guard.NonNegative(ambientPressure, nameof(ambientPressure));

        if (ambientPressure <= 0.0)
        {
            return false;
        }

        return exitPressure < 0.4 * ambientPressure;
    }

    /// <summary>
    /// Ambient pressure of a simple exponential atmosphere.
    /// </summary>
    /// <param name="altitudeMetres">Altitude above sea level, m.</param>
    /// <returns>Ambient pressure in Pa.</returns>
    /// <remarks>
    /// p = p0 * exp(-h / H) with H = <see cref="Constants.AtmosphericScaleHeight"/>. Crude above
    /// about 20 km, but entirely adequate for showing how the pressure-thrust term evolves
    /// during an ascent: pressure has dropped by a factor of e every 7.2 km, so by 50 km it is
    /// already under 0.1% of sea level and the engine is effectively in vacuum.
    /// </remarks>
    public static double AmbientPressureAtAltitude(double altitudeMetres)
    {
        Guard.NonNegative(altitudeMetres, nameof(altitudeMetres));

        return Constants.SeaLevelPressure
               * Math.Exp(-altitudeMetres / Constants.AtmosphericScaleHeight);
    }
}

/// <summary>
/// How a nozzle's exit pressure compares with the ambient pressure it is exhausting into.
/// </summary>
public enum ExpansionState
{
    /// <summary>
    /// p2 &gt; p3. The gas is still above ambient at the exit plane and continues to expand
    /// outside the nozzle. Thrust is lost because that remaining pressure energy is never
    /// converted to axial momentum, but the condition is entirely safe. Every first stage runs
    /// this way at liftoff.
    /// </summary>
    UnderExpanded,

    /// <summary>
    /// p2 is within tolerance of p3. Maximum thrust for the given chamber pressure and throat
    /// area. A fixed nozzle passes through this condition at exactly one altitude.
    /// </summary>
    Optimum,

    /// <summary>
    /// p2 &lt; p3. Ambient pressure pushes back on the exit cone, subtracting thrust. Mild
    /// over-expansion merely costs performance; severe over-expansion causes flow separation
    /// and potentially destructive side loads. See
    /// <see cref="NozzleLosses.IsFlowSeparationLikely"/>.
    /// </summary>
    OverExpanded
}
