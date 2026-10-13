namespace Astro.Core;

/// <summary>
/// Small internal argument-validation helpers. Keeping them in one place stops the
/// physics code from being buried under <c>if (x &lt;= 0) throw ...</c> noise.
/// </summary>
internal static class Guard
{
    /// <summary>Throws if <paramref name="value"/> is not strictly greater than zero.</summary>
    public static void Positive(double value, string parameterName)
    {
        if (double.IsNaN(value) || value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName, value, $"{parameterName} must be a positive, finite number.");
        }
    }

    /// <summary>Throws if <paramref name="value"/> is negative or NaN (zero is allowed).</summary>
    public static void NonNegative(double value, string parameterName)
    {
        if (double.IsNaN(value) || value < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName, value, $"{parameterName} must be zero or positive.");
        }
    }

    /// <summary>Throws if <paramref name="value"/> falls outside the inclusive range given.</summary>
    public static void InRange(double value, double minimum, double maximum, string parameterName)
    {
        if (double.IsNaN(value) || value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(
                parameterName, value, $"{parameterName} must lie between {minimum} and {maximum}.");
        }
    }
}
