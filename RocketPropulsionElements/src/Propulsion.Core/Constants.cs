namespace Propulsion.Core;

/// <summary>
/// Physical constants and unit conversions used throughout the propulsion library.
/// </summary>
/// <remarks>
/// <para>
/// Every quantity in this library is in SI base units unless the member name says otherwise:
/// metres, kilograms, seconds, newtons, pascals and kelvin. Sutton's <i>Rocket Propulsion
/// Elements</i> mixes SI and US customary units freely; keeping the library strictly SI and
/// converting only at the edges avoids the single most common class of bug in propulsion code.
/// </para>
/// <para>
/// The one place where the choice of unit is genuinely load-bearing is specific impulse. Isp is
/// quoted in seconds, and that number only means anything alongside a stated value of standard
/// gravity, because Isp = c / g0. Everyone uses the same g0 = 9.80665 m/s^2 by convention, even
/// for a vehicle that never goes near Earth, so that Isp values are comparable between engines.
/// </para>
/// </remarks>
public static class Constants
{
    /// <summary>
    /// Standard gravitational acceleration, 9.80665 m/s^2.
    /// </summary>
    /// <remarks>
    /// This is a <i>defined</i> constant, not a measurement, and it is not the local gravity at
    /// any particular place. It exists solely to convert between effective exhaust velocity
    /// (m/s) and specific impulse (s). Using 9.81 instead introduces a 0.03% error in every
    /// Isp figure the library produces, which is enough to make textbook comparisons annoying.
    /// </remarks>
    public const double StandardGravity = 9.80665;

    /// <summary>Universal gas constant, 8314.462618 J/(kmol*K).</summary>
    /// <remarks>
    /// Expressed per kilomole rather than per mole because combustion-gas molecular masses in
    /// the propulsion literature are always quoted in kg/kmol (e.g. 22 kg/kmol for LOX/LH2
    /// exhaust). R_specific = UniversalGasConstant / M then comes out directly in J/(kg*K).
    /// </remarks>
    public const double UniversalGasConstant = 8314.462618;

    /// <summary>Standard sea-level atmospheric pressure, 101325 Pa.</summary>
    /// <remarks>
    /// Used as the ambient back-pressure for sea-level thrust figures. The difference between a
    /// sea-level and a vacuum Isp quote is entirely the p_a * A_e pressure term.
    /// </remarks>
    public const double SeaLevelPressure = 101_325.0;

    /// <summary>Standard sea-level air density, 1.225 kg/m^3.</summary>
    public const double SeaLevelDensity = 1.225;

    /// <summary>Standard sea-level temperature, 288.15 K.</summary>
    public const double SeaLevelTemperature = 288.15;

    /// <summary>Scale height of a simple exponential atmosphere model, 7200 m.</summary>
    /// <remarks>
    /// A single-exponential atmosphere is crude above about 20 km, but it is the model Sutton
    /// uses for first-cut drag-loss estimates and it is entirely adequate for showing how
    /// ambient pressure collapses the pressure-thrust term during a launch.
    /// </remarks>
    public const double AtmosphericScaleHeight = 7200.0;

    /// <summary>Earth's equatorial radius in metres.</summary>
    public const double EarthRadius = 6_378_137.0;

    /// <summary>Earth's gravitational parameter in m^3/s^2.</summary>
    public const double EarthMu = 3.986004418e14;

    /// <summary>
    /// Multiplier converting an angle in degrees to radians (pi / 180).
    /// </summary>
    /// <remarks>
    /// Every angle in the public API of this library is expressed in <b>degrees</b>, because
    /// that is how nozzle half-angles, flight path angles and cone angles are quoted in
    /// Sutton and on real engineering drawings. These two constants exist so that the
    /// conversion to and from the radians that <see cref="System.Math"/> requires is done in
    /// exactly one place, spelled the same way everywhere.
    /// </remarks>
    public const double DegToRad = Math.PI / 180.0;

    /// <summary>
    /// Multiplier converting an angle in radians to degrees (180 / pi).
    /// </summary>
    public const double RadToDeg = 180.0 / Math.PI;

    /// <summary>Converts pascals to bar.</summary>
    /// <param name="pascals">Pressure in Pa.</param>
    /// <returns>The same pressure in bar.</returns>
    public static double PaToBar(double pascals) => pascals / 100_000.0;

    /// <summary>Converts bar to pascals.</summary>
    /// <param name="bar">Pressure in bar.</param>
    /// <returns>The same pressure in Pa.</returns>
    public static double BarToPa(double bar) => bar * 100_000.0;

    /// <summary>Converts pascals to pounds per square inch.</summary>
    /// <param name="pascals">Pressure in Pa.</param>
    /// <returns>The same pressure in psi.</returns>
    public static double PaToPsi(double pascals) => pascals / 6894.757293168;

    /// <summary>Converts pounds per square inch to pascals.</summary>
    /// <param name="psi">Pressure in psi.</param>
    /// <returns>The same pressure in Pa.</returns>
    public static double PsiToPa(double psi) => psi * 6894.757293168;

    /// <summary>Converts newtons to pounds-force.</summary>
    /// <param name="newtons">Force in N.</param>
    /// <returns>The same force in lbf.</returns>
    public static double NewtonsToPoundsForce(double newtons) => newtons / 4.4482216152605;

    /// <summary>Converts pounds-force to newtons.</summary>
    /// <param name="poundsForce">Force in lbf.</param>
    /// <returns>The same force in N.</returns>
    public static double PoundsForceToNewtons(double poundsForce) => poundsForce * 4.4482216152605;
}
