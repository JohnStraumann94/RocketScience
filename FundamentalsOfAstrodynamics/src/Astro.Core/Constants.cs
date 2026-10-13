namespace Astro.Core;

/// <summary>
/// Physical, astronomical and gravitational constants used throughout the library.
/// </summary>
/// <remarks>
/// <para>
/// <b>Unit convention.</b> Bate, Mueller and White (<i>Fundamentals of Astrodynamics</i>,
/// Dover, 1971 — hereafter "BMW") develop most of the two-body problem in "canonical"
/// units (Earth radii and time units). Canonical units are elegant on paper but a
/// constant source of bugs in software, so this library standardises on a single
/// concrete SI-derived system:
/// </para>
/// <list type="bullet">
///   <item><description>length ....... kilometres (km)</description></item>
///   <item><description>time ......... seconds (s)</description></item>
///   <item><description>speed ........ km/s</description></item>
///   <item><description>angles ....... radians internally, degrees at the API/UI boundary</description></item>
///   <item><description>gravitational parameter mu ... km^3/s^2</description></item>
/// </list>
/// <para>
/// Canonical conversions are still provided (<see cref="EarthDistanceUnitKm"/> and
/// <see cref="EarthTimeUnitSeconds"/>) so that numerical examples printed in BMW can be
/// reproduced and cross-checked against this code.
/// </para>
/// </remarks>
public static class Constants
{
    // ---------------------------------------------------------------------
    // Gravitational parameters, mu = G * M  [km^3 / s^2]
    //
    // NOTE: mu is known to far greater precision than either G or M alone,
    // because it is what actually gets measured by tracking spacecraft.
    // That is why astrodynamics always works with the product and never with
    // G and M separately.
    // ---------------------------------------------------------------------

    /// <summary>Gravitational parameter of the Sun, mu_Sun [km^3/s^2].</summary>
    public const double MuSun = 1.32712440018e11;

    /// <summary>Gravitational parameter of the Earth, mu_Earth [km^3/s^2] (EGM-96 value).</summary>
    public const double MuEarth = 398600.4418;

    /// <summary>Gravitational parameter of the Moon, mu_Moon [km^3/s^2].</summary>
    public const double MuMoon = 4902.800066;

    /// <summary>Gravitational parameter of Mars, mu_Mars [km^3/s^2].</summary>
    public const double MuMars = 42828.375214;

    /// <summary>Gravitational parameter of Venus, mu_Venus [km^3/s^2].</summary>
    public const double MuVenus = 324858.592;

    /// <summary>Gravitational parameter of Jupiter (system), mu_Jupiter [km^3/s^2].</summary>
    public const double MuJupiter = 1.26686534e8;

    // ---------------------------------------------------------------------
    // Body radii [km]
    // ---------------------------------------------------------------------

    /// <summary>Earth mean equatorial radius [km] (WGS-84).</summary>
    public const double EarthRadiusKm = 6378.137;

    /// <summary>Moon mean radius [km].</summary>
    public const double MoonRadiusKm = 1737.4;

    /// <summary>Mars mean equatorial radius [km].</summary>
    public const double MarsRadiusKm = 3396.19;

    /// <summary>Sun mean radius [km].</summary>
    public const double SunRadiusKm = 695700.0;

    // ---------------------------------------------------------------------
    // Earth shape / rotation
    // ---------------------------------------------------------------------

    /// <summary>
    /// Second zonal harmonic (oblateness) coefficient J2 of the Earth [dimensionless].
    /// This single term dominates every other non-spherical gravity perturbation by
    /// roughly three orders of magnitude and is the basis of the secular node and
    /// perigee drift formulas in <see cref="Perturbations"/> (BMW Chapter 9).
    /// </summary>
    public const double J2Earth = 1.08262668e-3;

    /// <summary>Earth inertial rotation rate [rad/s] (one sidereal day = 86164.0905 s).</summary>
    public const double EarthRotationRateRadPerSec = 7.2921159e-5;

    /// <summary>Length of a sidereal day [s].</summary>
    public const double SiderealDaySeconds = 86164.0905;

    /// <summary>Length of a mean solar day [s].</summary>
    public const double SolarDaySeconds = 86400.0;

    // ---------------------------------------------------------------------
    // Orbital radii / periods of the major bodies (circular-coplanar approximation).
    // These are deliberately simple mean values: BMW's interplanetary chapter
    // models the planets as coplanar circular orbits, and so do we.
    // ---------------------------------------------------------------------

    /// <summary>Astronomical unit [km] — the IAU 2012 exact definition.</summary>
    public const double AstronomicalUnitKm = 1.495978707e8;

    /// <summary>Mean Earth–Moon distance [km].</summary>
    public const double EarthMoonDistanceKm = 384400.0;

    /// <summary>Moon sidereal orbital period about the Earth [s] (27.321661 days).</summary>
    public const double MoonSiderealPeriodSeconds = 27.321661 * SolarDaySeconds;

    // ---------------------------------------------------------------------
    // Canonical (BMW) unit conversions
    // ---------------------------------------------------------------------

    /// <summary>
    /// One Earth canonical distance unit (DU) expressed in km. In BMW, 1 DU is defined
    /// as the Earth's equatorial radius, so a circular orbit "at the surface" has r = 1 DU.
    /// </summary>
    public const double EarthDistanceUnitKm = EarthRadiusKm;

    /// <summary>
    /// One Earth canonical time unit (TU) expressed in seconds. It is defined such that
    /// mu = 1 DU^3/TU^2, i.e. TU = sqrt(DU^3 / mu) ≈ 806.81 s. A satellite in a circular
    /// orbit of 1 DU therefore has a speed of exactly 1 DU/TU ≈ 7.905 km/s.
    /// </summary>
    public static readonly double EarthTimeUnitSeconds =
        Math.Sqrt(EarthDistanceUnitKm * EarthDistanceUnitKm * EarthDistanceUnitKm / MuEarth);

    // ---------------------------------------------------------------------
    // Handy angle conversions. (System.Double.DegreesToRadians exists in .NET 7+,
    // but explicit constants keep the intent obvious in the formulas below.)
    // ---------------------------------------------------------------------

    /// <summary>Multiply degrees by this to obtain radians.</summary>
    public const double DegToRad = Math.PI / 180.0;

    /// <summary>Multiply radians by this to obtain degrees.</summary>
    public const double RadToDeg = 180.0 / Math.PI;

    /// <summary>Two pi — one full revolution in radians.</summary>
    public const double TwoPi = 2.0 * Math.PI;
}
