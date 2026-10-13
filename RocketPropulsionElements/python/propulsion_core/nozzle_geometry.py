"""Geometric helper relations for rocket nozzles and chambers.

Chapter reference: these formulas support the nozzle layout and chamber-sizing
material in Rocket Propulsion Elements, connecting simple diameters, areas, and
length estimates to performance calculations.
"""

import math

from .constants import DEG_TO_RAD
from .guard import in_range, positive


def area_from_diameter(diameter: float) -> float:
    """Compute circular area from diameter.

    Args:
        diameter: Circle diameter in metres.

    Returns:
        Area in square metres.

    Raises:
        ValueError: If diameter is not strictly positive.

    Physics:
        Circular throat and exit sections are usually described by diameter in
        hardware drawings even though fluid relations require area.
    """
    positive(diameter, "diameter")
    return math.pi * diameter * diameter / 4.0


def diameter_from_area(area: float) -> float:
    """Compute equivalent circular diameter from area.

    Args:
        area: Circular area in square metres.

    Returns:
        Diameter in metres.

    Raises:
        ValueError: If area is not strictly positive.

    Physics:
        This is the inverse of ``area_from_diameter`` and is convenient when a
        required throat or exit area must be turned into a manufacturing size.
    """
    positive(area, "area")
    return math.sqrt(4.0 * area / math.pi)


def required_throat_area(thrust: float, thrust_coefficient: float, chamber_pressure: float) -> float:
    """Compute required throat area from the thrust coefficient definition.

    Args:
        thrust: Required thrust in newtons.
        thrust_coefficient: Ideal or effective thrust coefficient.
        chamber_pressure: Chamber pressure in pascals.

    Returns:
        Required throat area in square metres.

    Raises:
        ValueError: If any input is not strictly positive.

    Physics:
        Rearranging ``F = C_F p_c A_t`` gives the throat area needed to support
        a target thrust at the specified chamber pressure and nozzle efficiency.
    """
    positive(thrust, "thrust")
    positive(thrust_coefficient, "thrust_coefficient")
    positive(chamber_pressure, "chamber_pressure")
    return thrust / (thrust_coefficient * chamber_pressure)


def conical_divergence_factor(half_angle_degrees: float) -> float:
    """Compute the ideal divergence efficiency for a conical nozzle.

    Args:
        half_angle_degrees: Nozzle half-angle in degrees.

    Returns:
        Divergence factor between zero and one.

    Raises:
        ValueError: If the half-angle lies outside the stated range.

    Physics:
        Exhaust in a conical nozzle does not leave perfectly axial. Averaging
        the axial momentum component gives ``(1 + cos(alpha)) / 2``.
    """
    in_range(half_angle_degrees, 0.0, 89.0, "half_angle_degrees")
    return (1.0 + math.cos(half_angle_degrees * DEG_TO_RAD)) / 2.0


def conical_nozzle_length(
    throat_radius: float,
    area_ratio: float,
    half_angle_degrees: float,
    throat_arc_ratio: float = 1.5,
) -> float:
    """Estimate conical nozzle length from throat and expansion geometry.

    Args:
        throat_radius: Throat radius in metres.
        area_ratio: Exit area divided by throat area.
        half_angle_degrees: Conical half-angle in degrees.
        throat_arc_ratio: Radius of the throat blend divided by throat radius.

    Returns:
        Estimated nozzle length in metres.

    Raises:
        ValueError: If any geometry parameter is invalid.

    Physics:
        The nozzle wall length is modeled as a throat blend arc followed by a
        straight conical section. The exit radius scales with ``sqrt(area_ratio)``.
    """
    positive(throat_radius, "throat_radius")
    if area_ratio < 1.0:
        raise ValueError("area_ratio must be at least 1.0.")
    in_range(half_angle_degrees, 0.0, 89.0, "half_angle_degrees")
    positive(throat_arc_ratio, "throat_arc_ratio")
    alpha = half_angle_degrees * DEG_TO_RAD
    arc_radius = throat_arc_ratio * throat_radius
    straight_part = throat_radius * (math.sqrt(area_ratio) - 1.0)
    arc_part = arc_radius * ((1.0 / math.cos(alpha)) - 1.0)
    return (straight_part + arc_part) / math.tan(alpha)


def bell_nozzle_length(throat_radius: float, area_ratio: float, length_fraction: float = 0.8) -> float:
    """Estimate bell nozzle length as a fraction of a 15-degree cone.

    Args:
        throat_radius: Throat radius in metres.
        area_ratio: Exit area divided by throat area.
        length_fraction: Fraction of the equivalent 15-degree conical length.

    Returns:
        Estimated bell nozzle length in metres.

    Raises:
        ValueError: If geometry inputs are invalid.

    Physics:
        Bell nozzles are commonly characterized as a fraction of the longer
        conical nozzle that would provide the same expansion ratio.
    """
    positive(length_fraction, "length_fraction")
    return length_fraction * conical_nozzle_length(throat_radius, area_ratio, 15.0)


def chamber_volume(characteristic_length: float, throat_area: float) -> float:
    """Compute chamber volume from characteristic length and throat area.

    Args:
        characteristic_length: Characteristic length ``L*`` in metres.
        throat_area: Throat area in square metres.

    Returns:
        Chamber volume in cubic metres.

    Raises:
        ValueError: If either input is not strictly positive.

    Physics:
        ``L*`` is defined as chamber volume divided by throat area, so chamber
        volume follows directly from ``V_c = L* A_t``.
    """
    positive(characteristic_length, "characteristic_length")
    positive(throat_area, "throat_area")
    return characteristic_length * throat_area


def contraction_ratio(chamber_area: float, throat_area: float) -> float:
    """Compute chamber-to-throat contraction ratio.

    Args:
        chamber_area: Chamber cross-sectional area in square metres.
        throat_area: Throat area in square metres.

    Returns:
        Contraction ratio ``A_c / A_t``.

    Raises:
        ValueError: If either area is not strictly positive.

    Physics:
        Chamber contraction ratio indicates how strongly the flow must contract
        before reaching the choked throat and affects chamber residence time.
    """
    positive(chamber_area, "chamber_area")
    positive(throat_area, "throat_area")
    return chamber_area / throat_area


__all__ = [
    "area_from_diameter",
    "diameter_from_area",
    "required_throat_area",
    "conical_divergence_factor",
    "conical_nozzle_length",
    "bell_nozzle_length",
    "chamber_volume",
    "contraction_ratio",
]
