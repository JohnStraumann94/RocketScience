using Astro.Core;

namespace Astro.Core.Tests;

/// <summary>
/// Verification tests for Kepler's equation, the universal-variable propagator and
/// Lambert's problem (Chapters 4 and 5).
/// </summary>
public class KeplerAndPropagationTests
{
    /// <summary>
    /// For a circular orbit all three anomalies coincide, so the solver must be an identity.
    /// </summary>
    [Fact]
    public void KeplerSolver_IsIdentityForCircularOrbit()
    {
        double m = Angles.ToRadians(137.0);

        var solution = KeplerSolver.SolveEccentricAnomaly(m, 0.0);

        Assert.Equal(m, solution.AnomalyRad, 12);
    }

    /// <summary>
    /// Round-trip M -> E -> M across a wide range of eccentricities, including the
    /// high-eccentricity regime where Newton's method is most fragile.
    /// </summary>
    [Theory]
    [InlineData(0.01)]
    [InlineData(0.3)]
    [InlineData(0.7)]
    [InlineData(0.9)]
    [InlineData(0.99)]
    [InlineData(0.999)]
    public void MeanToEccentricAnomaly_RoundTrips(double eccentricity)
    {
        for (int degrees = 0; degrees < 360; degrees += 7)
        {
            double m = Angles.ToRadians(degrees);

            var solution = KeplerSolver.SolveEccentricAnomaly(m, eccentricity);
            double recovered = KeplerSolver.EccentricToMeanAnomaly(solution.AnomalyRad, eccentricity);

            Assert.Equal(m, recovered, 9);
            Assert.True(
                solution.Iterations < 60,
                $"Solver needed {solution.Iterations} iterations at e = {eccentricity}, M = {degrees} deg.");
        }
    }

    /// <summary>
    /// Round-trip nu -> E -> nu, which exercises the half-angle quadrant handling.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.4)]
    [InlineData(0.95)]
    public void TrueToEccentricAnomaly_RoundTrips(double eccentricity)
    {
        for (int degrees = 0; degrees < 360; degrees += 11)
        {
            double nu = Angles.ToRadians(degrees);

            double e = KeplerSolver.TrueToEccentricAnomaly(nu, eccentricity);
            double recovered = KeplerSolver.EccentricToTrueAnomaly(e, eccentricity);

            Assert.Equal(nu, recovered, 9);
        }
    }

    /// <summary>
    /// The hyperbolic Kepler equation must round-trip as well.
    /// </summary>
    [Theory]
    [InlineData(1.5, 2.0)]
    [InlineData(3.0, -4.0)]
    [InlineData(1.05, 0.5)]
    public void HyperbolicKepler_RoundTrips(double eccentricity, double meanAnomaly)
    {
        var solution = KeplerSolver.SolveHyperbolicAnomaly(meanAnomaly, eccentricity);

        double recovered = eccentricity * Math.Sinh(solution.AnomalyRad) - solution.AnomalyRad;

        Assert.Equal(meanAnomaly, recovered, 9);
    }

    /// <summary>
    /// Propagating by exactly one orbital period must return the satellite to its start.
    /// </summary>
    [Fact]
    public void Propagate_ByOnePeriod_ReturnsToStartingAnomaly()
    {
        OrbitalElements elements = new(
            10000.0, 0.35,
            Angles.ToRadians(30.0), Angles.ToRadians(40.0), Angles.ToRadians(50.0), Angles.ToRadians(60.0));

        double period = TwoBody.Period(elements.SemiMajorAxisKm);
        OrbitalElements after = KeplerSolver.Propagate(elements, period);

        Assert.Equal(elements.TrueAnomalyDeg, after.TrueAnomalyDeg, 6);
    }

    /// <summary>
    /// The universal-variable propagator and the classical Kepler propagator must agree for
    /// an elliptical orbit. They share no code, so this is a genuine cross-check.
    /// </summary>
    [Fact]
    public void UniversalPropagator_AgreesWithClassicalKeplerPropagator()
    {
        OrbitalElements elements = new(
            9000.0, 0.25,
            Angles.ToRadians(45.0), Angles.ToRadians(80.0), Angles.ToRadians(20.0), Angles.ToRadians(10.0));

        StateVector start = OrbitConversions.ToStateVector(elements);
        const double deltaTime = 3600.0;   // one hour

        StateVector viaUniversal = UniversalVariables.Propagate(start, deltaTime);
        StateVector viaKepler = OrbitConversions.ToStateVector(KeplerSolver.Propagate(elements, deltaTime));

        Assert.Equal(viaKepler.PositionKm.X, viaUniversal.PositionKm.X, 5);
        Assert.Equal(viaKepler.PositionKm.Y, viaUniversal.PositionKm.Y, 5);
        Assert.Equal(viaKepler.PositionKm.Z, viaUniversal.PositionKm.Z, 5);
        Assert.Equal(viaKepler.VelocityKmPerSec.X, viaUniversal.VelocityKmPerSec.X, 8);
    }

    /// <summary>
    /// The universal propagator must handle a hyperbolic trajectory, where the classical
    /// elliptical machinery does not apply at all.
    /// </summary>
    [Fact]
    public void UniversalPropagator_HandlesHyperbolicTrajectory()
    {
        StateVector start = new(
            new Vector3(7000.0, 0.0, 0.0),
            new Vector3(0.0, 12.0, 0.0));

        StateVector after = UniversalVariables.Propagate(start, 3600.0);

        // Energy must be conserved to high precision on the propagated state.
        double energyBefore = TwoBody.SpecificEnergy(start.RadiusKm, start.SpeedKmPerSec);
        double energyAfter = TwoBody.SpecificEnergy(after.RadiusKm, after.SpeedKmPerSec);

        Assert.Equal(energyBefore, energyAfter, 8);
        Assert.True(after.RadiusKm > start.RadiusKm, "A hyperbolic trajectory must keep receding.");
    }

    /// <summary>
    /// Propagating forwards and then backwards by the same interval must be the identity.
    /// </summary>
    [Fact]
    public void UniversalPropagator_IsReversible()
    {
        StateVector start = new(
            new Vector3(-6045.0, -3490.0, 2500.0),
            new Vector3(-3.457, 6.618, 2.533));

        StateVector there = UniversalVariables.Propagate(start, 2700.0);
        StateVector andBack = UniversalVariables.Propagate(there, -2700.0);

        Assert.Equal(start.PositionKm.X, andBack.PositionKm.X, 6);
        Assert.Equal(start.PositionKm.Y, andBack.PositionKm.Y, 6);
        Assert.Equal(start.PositionKm.Z, andBack.PositionKm.Z, 6);
    }

    /// <summary>
    /// The Stumpff functions must match their series values at psi = 0 (the parabolic case)
    /// and join continuously across it.
    /// </summary>
    [Fact]
    public void StumpffFunctions_AreContinuousThroughZero()
    {
        Assert.Equal(0.5, UniversalVariables.StumpffC(0.0), 12);
        Assert.Equal(1.0 / 6.0, UniversalVariables.StumpffS(0.0), 12);

        // Approach zero from both sides; the values must converge on the series result.
        Assert.Equal(UniversalVariables.StumpffC(1e-9), UniversalVariables.StumpffC(-1e-9), 9);
        Assert.Equal(UniversalVariables.StumpffS(1e-9), UniversalVariables.StumpffS(-1e-9), 9);
    }

    /// <summary>
    /// <b>The definitive Lambert test.</b> Take a known orbit, propagate it to obtain two
    /// positions and the exact time between them, then ask the Lambert solver to recover the
    /// connecting velocities. It must return the velocities we started with.
    /// </summary>
    [Fact]
    public void Lambert_RecoversTheVelocitiesOfAKnownTransferArc()
    {
        StateVector start = new(
            new Vector3(5000.0, 10000.0, 2100.0),
            new Vector3(-5.992, 1.925, 3.246));

        const double timeOfFlight = 3600.0;

        StateVector end = UniversalVariables.Propagate(start, timeOfFlight);

        LambertSolution solution = Lambert.Solve(
            start.PositionKm, end.PositionKm, timeOfFlight, TransferDirection.ShortWay);

        Assert.Equal(start.VelocityKmPerSec.X, solution.DepartureVelocityKmPerSec.X, 5);
        Assert.Equal(start.VelocityKmPerSec.Y, solution.DepartureVelocityKmPerSec.Y, 5);
        Assert.Equal(start.VelocityKmPerSec.Z, solution.DepartureVelocityKmPerSec.Z, 5);

        Assert.Equal(end.VelocityKmPerSec.X, solution.ArrivalVelocityKmPerSec.X, 5);
        Assert.Equal(end.VelocityKmPerSec.Y, solution.ArrivalVelocityKmPerSec.Y, 5);
        Assert.Equal(end.VelocityKmPerSec.Z, solution.ArrivalVelocityKmPerSec.Z, 5);
    }

    /// <summary>
    /// Collinear position vectors leave the transfer plane undefined and must be rejected.
    /// </summary>
    [Fact]
    public void Lambert_RejectsCollinearGeometry()
    {
        Vector3 r1 = new(7000.0, 0.0, 0.0);
        Vector3 r2 = new(14000.0, 0.0, 0.0);   // exactly 0 degrees of sweep

        Assert.Throws<ArgumentException>(() => Lambert.Solve(r1, r2, 3600.0));
    }
}
