"""
angles.py — Angle utilities.
============================

Port of ``Astro.Core/Angles.cs``.

Astrodynamics is full of quantities that are only meaningful modulo a full
revolution, and getting the wrapping wrong is one of the most common sources
of silently wrong answers.  This module centralises all angle operations so
the wrapping logic lives in exactly one place.
"""

import math
from . import constants


def normalize(angle_rad: float) -> float:
    """
    Wrap an angle into the interval [0, 2π).

    Args:
        angle_rad: Any angle in radians.

    Returns:
        The equivalent angle in [0, 2π).

    Why not just ``angle_rad % TWO_PI``?  Python's ``%`` keeps the sign of
    the dividend (like C's ``fmod``), so ``-0.1 % (2*pi)`` gives the correct
    positive value in Python — unlike C# where this needs an extra add step.
    Python's modulo already does the right thing, but we write it explicitly
    to match the C# algorithm and document the intent clearly.
    """
    wrapped = angle_rad % constants.TWO_PI
    # Python's % is always non-negative for a positive divisor, so this
    # guard is technically redundant — but it documents the intent.
    return wrapped if wrapped >= 0.0 else wrapped + constants.TWO_PI


def normalize_signed(angle_rad: float) -> float:
    """
    Wrap an angle into the interval (−π, π].

    Args:
        angle_rad: Any angle in radians.

    Returns:
        The equivalent angle in (−π, π].

    Use this for *differences* between angles, where "−179 degrees" is a much
    more useful answer than "181 degrees".  The implementation first normalises
    to [0, 2π) and then shifts the upper half down.
    """
    wrapped = normalize(angle_rad)
    return wrapped - constants.TWO_PI if wrapped > math.pi else wrapped


def to_radians(degrees: float) -> float:
    """
    Convert degrees to radians.

    Args:
        degrees: Angle in degrees.

    Returns:
        Angle in radians.
    """
    return degrees * constants.DEG_TO_RAD


def to_degrees(radians: float) -> float:
    """
    Convert radians to degrees.

    Args:
        radians: Angle in radians.

    Returns:
        Angle in degrees.
    """
    return radians * constants.RAD_TO_DEG


def safe_acos(cosine: float) -> float:
    """
    A numerically safe ``math.acos`` that clamps its argument to [−1, 1].

    Args:
        cosine: A value that should be in [−1, 1] but may be a few ULPs
                outside due to floating-point rounding.

    Returns:
        ``math.acos(clamp(cosine, -1, 1))``, in [0, π].

    Preventing NaN results is important throughout the library.  For example,
    when computing the inclination from ``h_z / |h|``, rounding can push the
    ratio to 1.000000000000001 for a near-polar orbit, and a raw ``acos``
    would return NaN.  The clamp costs essentially nothing and avoids an
    entire category of silent failures.
    """
    return math.acos(max(-1.0, min(1.0, cosine)))
