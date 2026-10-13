using System.Globalization;

namespace Astro.Core;

/// <summary>
/// An immutable three-dimensional Cartesian vector with the handful of operations that
/// classical astrodynamics actually needs (dot, cross, magnitude, unit, angle-between).
/// </summary>
/// <remarks>
/// <para>
/// Why not use <c>System.Numerics.Vector3</c>? Because it is single precision. Orbit
/// mechanics routinely differences numbers of very different magnitudes — for example the
/// eccentricity vector calculation subtracts two quantities that nearly cancel for a
/// near-circular orbit — and 32-bit floats lose so much significance that eccentricity
/// can come out negative. Every component here is a 64-bit <see cref="double"/>.
/// </para>
/// <para>
/// A <c>readonly record struct</c> gives us value semantics, structural equality and zero
/// heap allocation, which matters when propagating an orbit through thousands of steps.
/// </para>
/// </remarks>
/// <param name="X">Component along the inertial X axis.</param>
/// <param name="Y">Component along the inertial Y axis.</param>
/// <param name="Z">Component along the inertial Z axis.</param>
public readonly record struct Vector3(double X, double Y, double Z)
{
    /// <summary>The zero vector (0, 0, 0).</summary>
    public static Vector3 Zero => new(0.0, 0.0, 0.0);

    /// <summary>Unit vector along X — in an Earth-centred inertial frame this points at the vernal equinox.</summary>
    public static Vector3 UnitX => new(1.0, 0.0, 0.0);

    /// <summary>Unit vector along Y — completes the right-handed set in the equatorial plane.</summary>
    public static Vector3 UnitY => new(0.0, 1.0, 0.0);

    /// <summary>Unit vector along Z — parallel to the Earth's spin axis (towards the north pole).</summary>
    public static Vector3 UnitZ => new(0.0, 0.0, 1.0);

    /// <summary>
    /// The Euclidean length (magnitude) of the vector.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="Math.Sqrt"/> on the sum of squares. We deliberately do NOT use a
    /// hypot-style algorithm: astrodynamic magnitudes (10^3 km, 10^0 km/s) are nowhere
    /// near the overflow limits of a double, so the simple form is both exact enough and
    /// considerably faster.
    /// </remarks>
    public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>
    /// The squared magnitude. Preferred over <see cref="Magnitude"/> inside formulas such
    /// as the vis-viva or eccentricity-vector equations because it avoids a square root.
    /// </summary>
    public double MagnitudeSquared => X * X + Y * Y + Z * Z;

    /// <summary>Vector addition, component by component.</summary>
    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    /// <summary>Vector subtraction, component by component.</summary>
    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    /// <summary>Unary negation (reverses the direction of the vector).</summary>
    public static Vector3 operator -(Vector3 a) => new(-a.X, -a.Y, -a.Z);

    /// <summary>Scales a vector by a scalar (scalar on the right).</summary>
    public static Vector3 operator *(Vector3 a, double s) => new(a.X * s, a.Y * s, a.Z * s);

    /// <summary>Scales a vector by a scalar (scalar on the left).</summary>
    public static Vector3 operator *(double s, Vector3 a) => new(a.X * s, a.Y * s, a.Z * s);

    /// <summary>Divides a vector by a scalar.</summary>
    public static Vector3 operator /(Vector3 a, double s) => new(a.X / s, a.Y / s, a.Z / s);

    /// <summary>
    /// Scalar (dot) product: a · b = |a||b| cos(theta).
    /// </summary>
    /// <remarks>
    /// Used constantly: the radial velocity r · v / |r| tells you whether a satellite is
    /// climbing towards apoapsis or falling towards periapsis, which is exactly the
    /// quadrant test needed to resolve the true anomaly (see <see cref="OrbitConversions"/>).
    /// </remarks>
    public static double Dot(Vector3 a, Vector3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    /// <summary>
    /// Vector (cross) product: a x b, a vector perpendicular to both operands whose
    /// magnitude is |a||b| sin(theta) and whose direction follows the right-hand rule.
    /// </summary>
    /// <remarks>
    /// The single most important application in this library is the specific angular
    /// momentum vector h = r x v, which is constant for the two-body problem and defines
    /// the orbital plane.
    /// </remarks>
    public static Vector3 Cross(Vector3 a, Vector3 b) => new(
        a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X);

    /// <summary>
    /// Returns a unit-length vector pointing in the same direction as this one.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the vector is (numerically) the zero vector and therefore has no
    /// defined direction. This is not a theoretical concern: for an exactly equatorial
    /// orbit the node vector n = k x h is the zero vector, and callers must special-case
    /// that instead of silently producing NaNs.
    /// </exception>
    public Vector3 Normalized()
    {
        double m = Magnitude;
        if (m <= double.Epsilon)
        {
            throw new InvalidOperationException(
                "Cannot normalize a zero-length vector — its direction is undefined.");
        }

        return this / m;
    }

    /// <summary>
    /// The unsigned angle between two vectors, in radians, on the interval [0, pi].
    /// </summary>
    /// <remarks>
    /// The naive implementation <c>acos(dot(a,b) / (|a||b|))</c> is numerically dangerous:
    /// rounding can push the argument marginally outside [-1, 1] and <see cref="Math.Acos"/>
    /// then returns NaN. Because nearly-parallel vectors are the common case in orbit work
    /// (think of a transfer arc with a small sweep angle) we clamp the cosine before the
    /// inverse cosine is taken.
    /// </remarks>
    public static double AngleBetween(Vector3 a, Vector3 b)
    {
        double denominator = a.Magnitude * b.Magnitude;
        if (denominator <= double.Epsilon)
        {
            throw new InvalidOperationException(
                "The angle to a zero-length vector is undefined.");
        }

        double cosine = Math.Clamp(Dot(a, b) / denominator, -1.0, 1.0);
        return Math.Acos(cosine);
    }

    /// <summary>
    /// Formats the vector for display, e.g. <c>[ 6524.834, 6862.875, 6448.296 ]</c>.
    /// </summary>
    /// <param name="format">A standard or custom .NET numeric format string.</param>
    public string ToString(string format) => string.Create(
        CultureInfo.InvariantCulture,
        $"[ {X.ToString(format, CultureInfo.InvariantCulture)}, {Y.ToString(format, CultureInfo.InvariantCulture)}, {Z.ToString(format, CultureInfo.InvariantCulture)} ]");

    /// <inheritdoc/>
    public override string ToString() => ToString("F6");
}
