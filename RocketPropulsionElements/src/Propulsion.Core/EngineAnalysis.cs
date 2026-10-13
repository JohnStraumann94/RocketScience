namespace Propulsion.Core;

/// <summary>
/// A complete, self-consistent snapshot of an engine operating at one particular altitude.
/// </summary>
/// <param name="ChamberPressure">Chamber pressure p1, Pa.</param>
/// <param name="ChamberTemperature">Chamber temperature T1, K.</param>
/// <param name="Gamma">Ratio of specific heats used for the expansion.</param>
/// <param name="MolecularMass">Mean exhaust molecular mass, kg/kmol.</param>
/// <param name="AreaRatio">Expansion area ratio A_2 / A_t.</param>
/// <param name="ThroatArea">Throat area A_t, m^2.</param>
/// <param name="ExitArea">Exit area A_2, m^2.</param>
/// <param name="ThroatDiameter">Throat diameter, m.</param>
/// <param name="ExitDiameter">Exit diameter, m.</param>
/// <param name="AmbientPressure">Ambient pressure p3 for this operating point, Pa.</param>
/// <param name="ExitPressure">Nozzle exit static pressure p2, Pa.</param>
/// <param name="ExitMach">Exit Mach number.</param>
/// <param name="CharacteristicVelocity">Characteristic velocity c*, m/s.</param>
/// <param name="ThrustCoefficient">Thrust coefficient C_F at this altitude.</param>
/// <param name="MassFlowRate">Propellant mass flow rate, kg/s.</param>
/// <param name="Thrust">Thrust, N.</param>
/// <param name="EffectiveExhaustVelocity">Effective exhaust velocity c = F/mdot, m/s.</param>
/// <param name="SpecificImpulse">Specific impulse, s.</param>
/// <param name="Expansion">Whether the nozzle is under-, over- or optimally expanded here.</param>
/// <param name="FlowSeparationLikely">True if the Summerfield criterion predicts separation.</param>
public readonly record struct EngineOperatingPoint(
    double ChamberPressure,
    double ChamberTemperature,
    double Gamma,
    double MolecularMass,
    double AreaRatio,
    double ThroatArea,
    double ExitArea,
    double ThroatDiameter,
    double ExitDiameter,
    double AmbientPressure,
    double ExitPressure,
    double ExitMach,
    double CharacteristicVelocity,
    double ThrustCoefficient,
    double MassFlowRate,
    double Thrust,
    double EffectiveExhaustVelocity,
    double SpecificImpulse,
    ExpansionState Expansion,
    bool FlowSeparationLikely)
{
    /// <summary>Produces a readable multi-line report of the operating point.</summary>
    /// <returns>A formatted, aligned summary suitable for direct display.</returns>
    public override string ToString()
    {
        System.Text.StringBuilder report = new();

        report.AppendLine($"Chamber pressure p1     = {Constants.PaToBar(ChamberPressure),12:F3} bar");
        report.AppendLine($"Chamber temperature T1  = {ChamberTemperature,12:F1} K");
        report.AppendLine($"Gamma                   = {Gamma,12:F4}");
        report.AppendLine($"Molecular mass M        = {MolecularMass,12:F3} kg/kmol");
        report.AppendLine();
        report.AppendLine($"Throat area A_t         = {ThroatArea * 1e4,12:F3} cm^2");
        report.AppendLine($"Throat diameter         = {ThroatDiameter * 1000.0,12:F2} mm");
        report.AppendLine($"Area ratio A_2/A_t      = {AreaRatio,12:F3}");
        report.AppendLine($"Exit area A_2           = {ExitArea * 1e4,12:F3} cm^2");
        report.AppendLine($"Exit diameter           = {ExitDiameter * 1000.0,12:F2} mm");
        report.AppendLine();
        report.AppendLine($"Ambient pressure p3     = {Constants.PaToBar(AmbientPressure),12:F5} bar");
        report.AppendLine($"Exit pressure p2        = {Constants.PaToBar(ExitPressure),12:F5} bar");
        report.AppendLine($"Exit Mach number        = {ExitMach,12:F3}");
        report.AppendLine($"Expansion state         = {Expansion,12}");

        if (FlowSeparationLikely)
        {
            report.AppendLine("  WARNING: p2 < 0.4 * p3 - flow separation and side loads are likely.");
        }

        report.AppendLine();
        report.AppendLine($"Characteristic vel. c*  = {CharacteristicVelocity,12:F1} m/s");
        report.AppendLine($"Thrust coefficient C_F  = {ThrustCoefficient,12:F4}");
        report.AppendLine($"Mass flow rate          = {MassFlowRate,12:F3} kg/s");
        report.AppendLine($"Thrust                  = {Thrust / 1000.0,12:F3} kN");
        report.AppendLine($"Effective exh. vel. c   = {EffectiveExhaustVelocity,12:F1} m/s");
        report.AppendLine($"Specific impulse Isp    = {SpecificImpulse,12:F2} s");

        return report.ToString();
    }
}

/// <summary>
/// Ties the chamber, the nozzle and the atmosphere together into a single complete engine
/// analysis. This is the calculation an engine designer actually performs.
/// </summary>
/// <remarks>
/// The pieces already exist in <see cref="NozzleTheory"/>, <see cref="NozzleGeometry"/> and
/// <see cref="NozzleLosses"/>; what this class adds is the correct order of operations and the
/// bookkeeping. Getting the order right matters, because several quantities are only defined
/// once earlier ones are known - the exit pressure follows from the area ratio, the thrust
/// coefficient follows from the exit pressure, and the thrust follows from the coefficient.
/// </remarks>
public static class EngineAnalysis
{
    /// <summary>
    /// Analyses an engine of known geometry at a specified ambient pressure.
    /// </summary>
    /// <param name="gamma">Ratio of specific heats.</param>
    /// <param name="chamberTemperature">Chamber temperature T1, K.</param>
    /// <param name="molecularMass">Mean exhaust molecular mass, kg/kmol.</param>
    /// <param name="chamberPressure">Chamber pressure p1, Pa.</param>
    /// <param name="areaRatio">Expansion area ratio A_2 / A_t.</param>
    /// <param name="throatArea">Throat area A_t, m^2.</param>
    /// <param name="ambientPressure">Ambient pressure p3, Pa. Use 0 for vacuum.</param>
    /// <returns>A fully populated <see cref="EngineOperatingPoint"/>.</returns>
    public static EngineOperatingPoint Analyse(
        double gamma,
        double chamberTemperature,
        double molecularMass,
        double chamberPressure,
        double areaRatio,
        double throatArea,
        double ambientPressure)
    {
        Guard.Gamma(gamma);
        Guard.Positive(chamberTemperature, nameof(chamberTemperature));
        Guard.Positive(molecularMass, nameof(molecularMass));
        Guard.Positive(chamberPressure, nameof(chamberPressure));
        Guard.Positive(throatArea, nameof(throatArea));
        Guard.NonNegative(ambientPressure, nameof(ambientPressure));

        if (areaRatio <= 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(areaRatio), areaRatio, "The expansion area ratio must exceed 1.");
        }

        // 1. Geometry is fixed, so the exit pressure follows from the area ratio alone. Note
        //    that it does NOT depend on ambient pressure - a choked nozzle has no idea what is
        //    outside it.
        double exitPressure = NozzleTheory.ExitPressureFromAreaRatio(gamma, chamberPressure, areaRatio);
        double exitMach = NozzleTheory.ExitMachNumber(gamma, chamberPressure, exitPressure);

        // 2. Chamber figure of merit. Independent of the nozzle entirely.
        double characteristicVelocity =
            NozzleTheory.CharacteristicVelocity(gamma, chamberTemperature, molecularMass);

        // 3. Nozzle figure of merit. This IS altitude dependent, through the pressure term.
        double thrustCoefficient = NozzleTheory.ThrustCoefficient(
            gamma, chamberPressure, exitPressure, ambientPressure, areaRatio);

        // 4. Choked mass flow. Set by the throat, so also altitude independent.
        double massFlowRate = chamberPressure * throatArea / characteristicVelocity;

        // 5. The two figures of merit multiply to give thrust: F = C_F * p1 * A_t.
        double thrust = thrustCoefficient * chamberPressure * throatArea;

        double effectiveExhaustVelocity = thrust / massFlowRate;
        double specificImpulse = effectiveExhaustVelocity / Constants.StandardGravity;

        double exitArea = areaRatio * throatArea;

        return new EngineOperatingPoint(
            ChamberPressure: chamberPressure,
            ChamberTemperature: chamberTemperature,
            Gamma: gamma,
            MolecularMass: molecularMass,
            AreaRatio: areaRatio,
            ThroatArea: throatArea,
            ExitArea: exitArea,
            ThroatDiameter: NozzleGeometry.DiameterFromArea(throatArea),
            ExitDiameter: NozzleGeometry.DiameterFromArea(exitArea),
            AmbientPressure: ambientPressure,
            ExitPressure: exitPressure,
            ExitMach: exitMach,
            CharacteristicVelocity: characteristicVelocity,
            ThrustCoefficient: thrustCoefficient,
            MassFlowRate: massFlowRate,
            Thrust: thrust,
            EffectiveExhaustVelocity: effectiveExhaustVelocity,
            SpecificImpulse: specificImpulse,
            Expansion: NozzleLosses.ClassifyExpansion(exitPressure, ambientPressure),
            FlowSeparationLikely: NozzleLosses.IsFlowSeparationLikely(exitPressure, ambientPressure));
    }

    /// <summary>
    /// Sizes an engine's throat to hit a required thrust at a specified ambient pressure, then
    /// returns the resulting operating point.
    /// </summary>
    /// <param name="gamma">Ratio of specific heats.</param>
    /// <param name="chamberTemperature">Chamber temperature T1, K.</param>
    /// <param name="molecularMass">Mean exhaust molecular mass, kg/kmol.</param>
    /// <param name="chamberPressure">Chamber pressure p1, Pa.</param>
    /// <param name="areaRatio">Chosen expansion area ratio A_2 / A_t.</param>
    /// <param name="requiredThrust">Thrust the engine must produce, N.</param>
    /// <param name="ambientPressure">
    /// The ambient pressure at which the thrust requirement applies, Pa.
    /// </param>
    /// <returns>The operating point of the sized engine.</returns>
    /// <remarks>
    /// The design direction rather than the analysis direction. Because C_F does not depend on
    /// the throat area, it can be evaluated first and the throat solved for directly - no
    /// iteration is needed, which is a pleasant consequence of the way the theory factorises.
    /// </remarks>
    public static EngineOperatingPoint DesignForThrust(
        double gamma,
        double chamberTemperature,
        double molecularMass,
        double chamberPressure,
        double areaRatio,
        double requiredThrust,
        double ambientPressure)
    {
        Guard.Positive(requiredThrust, nameof(requiredThrust));

        double exitPressure =
            NozzleTheory.ExitPressureFromAreaRatio(gamma, chamberPressure, areaRatio);

        double thrustCoefficient = NozzleTheory.ThrustCoefficient(
            gamma, chamberPressure, exitPressure, ambientPressure, areaRatio);

        double throatArea = NozzleGeometry.RequiredThroatArea(
            requiredThrust, thrustCoefficient, chamberPressure);

        return Analyse(
            gamma, chamberTemperature, molecularMass, chamberPressure,
            areaRatio, throatArea, ambientPressure);
    }
}
