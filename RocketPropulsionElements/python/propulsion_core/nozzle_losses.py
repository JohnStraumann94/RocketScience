"""Simple nozzle-efficiency and atmospheric-loss helpers.

Chapter reference: these relations correspond to practical nozzle losses and
altitude effects discussed alongside ideal thrust-coefficient theory.
"""

import enum
import math

from .constants import ATMOSPHERIC_SCALE_HEIGHT, SEA_LEVEL_PRESSURE
from .guard import non_negative, positive


class ExpansionState(enum.Enum):
    """Qualitative nozzle expansion state relative to the ambient pressure."""

    UNDER_EXPANDED = "under_expanded"
    OPTIMUM = "optimum"
    OVER_EXPANDED = "over_expanded"


def combined_nozzle_efficiency(
    divergence_factor: float,
    friction_factor: float,
    two_phase_factor: float = 1.0,
    chemical_factor: float = 1.0,
) -> float:
    """Multiply the major nozzle-efficiency factors into one value.

    Args:
        divergence_factor: Loss factor from non-axial exhaust flow.
        friction_factor: Loss factor from wall friction and boundary layers.
        two_phase_factor: Optional loss factor for condensed phases.
        chemical_factor: Optional loss factor for incomplete chemistry.

    Returns:
        Combined nozzle efficiency.

    Raises:
        ValueError: If any factor is negative.

    Physics:
        Independent efficiency factors are commonly combined multiplicatively so
        each mechanism reduces the ideal performance in sequence.
    """
    non_negative(divergence_factor, "divergence_factor")
    non_negative(friction_factor, "friction_factor")
    non_negative(two_phase_factor, "two_phase_factor")
    non_negative(chemical_factor, "chemical_factor")
    return divergence_factor * friction_factor * two_phase_factor * chemical_factor


def cstar_efficiency(measured_cstar: float, theoretical_cstar: float) -> float:
    """Compare measured characteristic velocity with its theoretical ideal.

    Args:
        measured_cstar: Measured or achieved characteristic velocity.
        theoretical_cstar: Ideal characteristic velocity.

    Returns:
        The ratio ``c*_measured / c*_ideal``.

    Raises:
        ValueError: If measured ``c*`` is negative or theoretical ``c*`` is not positive.

    Physics:
        ``c*`` efficiency isolates combustion quality by comparing realized
        chamber performance to the ideal equilibrium prediction.
    """
    non_negative(measured_cstar, "measured_cstar")
    positive(theoretical_cstar, "theoretical_cstar")
    return measured_cstar / theoretical_cstar


def classify_expansion(exit_pressure: float, ambient_pressure: float, tolerance: float = 0.02) -> ExpansionState:
    """Classify whether a nozzle is under-expanded, optimum, or over-expanded.

    Args:
        exit_pressure: Nozzle exit pressure in pascals.
        ambient_pressure: Ambient pressure in pascals.
        tolerance: Relative pressure-match tolerance around perfect expansion.

    Returns:
        An ``ExpansionState`` enumeration value.

    Raises:
        ValueError: If pressures are invalid or tolerance is negative.

    Physics:
        Perfect expansion occurs when exit and ambient pressures match. A small
        relative tolerance avoids noisy classification changes near equality.
    """
    positive(exit_pressure, "exit_pressure")
    positive(ambient_pressure, "ambient_pressure")
    non_negative(tolerance, "tolerance")
    relative_error = abs(exit_pressure - ambient_pressure) / ambient_pressure
    if relative_error <= tolerance:
        return ExpansionState.OPTIMUM
    if exit_pressure > ambient_pressure:
        return ExpansionState.UNDER_EXPANDED
    return ExpansionState.OVER_EXPANDED


def is_flow_separation_likely(exit_pressure: float, ambient_pressure: float) -> bool:
    """Estimate whether an over-expanded nozzle may separate internally.

    Args:
        exit_pressure: Nozzle exit pressure in pascals.
        ambient_pressure: Ambient pressure in pascals.

    Returns:
        ``True`` when the simple exit-pressure criterion indicates separation risk.

    Raises:
        ValueError: If either pressure is not strictly positive.

    Physics:
        Strong over-expansion can trigger internal shock-induced separation.
        The source rule of thumb flags this when ``p_e < 0.4 * p_a``.
    """
    positive(exit_pressure, "exit_pressure")
    positive(ambient_pressure, "ambient_pressure")
    return exit_pressure < 0.4 * ambient_pressure


def ambient_pressure_at_altitude(altitude_metres: float) -> float:
    """Approximate atmospheric pressure with a single scale-height model.

    Args:
        altitude_metres: Geopotential altitude in metres.

    Returns:
        Ambient pressure in pascals.

    Raises:
        ValueError: If altitude is negative.

    Physics:
        A single exponential atmosphere is a crude but useful first estimate for
        launch-performance studies over the lower atmosphere.
    """
    non_negative(altitude_metres, "altitude_metres")
    return SEA_LEVEL_PRESSURE * math.exp(-altitude_metres / ATMOSPHERIC_SCALE_HEIGHT)


__all__ = [
    "ExpansionState",
    "combined_nozzle_efficiency",
    "cstar_efficiency",
    "classify_expansion",
    "is_flow_separation_likely",
    "ambient_pressure_at_altitude",
]
