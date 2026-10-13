namespace Propulsion.Core;

/// <summary>
/// Nozzle geometry: throat and exit sizing, cone and bell shapes, length, and the divergence
/// loss that comes from the exhaust not leaving perfectly axially. Sutton Chapter 3.
/// </summary>
public static class NozzleGeometry
{
    /// <summary>
    /// Circular area from a diameter.
    /// </summary>
    /// <param name="diameter">Diameter, m.</param>
    /// <returns>Area in m^2.</returns>
    public static double AreaFromDiameter(double diameter)
    {
        Guard.Positive(diameter, nameof(diameter));

        return Math.PI * diameter * diameter / 4.0;
    }

    /// <summary>
    /// Circular diameter from an area.
    /// </summary>
    /// <param name="area">Area, m^2.</param>
    /// <returns>Diameter in m.</returns>
    public static double DiameterFromArea(double area)
    {
        Guard.Positive(area, nameof(area));

        return Math.Sqrt(4.0 * area / Math.PI);
    }

    /// <summary>
    /// Throat area required to deliver a given thrust.
    /// </summary>
    /// <param name="thrust">Required thrust F, N.</param>
    /// <param name="thrustCoefficient">Thrust coefficient C_F for the operating point.</param>
    /// <param name="chamberPressure">Chamber pressure p1, Pa.</param>
    /// <returns>Throat area in m^2.</returns>
    /// <remarks>
    /// <para>
    /// A_t = F / (C_F * p1). This one line explains the entire industry-wide march towards
    /// higher chamber pressures: for a fixed thrust, doubling p1 halves the throat area, which
    /// shrinks the whole engine, its chamber, its cooling jacket and its mass. The staged
    /// combustion cycle exists because of this equation.
    /// </para>
    /// <para>
    /// The price is paid in heat flux, which scales roughly with p1^0.8, and in turbopump
    /// discharge pressure. That is the fundamental engineering tension in liquid rocket design.
    /// </para>
    /// </remarks>
    public static double RequiredThroatArea(
        double thrust,
        double thrustCoefficient,
        double chamberPressure)
    {
        Guard.Positive(thrust, nameof(thrust));
        Guard.Positive(thrustCoefficient, nameof(thrustCoefficient));
        Guard.Positive(chamberPressure, nameof(chamberPressure));

        return thrust / (thrustCoefficient * chamberPressure);
    }

    /// <summary>
    /// Divergence correction factor lambda for a conical nozzle.
    /// </summary>
    /// <param name="halfAngleDegrees">Half-angle of the diverging cone, degrees.</param>
    /// <returns>The dimensionless correction factor, between 0 and 1.</returns>
    /// <remarks>
    /// <para>
    /// lambda = (1 + cos(alpha)) / 2.
    /// </para>
    /// <para>
    /// Gas leaving a cone travels radially outward as well as axially, and only the axial
    /// component produces thrust. Averaging the axial component over the spherical exit surface
    /// gives this factor. At the traditional 15-degree half-angle it is 0.983, so a conical
    /// nozzle throws away 1.7% of its momentum - small, but free to recover.
    /// </para>
    /// <para>
    /// That recovery is the entire reason bell nozzles exist. A bell turns the flow back
    /// towards axial before the exit plane and reaches lambda around 0.99 in a much shorter
    /// package. Widening the cone instead shortens the nozzle but loses more to divergence;
    /// narrowing it recovers momentum but adds length, mass and wall friction. Fifteen degrees
    /// is where those curves cross for a cone.
    /// </para>
    /// </remarks>
    public static double ConicalDivergenceFactor(double halfAngleDegrees)
    {
        Guard.InRange(halfAngleDegrees, 0.0, 89.0, nameof(halfAngleDegrees));

        double halfAngleRadians = halfAngleDegrees * Math.PI / 180.0;

        return (1.0 + Math.Cos(halfAngleRadians)) / 2.0;
    }

    /// <summary>
    /// Axial length of a conical diverging section.
    /// </summary>
    /// <param name="throatRadius">Throat radius R_t, m.</param>
    /// <param name="areaRatio">Expansion area ratio A_2 / A_t.</param>
    /// <param name="halfAngleDegrees">Cone half-angle, degrees.</param>
    /// <param name="throatArcRatio">
    /// Radius of the circular arc joining the throat to the cone, expressed as a multiple of the
    /// throat radius. 1.5 is the conventional value.
    /// </param>
    /// <returns>Length of the diverging section in m.</returns>
    /// <remarks>
    /// L = (R_t * (sqrt(eps) - 1) + R * (sec(alpha) - 1)) / tan(alpha). Length matters because
    /// nozzle mass scales with it, and on an upper stage every kilogram of nozzle is a kilogram
    /// of payload.
    /// </remarks>
    public static double ConicalNozzleLength(
        double throatRadius,
        double areaRatio,
        double halfAngleDegrees,
        double throatArcRatio = 1.5)
    {
        Guard.Positive(throatRadius, nameof(throatRadius));
        Guard.InRange(areaRatio, 1.0, 1e6, nameof(areaRatio));
        Guard.InRange(halfAngleDegrees, 1.0, 60.0, nameof(halfAngleDegrees));
        Guard.Positive(throatArcRatio, nameof(throatArcRatio));

        double alpha = halfAngleDegrees * Math.PI / 180.0;
        double arcRadius = throatArcRatio * throatRadius;

        double straightPart = throatRadius * (Math.Sqrt(areaRatio) - 1.0);
        double arcPart = arcRadius * ((1.0 / Math.Cos(alpha)) - 1.0);

        return (straightPart + arcPart) / Math.Tan(alpha);
    }

    /// <summary>
    /// Length of a bell nozzle, expressed as a fraction of the equivalent 15-degree cone.
    /// </summary>
    /// <param name="throatRadius">Throat radius R_t, m.</param>
    /// <param name="areaRatio">Expansion area ratio A_2 / A_t.</param>
    /// <param name="lengthFraction">
    /// Bell length as a fraction of the equivalent 15-degree conical nozzle. 0.8 is the most
    /// common choice; 0.6 is aggressive, 1.0 gives no length saving at all.
    /// </param>
    /// <returns>Bell nozzle length in m.</returns>
    /// <remarks>
    /// Bell nozzles are always quoted this way - an "80% bell" means 80% of the length of the
    /// 15-degree cone with the same area ratio, while delivering equal or better performance.
    /// Rao's method of characteristics produces the actual contour; this function only gives
    /// the overall envelope.
    /// </remarks>
    public static double BellNozzleLength(
        double throatRadius,
        double areaRatio,
        double lengthFraction = 0.8)
    {
        Guard.InRange(lengthFraction, 0.5, 1.0, nameof(lengthFraction));

        return lengthFraction * ConicalNozzleLength(throatRadius, areaRatio, 15.0);
    }

    /// <summary>
    /// Chamber volume implied by a characteristic chamber length L*.
    /// </summary>
    /// <param name="characteristicLength">Characteristic chamber length L*, m.</param>
    /// <param name="throatArea">Throat area A_t, m^2.</param>
    /// <returns>Required chamber volume in m^3.</returns>
    /// <remarks>
    /// <para>
    /// V_c = L* * A_t. L* is a crude but durable proxy for propellant residence time: the
    /// propellant must have long enough in the chamber to atomise, vaporise, mix and burn to
    /// completion before it reaches the throat.
    /// </para>
    /// <para>
    /// Typical values: 0.8-1.0 m for LOX/RP-1, 0.6-0.8 m for LOX/LH2 (hydrogen burns fast),
    /// and 1.5-2.5 m for hypergolics such as N2O4/UDMH. Too short and you lose c* efficiency to
    /// incomplete combustion; too long and you add mass, cooled surface area and pressure drop.
    /// </para>
    /// </remarks>
    public static double ChamberVolume(double characteristicLength, double throatArea)
    {
        Guard.Positive(characteristicLength, nameof(characteristicLength));
        Guard.Positive(throatArea, nameof(throatArea));

        return characteristicLength * throatArea;
    }

    /// <summary>
    /// Contraction ratio, chamber cross-section divided by throat area.
    /// </summary>
    /// <param name="chamberArea">Chamber cross-sectional area A_c, m^2.</param>
    /// <param name="throatArea">Throat area A_t, m^2.</param>
    /// <returns>The dimensionless contraction ratio.</returns>
    /// <remarks>
    /// Usually between 2 and 5 for large engines and higher for small ones. It needs to be big
    /// enough that the chamber gas velocity stays low - otherwise the stagnation-pressure
    /// assumption behind all the nozzle theory in <see cref="NozzleTheory"/> quietly breaks
    /// down and the real chamber pressure at the injector face exceeds the value at the nozzle
    /// entrance.
    /// </remarks>
    public static double ContractionRatio(double chamberArea, double throatArea)
    {
        Guard.Positive(chamberArea, nameof(chamberArea));
        Guard.Positive(throatArea, nameof(throatArea));

        return chamberArea / throatArea;
    }
}
