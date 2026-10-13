"""Ideal quasi-one-dimensional nozzle-flow relations.

Chapter reference: this module covers the isentropic nozzle theory used in the
nozzle-design chapters of Rocket Propulsion Elements, including ``c*``, thrust
coefficient, Mach-area relations, and ideal exhaust velocity.
"""

import math

from .constants import UNIVERSAL_GAS_CONSTANT
from .guard import gamma as gamma_guard
from .guard import positive


def vandenkerckhove_function(gamma: float) -> float:
    """Compute the Vandenkerckhove function often denoted by capital Gamma.

    Args:
        gamma: Ratio of specific heats for the exhaust gas.

    Returns:
        The dimensionless choked-flow function.

    Raises:
        ValueError: If gamma lies outside the supported range.

    Physics:
        This factor appears in the choked mass-flow relation and in the ideal
        characteristic velocity. It packages the isentropic dependence on gamma.
    """
    gamma_guard(gamma)
    exponent = (gamma + 1.0) / (2.0 * (gamma - 1.0))
    return math.sqrt(gamma) * (2.0 / (gamma + 1.0)) ** exponent


def specific_gas_constant(molecular_mass: float) -> float:
    """Convert molecular mass into a mixture-specific gas constant.

    Args:
        molecular_mass: Mean molecular mass in kilograms per kilomole.

    Returns:
        Specific gas constant in joules per kilogram-kelvin.

    Raises:
        ValueError: If molecular mass is not strictly positive.

    Physics:
        The universal gas constant applies per kilomole. Dividing by molecular
        mass yields the per-unit-mass gas constant needed in propulsion formulas.
    """
    positive(molecular_mass, "molecular_mass")
    return UNIVERSAL_GAS_CONSTANT / molecular_mass


def ideal_exhaust_velocity(
    gamma: float,
    chamber_temperature: float,
    molecular_mass: float,
    chamber_pressure: float,
    exit_pressure: float,
) -> float:
    """Compute ideal isentropic exhaust velocity.

    Args:
        gamma: Ratio of specific heats.
        chamber_temperature: Chamber stagnation temperature in kelvin.
        molecular_mass: Mean molecular mass in kilograms per kilomole.
        chamber_pressure: Chamber pressure in pascals.
        exit_pressure: Exit pressure in pascals.

    Returns:
        Ideal exit velocity in metres per second.

    Raises:
        ValueError: If inputs are invalid or exit pressure exceeds chamber pressure.

    Physics:
        The equation comes from converting the drop in stagnation enthalpy into
        directed kinetic energy for an isentropic nozzle expansion.
    """
    gamma_guard(gamma)
    positive(chamber_temperature, "chamber_temperature")
    positive(molecular_mass, "molecular_mass")
    positive(chamber_pressure, "chamber_pressure")
    positive(exit_pressure, "exit_pressure")
    if exit_pressure > chamber_pressure:
        raise ValueError("exit_pressure cannot exceed chamber_pressure.")
    gas_constant = specific_gas_constant(molecular_mass)
    pressure_ratio_term = 1.0 - (exit_pressure / chamber_pressure) ** ((gamma - 1.0) / gamma)
    coefficient = (2.0 * gamma) / (gamma - 1.0)
    return math.sqrt(coefficient * gas_constant * chamber_temperature * pressure_ratio_term)


def characteristic_velocity(gamma: float, chamber_temperature: float, molecular_mass: float) -> float:
    """Compute ideal characteristic velocity ``c*``.

    Args:
        gamma: Ratio of specific heats.
        chamber_temperature: Chamber temperature in kelvin.
        molecular_mass: Mean molecular mass in kilograms per kilomole.

    Returns:
        Characteristic velocity in metres per second.

    Raises:
        ValueError: If any input is outside its physical range.

    Physics:
        ``c*`` measures how effectively chamber thermal energy and chemistry can
        feed mass through a choked throat, independent of expansion ratio.
    """
    gamma_guard(gamma)
    positive(chamber_temperature, "chamber_temperature")
    positive(molecular_mass, "molecular_mass")
    gas_constant = specific_gas_constant(molecular_mass)
    return math.sqrt(gas_constant * chamber_temperature) / vandenkerckhove_function(gamma)


def thrust_coefficient(
    gamma: float,
    chamber_pressure: float,
    exit_pressure: float,
    ambient_pressure: float,
    area_ratio: float,
) -> float:
    """Compute ideal nozzle thrust coefficient.

    Args:
        gamma: Ratio of specific heats.
        chamber_pressure: Chamber pressure in pascals.
        exit_pressure: Exit pressure in pascals.
        ambient_pressure: Ambient pressure in pascals.
        area_ratio: Exit area divided by throat area.

    Returns:
        Dimensionless thrust coefficient.

    Raises:
        ValueError: If the thermodynamic inputs are invalid.

    Physics:
        ``C_F`` combines the momentum thrust from gas acceleration and the exit
        pressure thrust into a nozzle-performance multiplier relative to ``p_c A_t``.
    """
    gamma_guard(gamma)
    positive(chamber_pressure, "chamber_pressure")
    positive(exit_pressure, "exit_pressure")
    positive(area_ratio, "area_ratio")
    if exit_pressure > chamber_pressure:
        raise ValueError("exit_pressure cannot exceed chamber_pressure.")
    if ambient_pressure < 0.0 or not math.isfinite(ambient_pressure):
        raise ValueError("ambient_pressure must be a finite non-negative value.")
    leading = (2.0 * gamma * gamma) / (gamma - 1.0)
    expansion = (2.0 / (gamma + 1.0)) ** ((gamma + 1.0) / (gamma - 1.0))
    pressure_drop = 1.0 - (exit_pressure / chamber_pressure) ** ((gamma - 1.0) / gamma)
    momentum_term = math.sqrt(leading * expansion * pressure_drop)
    pressure_term = ((exit_pressure - ambient_pressure) / chamber_pressure) * area_ratio
    return momentum_term + pressure_term


def maximum_thrust_coefficient(gamma: float) -> float:
    """Compute the limiting thrust coefficient for optimum expansion.

    Args:
        gamma: Ratio of specific heats.

    Returns:
        Maximum ideal thrust coefficient implied by the source expression.

    Raises:
        ValueError: If gamma is invalid.

    Physics:
        This expression packages the largest momentum contribution reachable by
        an ideal nozzle for the specified specific-heat ratio.
    """
    gamma_guard(gamma)
    return vandenkerckhove_function(gamma) * math.sqrt((2.0 * gamma) / (gamma - 1.0))


def mass_flow_rate(
    gamma: float,
    chamber_pressure: float,
    chamber_temperature: float,
    molecular_mass: float,
    throat_area: float,
) -> float:
    """Compute choked nozzle mass flow rate.

    Args:
        gamma: Ratio of specific heats.
        chamber_pressure: Chamber pressure in pascals.
        chamber_temperature: Chamber temperature in kelvin.
        molecular_mass: Mean molecular mass in kilograms per kilomole.
        throat_area: Nozzle throat area in square metres.

    Returns:
        Choked mass flow rate in kilograms per second.

    Raises:
        ValueError: If inputs are invalid.

    Physics:
        For a choked rocket nozzle, the mass flow is set by chamber conditions,
        gas properties, and throat area through ``m_dot = p_c A_t / c*``.
    """
    positive(chamber_pressure, "chamber_pressure")
    positive(throat_area, "throat_area")
    c_star = characteristic_velocity(gamma, chamber_temperature, molecular_mass)
    return chamber_pressure * throat_area / c_star


def throat_temperature(gamma: float, chamber_temperature: float) -> float:
    """Compute static temperature at a choked throat.

    Args:
        gamma: Ratio of specific heats.
        chamber_temperature: Chamber stagnation temperature in kelvin.

    Returns:
        Throat static temperature in kelvin.

    Raises:
        ValueError: If gamma or chamber temperature is invalid.

    Physics:
        A choked throat corresponds to Mach 1, which gives the familiar sonic
        temperature ratio ``T_t / T_0 = 2 / (gamma + 1)`` for isentropic flow.
    """
    gamma_guard(gamma)
    positive(chamber_temperature, "chamber_temperature")
    return chamber_temperature * (2.0 / (gamma + 1.0))


def throat_pressure(gamma: float, chamber_pressure: float) -> float:
    """Compute static pressure at a choked throat.

    Args:
        gamma: Ratio of specific heats.
        chamber_pressure: Chamber stagnation pressure in pascals.

    Returns:
        Throat static pressure in pascals.

    Raises:
        ValueError: If inputs are invalid.

    Physics:
        At Mach 1 the isentropic pressure ratio depends only on gamma, giving
        the sonic-throat pressure used throughout rocket nozzle sizing.
    """
    gamma_guard(gamma)
    positive(chamber_pressure, "chamber_pressure")
    exponent = gamma / (gamma - 1.0)
    return chamber_pressure * (2.0 / (gamma + 1.0)) ** exponent


def exit_mach_number(gamma: float, chamber_pressure: float, exit_pressure: float) -> float:
    """Infer exit Mach number from the chamber-to-exit pressure ratio.

    Args:
        gamma: Ratio of specific heats.
        chamber_pressure: Chamber pressure in pascals.
        exit_pressure: Exit pressure in pascals.

    Returns:
        Exit Mach number on the supersonic branch.

    Raises:
        ValueError: If pressures are invalid or exit pressure exceeds chamber pressure.

    Physics:
        Rearranging the isentropic pressure relation gives Mach number directly
        once chamber and exit static pressures are known.
    """
    gamma_guard(gamma)
    positive(chamber_pressure, "chamber_pressure")
    positive(exit_pressure, "exit_pressure")
    if exit_pressure > chamber_pressure:
        raise ValueError("exit_pressure cannot exceed chamber_pressure.")
    ratio_term = (chamber_pressure / exit_pressure) ** ((gamma - 1.0) / gamma) - 1.0
    return math.sqrt((2.0 / (gamma - 1.0)) * ratio_term)


def area_ratio_from_mach(gamma: float, mach_number: float) -> float:
    """Compute nozzle area ratio from Mach number.

    Args:
        gamma: Ratio of specific heats.
        mach_number: Local Mach number.

    Returns:
        Area ratio ``A / A*``.

    Raises:
        ValueError: If gamma or Mach number is invalid.

    Physics:
        This is the textbook isentropic area-Mach relation. The supplied C#
        formula is preserved exactly, including the exponent placement.
    """
    gamma_guard(gamma)
    positive(mach_number, "mach_number")
    exponent = (gamma + 1.0) / (2.0 * (gamma - 1.0))
    term = (2.0 / (gamma + 1.0)) * (1.0 + ((gamma - 1.0) / 2.0 * mach_number * mach_number))
    return term ** exponent / mach_number


def area_ratio_from_pressure_ratio(gamma: float, chamber_pressure: float, exit_pressure: float) -> float:
    """Compute area ratio from chamber and exit pressures.

    Args:
        gamma: Ratio of specific heats.
        chamber_pressure: Chamber pressure in pascals.
        exit_pressure: Exit pressure in pascals.

    Returns:
        Exit-to-throat area ratio.

    Raises:
        ValueError: If pressures or gamma are invalid.

    Physics:
        The pressure ratio first determines the exit Mach number, and that Mach
        number then maps to the required area ratio through isentropic flow.
    """
    return area_ratio_from_mach(gamma, exit_mach_number(gamma, chamber_pressure, exit_pressure))


def exit_pressure_from_area_ratio(gamma: float, chamber_pressure: float, area_ratio: float) -> float:
    """Invert the area-Mach relation with a bisection solve.

    Args:
        gamma: Ratio of specific heats.
        chamber_pressure: Chamber pressure in pascals.
        area_ratio: Exit area divided by throat area.

    Returns:
        Exit pressure in pascals.

    Raises:
        ValueError: If inputs are invalid or the area ratio is not supersonic.

    Physics:
        For the supersonic branch, area ratio grows monotonically with Mach, so
        bisection is a robust way to recover the exit Mach number and pressure.
    """
    gamma_guard(gamma)
    positive(chamber_pressure, "chamber_pressure")
    if area_ratio <= 1.0:
        raise ValueError("area_ratio must be greater than 1.0.")
    low_mach = 1.0 + 1e-9
    high_mach = 100.0
    for _ in range(200):
        mid_mach = 0.5 * (low_mach + high_mach)
        mid_ratio = area_ratio_from_mach(gamma, mid_mach)
        # On the supersonic branch, a larger Mach number means a larger area ratio.
        if mid_ratio < area_ratio:
            low_mach = mid_mach
        else:
            high_mach = mid_mach
    exit_mach = 0.5 * (low_mach + high_mach)
    pressure_ratio = (1.0 + ((gamma - 1.0) / 2.0) * exit_mach * exit_mach) ** (-gamma / (gamma - 1.0))
    return chamber_pressure * pressure_ratio


def optimum_area_ratio(gamma: float, chamber_pressure: float, ambient_pressure: float) -> float:
    """Compute the area ratio for perfect expansion at the ambient pressure.

    Args:
        gamma: Ratio of specific heats.
        chamber_pressure: Chamber pressure in pascals.
        ambient_pressure: Ambient pressure in pascals.

    Returns:
        Area ratio that makes exit pressure equal ambient pressure.

    Raises:
        ValueError: If the thermodynamic inputs are invalid.

    Physics:
        A nozzle is ideally expanded when exit pressure matches the surroundings,
        eliminating pressure-thrust losses from under- or over-expansion.
    """
    return area_ratio_from_pressure_ratio(gamma, chamber_pressure, ambient_pressure)


__all__ = [
    "vandenkerckhove_function",
    "specific_gas_constant",
    "ideal_exhaust_velocity",
    "characteristic_velocity",
    "thrust_coefficient",
    "maximum_thrust_coefficient",
    "mass_flow_rate",
    "throat_temperature",
    "throat_pressure",
    "exit_mach_number",
    "area_ratio_from_mach",
    "area_ratio_from_pressure_ratio",
    "exit_pressure_from_area_ratio",
    "optimum_area_ratio",
]
