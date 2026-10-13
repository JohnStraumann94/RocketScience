using Astro.Core;

namespace Astro.Core.Tests;

/// <summary>
/// Verification tests for the state-vector / orbital-element conversions of Chapter 2.
/// </summary>
public class OrbitConversionTests
{
    /// <summary>
    /// The standard worked example from Vallado, <i>Fundamentals of Astrodynamics and
    /// Applications</i>, Example 2-5 — the canonical regression test for any rv2coe routine.
    /// </summary>
    [Fact]
    public void ToOrbitalElements_MatchesValladoExample2_5()
    {
        StateVector state = new(
            new Vector3(6524.834, 6862.875, 6448.296),
            new Vector3(4.901327, 5.533756, -1.976341));

        OrbitalElements elements = OrbitConversions.ToOrbitalElements(state);

        Assert.Equal(36127.343, elements.SemiMajorAxisKm, 1);
        Assert.Equal(0.832853, elements.Eccentricity, 5);
        Assert.Equal(87.870, elements.InclinationDeg, 2);
        Assert.Equal(227.898, elements.RaanDeg, 2);
        Assert.Equal(53.38, elements.ArgumentOfPeriapsisDeg, 1);
        Assert.Equal(92.335, elements.TrueAnomalyDeg, 2);
        Assert.Equal(11067.790, elements.SemiLatusRectumKm, 1);
    }

    /// <summary>
    /// Converting elements to a state vector and back must return the original elements.
    /// This exercises every quadrant check in the algorithm.
    /// </summary>
    [Theory]
    [InlineData(8000.0, 0.10, 28.5, 45.0, 30.0, 120.0)]
    [InlineData(26600.0, 0.74, 63.4, 250.0, 270.0, 10.0)]   // Molniya-like
    [InlineData(7000.0, 0.001, 98.6, 15.0, 200.0, 300.0)]   // sun-synchronous-like
    [InlineData(9000.0, 0.30, 120.0, 340.0, 95.0, 200.0)]   // retrograde
    public void ElementsToStateAndBack_RoundTrips(
        double a, double e, double iDeg, double raanDeg, double argpDeg, double nuDeg)
    {
        OrbitalElements original = new(
            a, e,
            Angles.ToRadians(iDeg),
            Angles.ToRadians(raanDeg),
            Angles.ToRadians(argpDeg),
            Angles.ToRadians(nuDeg));

        StateVector state = OrbitConversions.ToStateVector(original);
        OrbitalElements recovered = OrbitConversions.ToOrbitalElements(state);

        Assert.Equal(original.SemiMajorAxisKm, recovered.SemiMajorAxisKm, 6);
        Assert.Equal(original.Eccentricity, recovered.Eccentricity, 10);
        Assert.Equal(original.InclinationDeg, recovered.InclinationDeg, 9);
        Assert.Equal(original.RaanDeg, recovered.RaanDeg, 9);
        Assert.Equal(original.ArgumentOfPeriapsisDeg, recovered.ArgumentOfPeriapsisDeg, 9);
        Assert.Equal(original.TrueAnomalyDeg, recovered.TrueAnomalyDeg, 9);
    }

    /// <summary>
    /// A hyperbolic state must be reported with a negative semi-major axis and e &gt; 1.
    /// </summary>
    [Fact]
    public void ToOrbitalElements_ClassifiesHyperbolicOrbit()
    {
        // Circular speed at 7000 km is ~7.546 km/s; 12 km/s is well above escape (~10.67).
        StateVector state = new(
            new Vector3(7000.0, 0.0, 0.0),
            new Vector3(0.0, 12.0, 0.0));

        OrbitalElements elements = OrbitConversions.ToOrbitalElements(state);

        Assert.Equal(OrbitType.Hyperbolic, elements.Type);
        Assert.True(elements.Eccentricity > 1.0);
        Assert.True(elements.SemiMajorAxisKm < 0.0);
        Assert.Equal(double.PositiveInfinity, elements.ApoapsisRadiusKm);
    }

    /// <summary>
    /// A circular equatorial orbit is the hardest degenerate case: both the node vector and
    /// the eccentricity vector vanish. The conversion must still round-trip.
    /// </summary>
    [Fact]
    public void CircularEquatorialOrbit_RoundTripsThroughTheDegenerateBranch()
    {
        const double radius = 42164.0;
        double v = TwoBody.CircularSpeed(radius);

        StateVector state = new(
            new Vector3(0.0, radius, 0.0),
            new Vector3(-v, 0.0, 0.0));

        OrbitalElements elements = OrbitConversions.ToOrbitalElements(state);

        Assert.Equal(OrbitType.Circular, elements.Type);
        Assert.Equal(radius, elements.SemiMajorAxisKm, 6);

        StateVector recovered = OrbitConversions.ToStateVector(elements);

        Assert.Equal(state.PositionKm.X, recovered.PositionKm.X, 6);
        Assert.Equal(state.PositionKm.Y, recovered.PositionKm.Y, 6);
        Assert.Equal(state.VelocityKmPerSec.X, recovered.VelocityKmPerSec.X, 9);
    }

    /// <summary>
    /// A purely radial trajectory has no orbital plane, so element extraction must fail
    /// loudly rather than return nonsense.
    /// </summary>
    [Fact]
    public void ToOrbitalElements_RejectsRectilinearTrajectory()
    {
        StateVector state = new(
            new Vector3(7000.0, 0.0, 0.0),
            new Vector3(1.0, 0.0, 0.0));   // velocity parallel to position

        Assert.Throws<ArgumentException>(() => OrbitConversions.ToOrbitalElements(state));
    }
}
