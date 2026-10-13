using Propulsion.Core;

namespace Propulsion.Core.Tests;

/// <summary>
/// Tests for the Chapter 3 isentropic nozzle theory.
/// </summary>
/// <remarks>
/// Ideal nozzle theory has an unusual property that makes it very testable: several exact
/// analytic identities connect quantities that are computed by completely different code paths.
/// The strongest of these is c = c* * C_F, which links the chamber figure of merit and the
/// nozzle figure of merit to the effective exhaust velocity. If any of the three implementations
/// is wrong, the identity breaks. Those identities are used here in preference to comparing
/// against transcribed table values, because they cannot be satisfied by accident.
/// </remarks>
public class NozzleTheoryTests
{
    /// <summary>
    /// The Vandenkerckhove function against its standard published values.
    /// </summary>
    [Theory]
    [InlineData(1.20, 0.6485)]
    [InlineData(1.30, 0.66726)]
    [InlineData(1.40, 0.6847)]
    public void VandenkerckhoveFunctionMatchesPublishedValues(double gamma, double expected)
    {
        double actual = NozzleTheory.VandenkerckhoveFunction(gamma);

        Assert.Equal(expected, actual, 4);
    }

    /// <summary>
    /// The theoretical ceiling on thrust coefficient, for infinite vacuum expansion.
    /// </summary>
    [Theory]
    [InlineData(1.20, 2.24658)]
    [InlineData(1.30, 1.96437)]
    public void MaximumThrustCoefficientMatchesPublishedValues(double gamma, double expected)
    {
        double actual = NozzleTheory.MaximumThrustCoefficient(gamma);

        Assert.Equal(expected, actual, 4);
    }

    /// <summary>
    /// Throat conditions: the classic critical ratios.
    /// </summary>
    [Fact]
    public void ThroatConditionsMatchCriticalRatios()
    {
        // T_t / T1 = 2/(g+1) = 0.909091 at gamma = 1.2.
        double throatTemperature = NozzleTheory.ThroatTemperature(1.2, 3300.0);
        Assert.Equal(3300.0 * 2.0 / 2.2, throatTemperature, 9);

        // p_t / p1 = (2/(g+1))^(g/(g-1)) = 0.564474 at gamma = 1.2.
        double throatPressure = NozzleTheory.ThroatPressure(1.2, 1.0);
        Assert.Equal(0.564474, throatPressure, 6);
    }

    /// <summary>
    /// The area ratio is exactly 1 at the throat, where the flow is sonic.
    /// </summary>
    [Theory]
    [InlineData(1.15)]
    [InlineData(1.20)]
    [InlineData(1.40)]
    public void AreaRatioIsUnityAtMachOne(double gamma)
    {
        Assert.Equal(1.0, NozzleTheory.AreaRatioFromMach(gamma, 1.0), 12);
    }

    /// <summary>
    /// The pressure-to-area-ratio conversion must invert cleanly in both directions. This
    /// exercises the bisection solver in <see cref="NozzleTheory.ExitPressureFromAreaRatio"/>.
    /// </summary>
    [Theory]
    [InlineData(1.20, 5.0)]
    [InlineData(1.20, 16.0)]
    [InlineData(1.20, 77.5)]
    [InlineData(1.24, 40.0)]
    [InlineData(1.30, 200.0)]
    public void AreaRatioAndExitPressureRoundTrip(double gamma, double areaRatio)
    {
        double chamberPressure = 7.0e6;

        double exitPressure =
            NozzleTheory.ExitPressureFromAreaRatio(gamma, chamberPressure, areaRatio);

        double recovered =
            NozzleTheory.AreaRatioFromPressureRatio(gamma, chamberPressure, exitPressure);

        // Relative agreement to nine significant figures - far tighter than any physical claim,
        // but it confirms the solver has genuinely converged rather than merely stopped.
        Assert.Equal(areaRatio, recovered, 6);
    }

    /// <summary>
    /// The central identity of nozzle theory: c = c* * C_F.
    /// </summary>
    /// <remarks>
    /// The chamber and the nozzle are analysed by entirely separate formulas, and their product
    /// must equal the effective exhaust velocity computed from thrust over mass flow. This is
    /// the single most valuable regression test in the library.
    /// </remarks>
    [Theory]
    [InlineData(1.20, 3300.0, 22.0, 7.0e6, 25.0, 0.0)]
    [InlineData(1.24, 3500.0, 20.0, 10.0e6, 40.0, 101_325.0)]
    [InlineData(1.30, 3000.0, 24.0, 3.0e6, 8.0, 101_325.0)]
    public void EffectiveExhaustVelocityEqualsCStarTimesThrustCoefficient(
        double gamma,
        double chamberTemperature,
        double molecularMass,
        double chamberPressure,
        double areaRatio,
        double ambientPressure)
    {
        EngineOperatingPoint point = EngineAnalysis.Analyse(
            gamma, chamberTemperature, molecularMass, chamberPressure,
            areaRatio, throatArea: 0.05, ambientPressure);

        double product = point.CharacteristicVelocity * point.ThrustCoefficient;

        Assert.Equal(point.EffectiveExhaustVelocity, product, 6);
    }

    /// <summary>
    /// At perfectly matched expansion the effective exhaust velocity must equal the ideal gas
    /// exit velocity, because the pressure-thrust term vanishes.
    /// </summary>
    /// <remarks>
    /// This ties the thrust-coefficient formulation to the energy-based exhaust velocity
    /// formula, which are derived completely independently in the textbook. They only agree if
    /// both are right.
    /// </remarks>
    [Theory]
    [InlineData(1.20, 3300.0, 22.0, 7.0e6, 20.0)]
    [InlineData(1.24, 3250.0, 10.0, 20.0e6, 60.0)]
    public void MatchedExpansionRecoversIdealExhaustVelocity(
        double gamma,
        double chamberTemperature,
        double molecularMass,
        double chamberPressure,
        double areaRatio)
    {
        // Find the exit pressure this geometry produces, then set ambient equal to it so the
        // nozzle is by construction perfectly expanded.
        double exitPressure =
            NozzleTheory.ExitPressureFromAreaRatio(gamma, chamberPressure, areaRatio);

        EngineOperatingPoint point = EngineAnalysis.Analyse(
            gamma, chamberTemperature, molecularMass, chamberPressure,
            areaRatio, throatArea: 0.05, ambientPressure: exitPressure);

        double idealVelocity = NozzleTheory.IdealExhaustVelocity(
            gamma, chamberTemperature, molecularMass, chamberPressure, exitPressure);

        Assert.Equal(idealVelocity, point.EffectiveExhaustVelocity, 4);
        Assert.Equal(ExpansionState.Optimum, point.Expansion);
    }

    /// <summary>
    /// Mass flow through a choked throat must not depend on anything downstream of it.
    /// </summary>
    /// <remarks>
    /// This is a physical claim, not just an arithmetic one: information cannot travel upstream
    /// through sonic flow, so changing the area ratio or the ambient pressure cannot alter the
    /// mass the throat passes.
    /// </remarks>
    [Fact]
    public void ChokedMassFlowIsIndependentOfNozzleAndAltitude()
    {
        EngineOperatingPoint seaLevel = EngineAnalysis.Analyse(
            1.22, 3400.0, 21.0, 8.0e6, areaRatio: 12.0,
            throatArea: 0.03, ambientPressure: Constants.SeaLevelPressure);

        EngineOperatingPoint vacuum = EngineAnalysis.Analyse(
            1.22, 3400.0, 21.0, 8.0e6, areaRatio: 12.0,
            throatArea: 0.03, ambientPressure: 0.0);

        EngineOperatingPoint bigNozzle = EngineAnalysis.Analyse(
            1.22, 3400.0, 21.0, 8.0e6, areaRatio: 120.0,
            throatArea: 0.03, ambientPressure: 0.0);

        Assert.Equal(seaLevel.MassFlowRate, vacuum.MassFlowRate, 9);
        Assert.Equal(seaLevel.MassFlowRate, bigNozzle.MassFlowRate, 9);
    }

    /// <summary>
    /// The same engine must produce more thrust in vacuum than at sea level.
    /// </summary>
    [Fact]
    public void VacuumThrustExceedsSeaLevelThrust()
    {
        EngineOperatingPoint seaLevel = EngineAnalysis.Analyse(
            1.22, 3400.0, 21.0, 8.0e6, 16.0, 0.03, Constants.SeaLevelPressure);

        EngineOperatingPoint vacuum = EngineAnalysis.Analyse(
            1.22, 3400.0, 21.0, 8.0e6, 16.0, 0.03, 0.0);

        Assert.True(vacuum.Thrust > seaLevel.Thrust);
        Assert.True(vacuum.SpecificImpulse > seaLevel.SpecificImpulse);

        // The entire difference is the pressure term p_a * A_e, and it must account for the
        // thrust gap exactly.
        double expectedGap = Constants.SeaLevelPressure * seaLevel.ExitArea;
        Assert.Equal(expectedGap, vacuum.Thrust - seaLevel.Thrust, 4);
    }

    /// <summary>
    /// Thrust is maximised at matched expansion, which is the justification for
    /// <see cref="NozzleTheory.OptimumAreaRatio"/>.
    /// </summary>
    [Fact]
    public void OptimumAreaRatioMaximisesThrustAtItsDesignAltitude()
    {
        double gamma = 1.22;
        double chamberPressure = 8.0e6;
        double ambientPressure = Constants.SeaLevelPressure;

        double optimumRatio =
            NozzleTheory.OptimumAreaRatio(gamma, chamberPressure, ambientPressure);

        double optimumThrust = EngineAnalysis
            .Analyse(gamma, 3400.0, 21.0, chamberPressure, optimumRatio, 0.03, ambientPressure)
            .Thrust;

        // Perturb the area ratio in both directions; thrust must fall away either side.
        double smaller = EngineAnalysis
            .Analyse(gamma, 3400.0, 21.0, chamberPressure, optimumRatio * 0.7, 0.03, ambientPressure)
            .Thrust;

        double larger = EngineAnalysis
            .Analyse(gamma, 3400.0, 21.0, chamberPressure, optimumRatio * 1.4, 0.03, ambientPressure)
            .Thrust;

        Assert.True(optimumThrust > smaller);
        Assert.True(optimumThrust > larger);
    }

    /// <summary>
    /// Expansion state classification and the flow separation criterion.
    /// </summary>
    [Fact]
    public void ExpansionStateIsClassifiedCorrectly()
    {
        Assert.Equal(ExpansionState.UnderExpanded,
            NozzleLosses.ClassifyExpansion(80_000.0, 50_000.0));

        Assert.Equal(ExpansionState.OverExpanded,
            NozzleLosses.ClassifyExpansion(30_000.0, 50_000.0));

        Assert.Equal(ExpansionState.Optimum,
            NozzleLosses.ClassifyExpansion(50_100.0, 50_000.0));

        // In vacuum there is nothing to match against.
        Assert.Equal(ExpansionState.UnderExpanded,
            NozzleLosses.ClassifyExpansion(1000.0, 0.0));

        // Summerfield: separation once p2 falls below 0.4 * p3.
        Assert.True(NozzleLosses.IsFlowSeparationLikely(19_000.0, 50_000.0));
        Assert.False(NozzleLosses.IsFlowSeparationLikely(21_000.0, 50_000.0));
        Assert.False(NozzleLosses.IsFlowSeparationLikely(1.0, 0.0));
    }

    /// <summary>
    /// The 15-degree conical divergence factor is the textbook 0.983.
    /// </summary>
    [Fact]
    public void ConicalDivergenceFactorMatchesTextbook()
    {
        Assert.Equal(0.9830, NozzleGeometry.ConicalDivergenceFactor(15.0), 4);

        // A wider cone loses more; a perfectly axial exit loses nothing.
        Assert.True(NozzleGeometry.ConicalDivergenceFactor(25.0) <
                    NozzleGeometry.ConicalDivergenceFactor(15.0));

        Assert.Equal(1.0, NozzleGeometry.ConicalDivergenceFactor(0.0), 12);
    }

    /// <summary>
    /// Designing for a thrust and then analysing the result must return that thrust.
    /// </summary>
    [Fact]
    public void DesignForThrustIsSelfConsistent()
    {
        EngineOperatingPoint point = EngineAnalysis.DesignForThrust(
            gamma: 1.22,
            chamberTemperature: 3500.0,
            molecularMass: 21.0,
            chamberPressure: 10.0e6,
            areaRatio: 35.0,
            requiredThrust: 800_000.0,
            ambientPressure: 0.0);

        Assert.Equal(800_000.0, point.Thrust, 3);

        // And the geometry must be internally consistent.
        Assert.Equal(point.AreaRatio * point.ThroatArea, point.ExitArea, 12);
    }

    /// <summary>
    /// Gamma outside the physical range must be rejected rather than producing NaN.
    /// </summary>
    [Theory]
    [InlineData(1.0)]
    [InlineData(0.9)]
    [InlineData(2.0)]
    public void InvalidGammaIsRejected(double gamma)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => NozzleTheory.VandenkerckhoveFunction(gamma));
    }

    /// <summary>
    /// A converging-diverging nozzle must actually diverge.
    /// </summary>
    [Fact]
    public void SubsonicAreaRatioIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => NozzleTheory.ExitPressureFromAreaRatio(1.2, 5.0e6, 0.9));
    }
}
