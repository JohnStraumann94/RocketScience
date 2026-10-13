"""
vector3.py — Immutable 3-D Cartesian vector.
============================================

Port of ``Astro.Core/Vector3.cs``.

Why not NumPy?  NumPy arrays are mutable and single-precision by default.
Orbit mechanics routinely differences numbers of very different magnitudes —
for example the eccentricity vector calculation subtracts two quantities that
nearly cancel for a near-circular orbit — and 32-bit floats lose so much
significance that eccentricity can come out negative.  Every component here
is a 64-bit Python ``float`` (IEEE 754 double).

The C# original is a ``readonly record struct`` for zero heap allocation.
In Python, ``@dataclass(frozen=True)`` gives the same value semantics and
structural equality, and ``__slots__`` eliminates the per-instance dictionary.
"""

from __future__ import annotations
import math
from dataclasses import dataclass


@dataclass(frozen=True, slots=True)
class Vector3:
    """
    An immutable three-dimensional Cartesian vector.

    Attributes:
        x: Component along the inertial X axis (in an ECI frame, points at
           the vernal equinox).
        y: Component along the inertial Y axis (completes the right-handed
           set in the equatorial plane).
        z: Component along the inertial Z axis (parallel to Earth's spin
           axis, towards the north pole).

    All arithmetic operators create new instances; the original is never
    mutated — which is essential when a vector is used as a dictionary key or
    as a default argument.
    """

    x: float
    y: float
    z: float

    # ------------------------------------------------------------------
    # Factory helpers  (analogues of C# static properties)
    # ------------------------------------------------------------------

    @staticmethod
    def zero() -> "Vector3":
        """The zero vector (0, 0, 0)."""
        return Vector3(0.0, 0.0, 0.0)

    @staticmethod
    def unit_x() -> "Vector3":
        """Unit vector along X."""
        return Vector3(1.0, 0.0, 0.0)

    @staticmethod
    def unit_y() -> "Vector3":
        """Unit vector along Y."""
        return Vector3(0.0, 1.0, 0.0)

    @staticmethod
    def unit_z() -> "Vector3":
        """Unit vector along Z."""
        return Vector3(0.0, 0.0, 1.0)

    # ------------------------------------------------------------------
    # Derived scalar quantities
    # ------------------------------------------------------------------

    @property
    def magnitude(self) -> float:
        """
        The Euclidean length (magnitude) of the vector.

        Uses ``math.sqrt`` on the sum of squares.  We deliberately do *not*
        use a ``hypot``-style algorithm: astrodynamic magnitudes (10³ km,
        10⁰ km/s) are nowhere near the overflow limits of a double, so the
        simple form is both exact enough and considerably faster.
        """
        return math.sqrt(self.x * self.x + self.y * self.y + self.z * self.z)

    @property
    def magnitude_squared(self) -> float:
        """
        The squared magnitude.

        Preferred over :attr:`magnitude` inside formulas such as the vis-viva
        or eccentricity-vector equations, because it avoids a square root.
        """
        return self.x * self.x + self.y * self.y + self.z * self.z

    # ------------------------------------------------------------------
    # Arithmetic operators — all return new Vector3 instances
    # ------------------------------------------------------------------

    def __add__(self, other: "Vector3") -> "Vector3":
        """Vector addition, component by component."""
        return Vector3(self.x + other.x, self.y + other.y, self.z + other.z)

    def __sub__(self, other: "Vector3") -> "Vector3":
        """Vector subtraction, component by component."""
        return Vector3(self.x - other.x, self.y - other.y, self.z - other.z)

    def __neg__(self) -> "Vector3":
        """Unary negation (reverses the direction of the vector)."""
        return Vector3(-self.x, -self.y, -self.z)

    def __mul__(self, s: float) -> "Vector3":
        """Scale the vector by a scalar (vector * scalar)."""
        return Vector3(self.x * s, self.y * s, self.z * s)

    def __rmul__(self, s: float) -> "Vector3":
        """Scale the vector by a scalar (scalar * vector)."""
        return Vector3(self.x * s, self.y * s, self.z * s)

    def __truediv__(self, s: float) -> "Vector3":
        """Divide the vector by a scalar."""
        return Vector3(self.x / s, self.y / s, self.z / s)

    # ------------------------------------------------------------------
    # Vector operations
    # ------------------------------------------------------------------

    @staticmethod
    def dot(a: "Vector3", b: "Vector3") -> float:
        """
        Scalar (dot) product: a · b = |a||b| cos(θ).

        The dot product appears constantly: the radial velocity r · v / |r|
        tells you whether a satellite is climbing towards apoapsis or falling
        towards periapsis, which is exactly the quadrant test needed to resolve
        the true anomaly (see :mod:`orbit_conversions`).
        """
        return a.x * b.x + a.y * b.y + a.z * b.z

    @staticmethod
    def cross(a: "Vector3", b: "Vector3") -> "Vector3":
        """
        Vector (cross) product: a × b.

        The result is perpendicular to both operands; its magnitude is
        |a||b| sin(θ); its direction follows the right-hand rule.

        The single most important application in this library is the specific
        angular momentum vector **h** = **r** × **v**, which is constant for
        the two-body problem and defines the orbital plane.
        """
        return Vector3(
            a.y * b.z - a.z * b.y,
            a.z * b.x - a.x * b.z,
            a.x * b.y - a.y * b.x,
        )

    def normalized(self) -> "Vector3":
        """
        Return a unit-length vector pointing in the same direction.

        Raises:
            ValueError: When the vector is (numerically) the zero vector and
                therefore has no defined direction.  This is not theoretical:
                for an exactly equatorial orbit the node vector **n** = **K** × **h**
                is the zero vector, and callers must special-case that instead
                of silently producing NaNs.
        """
        m = self.magnitude
        if m <= float_info_epsilon():
            raise ValueError(
                "Cannot normalise a zero-length vector — its direction is undefined."
            )
        return self / m

    @staticmethod
    def angle_between(a: "Vector3", b: "Vector3") -> float:
        """
        The unsigned angle between two vectors, in radians, on [0, π].

        Args:
            a: First vector (non-zero).
            b: Second vector (non-zero).

        Returns:
            The angle in radians.

        Raises:
            ValueError: If either vector is the zero vector.

        The naïve implementation ``acos(dot(a, b) / (|a||b|))`` is numerically
        dangerous: rounding can push the argument marginally outside [−1, 1]
        and ``math.acos`` then returns NaN.  Because nearly-parallel vectors
        are the common case in orbit work (think of a transfer arc with a
        small sweep angle) we clamp the cosine before the inverse cosine is
        taken.
        """
        denominator = a.magnitude * b.magnitude
        if denominator <= float_info_epsilon():
            raise ValueError("The angle to a zero-length vector is undefined.")
        cosine = max(-1.0, min(1.0, Vector3.dot(a, b) / denominator))
        return math.acos(cosine)

    # ------------------------------------------------------------------
    # Display
    # ------------------------------------------------------------------

    def to_string(self, precision: int = 6) -> str:
        """
        Format the vector for display, e.g. ``[ 6524.834, 6862.875, 6448.296 ]``.

        Args:
            precision: Number of decimal places.

        Returns:
            A bracket-delimited string, matching the C# ``ToString(format)``
            output when the format is ``Fn`` (fixed-point, n decimal places).
        """
        fmt = f".{precision}f"
        return f"[ {self.x:{fmt}}, {self.y:{fmt}}, {self.z:{fmt}} ]"

    def __repr__(self) -> str:
        return self.to_string(6)

    def __str__(self) -> str:
        return self.to_string(6)


def float_info_epsilon() -> float:
    """Return sys.float_info.epsilon (used as a near-zero threshold)."""
    import sys
    return sys.float_info.epsilon
