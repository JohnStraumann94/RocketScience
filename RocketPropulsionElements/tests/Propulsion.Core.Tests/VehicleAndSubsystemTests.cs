using Propulsion.Core;

namespace Propulsion.Core.Tests;

/// <summary>
/// Tests for staging, flight performance, solid motors, liquid feed systems and electric
/// propulsion - Sutton Chapters 4, 6, 8, 11, 12 and 19.
/// </summary>
public class VehicleAndSubsystemTests
{
    // ------------------------------------------------------------------------------------
    // Staging - Chapter 4
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// A single-stage analysis must reduce exactly to the rocket equation.
    /// </summary>
    [Fact]
    public void SingleStageVehicleMatchesRocketEquation()
    {
        Stage stage = new("Stage 1", PropellantMass: 9000.0, StructuralMass: 1000.0,
            SpecificImpulse: 300.0);

        VehicleResult result = Staging.AnalyseVehicle([stage], payloadMass: 500.0);

        double expected = RocketEquation.DeltaVFromIsp(300.0, 10_500.0, 1500.0);

        Assert.Equal(expected, result.TotalDeltaV, 6);
        Assert.Equal(10_500.0, result.GrossLiftoffMass, 9);
    }

    /// <summary>
    /// Structure is jettisoned at separation, not at burnout, so a stage's burnout mass must
    /// still include its own structure.
    /// </summary>
    /// <remarks>
    /// This is the classic staging bookkeeping error and it flatters the answer, so it is worth
    /// pinning explicitly.
    /// </remarks>
    [Fact]
    public void StageBurnoutMassStillIncludesItsOwnStructure()
    {
        Stage first = new("Stage 1", 80_000.0, 8_000.0, 290.0);
        Stage second = new("Stage 2", 20_000.0, 2_500.0, 340.0);

        VehicleResult result = Staging.AnalyseVehicle([first, second], payloadMass: 2_000.0);

        StageResult firstResult = result.Stages[0];

        // Ignition: everything. Burnout: everything minus stage 1 propellant only.
        Assert.Equal(112_500.0, firstResult.IgnitionMass, 9);
        Assert.Equal(32_500.0, firstResult.BurnoutMass, 9);

        // Stage 2 ignites after stage 1's structure has gone.
        Assert.Equal(24_500.0, result.Stages[1].IgnitionMass, 9);
        Assert.Equal(4_500.0, result.Stages[1].BurnoutMass, 9);
    }

    /// <summary>
    /// Stage delta-v contributions add up to the vehicle total.
    /// </summary>
    [Fact]
    public void StageDeltaVsSumToVehicleTotal()
    {
        Stage first = new("Stage 1", 80_000.0, 8_000.0, 290.0);
        Stage second = new("Stage 2", 20_000.0, 2_500.0, 340.0);
        Stage third = new("Stage 3", 4_000.0, 600.0, 450.0);

        VehicleResult result = Staging.AnalyseVehicle([first, second, third], 1_500.0);

        double sum = result.Stages.Sum(s => s.DeltaV);

        Assert.Equal(result.TotalDeltaV, sum, 9);
    }

    /// <summary>
    /// Staging must beat a single stage for a demanding mission - the entire reason it exists.
    /// </summary>
    [Fact]
    public void MoreStagesBeatFewerForOrbitalDeltaV()
    {
        double deltaV = 9400.0;
        double c = 3200.0;
        double epsilon = 0.08;

        double one = Staging.IdealPayloadFraction(deltaV, c, epsilon, 1);
        double two = Staging.IdealPayloadFraction(deltaV, c, epsilon, 2);
        double three = Staging.IdealPayloadFraction(deltaV, c, epsilon, 3);

        // A single stage cannot do it at all with this hardware.
        Assert.Equal(0.0, one);
        Assert.True(two > 0.0);
        Assert.True(three > two);
    }

    /// <summary>
    /// In the idealised equal-stage model the payload fraction is monotonically increasing in the
    /// number of stages, so the "optimum" is always the largest count offered.
    /// </summary>
    /// <remarks>
    /// This is a genuine property of the simplified model, not a bug: the model charges no fixed
    /// cost per stage (no interstage, no extra avionics, no separation hardware, no reliability
    /// penalty). Real vehicles stop at two or three stages because those neglected costs grow
    /// faster than the diminishing delta-v benefit. The test pins the model's actual behaviour and
    /// documents the limitation, which is exactly the lesson Sutton draws in Chapter 4.
    /// </remarks>
    [Fact]
    public void OptimumStageCountSaturatesInTheIdealisedModel()
    {
        (int stageCount, double fraction) =
            Staging.OptimumStageCount(9400.0, 3200.0, 0.08, maximumStages: 8);

        Assert.Equal(8, stageCount);
        Assert.True(fraction > 0.0);

        // The benefit is strongly diminishing: going 2 -> 3 stages helps far more than 7 -> 8.
        double gainEarly = Staging.IdealPayloadFraction(9400.0, 3200.0, 0.08, 3)
                         - Staging.IdealPayloadFraction(9400.0, 3200.0, 0.08, 2);

        double gainLate = Staging.IdealPayloadFraction(9400.0, 3200.0, 0.08, 8)
                        - Staging.IdealPayloadFraction(9400.0, 3200.0, 0.08, 7);

        Assert.True(gainEarly > gainLate);
    }

    // ------------------------------------------------------------------------------------
    // Flight performance - Chapter 4
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// Gravity loss vanishes when flying horizontally and is maximal when flying straight up.
    /// </summary>
    [Fact]
    public void GravityLossDependsOnFlightPathAngle()
    {
        Assert.Equal(0.0, FlightPerformance.GravityLoss(100.0, 0.0), 9);

        // Straight up for 100 s costs g0 * 100 = 980.665 m/s.
        Assert.Equal(980.665, FlightPerformance.GravityLoss(100.0, 90.0), 6);

        // 45 degrees costs sin(45) of the vertical value.
        Assert.Equal(980.665 * Math.Sqrt(0.5), FlightPerformance.GravityLoss(100.0, 45.0), 6);
    }

    /// <summary>
    /// Circular orbit velocity at 200 km is the familiar 7784 m/s.
    /// </summary>
    [Fact]
    public void CircularOrbitVelocityMatchesKnownValue()
    {
        double velocity = FlightPerformance.CircularOrbitVelocity(200_000.0);

        Assert.Equal(7784.0, velocity, 0);
    }

    /// <summary>
    /// A vertical ascent must always fall short of the ideal delta-v, and the shortfall must be
    /// accounted for by the gravity and drag losses.
    /// </summary>
    [Fact]
    public void VerticalAscentLossesAccountForTheShortfall()
    {
        AscentResult result = FlightPerformance.VerticalAscent(
            initialMass: 50_000.0,
            propellantMass: 42_000.0,
            vacuumThrust: 1_000_000.0,
            specificImpulseSeconds: 300.0,
            exitArea: 1.2,
            exitPressure: 60_000.0,
            dragCoefficient: 0.3,
            referenceArea: 3.5,
            timeStep: 0.05);

        Assert.True(result.BurnoutVelocity < result.IdealDeltaV);
        Assert.True(result.GravityLoss > 0.0);
        Assert.True(result.DragLoss > 0.0);

        // Max-Q must occur during the burn, not at the very start or the very end.
        Assert.True(result.MaxDynamicPressure > 0.0);
        Assert.InRange(result.MaxDynamicPressureTime, 0.0, result.BurnoutTime);

        // The loss accounting should explain most of the gap. It cannot be exact, because the
        // ambient-pressure thrust term also differs from the vacuum ideal, but it must be the
        // dominant part.
        double shortfall = result.IdealDeltaV - result.BurnoutVelocity;
        double accountedFor = result.GravityLoss + result.DragLoss;

        Assert.InRange(accountedFor / shortfall, 0.7, 1.3);
    }

    // ------------------------------------------------------------------------------------
    // Solid motors - Chapters 11 and 12
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// The burn rate exponent stability criterion.
    /// </summary>
    [Theory]
    [InlineData(0.35, true)]
    [InlineData(0.99, true)]
    [InlineData(1.0, false)]
    [InlineData(1.2, false)]
    public void BurnRateExponentStabilityCriterion(double exponent, bool expectedStable)
    {
        Assert.Equal(expectedStable, SolidMotor.IsStable(exponent));
    }

    /// <summary>
    /// The equilibrium chamber pressure must actually be an equilibrium: at that pressure, gas
    /// generation and gas discharge must balance exactly.
    /// </summary>
    /// <remarks>
    /// This checks the derivation rather than the arithmetic. A_b * rho * r must equal
    /// p1 * A_t / c* at the returned pressure.
    /// </remarks>
    [Fact]
    public void EquilibriumChamberPressureBalancesGenerationAndDischarge()
    {
        double burningArea = 2.5;
        double throatArea = 0.005;
        double density = 1800.0;
        double cStar = 1550.0;
        double a = 3.5e-5;
        double n = 0.35;

        double pressure = SolidMotor.EquilibriumChamberPressure(
            burningArea, throatArea, density, cStar, a, n);

        double burnRate = SolidMotor.BurnRate(a, pressure, n);

        double generation = burningArea * density * burnRate;
        double discharge = pressure * throatArea / cStar;

        Assert.Equal(discharge, generation, 6);
    }

    /// <summary>
    /// Chamber pressure rises with Klemmung, and does so faster than linearly.
    /// </summary>
    [Fact]
    public void ChamberPressureIsSuperlinearInKlemmung()
    {
        double Pressure(double burningArea) => SolidMotor.EquilibriumChamberPressure(
            burningArea, 0.005, 1800.0, 1550.0, 3.5e-5, 0.35);

        double baseline = Pressure(2.5);
        double increased = Pressure(2.75);   // +10% burning area

        double ratio = increased / baseline;

        // 1.10 ^ (1/(1-0.35)) = 1.10 ^ 1.53846 = 1.15793.
        Assert.Equal(1.15793, ratio, 4);
    }

    /// <summary>
    /// An uninhibited BATES grain burns out, and its area goes to zero rather than negative.
    /// </summary>
    [Fact]
    public void BatesGrainAreaGoesToZeroAtBurnout()
    {
        double outerDiameter = 0.075;
        double coreDiameter = 0.025;
        double length = 0.150;

        // The web is (0.075 - 0.025)/2 = 0.025 m before the core reaches the case.
        Assert.True(SolidMotor.BatesGrainBurningArea(
            outerDiameter, coreDiameter, length, 0.020) > 0.0);

        Assert.Equal(0.0, SolidMotor.BatesGrainBurningArea(
            outerDiameter, coreDiameter, length, 0.025));

        Assert.Equal(0.0, SolidMotor.BatesGrainBurningArea(
            outerDiameter, coreDiameter, length, 0.050));
    }

    /// <summary>
    /// Inhibiting the ends makes the grain strongly progressive: burning area only grows.
    /// </summary>
    [Fact]
    public void InhibitedEndsGiveProgressiveBurn()
    {
        double start = SolidMotor.BatesGrainBurningArea(
            0.075, 0.025, 0.150, 0.000, endsInhibited: true);

        double later = SolidMotor.BatesGrainBurningArea(
            0.075, 0.025, 0.150, 0.015, endsInhibited: true);

        Assert.True(later > start);
    }

    // ------------------------------------------------------------------------------------
    // Liquid feed systems - Chapters 6 and 8
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// Injector sizing must round trip: size an orifice for a flow, then compute the flow back.
    /// </summary>
    [Fact]
    public void InjectorSizingRoundTrips()
    {
        double massFlow = 12.5;
        double cd = 0.75;
        double density = 810.0;
        double pressureDrop = 1.2e6;

        double area = LiquidEngine.RequiredOrificeArea(massFlow, cd, density, pressureDrop);
        double recovered = LiquidEngine.InjectorMassFlow(cd, area, density, pressureDrop);

        Assert.Equal(massFlow, recovered, 9);
    }

    /// <summary>
    /// Injector flow scales as the square root of pressure drop - the reason injector stiffness
    /// is a stability problem.
    /// </summary>
    [Fact]
    public void InjectorFlowScalesAsSquareRootOfPressureDrop()
    {
        double single = LiquidEngine.InjectorMassFlow(0.75, 1e-4, 810.0, 1.0e6);
        double quadruple = LiquidEngine.InjectorMassFlow(0.75, 1e-4, 810.0, 4.0e6);

        Assert.Equal(2.0, quadruple / single, 9);
    }

    /// <summary>
    /// Pumping a less dense propellant to the same pressure costs proportionally more power.
    /// </summary>
    [Fact]
    public void PumpPowerScalesInverselyWithDensity()
    {
        double oxygenPower = LiquidEngine.PumpPower(100.0, 20.0e6, 1141.0, 0.7);
        double hydrogenPower = LiquidEngine.PumpPower(100.0, 20.0e6, 71.0, 0.7);

        Assert.Equal(1141.0 / 71.0, hydrogenPower / oxygenPower, 9);
    }

    /// <summary>
    /// The mixture ratio split must conserve total mass flow.
    /// </summary>
    [Theory]
    [InlineData(2.56)]
    [InlineData(5.5)]
    [InlineData(0.5)]
    public void MixtureRatioSplitConservesMass(double mixtureRatio)
    {
        (double ox, double fuel) = Propellants.SplitFlow(250.0, mixtureRatio);

        Assert.Equal(250.0, ox + fuel, 9);
        Assert.Equal(mixtureRatio, ox / fuel, 9);
    }

    /// <summary>
    /// LOX/LH2 must have a far lower bulk density than LOX/RP-1 - the reason kerosene survives
    /// at the bottom of launch vehicles.
    /// </summary>
    [Fact]
    public void HydrogenHasPoorBulkDensityButHighIsp()
    {
        double chamberPressure = 10.0e6;
        double areaRatio = 40.0;

        Assert.True(Propellants.LoxLh2.BulkDensity < Propellants.LoxRp1.BulkDensity);
        Assert.True(Propellants.LoxLh2.IdealVacuumIsp(chamberPressure, areaRatio) >
                    Propellants.LoxRp1.IdealVacuumIsp(chamberPressure, areaRatio));

        // But when volume is the constraint, kerosene wins outright.
        Assert.True(Propellants.LoxRp1.DensityImpulse(chamberPressure, areaRatio) >
                    Propellants.LoxLh2.DensityImpulse(chamberPressure, areaRatio));
    }

    // ------------------------------------------------------------------------------------
    // Electric propulsion - Chapter 19
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// Thrust per watt falls as exhaust velocity rises - the defining constraint of a
    /// power-limited system.
    /// </summary>
    [Fact]
    public void ThrustPerWattFallsWithExhaustVelocity()
    {
        double lowIsp = ElectricPropulsion.ThrustFromPower(1000.0, 15_000.0, 0.5);
        double highIsp = ElectricPropulsion.ThrustFromPower(1000.0, 30_000.0, 0.5);

        Assert.Equal(2.0, lowIsp / highIsp, 9);

        // 1 kW at 30 km/s and 50% efficiency gives 2*0.5*1000/30000 = 33.3 mN.
        Assert.Equal(0.03333, highIsp, 5);
    }

    /// <summary>
    /// Jet power and required electrical power must be consistent with each other.
    /// </summary>
    [Fact]
    public void PowerRelationsAreSelfConsistent()
    {
        double thrust = 0.09;
        double exhaustVelocity = 30_000.0;
        double efficiency = 0.6;

        double electrical = ElectricPropulsion.RequiredElectricalPower(
            thrust, exhaustVelocity, efficiency);

        double recovered = ElectricPropulsion.ThrustFromPower(
            electrical, exhaustVelocity, efficiency);

        Assert.Equal(thrust, recovered, 12);
        Assert.Equal(ElectricPropulsion.JetPower(thrust, exhaustVelocity),
                     electrical * efficiency, 9);
    }

    /// <summary>
    /// The optimum exhaust velocity rises with available thrusting time, which is why deep-space
    /// missions run much higher Isp than station-keeping missions.
    /// </summary>
    [Fact]
    public void OptimumExhaustVelocityRisesWithBurnTime()
    {
        double shortMission = ElectricPropulsion.OptimumExhaustVelocity(
            5000.0, burnTimeSeconds: 30.0 * 86_400.0, specificPower: 100.0,
            thrusterEfficiency: 0.5);

        double longMission = ElectricPropulsion.OptimumExhaustVelocity(
            5000.0, burnTimeSeconds: 365.0 * 86_400.0, specificPower: 100.0,
            thrusterEfficiency: 0.5);

        Assert.True(longMission > shortMission);

        // The ratio must be exactly sqrt of the time ratio.
        Assert.Equal(Math.Sqrt(365.0 / 30.0), longMission / shortMission, 9);
    }

    /// <summary>
    /// The coupled mission mass breakdown must converge and be internally consistent.
    /// </summary>
    [Fact]
    public void ElectricMissionMassBreakdownIsSelfConsistent()
    {
        (double propellant, double powerPlant, double thrust, double power, double initial) =
            ElectricPropulsion.MissionMassBreakdown(
                payloadMass: 1000.0,
                deltaV: 5000.0,
                exhaustVelocity: 30_000.0,
                burnTimeSeconds: 200.0 * 86_400.0,
                specificPower: 100.0,
                thrusterEfficiency: 0.6);

        double dryMass = 1000.0 + powerPlant;

        // The rocket equation must hold on the converged masses. The fixed-point iteration stops
        // at a relative tolerance, so a milli-metre-per-second residual is expected and fine.
        Assert.Equal(5000.0, RocketEquation.DeltaV(30_000.0, initial, dryMass), 1);
        Assert.Equal(initial - dryMass, propellant, 3);

        // And the power plant must match the power the thruster actually demands.
        Assert.Equal(power / 100.0, powerPlant, 3);
        Assert.True(thrust > 0.0);
    }
}
