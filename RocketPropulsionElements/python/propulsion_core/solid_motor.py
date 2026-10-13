"""Solid-rocket internal ballistics and grain-geometry helpers.

Chapter reference: these utilities cover simple burn-rate laws, chamber-pressure
equilibrium, BATES grain geometry, and stability indicators from the solid
propulsion chapters of Rocket Propulsion Elements.
"""

import math

from .guard import non_negative, positive


def burn_rate(burn_rate_coefficient: float, chamber_pressure: float, pressure_exponent: float) -> float:
    """Compute solid-propellant burn rate from Saint Robert's law.

    Args:
        burn_rate_coefficient: Burn-rate coefficient ``a``.
        chamber_pressure: Chamber pressure in pascals.
        pressure_exponent: Pressure exponent ``n``.

    Returns:
        Burn rate in metres per second if ``a`` is expressed consistently.

    Raises:
        ValueError: If coefficient or chamber pressure is invalid.

    Physics:
        A common empirical solid-propellant law is ``r = a p^n``, which links
        chamber pressure directly to regression rate.
    """
    positive(burn_rate_coefficient, "burn_rate_coefficient")
    non_negative(chamber_pressure, "chamber_pressure")
    return burn_rate_coefficient * chamber_pressure ** pressure_exponent


def klemmung(burning_surface_area: float, throat_area: float) -> float:
    """Compute the solid-motor loading ratio ``A_b / A_t``.

    Args:
        burning_surface_area: Burning propellant area in square metres.
        throat_area: Nozzle throat area in square metres.

    Returns:
        Dimensionless loading ratio.

    Raises:
        ValueError: If either area is not strictly positive.

    Physics:
        The ratio of burning area to throat area strongly influences the
        equilibrium chamber pressure of a solid motor.
    """
    positive(burning_surface_area, "burning_surface_area")
    positive(throat_area, "throat_area")
    return burning_surface_area / throat_area


def equilibrium_chamber_pressure(
    burning_surface_area: float,
    throat_area: float,
    propellant_density: float,
    characteristic_velocity: float,
    burn_rate_coefficient: float,
    pressure_exponent: float,
) -> float:
    """Estimate steady chamber pressure for a simple solid motor.

    Args:
        burning_surface_area: Burning surface area in square metres.
        throat_area: Throat area in square metres.
        propellant_density: Propellant density in kilograms per cubic metre.
        characteristic_velocity: Characteristic velocity in metres per second.
        burn_rate_coefficient: Burn-rate coefficient ``a``.
        pressure_exponent: Pressure exponent ``n``.

    Returns:
        Equilibrium chamber pressure in pascals.

    Raises:
        ValueError: If any geometric or propellant input is invalid or if ``n >= 1``.

    Physics:
        Equilibrium comes from balancing propellant generation ``rho A_b r`` with
        nozzle discharge ``p_c A_t / c*`` and substituting ``r = a p^n``.
    """
    positive(propellant_density, "propellant_density")
    positive(characteristic_velocity, "characteristic_velocity")
    positive(burn_rate_coefficient, "burn_rate_coefficient")
    if pressure_exponent >= 1.0:
        raise ValueError("pressure_exponent must be less than 1.0.")
    loading = klemmung(burning_surface_area, throat_area)
    return (loading * propellant_density * burn_rate_coefficient * characteristic_velocity) ** (1.0 / (1.0 - pressure_exponent))


def is_stable(pressure_exponent: float) -> bool:
    """Return whether the source stability criterion is satisfied.

    Args:
        pressure_exponent: Burn-rate pressure exponent ``n``.

    Returns:
        ``True`` when ``n < 1``.

    Raises:
        None.

    Physics:
        When ``n`` approaches or exceeds one, burn-rate feedback with pressure
        becomes strong enough to undermine steady equilibrium.
    """
    return pressure_exponent < 1.0


def web_burned(burn_rate: float, time_seconds: float) -> float:
    """Compute how much propellant web has regressed in a given time.

    Args:
        burn_rate: Regression rate in metres per second.
        time_seconds: Burn duration in seconds.

    Returns:
        Burned web thickness in metres.

    Raises:
        ValueError: If burn rate or time is negative.

    Physics:
        At constant regression rate, web burned grows linearly with time.
    """
    non_negative(burn_rate, "burn_rate")
    non_negative(time_seconds, "time_seconds")
    return burn_rate * time_seconds


def bates_grain_burning_area(
    outer_diameter: float,
    initial_core_diameter: float,
    grain_length: float,
    web_burned_value: float,
    ends_inhibited: bool = False,
) -> float:
    """Compute instantaneous burning area for a BATES grain.

    Args:
        outer_diameter: Grain outer diameter in metres.
        initial_core_diameter: Initial inner core diameter in metres.
        grain_length: Grain length in metres.
        web_burned_value: Radial regression distance in metres.
        ends_inhibited: Whether the grain ends are inhibited from burning.

    Returns:
        Burning area in square metres, or zero once the grain is consumed.

    Raises:
        ValueError: If the grain geometry inputs are invalid.

    Physics:
        The core burns outward and, when not inhibited, the grain ends also burn
        inward. The total area is the core lateral area plus any active end area.
    """
    positive(outer_diameter, "outer_diameter")
    positive(initial_core_diameter, "initial_core_diameter")
    positive(grain_length, "grain_length")
    non_negative(web_burned_value, "web_burned")
    if initial_core_diameter >= outer_diameter:
        raise ValueError("initial_core_diameter must be less than outer_diameter.")
    current_core_diameter = initial_core_diameter + 2.0 * web_burned_value
    current_length = grain_length if ends_inhibited else grain_length - 2.0 * web_burned_value
    if current_core_diameter >= outer_diameter or current_length <= 0.0:
        return 0.0
    core_area = math.pi * current_core_diameter * current_length
    if ends_inhibited:
        return core_area
    end_area = 2.0 * math.pi / 4.0 * (outer_diameter * outer_diameter - current_core_diameter * current_core_diameter)
    return core_area + end_area


def bates_grain_mass(outer_diameter: float, core_diameter: float, grain_length: float, propellant_density: float) -> float:
    """Compute BATES grain propellant mass from cylindrical volume.

    Args:
        outer_diameter: Grain outer diameter in metres.
        core_diameter: Core diameter in metres.
        grain_length: Grain length in metres.
        propellant_density: Propellant density in kilograms per cubic metre.

    Returns:
        Propellant mass in kilograms.

    Raises:
        ValueError: If geometry or density inputs are invalid.

    Physics:
        Grain mass is density times the annular cylinder volume remaining
        between the outer case wall and the internal core.
    """
    positive(outer_diameter, "outer_diameter")
    non_negative(core_diameter, "core_diameter")
    positive(grain_length, "grain_length")
    positive(propellant_density, "propellant_density")
    if core_diameter >= outer_diameter:
        raise ValueError("core_diameter must be less than outer_diameter.")
    return propellant_density * math.pi / 4.0 * (outer_diameter ** 2 - core_diameter ** 2) * grain_length


def volumetric_loading_fraction(propellant_volume: float, case_volume: float) -> float:
    """Compute case volumetric loading fraction.

    Args:
        propellant_volume: Propellant volume in cubic metres.
        case_volume: Available case volume in cubic metres.

    Returns:
        Volumetric loading fraction.

    Raises:
        ValueError: If propellant volume is negative or case volume is not positive.

    Physics:
        This fraction shows how efficiently the case volume is filled with
        propellant rather than insulation, structural walls, or voids.
    """
    non_negative(propellant_volume, "propellant_volume")
    positive(case_volume, "case_volume")
    return propellant_volume / case_volume


def temperature_sensitivity(
    pressure_at_high_temperature: float,
    pressure_at_low_temperature: float,
    temperature_difference: float,
) -> float:
    """Compute logarithmic chamber-pressure sensitivity to temperature.

    Args:
        pressure_at_high_temperature: Chamber pressure at the higher temperature.
        pressure_at_low_temperature: Chamber pressure at the lower temperature.
        temperature_difference: Temperature difference between the two cases.

    Returns:
        Logarithmic sensitivity in inverse kelvin.

    Raises:
        ValueError: If either pressure is not positive or the temperature difference is zero.

    Physics:
        A logarithmic pressure ratio divided by temperature difference provides a
        convenient empirical sensitivity measure for solid propellants.
    """
    positive(pressure_at_high_temperature, "pressure_at_high_temperature")
    positive(pressure_at_low_temperature, "pressure_at_low_temperature")
    if temperature_difference == 0.0:
        raise ValueError("temperature_difference cannot be zero.")
    return math.log(pressure_at_high_temperature / pressure_at_low_temperature) / temperature_difference


__all__ = [
    "burn_rate",
    "klemmung",
    "equilibrium_chamber_pressure",
    "is_stable",
    "web_burned",
    "bates_grain_burning_area",
    "bates_grain_mass",
    "volumetric_loading_fraction",
    "temperature_sensitivity",
]
