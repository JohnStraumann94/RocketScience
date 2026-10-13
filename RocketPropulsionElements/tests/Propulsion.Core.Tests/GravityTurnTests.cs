using Propulsion.Core;

namespace Propulsion.Core.Tests;

/// <summary>
/// Tests for the two-dimensional gravity-turn ascent integration - Sutton Chapter 4.
/// </summary>
/// <remarks>
/// <para>
/// A trajectory integrator cannot be checked against a single published number the way a
/// closed-form equation can, because its answer depends on the atmosphere model, the step
/// size and the integration scheme. So instead of asserting one magic value, these tests
/// assert the PROPERTIES the physics guarantees regardless of implementation:
/// </para>
/// <list type="bullet">
///   <item><description>conservation and consistency identities that must hold exactly;</description></item>
///   <item><description>limiting cases that must reduce to a simpler, independently known result;</description></item>
///   <item><description>monotonicity and sign conditions the equations of motion force;</description></item>
///   <item><description>numerical convergence as the step size is refined.</description></item>
/// </list>
/// <para>
/// That combination pins the implementation down far more tightly than a golden number would,
/// and it does not break every time the model is legitimately refined.
/// </para>
/// </remarks>
public class GravityTurnTests
{
    // A single reference vehicle used by most tests: roughly a small launcher, deliberately
    // the same numbers the corresponding UI sample defaults to.
    private const double InitialMass = 50_000.0;
    private const double PropellantMass = 42_000.0;
    private const double VacuumThrust = 900_000.0;
    private const double SpecificImpulse = 310.0;
    private const double ExitArea = 0.8;
    private const double ExitPressure = 50_000.0;
    private const double DragCoefficient = 0.3;
    private const double ReferenceArea = 7.0;

    /// <summary>Runs the reference vehicle with overridable steering and step size.</summary>
    /// <param name="pitchOverAngleDegrees">Pitch-over kick size, degrees.</param>
    /// <param name="pitchOverTime">Time of the pitch-over, s.</param>
    /// <param name="coastSeconds">Ballistic coast appended after burnout, s.</param>
    /// <param name="timeStep">Integration step, s.</param>
    /// <returns>The integrated result.</returns>
    private static GravityTurnResult Reference(
        double pitchOverAngleDegrees = 3.5,
        double pitchOverTime = 12.0,
        double coastSeconds = 120.0,
        double timeStep = 0.1) =>
        GravityTurn.Ascend(
            InitialMass,
            PropellantMass,
            VacuumThrust,
            SpecificImpulse,
            ExitArea,
            ExitPressure,
            DragCoefficient,
            ReferenceArea,
            pitchOverTime,
            pitchOverAngleDegrees,
            coastSeconds,
            timeStep);

    // ------------------------------------------------------------------------------------
    // Exact identities
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// Sea-level gravity from the inverse-square law must match the accepted value.
    /// </summary>
    /// <remarks>
    /// mu / R^2 = 3.986004418e14 / 6378137^2 = 9.7983 m/s^2. Note this is NOT 9.80665: the
    /// standard value includes the centrifugal effect of Earth's rotation and an average over
    /// latitude, whereas this is the pure gravitational term at the equatorial radius.
    /// </remarks>
    [Fact]
    public void SeaLevelGravityMatchesTheInverseSquareLaw()
    {
        Assert.Equal(9.7983, GravityTurn.LocalGravity(0.0), 3);
    }

    /// <summary>Gravity must fall off as the inverse square of the distance from the centre.</summary>
    [Fact]
    public void GravityFallsOffAsInverseSquare()
    {
        double surface = GravityTurn.LocalGravity(0.0);
        double atRadius = GravityTurn.LocalGravity(Constants.EarthRadius);

        // Doubling the distance from the centre must quarter the acceleration.
        Assert.Equal(surface / 4.0, atRadius, 6);

        // The everyday intuition check: gravity at 100 km is still 97% of its surface value.
        double at100Km = GravityTurn.LocalGravity(100_000.0);
        Assert.InRange(at100Km / surface, 0.96, 0.98);
    }

    /// <summary>
    /// The reported ideal delta-v must be exactly the rocket equation for the same masses.
    /// </summary>
    [Fact]
    public void IdealDeltaVMatchesTheRocketEquation()
    {
        GravityTurnResult result = Reference();

        double expected = RocketEquation.DeltaVFromIsp(
            SpecificImpulse, InitialMass, InitialMass - PropellantMass);

        Assert.Equal(expected, result.IdealDeltaV, 6);
    }

    /// <summary>
    /// Burnout must occur when the propellant runs out: tb = mp / (F / c).
    /// </summary>
    [Fact]
    public void BurnoutTimeMatchesThePropellantLoad()
    {
        GravityTurnResult result = Reference(timeStep: 0.05);

        double exhaustVelocity = Performance.ExhaustVelocityFromIsp(SpecificImpulse);
        double massFlowRate = VacuumThrust / exhaustVelocity;
        double expectedBurnTime = PropellantMass / massFlowRate;

        // The reported burnout is the last POWERED sample, so it can lag the exact instant by
        // at most one integration step. Asserted as an interval rather than a rounded number
        // of decimal places precisely because the true answer legitimately falls anywhere
        // inside that one-step window.
        Assert.InRange(result.BurnoutTime, expectedBurnTime - 0.06, expectedBurnTime);
    }

    // ------------------------------------------------------------------------------------
    // Limiting cases
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// With no pitch-over the flight must remain exactly vertical, and the gravity loss must
    /// reduce to the classical g * tb.
    /// </summary>
    /// <remarks>
    /// This is the strongest single test in the file. A zero-degree kick removes the steering
    /// entirely, at which point the two-dimensional integrator must collapse onto the
    /// one-dimensional problem whose answer is known in closed form. Any sign error in the
    /// gamma equation, any leaked horizontal velocity, or any mistake in the loss integral
    /// shows up here immediately.
    /// </remarks>
    [Fact]
    public void ZeroPitchOverGivesPurelyVerticalFlight()
    {
        GravityTurnResult result = Reference(pitchOverAngleDegrees: 0.0, coastSeconds: 0.0);

        foreach (GravityTurnSample sample in result.Samples)
        {
            // Not asserted as exactly zero: cos(pi/2) in double precision is 6.1e-17 rather
            // than 0, and integrating that over a few thousand steps accumulates a downrange
            // distance of well under a micrometre. Demanding a hard zero would be testing the
            // floating-point representation of pi, not the flight mechanics.
            Assert.True(
                Math.Abs(sample.Downrange) < 1.0e-3,
                $"Vertical flight drifted {sample.Downrange:E3} m downrange.");

            Assert.Equal(90.0, sample.FlightPathAngle, 6);
        }

        // Gravity loss for vertical flight is the integral of g dt. Local gravity is very
        // slightly below the standard value and decreases with altitude, so the loss must be
        // just under g0 * tb - within about 1%.
        double classical = Constants.StandardGravity * result.BurnoutTime;

        Assert.InRange(result.GravityLoss / classical, 0.97, 1.0);
    }

    /// <summary>
    /// A pitched-over vehicle must lose LESS to gravity than a vertical one, which is the
    /// entire justification for flying a gravity turn.
    /// </summary>
    [Fact]
    public void TurningOverReducesTheGravityLoss()
    {
        GravityTurnResult vertical = Reference(pitchOverAngleDegrees: 0.0, coastSeconds: 0.0);
        GravityTurnResult turned = Reference(pitchOverAngleDegrees: 4.0, coastSeconds: 0.0);

        Assert.True(
            turned.GravityLoss < vertical.GravityLoss,
            $"Gravity loss turned ({turned.GravityLoss:F1}) should be below " +
            $"vertical ({vertical.GravityLoss:F1}).");

        // ... and the saved velocity must show up as extra speed at burnout.
        Assert.True(turned.BurnoutVelocity > vertical.BurnoutVelocity);
    }

    /// <summary>
    /// A larger pitch-over must produce a shallower flight path angle at burnout and more
    /// downrange distance.
    /// </summary>
    [Fact]
    public void LargerPitchOverTurnsTheVehicleFurther()
    {
        GravityTurnResult gentle = Reference(pitchOverAngleDegrees: 2.0, coastSeconds: 0.0);
        GravityTurnResult sharp = Reference(pitchOverAngleDegrees: 6.0, coastSeconds: 0.0);

        Assert.True(sharp.BurnoutFlightPathAngle < gentle.BurnoutFlightPathAngle);
        Assert.True(sharp.BurnoutDownrange > gentle.BurnoutDownrange);
    }

    // ------------------------------------------------------------------------------------
    // Shape of the trajectory
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// The vehicle must fly vertically until the pitch-over, and only then start going
    /// downrange.
    /// </summary>
    [Fact]
    public void NoDownrangeMotionBeforeThePitchOver()
    {
        const double pitchTime = 15.0;

        GravityTurnResult result = Reference(pitchOverTime: pitchTime, coastSeconds: 0.0);

        foreach (GravityTurnSample sample in result.Samples)
        {
            if (sample.Time <= pitchTime)
            {
                Assert.True(Math.Abs(sample.Downrange) < 1.0e-3);
                Assert.Equal(90.0, sample.FlightPathAngle, 6);
            }
        }

        Assert.True(result.BurnoutDownrange > 0.0);
    }

    /// <summary>
    /// Once turning, the flight path angle must decrease monotonically: with no lift, gravity
    /// can only ever rotate the velocity vector downwards.
    /// </summary>
    [Fact]
    public void FlightPathAngleDecreasesMonotonicallyAfterPitchOver()
    {
        GravityTurnResult result = Reference(pitchOverTime: 10.0, coastSeconds: 60.0);

        double previous = double.MaxValue;

        foreach (GravityTurnSample sample in result.Samples)
        {
            if (sample.Time <= 10.5)
            {
                continue;
            }

            Assert.True(
                sample.FlightPathAngle <= previous + 1.0e-6,
                $"Flight path angle rose from {previous:F4} to {sample.FlightPathAngle:F4} " +
                $"at t = {sample.Time:F2} s, which zero-lift flight cannot do.");

            previous = sample.FlightPathAngle;
        }
    }

    /// <summary>Mass must decrease linearly while burning and stay constant afterwards.</summary>
    [Fact]
    public void MassDecreasesWhileBurningAndIsConstantOnCoast()
    {
        GravityTurnResult result = Reference(coastSeconds: 100.0);

        double finalMass = InitialMass - PropellantMass;

        Assert.Equal(InitialMass, result.Samples[0].Mass, 6);

        foreach (GravityTurnSample sample in result.Samples)
        {
            if (!sample.Powered)
            {
                Assert.Equal(finalMass, sample.Mass, 6);
                Assert.Equal(0.0, sample.Thrust, 6);
            }
            else
            {
                Assert.InRange(sample.Mass, finalMass - 1.0e-6, InitialMass + 1.0e-6);
                Assert.True(sample.Thrust > 0.0);
            }
        }
    }

    /// <summary>
    /// Thrust must rise with altitude as the atmospheric back-pressure on the exit plane falls
    /// away, approaching the vacuum value.
    /// </summary>
    [Fact]
    public void ThrustRisesTowardsTheVacuumValueWithAltitude()
    {
        GravityTurnResult result = Reference(coastSeconds: 0.0);

        double first = result.Samples[0].Thrust;
        double last = result.Samples[result.BurnoutIndex].Thrust;

        double expectedSeaLevel = VacuumThrust - Constants.SeaLevelPressure * ExitArea;

        Assert.Equal(expectedSeaLevel, first, 1);
        Assert.True(last > first);
        Assert.True(last <= VacuumThrust + 1.0e-6);
    }

    /// <summary>
    /// Max-Q must occur in mid-flight, not at either end - it is the crossover between rising
    /// v^2 and exponentially falling density.
    /// </summary>
    [Fact]
    public void MaxDynamicPressureOccursInMidFlight()
    {
        GravityTurnResult result = Reference(coastSeconds: 0.0);

        Assert.True(result.MaxDynamicPressureIndex > 0);
        Assert.True(result.MaxDynamicPressureIndex < result.Samples.Count - 1);
        Assert.True(result.MaxDynamicPressure > 0.0);

        // No sample may exceed the reported peak.
        foreach (GravityTurnSample sample in result.Samples)
        {
            Assert.True(sample.DynamicPressure <= result.MaxDynamicPressure + 1.0e-9);
        }

        // Sanity band: a small launcher of this class peaks somewhere in the tens of kPa.
        Assert.InRange(result.MaxDynamicPressure / 1000.0, 5.0, 200.0);
    }

    /// <summary>The reported apogee must genuinely be the highest sample.</summary>
    [Fact]
    public void ApogeeIsTheHighestPointOfTheWholeTrajectory()
    {
        GravityTurnResult result = Reference(coastSeconds: 400.0);

        foreach (GravityTurnSample sample in result.Samples)
        {
            Assert.True(sample.Altitude <= result.ApogeeAltitude + 1.0e-6);
        }

        Assert.Equal(result.Samples[result.ApogeeIndex].Altitude, result.ApogeeAltitude, 6);
        Assert.Equal(result.Samples[result.ApogeeIndex].Time, result.ApogeeTime, 6);
    }

    /// <summary>Time must increase monotonically so playback and interpolation are well defined.</summary>
    [Fact]
    public void TimeIncreasesMonotonically()
    {
        GravityTurnResult result = Reference(coastSeconds: 150.0);

        for (int i = 1; i < result.Samples.Count; i++)
        {
            Assert.True(result.Samples[i].Time > result.Samples[i - 1].Time);
        }
    }

    // ------------------------------------------------------------------------------------
    // Loss budget
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// The achieved burnout speed must fall short of the ideal delta-v by the losses - a real
    /// vehicle never gets the rocket equation's answer.
    /// </summary>
    [Fact]
    public void AchievedVelocityIsBelowTheIdealDeltaV()
    {
        GravityTurnResult result = Reference(coastSeconds: 0.0);

        Assert.True(result.BurnoutVelocity < result.IdealDeltaV);
        Assert.True(result.GravityLoss > 0.0);
        Assert.True(result.DragLoss > 0.0);

        // Drag is the small term: for a launcher it is typically well under a tenth of the
        // gravity loss. If this ever inverts, something is wrong with the atmosphere model.
        Assert.True(result.DragLoss < result.GravityLoss);

        // Efficiency for a vehicle of this class lands in a broad but definitely bounded band.
        double efficiency = result.BurnoutVelocity / result.IdealDeltaV;
        Assert.InRange(efficiency, 0.5, 0.95);
    }

    // ------------------------------------------------------------------------------------
    // Numerical behaviour
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// Halving the step size must barely change the answer. RK4 is fourth-order accurate, so
    /// the residual difference should be a fraction of a percent.
    /// </summary>
    [Fact]
    public void ResultConvergesAsTheStepSizeIsRefined()
    {
        GravityTurnResult coarse = Reference(coastSeconds: 0.0, timeStep: 0.2);
        GravityTurnResult fine = Reference(coastSeconds: 0.0, timeStep: 0.05);

        double velocityError =
            Math.Abs(coarse.BurnoutVelocity - fine.BurnoutVelocity) / fine.BurnoutVelocity;

        double altitudeError =
            Math.Abs(coarse.BurnoutAltitude - fine.BurnoutAltitude) / fine.BurnoutAltitude;

        Assert.True(velocityError < 0.01, $"Burnout velocity differed by {velocityError:P3}.");
        Assert.True(altitudeError < 0.01, $"Burnout altitude differed by {altitudeError:P3}.");
    }

    /// <summary>Every state variable must stay finite over the whole integration.</summary>
    [Fact]
    public void NoSampleContainsNaNOrInfinity()
    {
        GravityTurnResult result = Reference(coastSeconds: 300.0);

        foreach (GravityTurnSample sample in result.Samples)
        {
            Assert.True(double.IsFinite(sample.Time));
            Assert.True(double.IsFinite(sample.Downrange));
            Assert.True(double.IsFinite(sample.Altitude));
            Assert.True(double.IsFinite(sample.Velocity));
            Assert.True(double.IsFinite(sample.FlightPathAngle));
            Assert.True(double.IsFinite(sample.Mass));
            Assert.True(double.IsFinite(sample.Thrust));
            Assert.True(double.IsFinite(sample.Drag));
            Assert.True(double.IsFinite(sample.Acceleration));
            Assert.True(double.IsFinite(sample.DynamicPressure));
        }
    }

    /// <summary>The trajectory must contain enough points to animate smoothly.</summary>
    [Fact]
    public void TrajectoryContainsEnoughPointsToAnimate()
    {
        GravityTurnResult result = Reference(coastSeconds: 200.0);

        Assert.True(result.Samples.Count > 100);
    }

    // ------------------------------------------------------------------------------------
    // Validation
    // ------------------------------------------------------------------------------------

    /// <summary>A vehicle that is all propellant and no structure must be rejected.</summary>
    [Fact]
    public void PropellantMassNotLessThanInitialMassThrows()
    {
        Assert.Throws<ArgumentException>(() => GravityTurn.Ascend(
            initialMass: 1000.0,
            propellantMass: 1000.0,
            vacuumThrust: 50_000.0,
            specificImpulseSeconds: 300.0,
            exitArea: 0.1,
            exitPressure: 50_000.0,
            dragCoefficient: 0.3,
            referenceArea: 1.0,
            pitchOverTime: 5.0,
            pitchOverAngleDegrees: 3.0));
    }

    /// <summary>A vehicle with thrust-to-weight below one must be rejected, not integrated.</summary>
    /// <remarks>
    /// Without this check the integrator would happily produce a trajectory that sinks below
    /// the launch pad, which is numerically valid and physically nonsense.
    /// </remarks>
    [Fact]
    public void InsufficientThrustToLeaveThePadThrows()
    {
        Assert.Throws<ArgumentException>(() => GravityTurn.Ascend(
            initialMass: 50_000.0,
            propellantMass: 40_000.0,
            vacuumThrust: 100_000.0,
            specificImpulseSeconds: 300.0,
            exitArea: 0.8,
            exitPressure: 50_000.0,
            dragCoefficient: 0.3,
            referenceArea: 7.0,
            pitchOverTime: 10.0,
            pitchOverAngleDegrees: 3.0));
    }

    /// <summary>An absurd pitch-over angle must be rejected by the guard.</summary>
    [Fact]
    public void ExcessivePitchOverAngleThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Reference(pitchOverAngleDegrees: 75.0));
    }

    /// <summary>A non-positive time step must be rejected.</summary>
    [Fact]
    public void NonPositiveTimeStepThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Reference(timeStep: 0.0));
    }
}
