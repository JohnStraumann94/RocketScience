"""
guard.py — Internal argument-validation helpers.
================================================

Port of ``Astro.Core/Guard.cs``.

Keeping the validation in one place stops the physics code from being buried
under ``if x <= 0: raise ...`` noise.  Every function raises :exc:`ValueError`
(the Python analogue of C#'s ``ArgumentOutOfRangeException`` /
``ArgumentException``).
"""

import math


def positive(value: float, param_name: str) -> None:
    """
    Raise if *value* is not strictly greater than zero (or is NaN).

    Args:
        value:      The number to validate.
        param_name: Name of the caller's parameter, used in the error message.

    Raises:
        ValueError: When *value* is NaN, negative, or zero.

    The check ``math.isnan`` catches the NaN case that ``value <= 0`` would
    silently pass (because ``float('nan') <= 0`` is ``False`` in Python).
    """
    if math.isnan(value) or value <= 0.0:
        raise ValueError(
            f"{param_name} must be a positive, finite number; got {value!r}."
        )


def non_negative(value: float, param_name: str) -> None:
    """
    Raise if *value* is negative or NaN (zero is allowed).

    Args:
        value:      The number to validate.
        param_name: Name of the caller's parameter.

    Raises:
        ValueError: When *value* is NaN or negative.
    """
    if math.isnan(value) or value < 0.0:
        raise ValueError(
            f"{param_name} must be zero or positive; got {value!r}."
        )


def in_range(value: float, minimum: float, maximum: float, param_name: str) -> None:
    """
    Raise if *value* falls outside the inclusive range [*minimum*, *maximum*].

    Args:
        value:      The number to validate.
        minimum:    Lower bound (inclusive).
        maximum:    Upper bound (inclusive).
        param_name: Name of the caller's parameter.

    Raises:
        ValueError: When *value* is NaN or outside [minimum, maximum].
    """
    if math.isnan(value) or value < minimum or value > maximum:
        raise ValueError(
            f"{param_name} must lie between {minimum} and {maximum}; got {value!r}."
        )
