"""Shared physical constants and unit conversions.

Chapter reference: these constants support the thermodynamics, nozzle-flow,
and trajectory relations used throughout Rocket Propulsion Elements. The values
match the supplied C# source so calculations stay numerically consistent across
both ports.
"""

import math

STANDARD_GRAVITY = 9.80665
UNIVERSAL_GAS_CONSTANT = 8314.462618
SEA_LEVEL_PRESSURE = 101_325.0
SEA_LEVEL_DENSITY = 1.225
SEA_LEVEL_TEMPERATURE = 288.15
ATMOSPHERIC_SCALE_HEIGHT = 7200.0
EARTH_RADIUS = 6_378_137.0
EARTH_MU = 3.986004418e14
DEG_TO_RAD = math.pi / 180.0
RAD_TO_DEG = 180.0 / math.pi


def pa_to_bar(pa: float) -> float:
    """Convert pressure from pascals to bar.

    Args:
        pa: Absolute or gauge pressure in pascals.

    Returns:
        The same pressure expressed in bar.

    Raises:
        None.

    Physics:
        One bar is defined as exactly 100,000 pascals, so this is a pure scale
        conversion with no change in the physical state being described.
    """
    return pa / 100000.0


def bar_to_pa(bar: float) -> float:
    """Convert pressure from bar to pascals.

    Args:
        bar: Pressure in bar.

    Returns:
        The same pressure expressed in pascals.

    Raises:
        None.

    Physics:
        Rocket chamber and feed-system calculations are usually performed in SI
        units, so bar values are scaled back to pascals before use.
    """
    return bar * 100000.0


def pa_to_psi(pa: float) -> float:
    """Convert pressure from pascals to pounds per square inch.

    Args:
        pa: Pressure in pascals.

    Returns:
        Pressure in psi.

    Raises:
        None.

    Physics:
        This conversion is often needed when comparing SI calculations with
        legacy propulsion hardware data published in imperial units.
    """
    return pa / 6894.757293168


def psi_to_pa(psi: float) -> float:
    """Convert pressure from pounds per square inch to pascals.

    Args:
        psi: Pressure in psi.

    Returns:
        Pressure in pascals.

    Raises:
        None.

    Physics:
        Pressure is force per unit area; the constant below embeds the exact
        pounds-force to newtons and square-inch to square-metre conversion.
    """
    return psi * 6894.757293168


def newtons_to_pounds_force(newtons: float) -> float:
    """Convert force from newtons to pounds-force.

    Args:
        newtons: Force in newtons.

    Returns:
        Force in pounds-force.

    Raises:
        None.

    Physics:
        Thrust is fundamentally a force, so converting between SI and imperial
        systems is a direct scaling of the same momentum flux.
    """
    return newtons / 4.4482216152605


def pounds_force_to_newtons(pounds_force: float) -> float:
    """Convert force from pounds-force to newtons.

    Args:
        pounds_force: Force in pounds-force.

    Returns:
        Force in newtons.

    Raises:
        None.

    Physics:
        This is the inverse of ``newtons_to_pounds_force`` and is commonly used
        when translating engine specifications from catalog data into SI form.
    """
    return pounds_force * 4.4482216152605


__all__ = [
    "STANDARD_GRAVITY",
    "UNIVERSAL_GAS_CONSTANT",
    "SEA_LEVEL_PRESSURE",
    "SEA_LEVEL_DENSITY",
    "SEA_LEVEL_TEMPERATURE",
    "ATMOSPHERIC_SCALE_HEIGHT",
    "EARTH_RADIUS",
    "EARTH_MU",
    "DEG_TO_RAD",
    "RAD_TO_DEG",
    "pa_to_bar",
    "bar_to_pa",
    "pa_to_psi",
    "psi_to_pa",
    "newtons_to_pounds_force",
    "pounds_force_to_newtons",
]
