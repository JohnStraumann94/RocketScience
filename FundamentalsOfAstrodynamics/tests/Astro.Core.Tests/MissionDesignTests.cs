using Astro.Core;

namespace Astro.Core.Tests;

/// <summary>
/// Verification tests for orbital manoeuvres, ballistic trajectories, lunar and
/// interplanetary transfers, and J2 perturbations (Chapters 3, 6, 7, 8 and 9).
/// </summary>
public class MissionDesignTests
{
    /// <summary>
    /// The textbook LEO-to-GEO Hohmann transfer: 6,678 km to 42,164 km costs about
    /// 3.89 km/s in two burns and takes roughly 5.25 hours.
    /// </summary>
    [Fact]
    public void HohmannTransfer_LeoToGeo_MatchesTextbookBudget()
    {
        TransferResult result = Maneuvers.HohmannTransfer(6678.0, 42164.0);

        Assert.Equal(2.4256, result.BurnsKmPerSec[0], 3);
        Assert.Equal(1.4668, result.BurnsKmPerSec[1], 3);
        Assert.Equal(3.8926, result.TotalDeltaVKmPerSec, 3);
        Assert.Equal(5.275, result.TransferTimeHours, 2);
    }

    /// <summary>
    /// Lowering an orbit must cost exactly the same as raising it (both burns simply
    /// reverse sign), which is a useful symmetry check on the sign conventions.
    /// </summary>
    [Fact]
    public void HohmannTransfer_IsSymmetricUnderReversal()
    {
        TransferResult up = Maneuvers.HohmannTransfer(6678.0, 42164.0);
        TransferResult down = Maneuvers.HohmannTransfer(42164.0, 6678.0);

        Assert.Equal(up.TotalDeltaVKmPerSec, down.TotalDeltaVKmPerSec, 10);
        Assert.Equal(up.TransferTimeSec, down.TransferTimeSec, 6);
        Assert.True(down.BurnsKmPerSec[0] < 0.0, "Lowering an orbit must start with a retrograde burn.");
    }

    /// <summary>
    /// Below the break-even radius ratio (~11.94) Hohmann must beat bi-elliptic; well above
    /// it, bi-elliptic must win. This is the headline result of Chapter 3.
    /// </summary>
    [Fact]
    public void BiElliptic_BeatsHohmann_OnlyAboveTheBreakEvenRatio()
    {
        const double r1 = 7000.0;

        // Ratio of 10 -- below break-even, so Hohmann should win.
        double lowRatioHohmann = Maneuvers.HohmannTransfer(r1, r1 * 10.0).TotalDeltaVKmPerSec;
        double lowRatioBiElliptic = Maneuvers.BiEllipticTransfer(r1, r1 * 60.0, r1 * 10.0).TotalDeltaVKmPerSec;
        Assert.True(
            lowRatioHohmann < lowRatioBiElliptic,
            $"At a ratio of 10 Hohmann ({lowRatioHohmann:F4}) should beat bi-elliptic ({lowRatioBiElliptic:F4}).");

        // Ratio of 20 -- above break-even, so a distant bi-elliptic should win.
        double highRatioHohmann = Maneuvers.HohmannTransfer(r1, r1 * 20.0).TotalDeltaVKmPerSec;
        double highRatioBiElliptic = Maneuvers.BiEllipticTransfer(r1, r1 * 400.0, r1 * 20.0).TotalDeltaVKmPerSec;
        Assert.True(
            highRatioBiElliptic < highRatioHohmann,
            $"At a ratio of 20 bi-elliptic ({highRatioBiElliptic:F4}) should beat Hohmann ({highRatioHohmann:F4}).");
    }

    /// <summary>
    /// A 60-degree plane change costs exactly one orbital speed, because 2 sin(30 deg) = 1.
    /// </summary>
    [Fact]
    public void SimplePlaneChange_OfSixtyDegrees_CostsOneOrbitalSpeed()
    {
        const double speed = 7.7258;

        double deltaV = Maneuvers.SimplePlaneChange(speed, Angles.ToRadians(60.0));

        Assert.Equal(speed, deltaV, 9);
    }

    /// <summary>
    /// A combined manoeuvre must always be cheaper than performing the speed change and the
    /// plane change as two separate burns.
    /// </summary>
    [Fact]
    public void CombinedManeuver_IsCheaperThanTwoSeparateBurns()
    {
        const double vInitial = 1.6079;   // GTO apogee speed
        const double vFinal = 3.0747;     // GEO circular speed
        double planeChange = Angles.ToRadians(28.5);

        double combined = Maneuvers.CombinedManeuver(vInitial, vFinal, planeChange);
        double separate = Math.Abs(vFinal - vInitial) + Maneuvers.SimplePlaneChange(vFinal, planeChange);

        Assert.True(combined < separate, $"Combined ({combined:F4}) must beat separate ({separate:F4}).");
        Assert.Equal(1.8302, combined, 3);
    }

    /// <summary>
    /// For a short-range shot (Psi -> 0) the optimal loft angle is the familiar 45 degrees.
    /// </summary>
    [Fact]
    public void OptimalFlightPathAngle_TendsToFortyFiveDegreesAtShortRange()
    {
        Assert.Equal(45.0, Angles.ToDegrees(BallisticMissile.OptimalFlightPathAngle(0.0)), 9);
    }

    /// <summary>
    /// A ballistic trajectory flown at the optimal angle for its range must be internally
    /// consistent: feeding the minimum burnout speed back in must reproduce the range.
    /// </summary>
    [Fact]
    public void BallisticMissile_MinimumSpeedAndOptimalAngle_ReproduceTheRange()
    {
        double targetRangeAngle = Angles.ToRadians(60.0);
        const double burnoutRadius = Constants.EarthRadiusKm + 100.0;

        double speed = BallisticMissile.MinimumBurnoutSpeed(targetRangeAngle, burnoutRadius);
        double angle = BallisticMissile.OptimalFlightPathAngle(targetRangeAngle);

        BallisticTrajectory trajectory = BallisticMissile.Compute(burnoutRadius, speed, angle);

        Assert.Equal(
            Angles.ToDegrees(targetRangeAngle),
            trajectory.FreeFlightRangeAngleDeg,
            6);
        Assert.True(trajectory.Eccentricity < 1.0, "A ballistic trajectory must remain an ellipse.");
        Assert.True(trajectory.QParameter < 2.0, "Q must stay below the escape value of 2.");
    }

    /// <summary>
    /// The Moon's sphere of influence is about 66,200 km — roughly one sixth of the
    /// Earth-Moon distance.
    /// </summary>
    [Fact]
    public void MoonSphereOfInfluence_IsAboutSixtySixThousandKilometres()
    {
        double soi = LunarTrajectory.MoonSphereOfInfluenceKm;

        Assert.InRange(soi, 66000.0, 66400.0);
    }

    /// <summary>
    /// A trans-lunar injection from a 185 km parking orbit costs a little over 3.1 km/s and
    /// takes about five days on a minimum-energy ellipse — the Apollo ballpark.
    /// </summary>
    [Fact]
    public void LunarTransfer_MatchesTheApolloBallpark()
    {
        double parkingRadius = Constants.EarthRadiusKm + 185.0;
        double arrivalRadius = Constants.EarthMoonDistanceKm - LunarTrajectory.MoonSphereOfInfluenceKm;

        var leg = LunarTrajectory.ComputeGeocentricLeg(parkingRadius, arrivalRadius);

        Assert.InRange(leg.InjectionDeltaVKmPerSec, 3.0, 3.2);
        // A minimum-energy (Hohmann-like) lunar transfer coasts for roughly 3.8 days;
        // Apollo used a faster, higher-energy trajectory of about 3 days.
        Assert.InRange(leg.TimeOfFlightDays, 3.0, 6.0);
        Assert.True(leg.TransferEccentricity is > 0.9 and < 1.0);
    }

    /// <summary>
    /// The Earth-Mars synodic period is about 780 days, which is why Mars launch windows
    /// open roughly every 26 months.
    /// </summary>
    [Fact]
    public void EarthMarsSynodicPeriod_IsAboutSevenHundredAndEightyDays()
    {
        double earthPeriod = TwoBody.Period(Constants.AstronomicalUnitKm, Constants.MuSun);
        double marsPeriod = TwoBody.Period(1.523679 * Constants.AstronomicalUnitKm, Constants.MuSun);

        double synodic = Interplanetary.SynodicPeriod(earthPeriod, marsPeriod) / Constants.SolarDaySeconds;

        Assert.InRange(synodic, 770.0, 790.0);
    }

    /// <summary>
    /// The classic Earth-to-Mars Hohmann transfer: about 259 days of flight, a departure
    /// C3 near 8.7 km^2/s^2, and a required Mars lead angle of roughly 44 degrees.
    /// </summary>
    [Fact]
    public void EarthToMarsHohmann_MatchesTheStandardMissionNumbers()
    {
        var transfer = Interplanetary.ComputeHohmannTransfer(
            departureOrbitRadiusKm: Constants.AstronomicalUnitKm,
            arrivalOrbitRadiusKm: 1.523679 * Constants.AstronomicalUnitKm,
            departurePlanetMu: Constants.MuEarth,
            departureParkingRadiusKm: Constants.EarthRadiusKm + 200.0,
            arrivalPlanetMu: Constants.MuMars,
            arrivalParkingRadiusKm: Constants.MarsRadiusKm + 500.0);

        Assert.InRange(transfer.TransferTimeDays, 255.0, 262.0);
        Assert.InRange(transfer.DepartureVInfinityKmPerSec, 2.8, 3.1);
        Assert.InRange(transfer.DepartureC3, 8.0, 9.5);
        Assert.InRange(transfer.DeparturePhaseAngleDeg, 40.0, 48.0);
        Assert.InRange(transfer.SynodicPeriodDays, 770.0, 790.0);
    }

    /// <summary>
    /// A gravity assist turns the v_infinity vector without changing its magnitude; the
    /// turning angle must fall as the flyby gets faster (higher eccentricity).
    /// </summary>
    [Fact]
    public void FlybyTurningAngle_DecreasesWithEccentricity()
    {
        double slowFlyby = Interplanetary.FlybyTurningAngle(1.2);
        double fastFlyby = Interplanetary.FlybyTurningAngle(5.0);

        Assert.True(slowFlyby > fastFlyby);
        Assert.Equal(180.0, Angles.ToDegrees(Interplanetary.FlybyTurningAngle(1.0 + 1e-15)), 3);
    }

    /// <summary>
    /// The critical inclination at which perigee stops moving is 63.435 degrees — the
    /// inclination used by every Molniya satellite ever flown.
    /// </summary>
    [Fact]
    public void CriticalInclination_IsSixtyThreePointFourDegrees()
    {
        double critical = Angles.ToDegrees(Perturbations.CriticalInclinationRad);

        Assert.Equal(63.4349, critical, 3);

        // At exactly that inclination the apsidal rotation rate must vanish.
        double rate = Perturbations.ApsidalRotationRate(26600.0, 0.74, Perturbations.CriticalInclinationRad);
        Assert.Equal(0.0, rate, 15);
    }

    /// <summary>
    /// A sun-synchronous orbit at 800 km altitude requires an inclination of about
    /// 98.6 degrees — retrograde, as the theory demands.
    /// </summary>
    [Fact]
    public void SunSynchronousInclination_At800Km_IsAboutNinetyEightPointSixDegrees()
    {
        double inclination = Angles.ToDegrees(
            Perturbations.SunSynchronousInclination(Constants.EarthRadiusKm + 800.0));

        Assert.InRange(inclination, 98.4, 98.8);
    }

    /// <summary>
    /// A prograde low Earth orbit must regress its node westward by several degrees per day.
    /// </summary>
    [Fact]
    public void NodalRegression_IsWestwardForAProgradeOrbit()
    {
        double ratePerSecond = Perturbations.NodalRegressionRate(
            Constants.EarthRadiusKm + 400.0, 0.0, Angles.ToRadians(51.6));   // ISS-like

        double degreesPerDay = Angles.ToDegrees(ratePerSecond) * Constants.SolarDaySeconds;

        Assert.True(degreesPerDay < 0.0, "A prograde orbit's node must drift westward.");
        Assert.InRange(degreesPerDay, -6.0, -4.0);
    }

    /// <summary>
    /// A polar orbit experiences no nodal regression at all, because cos(90 deg) = 0.
    /// </summary>
    [Fact]
    public void NodalRegression_VanishesForAPolarOrbit()
    {
        double rate = Perturbations.NodalRegressionRate(7000.0, 0.0, Math.PI / 2.0);

        Assert.Equal(0.0, rate, 15);
    }
}
