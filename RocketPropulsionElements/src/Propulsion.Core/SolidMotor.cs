namespace Propulsion.Core;

/// <summary>
/// Solid propellant rocket motors: burn rate law, chamber pressure equilibrium and grain
/// geometry. Sutton Chapters 11 and 12.
/// </summary>
/// <remarks>
/// <para>
/// A solid motor has no pumps, no valves and no plumbing. Everything is decided by one balance:
/// the rate at which the burning grain generates gas versus the rate at which the throat lets it
/// out. Where those two curves cross is the chamber pressure, and it settles there within
/// milliseconds of ignition.
/// </para>
/// <para>
/// The consequence is that all design freedom lives in the geometry of the grain. The shape of
/// the burning surface as it recedes is what shapes the entire thrust-time curve, and it is cast
/// in at manufacture. There is no throttle and, for most designs, no off switch.
/// </para>
/// </remarks>
public static class SolidMotor
{
    /// <summary>
    /// Saint-Robert's burn rate law, r = a * p^n.
    /// </summary>
    /// <param name="burnRateCoefficient">
    /// The coefficient a, in units that give metres per second when pressure is in pascals.
    /// It is strongly temperature dependent - a cold motor burns measurably slower.
    /// </param>
    /// <param name="chamberPressure">Chamber pressure p, Pa.</param>
    /// <param name="pressureExponent">The burn rate pressure exponent n.</param>
    /// <returns>Linear burn rate in m/s.</returns>
    /// <remarks>
    /// <para>
    /// The surface regresses perpendicular to itself at this rate - typically 5-20 mm/s, which
    /// is why a large booster needs an enormous burning surface area to feed its throat.
    /// </para>
    /// <para>
    /// The exponent n is the single most safety-critical number in solid propulsion, and the
    /// reason is <see cref="IsStable"/>: n must stay below 1. Practical propellants sit between
    /// 0.2 and 0.6.
    /// </para>
    /// </remarks>
    public static double BurnRate(
        double burnRateCoefficient,
        double chamberPressure,
        double pressureExponent)
    {
        Guard.Positive(burnRateCoefficient, nameof(burnRateCoefficient));
        Guard.Positive(chamberPressure, nameof(chamberPressure));
        Guard.InRange(pressureExponent, 0.0, 2.0, nameof(pressureExponent));

        return burnRateCoefficient * Math.Pow(chamberPressure, pressureExponent);
    }

    /// <summary>
    /// Klemmung K, the ratio of burning surface area to throat area.
    /// </summary>
    /// <param name="burningSurfaceArea">Instantaneous burning surface area A_b, m^2.</param>
    /// <param name="throatArea">Throat area A_t, m^2.</param>
    /// <returns>The dimensionless ratio K = A_b / A_t.</returns>
    /// <remarks>
    /// The master design variable of a solid motor. It typically runs from 200 to 1000, and
    /// because chamber pressure goes as K^(1/(1-n)), a modest change in K moves the pressure a
    /// great deal. As the grain burns, K changes with the geometry, and that variation is what
    /// produces a progressive, neutral or regressive thrust curve.
    /// </remarks>
    public static double Klemmung(double burningSurfaceArea, double throatArea)
    {
        Guard.Positive(burningSurfaceArea, nameof(burningSurfaceArea));
        Guard.Positive(throatArea, nameof(throatArea));

        return burningSurfaceArea / throatArea;
    }

    /// <summary>
    /// Equilibrium chamber pressure of a solid motor.
    /// </summary>
    /// <param name="burningSurfaceArea">Burning surface area A_b, m^2.</param>
    /// <param name="throatArea">Throat area A_t, m^2.</param>
    /// <param name="propellantDensity">Solid propellant density, kg/m^3.</param>
    /// <param name="characteristicVelocity">Characteristic velocity c*, m/s.</param>
    /// <param name="burnRateCoefficient">Burn rate coefficient a.</param>
    /// <param name="pressureExponent">Burn rate pressure exponent n.</param>
    /// <returns>Equilibrium chamber pressure in Pa.</returns>
    /// <remarks>
    /// <para>
    /// Equate gas generation with gas discharge:
    /// </para>
    /// <code>
    ///     A_b * rho_p * r  =  p1 * A_t / c*
    /// </code>
    /// <para>
    /// Substitute r = a*p^n and solve for p, giving
    /// </para>
    /// <code>
    ///     p1 = (K * rho_p * a * c*) ^ (1 / (1 - n))
    /// </code>
    /// <para>
    /// The exponent 1/(1-n) is where all the drama lies. At n = 0.35 it is 1.54, so a 10%
    /// increase in burning area raises chamber pressure by about 16%. As n approaches 1 the
    /// exponent runs away to infinity, which is the mathematical signature of the instability
    /// described in <see cref="IsStable"/>.
    /// </para>
    /// </remarks>
    public static double EquilibriumChamberPressure(
        double burningSurfaceArea,
        double throatArea,
        double propellantDensity,
        double characteristicVelocity,
        double burnRateCoefficient,
        double pressureExponent)
    {
        Guard.Positive(propellantDensity, nameof(propellantDensity));
        Guard.Positive(characteristicVelocity, nameof(characteristicVelocity));
        Guard.Positive(burnRateCoefficient, nameof(burnRateCoefficient));
        Guard.InRange(pressureExponent, 0.0, 0.999, nameof(pressureExponent));

        double klemmung = Klemmung(burningSurfaceArea, throatArea);

        double baseValue = klemmung * propellantDensity * burnRateCoefficient * characteristicVelocity;

        return Math.Pow(baseValue, 1.0 / (1.0 - pressureExponent));
    }

    /// <summary>
    /// Whether a burn rate exponent gives a stable motor.
    /// </summary>
    /// <param name="pressureExponent">Burn rate pressure exponent n.</param>
    /// <returns>True when n is less than 1.</returns>
    /// <remarks>
    /// <para>
    /// This is the most important stability criterion in solid propulsion, and it is worth
    /// following the argument through.
    /// </para>
    /// <para>
    /// Suppose the chamber pressure rises slightly. Gas generation goes up as p^n, while
    /// discharge through the choked throat goes up as p^1. If n &lt; 1, discharge grows faster
    /// than generation, the excess is vented, and the pressure falls back - a negative feedback
    /// loop, and the motor is self-regulating. If n &gt; 1, generation outruns discharge, which
    /// raises the pressure further, which raises generation again. The feedback is positive and
    /// the motor detonates.
    /// </para>
    /// <para>
    /// This is not a theoretical concern: it is why propellant formulation is regulated, why
    /// cracks in a grain are catastrophic (a crack abruptly exposes new burning area, spiking K
    /// and therefore pressure), and why solid motors are X-rayed before flight.
    /// </para>
    /// </remarks>
    public static bool IsStable(double pressureExponent) => pressureExponent < 1.0;

    /// <summary>
    /// Web thickness burned in a given time at constant pressure.
    /// </summary>
    /// <param name="burnRate">Linear burn rate r, m/s.</param>
    /// <param name="timeSeconds">Elapsed time, s.</param>
    /// <returns>Distance the burning surface has receded, m.</returns>
    /// <remarks>
    /// "Web" is the solid-motor term for the propellant thickness that must burn through before
    /// the motor goes out. Web divided by burn rate gives the action time, so web thickness is
    /// what sets burn duration - and hence why a long-burning motor is a thick-walled one.
    /// </remarks>
    public static double WebBurned(double burnRate, double timeSeconds)
    {
        Guard.Positive(burnRate, nameof(burnRate));
        Guard.NonNegative(timeSeconds, nameof(timeSeconds));

        return burnRate * timeSeconds;
    }

    /// <summary>
    /// Burning surface area of a cylindrical BATES grain segment at a given web distance.
    /// </summary>
    /// <param name="outerDiameter">Grain outer diameter, m.</param>
    /// <param name="initialCoreDiameter">Initial central port diameter, m.</param>
    /// <param name="grainLength">Initial grain segment length, m.</param>
    /// <param name="webBurned">Distance the surface has receded so far, m.</param>
    /// <param name="endsInhibited">
    /// True if the flat ends are coated with an inhibitor so they do not burn. False for a
    /// classic BATES segment, whose ends do burn.
    /// </param>
    /// <returns>Instantaneous burning surface area in m^2.</returns>
    /// <remarks>
    /// <para>
    /// A BATES grain is the standard teaching and hobby geometry: a hollow cylinder burning
    /// outward from the central port and inward from both ends. The two contributions pull in
    /// opposite directions as the burn proceeds - the core surface grows as its diameter
    /// increases, while the end surfaces shrink and the segment shortens.
    /// </para>
    /// <para>
    /// Choosing the length-to-diameter ratio so those two effects cancel gives a nearly
    /// <i>neutral</i> thrust curve, which is usually what you want. Inhibiting the ends leaves
    /// only the growing core surface, giving a strongly <i>progressive</i> curve. A pure
    /// end-burner, at the opposite extreme, has constant area and burns for a very long time at
    /// low thrust. Star, wagon-wheel and finocyl cross-sections are all attempts to control
    /// this same area-versus-web relationship.
    /// </para>
    /// </remarks>
    public static double BatesGrainBurningArea(
        double outerDiameter,
        double initialCoreDiameter,
        double grainLength,
        double webBurned,
        bool endsInhibited = false)
    {
        Guard.Positive(outerDiameter, nameof(outerDiameter));
        Guard.Positive(initialCoreDiameter, nameof(initialCoreDiameter));
        Guard.Positive(grainLength, nameof(grainLength));
        Guard.NonNegative(webBurned, nameof(webBurned));

        if (initialCoreDiameter >= outerDiameter)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialCoreDiameter),
                initialCoreDiameter,
                "The core must be smaller than the outer diameter - there has to be propellant.");
        }

        // The core opens up at twice the web distance (radius grows by the web on each side of
        // the diameter).
        double currentCoreDiameter = initialCoreDiameter + (2.0 * webBurned);

        // The ends burn inward too, unless inhibited, shortening the segment from both faces.
        double currentLength = endsInhibited
            ? grainLength
            : grainLength - (2.0 * webBurned);

        // Burnout: either the core has reached the case wall or the segment has been consumed.
        if (currentCoreDiameter >= outerDiameter || currentLength <= 0.0)
        {
            return 0.0;
        }

        // Lateral surface of the cylindrical port.
        double coreArea = Math.PI * currentCoreDiameter * currentLength;

        if (endsInhibited)
        {
            return coreArea;
        }

        // Two annular end faces, outer disc minus the port.
        double endArea = 2.0 * Math.PI / 4.0
            * ((outerDiameter * outerDiameter) - (currentCoreDiameter * currentCoreDiameter));

        return coreArea + endArea;
    }

    /// <summary>
    /// Propellant mass of a BATES grain segment.
    /// </summary>
    /// <param name="outerDiameter">Grain outer diameter, m.</param>
    /// <param name="coreDiameter">Central port diameter, m.</param>
    /// <param name="grainLength">Grain segment length, m.</param>
    /// <param name="propellantDensity">Propellant density, kg/m^3.</param>
    /// <returns>Propellant mass in kg.</returns>
    public static double BatesGrainMass(
        double outerDiameter,
        double coreDiameter,
        double grainLength,
        double propellantDensity)
    {
        Guard.Positive(outerDiameter, nameof(outerDiameter));
        Guard.Positive(coreDiameter, nameof(coreDiameter));
        Guard.Positive(grainLength, nameof(grainLength));
        Guard.Positive(propellantDensity, nameof(propellantDensity));

        double volume = Math.PI / 4.0
            * ((outerDiameter * outerDiameter) - (coreDiameter * coreDiameter))
            * grainLength;

        return volume * propellantDensity;
    }

    /// <summary>
    /// Volumetric loading fraction: how much of the case volume is actually propellant.
    /// </summary>
    /// <param name="propellantVolume">Propellant volume, m^3.</param>
    /// <param name="caseVolume">Internal case volume, m^3.</param>
    /// <returns>The loading fraction, between 0 and 1.</returns>
    /// <remarks>
    /// Directly in tension with the grain geometry. A big central port gives lots of burning
    /// area and high thrust but wastes case volume; a small port loads more propellant but
    /// burns slowly. End-burners approach 0.95 loading with very low thrust; a high-thrust
    /// star grain might manage only 0.80.
    /// </remarks>
    public static double VolumetricLoadingFraction(double propellantVolume, double caseVolume)
    {
        Guard.Positive(propellantVolume, nameof(propellantVolume));
        Guard.Positive(caseVolume, nameof(caseVolume));

        return propellantVolume / caseVolume;
    }

    /// <summary>
    /// Temperature sensitivity of chamber pressure, pi_K.
    /// </summary>
    /// <param name="pressureAtHighTemperature">Chamber pressure at the hot soak condition, Pa.</param>
    /// <param name="pressureAtLowTemperature">Chamber pressure at the cold soak condition, Pa.</param>
    /// <param name="temperatureDifference">Difference between the two soak temperatures, K.</param>
    /// <returns>Fractional change in chamber pressure per kelvin.</returns>
    /// <remarks>
    /// Typically 0.001-0.009 per kelvin. It sounds negligible until you multiply by a realistic
    /// operational range: a motor qualified from -40 C to +60 C sees a 100 K swing, and at
    /// 0.005/K that is a 50% change in chamber pressure between a cold morning and a hot
    /// afternoon. The case must be designed for the hot end and the guidance must cope with the
    /// cold end.
    /// </remarks>
    public static double TemperatureSensitivity(
        double pressureAtHighTemperature,
        double pressureAtLowTemperature,
        double temperatureDifference)
    {
        Guard.Positive(pressureAtHighTemperature, nameof(pressureAtHighTemperature));
        Guard.Positive(pressureAtLowTemperature, nameof(pressureAtLowTemperature));
        Guard.Positive(temperatureDifference, nameof(temperatureDifference));

        return Math.Log(pressureAtHighTemperature / pressureAtLowTemperature) / temperatureDifference;
    }
}
