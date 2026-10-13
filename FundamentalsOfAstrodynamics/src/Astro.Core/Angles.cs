namespace Astro.Core;

/// <summary>
/// Angle utilities. Astrodynamics is full of quantities that are only meaningful modulo a
/// full revolution, and getting the wrapping wrong is one of the most common sources of
/// silently wrong answers.
/// </summary>
public static class Angles
{
    /// <summary>
    /// Wraps an angle into the interval [0, 2pi).
    /// </summary>
    /// <remarks>
    /// C#'s <c>%</c> operator keeps the sign of the dividend, so <c>-0.1 % 2pi</c> is
    /// <c>-0.1</c> rather than <c>2pi - 0.1</c>. The extra add-and-remainder below fixes that.
    /// </remarks>
    public static double Normalize(double angleRad)
    {
        double wrapped = angleRad % Constants.TwoPi;
        return wrapped < 0.0 ? wrapped + Constants.TwoPi : wrapped;
    }

    /// <summary>
    /// Wraps an angle into the interval (−pi, pi]. Use this for <i>differences</i> between
    /// angles, where "−179 degrees" is a much more useful answer than "181 degrees".
    /// </summary>
    public static double NormalizeSigned(double angleRad)
    {
        double wrapped = Normalize(angleRad);
        return wrapped > Math.PI ? wrapped - Constants.TwoPi : wrapped;
    }

    /// <summary>Converts degrees to radians.</summary>
    public static double ToRadians(double degrees) => degrees * Constants.DegToRad;

    /// <summary>Converts radians to degrees.</summary>
    public static double ToDegrees(double radians) => radians * Constants.RadToDeg;

    /// <summary>
    /// A numerically safe <see cref="Math.Acos"/> that clamps its argument to [-1, 1] first,
    /// preventing NaN results when rounding pushes a cosine a few ulps out of range.
    /// </summary>
    public static double SafeAcos(double cosine) => Math.Acos(Math.Clamp(cosine, -1.0, 1.0));
}
