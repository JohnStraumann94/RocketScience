namespace Propulsion.Core;

/// <summary>
/// Argument-validation helpers shared by the whole library.
/// </summary>
/// <remarks>
/// <para>
/// Propulsion formulas are riddled with square roots, logarithms and divisions that silently
/// produce <see cref="double.NaN"/> or <see cref="double.PositiveInfinity"/> when handed a
/// nonsensical input. A NaN that appears three formulas upstream of where it is noticed is
/// genuinely painful to track down, so every public entry point validates first and throws with
/// the offending parameter name.
/// </para>
/// <para>
/// The methods return the validated value so they can be used inline in a constructor or
/// expression body: <c>_pressure = Guard.Positive(pressure, nameof(pressure));</c>
/// </para>
/// </remarks>
internal static class Guard
{
    /// <summary>Requires a strictly positive, finite value.</summary>
    /// <param name="value">The value to check.</param>
    /// <param name="name">The parameter name, for the exception message.</param>
    /// <returns><paramref name="value"/>, unchanged.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is zero, negative, NaN or infinite.
    /// </exception>
    public static double Positive(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                name, value, $"{name} must be a finite value greater than zero.");
        }

        return value;
    }

    /// <summary>Requires a non-negative, finite value (zero is allowed).</summary>
    /// <param name="value">The value to check.</param>
    /// <param name="name">The parameter name, for the exception message.</param>
    /// <returns><paramref name="value"/>, unchanged.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is negative, NaN or infinite.
    /// </exception>
    public static double NonNegative(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                name, value, $"{name} must be a finite value of zero or more.");
        }

        return value;
    }

    /// <summary>Requires a value within an inclusive range.</summary>
    /// <param name="value">The value to check.</param>
    /// <param name="minimum">Lowest acceptable value.</param>
    /// <param name="maximum">Highest acceptable value.</param>
    /// <param name="name">The parameter name, for the exception message.</param>
    /// <returns><paramref name="value"/>, unchanged.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value falls outside the range or is not finite.
    /// </exception>
    public static double InRange(double value, double minimum, double maximum, string name)
    {
        if (!double.IsFinite(value) || value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(
                name, value, $"{name} must lie between {minimum} and {maximum} inclusive.");
        }

        return value;
    }

    /// <summary>
    /// Requires a valid ratio of specific heats, gamma.
    /// </summary>
    /// <param name="gamma">The ratio of specific heats, cp / cv.</param>
    /// <param name="name">The parameter name, for the exception message.</param>
    /// <returns><paramref name="gamma"/>, unchanged.</returns>
    /// <remarks>
    /// Gamma must be strictly greater than 1 for any real gas: cp always exceeds cv because
    /// heating at constant pressure also does expansion work. Values of exactly 1 make the
    /// exponent 2*gamma/(gamma-1) blow up, and values below 1 are unphysical. The upper bound
    /// of 5/3 is the monatomic ideal-gas limit; real combustion gases sit near 1.2.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when gamma is outside the open interval (1, 5/3].
    /// </exception>
    public static double Gamma(double gamma, string name = "gamma")
    {
        if (!double.IsFinite(gamma) || gamma <= 1.0 || gamma > 1.6667)
        {
            throw new ArgumentOutOfRangeException(
                name,
                gamma,
                "The ratio of specific heats must be greater than 1 and no more than 5/3. " +
                "Typical rocket combustion gases are between 1.13 and 1.30.");
        }

        return gamma;
    }
}
