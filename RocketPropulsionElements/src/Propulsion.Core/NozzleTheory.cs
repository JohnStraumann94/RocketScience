namespace Propulsion.Core;

/// <summary>
/// Ideal one-dimensional isentropic nozzle theory: Sutton Chapter 3, "Nozzle Theory and
/// Thermodynamic Relations". This is the heart of the whole subject - everything an engine
/// does is decided in the converging-diverging nozzle.
/// </summary>
/// <remarks>
/// <para>
/// The "ideal rocket" assumptions behind every formula here are worth stating explicitly,
/// because knowing which one you have violated is how you explain a discrepancy against test
/// data:
/// </para>
/// <list type="number">
///   <item><description>The working fluid is homogeneous and obeys the perfect gas law.</description></item>
///   <item><description>The specific heats, and hence gamma, are constant throughout.</description></item>
///   <item><description>The flow is steady, one-dimensional, axial and frictionless.</description></item>
///   <item><description>The expansion is adiabatic and isentropic - no heat loss, no shocks.</description></item>
///   <item><description>Chamber velocity is negligible compared with exit velocity.</description></item>
///   <item><description>Chemical equilibrium is frozen at the chamber composition.</description></item>
/// </list>
/// <para>
/// Real engines land within 1-6% of these predictions, which is remarkable given how strong the
/// assumptions are. The corrections that close the gap are collected in
/// <see cref="NozzleLosses"/>.
/// </para>
/// </remarks>
public static class NozzleTheory
{
    /// <summary>
    /// The Vandenkerckhove function, usually written as the capital Greek letter Gamma.
    /// </summary>
    /// <param name="gamma">Ratio of specific heats.</param>
    /// <returns>The dimensionless value of the function.</returns>
    /// <remarks>
    /// <para>
    /// Defined as
    /// </para>
    /// <code>
    ///     Gamma(g) = sqrt(g) * (2 / (g + 1)) ^ ((g + 1) / (2 * (g - 1)))
    /// </code>
    /// <para>
    /// This grouping appears so often in choked-flow algebra that giving it a name makes every
    /// subsequent formula readable. It is a slowly varying function: 0.6485 at gamma = 1.20,
    /// 0.6673 at gamma = 1.30, 0.6847 at gamma = 1.40. Because it barely moves, the mass flow
    /// through a choked throat is far more sensitive to chamber pressure and temperature than
    /// to the exact gas composition.
    /// </para>
    /// </remarks>
    public static double VandenkerckhoveFunction(double gamma)
    {
        Guard.Gamma(gamma);

        double exponent = (gamma + 1.0) / (2.0 * (gamma - 1.0));

        return Math.Sqrt(gamma) * Math.Pow(2.0 / (gamma + 1.0), exponent);
    }

    /// <summary>
    /// Specific gas constant of the exhaust, R = R_universal / M.
    /// </summary>
    /// <param name="molecularMass">Mean molecular mass of the exhaust, kg/kmol.</param>
    /// <returns>The specific gas constant in J/(kg*K).</returns>
    /// <remarks>
    /// Low molecular mass is the whole reason hydrogen is the best chemical fuel there is.
    /// Exhaust velocity scales with sqrt(T/M), so the ~10 kg/kmol of a hydrogen-rich LOX/LH2
    /// exhaust beats the ~22 kg/kmol of kerosene combustion by a factor of sqrt(2.2) = 1.48 -
    /// far more than the modest difference in flame temperature could ever give back.
    /// </remarks>
    public static double SpecificGasConstant(double molecularMass)
    {
        Guard.Positive(molecularMass, nameof(molecularMass));

        return Constants.UniversalGasConstant / molecularMass;
    }

    /// <summary>
    /// Ideal exhaust velocity from the chamber conditions and the exit pressure ratio.
    /// </summary>
    /// <param name="gamma">Ratio of specific heats.</param>
    /// <param name="chamberTemperature">Chamber (stagnation) temperature T1, K.</param>
    /// <param name="molecularMass">Mean molecular mass of the exhaust, kg/kmol.</param>
    /// <param name="chamberPressure">Chamber pressure p1, Pa.</param>
    /// <param name="exitPressure">Nozzle exit static pressure p2, Pa.</param>
    /// <returns>Ideal exit gas velocity v2 in m/s.</returns>
    /// <remarks>
    /// <para>
    /// This is the central equation of the subject:
    /// </para>
    /// <code>
    ///     v2 = sqrt( (2*g/(g-1)) * (R_u*T1/M) * (1 - (p2/p1)^((g-1)/g)) )
    /// </code>
    /// <para>
    /// Read it as an energy statement. The bracket is the fraction of the available thermal
    /// energy that the expansion actually converts to directed kinetic energy, and it is
    /// governed entirely by the pressure ratio. Three design levers appear, in descending order
    /// of usefulness:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>Molecular mass</b> - v2 scales as 1/sqrt(M). Enormous lever.</description></item>
    ///   <item><description><b>Chamber temperature</b> - v2 scales as sqrt(T1). Real, but limited by materials.</description></item>
    ///   <item><description><b>Pressure ratio</b> - inside a fractional power inside a square root. Strong diminishing returns: going from 100:1 to 200:1 buys only a few percent.</description></item>
    /// </list>
    /// <para>
    /// Note that p2 = 0 (perfect vacuum expansion) makes the bracket exactly 1, giving the
    /// theoretical maximum velocity for the propellant. No finite nozzle reaches it.
    /// </para>
    /// </remarks>
    public static double IdealExhaustVelocity(
        double gamma,
        double chamberTemperature,
        double molecularMass,
        double chamberPressure,
        double exitPressure)
    {
        Guard.Gamma(gamma);
        Guard.Positive(chamberTemperature, nameof(chamberTemperature));
        Guard.Positive(molecularMass, nameof(molecularMass));
        Guard.Positive(chamberPressure, nameof(chamberPressure));
        Guard.NonNegative(exitPressure, nameof(exitPressure));

        if (exitPressure > chamberPressure)
        {
            throw new ArgumentOutOfRangeException(
                nameof(exitPressure),
                exitPressure,
                "Exit pressure cannot exceed chamber pressure - the gas would flow backwards.");
        }

        double specificGasConstant = SpecificGasConstant(molecularMass);
        double pressureRatio = exitPressure / chamberPressure;
        double expansionTerm = 1.0 - Math.Pow(pressureRatio, (gamma - 1.0) / gamma);

        return Math.Sqrt(
            (2.0 * gamma / (gamma - 1.0)) * specificGasConstant * chamberTemperature * expansionTerm);
    }

    /// <summary>
    /// Characteristic velocity c*, a figure of merit for the combustion chamber alone.
    /// </summary>
    /// <param name="gamma">Ratio of specific heats.</param>
    /// <param name="chamberTemperature">Chamber temperature T1, K.</param>
    /// <param name="molecularMass">Mean molecular mass of the exhaust, kg/kmol.</param>
    /// <returns>Characteristic velocity in m/s.</returns>
    /// <remarks>
    /// <para>
    /// c* = sqrt(R*T1) / Gamma(g), and equivalently c* = p1 * A_t / mdot.
    /// </para>
    /// <para>
    /// The second form is why c* matters so much in practice: chamber pressure, throat area and
    /// mass flow are all directly measurable on a test stand, so c* can be computed from a
    /// static fire without knowing anything about the nozzle downstream. It isolates how well
    /// the injector and chamber are converting propellant into hot, low-molecular-mass gas.
    /// The ratio of measured to theoretical c* is the <i>c-star efficiency</i>, typically 0.92
    /// to 0.99, and it is the number a combustion engineer is judged on. The nozzle's separate
    /// contribution is captured by <see cref="ThrustCoefficient"/>, and the two multiply:
    /// c = c* * C_F.
    /// </para>
    /// </remarks>
    public static double CharacteristicVelocity(
        double gamma,
        double chamberTemperature,
        double molecularMass)
    {
        Guard.Gamma(gamma);
        Guard.Positive(chamberTemperature, nameof(chamberTemperature));
        Guard.Positive(molecularMass, nameof(molecularMass));

        double specificGasConstant = SpecificGasConstant(molecularMass);

        return Math.Sqrt(specificGasConstant * chamberTemperature) / VandenkerckhoveFunction(gamma);
    }

    /// <summary>
    /// Thrust coefficient C_F, a figure of merit for the nozzle alone.
    /// </summary>
    /// <param name="gamma">Ratio of specific heats.</param>
    /// <param name="chamberPressure">Chamber pressure p1, Pa.</param>
    /// <param name="exitPressure">Nozzle exit static pressure p2, Pa.</param>
    /// <param name="ambientPressure">Ambient pressure p3, Pa. Zero for vacuum.</param>
    /// <param name="areaRatio">Nozzle expansion area ratio A_2 / A_t.</param>
    /// <returns>The dimensionless thrust coefficient.</returns>
    /// <remarks>
    /// <para>
    /// Defined by F = C_F * p1 * A_t, so C_F answers "how much thrust does this nozzle extract
    /// per unit of chamber pressure and throat area?" The momentum term is
    /// </para>
    /// <code>
    ///     sqrt( (2g^2/(g-1)) * (2/(g+1))^((g+1)/(g-1)) * (1 - (p2/p1)^((g-1)/g)) )
    /// </code>
    /// <para>
    /// plus the pressure correction ((p2 - p3)/p1) * areaRatio.
    /// </para>
    /// <para>
    /// Typical values run from about 1.2 for a sea-level nozzle to over 1.9 for a large vacuum
    /// nozzle, and the theoretical ceiling for infinite expansion into vacuum at gamma = 1.20 is
    /// 2.246. A single number therefore tells you almost everything about how hard the nozzle is
    /// working, which is why C_F is the standard currency for comparing nozzle designs.
    /// </para>
    /// </remarks>
    public static double ThrustCoefficient(
        double gamma,
        double chamberPressure,
        double exitPressure,
        double ambientPressure,
        double areaRatio)
    {
        Guard.Gamma(gamma);
        Guard.Positive(chamberPressure, nameof(chamberPressure));
        Guard.NonNegative(exitPressure, nameof(exitPressure));
        Guard.NonNegative(ambientPressure, nameof(ambientPressure));
        Guard.Positive(areaRatio, nameof(areaRatio));

        double pressureRatio = exitPressure / chamberPressure;

        double momentumTerm = Math.Sqrt(
            (2.0 * gamma * gamma / (gamma - 1.0))
            * Math.Pow(2.0 / (gamma + 1.0), (gamma + 1.0) / (gamma - 1.0))
            * (1.0 - Math.Pow(pressureRatio, (gamma - 1.0) / gamma)));

        double pressureTerm = (exitPressure - ambientPressure) / chamberPressure * areaRatio;

        return momentumTerm + pressureTerm;
    }

    /// <summary>
    /// The theoretical maximum thrust coefficient: infinite expansion into a perfect vacuum.
    /// </summary>
    /// <param name="gamma">Ratio of specific heats.</param>
    /// <returns>The limiting dimensionless thrust coefficient.</returns>
    /// <remarks>
    /// Obtained by letting p2 go to zero, which kills the pressure term and drives the bracket
    /// to 1, leaving C_F,max = Gamma(g) * sqrt(2g/(g-1)). For gamma = 1.20 this is 2.246 and for
    /// gamma = 1.30 it is 1.964. No real nozzle gets close, because the area ratio needed grows
    /// without bound, but it is the yardstick against which nozzle efficiency is measured.
    /// </remarks>
    public static double MaximumThrustCoefficient(double gamma)
    {
        Guard.Gamma(gamma);

        return VandenkerckhoveFunction(gamma) * Math.Sqrt(2.0 * gamma / (gamma - 1.0));
    }

    /// <summary>
    /// Choked mass flow rate through the throat.
    /// </summary>
    /// <param name="gamma">Ratio of specific heats.</param>
    /// <param name="chamberPressure">Chamber pressure p1, Pa.</param>
    /// <param name="chamberTemperature">Chamber temperature T1, K.</param>
    /// <param name="molecularMass">Mean molecular mass of the exhaust, kg/kmol.</param>
    /// <param name="throatArea">Throat cross-sectional area A_t, m^2.</param>
    /// <returns>Mass flow rate in kg/s.</returns>
    /// <remarks>
    /// <para>
    /// mdot = Gamma(g) * p1 * A_t / sqrt(R*T1), equivalently mdot = p1 * A_t / c*.
    /// </para>
    /// <para>
    /// The critical insight is that this depends on <i>nothing downstream of the throat</i>.
    /// Once the flow is choked - which it is for any pressure ratio above about 1.8 - the
    /// throat passes the same mass whether the nozzle exhausts to sea level or to vacuum, and
    /// no amount of change to the diverging section alters it. Information cannot propagate
    /// upstream through sonic flow. This decoupling is exactly what lets c* and C_F be treated
    /// as independent figures of merit that simply multiply.
    /// </para>
    /// </remarks>
    public static double MassFlowRate(
        double gamma,
        double chamberPressure,
        double chamberTemperature,
        double molecularMass,
        double throatArea)
    {
        Guard.Positive(throatArea, nameof(throatArea));
        Guard.Positive(chamberPressure, nameof(chamberPressure));

        double characteristicVelocity =
            CharacteristicVelocity(gamma, chamberTemperature, molecularMass);

        return chamberPressure * throatArea / characteristicVelocity;
    }

    /// <summary>
    /// Static temperature at the throat, where the flow is exactly sonic.
    /// </summary>
    /// <param name="gamma">Ratio of specific heats.</param>
    /// <param name="chamberTemperature">Chamber (stagnation) temperature T1, K.</param>
    /// <returns>Throat static temperature in K.</returns>
    /// <remarks>
    /// T_t = T1 * 2/(g+1). For gamma = 1.2 that is 0.909 * T1 - the gas has given up only about
    /// 9% of its temperature by the time it reaches Mach 1. Almost all of the acceleration
    /// happens in the diverging section, which is why the diverging section is where all the
    /// design effort goes.
    /// </remarks>
    public static double ThroatTemperature(double gamma, double chamberTemperature)
    {
        Guard.Gamma(gamma);
        Guard.Positive(chamberTemperature, nameof(chamberTemperature));

        return chamberTemperature * 2.0 / (gamma + 1.0);
    }

    /// <summary>
    /// Static pressure at the throat.
    /// </summary>
    /// <param name="gamma">Ratio of specific heats.</param>
    /// <param name="chamberPressure">Chamber (stagnation) pressure p1, Pa.</param>
    /// <returns>Throat static pressure in Pa.</returns>
    /// <remarks>
    /// p_t = p1 * (2/(g+1))^(g/(g-1)), about 0.564 * p1 for gamma = 1.2. The ratio p1/p_t is the
    /// <i>critical pressure ratio</i>; whenever the true back pressure is below this, the throat
    /// is choked and further reductions cannot increase the mass flow.
    /// </remarks>
    public static double ThroatPressure(double gamma, double chamberPressure)
    {
        Guard.Gamma(gamma);
        Guard.Positive(chamberPressure, nameof(chamberPressure));

        return chamberPressure * Math.Pow(2.0 / (gamma + 1.0), gamma / (gamma - 1.0));
    }

    /// <summary>
    /// Exit Mach number implied by a chamber-to-exit pressure ratio.
    /// </summary>
    /// <param name="gamma">Ratio of specific heats.</param>
    /// <param name="chamberPressure">Chamber pressure p1, Pa.</param>
    /// <param name="exitPressure">Exit static pressure p2, Pa.</param>
    /// <returns>The exit Mach number.</returns>
    /// <remarks>
    /// Straight from the isentropic stagnation relation
    /// p0/p = (1 + (g-1)/2 * M^2)^(g/(g-1)), solved for M.
    /// </remarks>
    public static double ExitMachNumber(double gamma, double chamberPressure, double exitPressure)
    {
        Guard.Gamma(gamma);
        Guard.Positive(chamberPressure, nameof(chamberPressure));
        Guard.Positive(exitPressure, nameof(exitPressure));

        double ratio = Math.Pow(chamberPressure / exitPressure, (gamma - 1.0) / gamma);

        return Math.Sqrt(2.0 / (gamma - 1.0) * (ratio - 1.0));
    }

    /// <summary>
    /// Area ratio A/A_t corresponding to a given Mach number.
    /// </summary>
    /// <param name="gamma">Ratio of specific heats.</param>
    /// <param name="machNumber">Local Mach number.</param>
    /// <returns>The local area divided by the throat area.</returns>
    /// <remarks>
    /// The classic isentropic area-Mach relation. It is famously double-valued: every area
    /// ratio above 1 corresponds to one subsonic and one supersonic solution. A physical nozzle
    /// picks the supersonic branch downstream of the throat only because the back pressure is
    /// low enough to sustain it; raise the back pressure and the flow jumps to the subsonic
    /// branch through a shock inside the nozzle.
    /// </remarks>
    public static double AreaRatioFromMach(double gamma, double machNumber)
    {
        Guard.Gamma(gamma);
        Guard.Positive(machNumber, nameof(machNumber));

        double exponent = (gamma + 1.0) / (2.0 * (gamma - 1.0));
        double term = (2.0 / (gamma + 1.0)) * (1.0 + ((gamma - 1.0) / 2.0 * machNumber * machNumber));

        return Math.Pow(term, exponent) / machNumber;
    }

    /// <summary>
    /// Area ratio required to expand from the chamber pressure down to a given exit pressure.
    /// </summary>
    /// <param name="gamma">Ratio of specific heats.</param>
    /// <param name="chamberPressure">Chamber pressure p1, Pa.</param>
    /// <param name="exitPressure">Desired exit static pressure p2, Pa.</param>
    /// <returns>The expansion area ratio A_2 / A_t.</returns>
    /// <remarks>
    /// This is the design equation for the diverging section: state where you want the exhaust
    /// pressure to end up, and it tells you how big to make the exit. The area ratio climbs
    /// steeply as p2 falls, which is why vacuum nozzles are enormous bells and why a true
    /// vacuum-optimised nozzle (p2 = 0) would need an infinite one.
    /// </remarks>
    public static double AreaRatioFromPressureRatio(
        double gamma,
        double chamberPressure,
        double exitPressure)
    {
        double mach = ExitMachNumber(gamma, chamberPressure, exitPressure);

        return AreaRatioFromMach(gamma, mach);
    }

    /// <summary>
    /// Exit static pressure produced by a nozzle of a given area ratio.
    /// </summary>
    /// <param name="gamma">Ratio of specific heats.</param>
    /// <param name="chamberPressure">Chamber pressure p1, Pa.</param>
    /// <param name="areaRatio">Expansion area ratio A_2 / A_t, which must exceed 1.</param>
    /// <returns>Exit static pressure in Pa.</returns>
    /// <remarks>
    /// <para>
    /// The inverse of <see cref="AreaRatioFromPressureRatio"/>, and the direction you almost
    /// always need in practice: the hardware exists, its area ratio is fixed, and you want to
    /// know what it does at some altitude.
    /// </para>
    /// <para>
    /// The area-Mach relation cannot be inverted in closed form, so this brackets the supersonic
    /// root and bisects. Bisection rather than Newton is deliberate - the function is monotonic
    /// on the supersonic branch, so bisection cannot diverge, and 200 halvings of the initial
    /// bracket is far more precision than double arithmetic can even represent. Robustness is
    /// worth more here than the handful of microseconds Newton would save.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the area ratio is not greater than 1 - a converging-diverging nozzle must
    /// expand.
    /// </exception>
    public static double ExitPressureFromAreaRatio(
        double gamma,
        double chamberPressure,
        double areaRatio)
    {
        Guard.Gamma(gamma);
        Guard.Positive(chamberPressure, nameof(chamberPressure));

        if (areaRatio <= 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(areaRatio),
                areaRatio,
                "The expansion area ratio must be greater than 1 for a supersonic nozzle.");
        }

        // Search over the exit Mach number rather than the pressure directly: the area-Mach
        // relation is smooth and strictly increasing for M > 1, so the bracket is trivial.
        double lowMach = 1.0 + 1e-9;
        double highMach = 100.0;

        for (int iteration = 0; iteration < 200; iteration++)
        {
            double midMach = 0.5 * (lowMach + highMach);

            if (AreaRatioFromMach(gamma, midMach) < areaRatio)
            {
                lowMach = midMach;
            }
            else
            {
                highMach = midMach;
            }
        }

        double exitMach = 0.5 * (lowMach + highMach);

        // Convert the converged Mach number back to a static pressure with the isentropic
        // stagnation relation.
        double stagnationRatio =
            Math.Pow(1.0 + ((gamma - 1.0) / 2.0 * exitMach * exitMach), gamma / (gamma - 1.0));

        return chamberPressure / stagnationRatio;
    }

    /// <summary>
    /// The area ratio that makes a nozzle perfectly expanded at a given ambient pressure.
    /// </summary>
    /// <param name="gamma">Ratio of specific heats.</param>
    /// <param name="chamberPressure">Chamber pressure p1, Pa.</param>
    /// <param name="ambientPressure">Ambient pressure p3, Pa.</param>
    /// <returns>The optimum expansion area ratio.</returns>
    /// <remarks>
    /// <para>
    /// Thrust is maximised, for a fixed chamber pressure and throat area, when p2 = p3 exactly.
    /// The proof is short: differentiate the thrust equation with respect to the exit area and
    /// the stationary point falls precisely on matched pressure. Under-expand and you leave
    /// unconverted pressure energy in the jet; over-expand and ambient pressure pushes back on
    /// the oversized exit cone.
    /// </para>
    /// <para>
    /// Because a launch vehicle climbs through several orders of magnitude of ambient pressure,
    /// no fixed nozzle can stay optimal. First stages are deliberately under-expanded at sea
    /// level so they are closer to optimum higher up, and altitude-compensating designs
    /// (aerospike, dual-bell, extendible skirts) exist entirely to attack this compromise.
    /// </para>
    /// </remarks>
    public static double OptimumAreaRatio(double gamma, double chamberPressure, double ambientPressure)
        => AreaRatioFromPressureRatio(gamma, chamberPressure, ambientPressure);
}
