using Propulsion.Core;

namespace Propulsion.Core.Tests;

/// <summary>
/// Tests for the Chapter 2 performance definitions and the Chapter 4 rocket equation.
/// </summary>
/// <remarks>
/// The values here are either exact by definition, hand-verifiable, or quoted from Sutton.
/// Where a tolerance is used it is stated as a number of decimal places on the SI value, which
/// keeps the assertions honest about how much precision is actually being claimed.
/// </remarks>
public class PerformanceTests
{
    /// <summary>
    /// Isp and effective exhaust velocity must be exact inverses of each other through g0.
    /// </summary>
    [Theory]
    [InlineData(250.0)]
    [InlineData(311.0)]
    [InlineData(452.3)]
    [InlineData(3000.0)]
    public void IspAndExhaustVelocityRoundTrip(double isp)
    {
        double c = Performance.ExhaustVelocityFromIsp(isp);
        double recovered = Performance.SpecificImpulse(c);

        Assert.Equal(isp, recovered, 10);
    }

    /// <summary>
    /// An Isp of 300 s corresponds to c = 300 * 9.80665 = 2941.995 m/s exactly.
    /// </summary>
    [Fact]
    public void ExhaustVelocityMatchesStandardGravityDefinition()
    {
        double c = Performance.ExhaustVelocityFromIsp(300.0);

        Assert.Equal(2941.995, c, 6);
    }

    /// <summary>
    /// In vacuum with matched pressure terms, thrust is just mdot * v_e.
    /// </summary>
    [Fact]
    public void ThrustReducesToMomentumTermWhenPressuresMatch()
    {
        double thrust = Performance.Thrust(
            massFlowRate: 100.0,
            exitVelocity: 2500.0,
            exitPressure: 50_000.0,
            ambientPressure: 50_000.0,
            exitArea: 0.5);

        Assert.Equal(250_000.0, thrust, 6);
    }

    /// <summary>
    /// Under-expansion adds thrust; over-expansion subtracts it.
    /// </summary>
    [Fact]
    public void PressureTermSignFollowsExpansionState()
    {
        double underExpanded = Performance.Thrust(100.0, 2500.0, 60_000.0, 50_000.0, 0.5);
        double matched = Performance.Thrust(100.0, 2500.0, 50_000.0, 50_000.0, 0.5);
        double overExpanded = Performance.Thrust(100.0, 2500.0, 40_000.0, 50_000.0, 0.5);

        Assert.True(underExpanded > matched);
        Assert.True(overExpanded < matched);

        // The increments should be symmetric: +/- 10,000 Pa over 0.5 m^2 is +/- 5000 N.
        Assert.Equal(5000.0, underExpanded - matched, 6);
        Assert.Equal(-5000.0, overExpanded - matched, 6);
    }

    /// <summary>
    /// A mass ratio of e gives exactly one exhaust velocity of delta-v. This is the cleanest
    /// possible check of the rocket equation.
    /// </summary>
    [Fact]
    public void RocketEquationGivesOneExhaustVelocityAtMassRatioE()
    {
        double c = 3000.0;
        double finalMass = 1000.0;
        double initialMass = finalMass * Math.E;

        double deltaV = RocketEquation.DeltaV(c, initialMass, finalMass);

        Assert.Equal(c, deltaV, 6);
    }

    /// <summary>
    /// Sutton's worked staging arithmetic: a stage with Isp 300 s and a 5:1 mass ratio.
    /// c = 2941.995 m/s, ln(5) = 1.6094379, so dv = 4734.96 m/s.
    /// </summary>
    [Fact]
    public void RocketEquationMatchesHandComputedValue()
    {
        double deltaV = RocketEquation.DeltaVFromIsp(300.0, 5000.0, 1000.0);

        Assert.Equal(4734.96, deltaV, 2);
    }

    /// <summary>
    /// Required mass ratio and achieved delta-v must invert one another.
    /// </summary>
    [Theory]
    [InlineData(1000.0, 2500.0)]
    [InlineData(5000.0, 3000.0)]
    [InlineData(9400.0, 3400.0)]
    public void MassRatioAndDeltaVAreInverses(double deltaV, double c)
    {
        double massRatio = RocketEquation.RequiredMassRatio(deltaV, c);
        double recovered = RocketEquation.DeltaV(c, massRatio, 1.0);

        Assert.Equal(deltaV, recovered, 6);
    }

    /// <summary>
    /// Required propellant mass must reproduce the requested delta-v when fed back through the
    /// rocket equation.
    /// </summary>
    [Fact]
    public void RequiredPropellantMassIsSelfConsistent()
    {
        double deltaV = 3200.0;
        double c = 3100.0;
        double finalMass = 2500.0;

        double propellant = RocketEquation.RequiredPropellantMass(deltaV, c, finalMass);
        double recovered = RocketEquation.DeltaV(c, finalMass + propellant, finalMass);

        Assert.Equal(deltaV, recovered, 6);
    }

    /// <summary>
    /// Payload fraction must go to zero exactly at the single-stage delta-v limit.
    /// </summary>
    [Fact]
    public void PayloadFractionVanishesAtTheSingleStageLimit()
    {
        double c = 3400.0;
        double epsilon = 0.08;

        double limit = RocketEquation.MaximumSingleStageDeltaV(c, epsilon);

        // Just inside the limit there is still payload; just outside there is none.
        Assert.True(RocketEquation.PayloadFraction(limit * 0.99, c, epsilon) > 0.0);
        Assert.Equal(0.0, RocketEquation.PayloadFraction(limit * 1.01, c, epsilon));

        // -3400 * ln(0.08) = 8587.5 m/s - short of the ~9400 m/s a launch needs, which is the
        // whole argument for staging.
        Assert.Equal(8587.5, limit, 1);
    }

    /// <summary>
    /// Thrust-to-weight below 1 means the vehicle cannot lift off.
    /// </summary>
    [Fact]
    public void ThrustToWeightRatioIsDimensionless()
    {
        // 1000 kg weighs 9806.65 N, so 9806.65 N of thrust is exactly T/W = 1.
        double ratio = Performance.ThrustToWeightRatio(9806.65, 1000.0);

        Assert.Equal(1.0, ratio, 9);
    }

    /// <summary>
    /// A vehicle cannot gain propellant, so a final mass above the initial mass must throw.
    /// </summary>
    [Fact]
    public void RocketEquationRejectsGainingMass()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => RocketEquation.DeltaV(3000.0, 1000.0, 2000.0));
    }

    /// <summary>
    /// Isp from integrated test data must agree with the steady-state definition.
    /// </summary>
    [Fact]
    public void SpecificImpulseFromTotalImpulseAgreesWithSteadyState()
    {
        double thrust = 50_000.0;
        double massFlow = 17.0;
        double burnTime = 120.0;

        double totalImpulse = Performance.TotalImpulse(thrust, burnTime);
        double propellantMass = massFlow * burnTime;

        double fromIntegral =
            Performance.SpecificImpulseFromTotalImpulse(totalImpulse, propellantMass);

        double fromSteadyState =
            Performance.SpecificImpulse(Performance.EffectiveExhaustVelocity(thrust, massFlow));

        Assert.Equal(fromSteadyState, fromIntegral, 9);
    }
}
