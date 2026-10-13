"""Basic thermochemical and gas-dynamics helper relations.

Chapter reference: these formulas support the combustion and nozzle chapters of
Rocket Propulsion Elements by relating gas properties, temperature, pressure,
and density under ideal-gas assumptions.
"""

import math

from .guard import gamma as gamma_guard
from .guard import non_negative, positive


def specific_heat_constant_pressure(gamma: float, specific_gas_constant: float) -> float:
    """Compute constant-pressure specific heat for an ideal gas.

    Args:
        gamma: Ratio of specific heats.
        specific_gas_constant: Specific gas constant in joules per kilogram-kelvin.

    Returns:
        Constant-pressure specific heat in joules per kilogram-kelvin.

    Raises:
        ValueError: If gamma or gas constant is invalid.

    Physics:
        For an ideal gas, ``c_p`` and ``R`` are linked through the heat-capacity
        ratio by ``c_p = gamma R / (gamma - 1)``.
    """
    gamma_guard(gamma)
    positive(specific_gas_constant, "specific_gas_constant")
    return gamma * specific_gas_constant / (gamma - 1.0)


def specific_heat_constant_volume(gamma: float, specific_gas_constant: float) -> float:
    """Compute constant-volume specific heat for an ideal gas.

    Args:
        gamma: Ratio of specific heats.
        specific_gas_constant: Specific gas constant in joules per kilogram-kelvin.

    Returns:
        Constant-volume specific heat in joules per kilogram-kelvin.

    Raises:
        ValueError: If gamma or gas constant is invalid.

    Physics:
        Ideal-gas relations give ``c_v = R / (gamma - 1)``, which is useful in
        energy-balance and combustion-temperature calculations.
    """
    gamma_guard(gamma)
    positive(specific_gas_constant, "specific_gas_constant")
    return specific_gas_constant / (gamma - 1.0)


def speed_of_sound(gamma: float, specific_gas_constant: float, temperature: float) -> float:
    """Compute ideal-gas speed of sound.

    Args:
        gamma: Ratio of specific heats.
        specific_gas_constant: Specific gas constant in joules per kilogram-kelvin.
        temperature: Static temperature in kelvin.

    Returns:
        Speed of sound in metres per second.

    Raises:
        ValueError: If inputs are invalid.

    Physics:
        Acoustic speed in an ideal gas follows ``a = sqrt(gamma R T)`` and sets
        the scale for Mach number in nozzles and high-speed flight.
    """
    gamma_guard(gamma)
    positive(specific_gas_constant, "specific_gas_constant")
    positive(temperature, "temperature")
    return math.sqrt(gamma * specific_gas_constant * temperature)


def stagnation_temperature_ratio(gamma: float, mach_number: float) -> float:
    """Compute the total-to-static temperature ratio.

    Args:
        gamma: Ratio of specific heats.
        mach_number: Local Mach number.

    Returns:
        ``T0 / T`` for isentropic flow.

    Raises:
        ValueError: If gamma is invalid or Mach number is negative.

    Physics:
        The ratio expresses how translational kinetic energy augments static
        temperature when a compressible flow is brought to rest isentropically.
    """
    gamma_guard(gamma)
    non_negative(mach_number, "mach_number")
    return 1.0 + ((gamma - 1.0) / 2.0) * mach_number * mach_number


def stagnation_pressure_ratio(gamma: float, mach_number: float) -> float:
    """Compute the total-to-static pressure ratio for isentropic flow.

    Args:
        gamma: Ratio of specific heats.
        mach_number: Local Mach number.

    Returns:
        ``p0 / p`` for isentropic flow.

    Raises:
        ValueError: If gamma is invalid or Mach number is negative.

    Physics:
        Pressure ratio follows from the temperature ratio raised to the
        isentropic exponent ``gamma / (gamma - 1)``.
    """
    gamma_guard(gamma)
    non_negative(mach_number, "mach_number")
    return stagnation_temperature_ratio(gamma, mach_number) ** (gamma / (gamma - 1.0))


def static_temperature(gamma: float, chamber_temperature: float, mach_number: float) -> float:
    """Compute static temperature from stagnation temperature and Mach number.

    Args:
        gamma: Ratio of specific heats.
        chamber_temperature: Stagnation temperature in kelvin.
        mach_number: Local Mach number.

    Returns:
        Static temperature in kelvin.

    Raises:
        ValueError: If inputs are invalid.

    Physics:
        Accelerating a gas trades stagnation enthalpy for directed kinetic
        energy, so the static temperature falls by the stagnation ratio.
    """
    gamma_guard(gamma)
    positive(chamber_temperature, "chamber_temperature")
    non_negative(mach_number, "mach_number")
    return chamber_temperature / stagnation_temperature_ratio(gamma, mach_number)


def adiabatic_flame_temperature(heat_of_reaction: float, specific_heat: float, initial_temperature: float = 298.15) -> float:
    """Estimate adiabatic flame temperature from a lumped heat release.

    Args:
        heat_of_reaction: Released heat per unit mass in joules per kilogram.
        specific_heat: Effective specific heat in joules per kilogram-kelvin.
        initial_temperature: Initial reactant temperature in kelvin.

    Returns:
        Estimated adiabatic flame temperature in kelvin.

    Raises:
        ValueError: If specific heat or initial temperature is not positive.

    Physics:
        In a constant-pressure energy balance, released chemical energy raises
        temperature by ``delta_T = Q / c_p`` when losses are neglected.
    """
    positive(specific_heat, "specific_heat")
    positive(initial_temperature, "initial_temperature")
    return initial_temperature + heat_of_reaction / specific_heat


def density(pressure: float, specific_gas_constant: float, temperature: float) -> float:
    """Compute ideal-gas density.

    Args:
        pressure: Gas pressure in pascals.
        specific_gas_constant: Specific gas constant in joules per kilogram-kelvin.
        temperature: Temperature in kelvin.

    Returns:
        Density in kilograms per cubic metre.

    Raises:
        ValueError: If any input is not strictly positive.

    Physics:
        The ideal gas law rearranges to ``rho = p / (R T)``, linking state
        variables needed in chamber, injector, and trajectory calculations.
    """
    positive(pressure, "pressure")
    positive(specific_gas_constant, "specific_gas_constant")
    positive(temperature, "temperature")
    return pressure / (specific_gas_constant * temperature)


__all__ = [
    "specific_heat_constant_pressure",
    "specific_heat_constant_volume",
    "speed_of_sound",
    "stagnation_temperature_ratio",
    "stagnation_pressure_ratio",
    "static_temperature",
    "adiabatic_flame_temperature",
    "density",
]
