using Astro.Core;

namespace Astro.Core.Tests;

/// <summary>
/// Verification tests for the two-body fundamentals of Chapter 1.
/// </summary>
public class TwoBodyTests
{
    /// <summary>
    /// A satellite in a circular orbit at the Earth's surface should travel at
    /// approximately 7.905 km/s — the canonical "1 DU/TU" speed used throughout BMW.
    /// </summary>
    [Fact]
    public void CircularSpeedAtEarthSurface_MatchesCanonicalUnitSpeed()
    {
        double v = TwoBody.CircularSpeed(Constants.EarthRadiusKm);

        Assert.Equal(7.905, v, 3);
    }

    /// <summary>
    /// Escape speed must be exactly sqrt(2) times circular speed at the same radius.
    /// </summary>
    [Fact]
    public void EscapeSpeed_IsSqrtTwoTimesCircularSpeed()
    {
        const double radius = 6678.137;   // 300 km altitude

        double circular = TwoBody.CircularSpeed(radius);
        double escape = TwoBody.EscapeSpeed(radius);

        Assert.Equal(Math.Sqrt(2.0) * circular, escape, 10);
    }

    /// <summary>
    /// For a circular orbit (r = a) the vis-viva equation must reduce to sqrt(mu/r).
    /// </summary>
    [Fact]
    public void VisViva_ReducesToCircularSpeedWhenRadiusEqualsSemiMajorAxis()
    {
        const double radius = 42164.0;   // geostationary

        Assert.Equal(
            TwoBody.CircularSpeed(radius),
            TwoBody.VisViva(radius, radius),
            10);
    }

    /// <summary>
    /// The geostationary orbit is defined by having a period of exactly one sidereal day,
    /// which puts it at a radius of about 42,164 km.
    /// </summary>
    [Fact]
    public void Period_AtGeostationaryRadius_IsOneSiderealDay()
    {
        double period = TwoBody.Period(42164.17);

        Assert.Equal(Constants.SiderealDaySeconds, period, 0);
    }

    /// <summary>
    /// Specific energy must be negative for a bound orbit and its inverse relation to the
    /// semi-major axis must round-trip.
    /// </summary>
    [Fact]
    public void SpecificEnergy_AndSemiMajorAxis_RoundTrip()
    {
        const double radius = 7000.0;
        const double semiMajorAxis = 8000.0;

        double speed = TwoBody.VisViva(radius, semiMajorAxis);
        double energy = TwoBody.SpecificEnergy(radius, speed);

        Assert.True(energy < 0.0, "A bound elliptical orbit must have negative specific energy.");
        Assert.Equal(semiMajorAxis, TwoBody.SemiMajorAxisFromEnergy(energy), 8);
    }

    /// <summary>
    /// The flight-path angle is zero at periapsis and apoapsis, where motion is horizontal.
    /// </summary>
    [Theory]
    [InlineData(0.0)]          // periapsis
    [InlineData(Math.PI)]      // apoapsis
    public void FlightPathAngle_IsZeroAtApsides(double trueAnomaly)
    {
        Assert.Equal(0.0, TwoBody.FlightPathAngle(0.3, trueAnomaly), 12);
    }

    /// <summary>
    /// Requesting a radius that lies outside the orbit must be rejected rather than
    /// silently returning NaN.
    /// </summary>
    [Fact]
    public void VisViva_ThrowsWhenRadiusExceedsApoapsis()
    {
        // a = 7000 km, so the maximum possible radius is 2a = 14,000 km even for e -> 1.
        Assert.Throws<ArgumentOutOfRangeException>(() => TwoBody.VisViva(20000.0, 7000.0));
    }
}
