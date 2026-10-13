"""Validation helpers shared by the propulsion package.

Chapter reference: these checks are not tied to a single chapter; they provide
consistent input validation for all of the physical models in the package.
"""

import math


def positive(value: float, name: str) -> float:
    """Require a strictly positive finite scalar.

    Args:
        value: Numeric value to validate.
        name: Parameter name used in the error message.

    Returns:
        The validated value so callers can use the helper inline.

    Raises:
        ValueError: If the value is NaN, infinite, zero, or negative.

    Physics:
        Quantities such as mass, pressure, temperature, and area only make
        physical sense in these models when they are finite and positive.
    """
    if not math.isfinite(value) or value <= 0.0:
        raise ValueError(f"{name} must be a finite positive value.")
    return value


def non_negative(value: float, name: str) -> float:
    """Require a finite scalar that is zero or greater.

    Args:
        value: Numeric value to validate.
        name: Parameter name used in the error message.

    Returns:
        The validated value.

    Raises:
        ValueError: If the value is NaN, infinite, or negative.

    Physics:
        Time, altitude, mixture ratio allowances, and similar bookkeeping
        quantities are often allowed to be zero but cannot be negative.
    """
    if not math.isfinite(value) or value < 0.0:
        raise ValueError(f"{name} must be a finite non-negative value.")
    return value


def in_range(value: float, minimum: float, maximum: float, name: str) -> float:
    """Require a finite scalar inside a closed interval.

    Args:
        value: Numeric value to validate.
        minimum: Inclusive lower bound.
        maximum: Inclusive upper bound.
        name: Parameter name used in the error message.

    Returns:
        The validated value.

    Raises:
        ValueError: If the value is not finite or lies outside the bounds.

    Physics:
        Several empirical nozzle and trajectory relations are only valid over a
        limited interval, so this helper keeps those models inside their stated
        domain of applicability.
    """
    if not math.isfinite(value) or value < minimum or value > maximum:
        raise ValueError(f"{name} must be between {minimum} and {maximum}, inclusive.")
    return value


def gamma(value: float, name: str = "gamma") -> float:
    """Require a plausible ratio of specific heats.

    Args:
        value: Candidate heat-capacity ratio.
        name: Parameter name used in the error message.

    Returns:
        The validated gamma value.

    Raises:
        ValueError: If gamma is not finite, is less than or equal to one, or is
            above the supplied C# upper limit.

    Physics:
        For calorically perfect gases used in textbook rocket analysis,
        ``gamma`` must exceed one. The upper bound preserves the same practical
        range accepted by the source library.
    """
    if not math.isfinite(value) or value <= 1.0 or value > 1.6667:
        raise ValueError(f"{name} must be finite, greater than 1.0, and at most 1.6667.")
    return value


__all__ = ["positive", "non_negative", "in_range", "gamma"]
