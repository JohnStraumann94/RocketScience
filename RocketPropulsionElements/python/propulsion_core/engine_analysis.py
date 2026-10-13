"""Integrated ideal engine operating-point analysis.

Chapter reference: this module ties chamber thermodynamics, nozzle theory, and
performance metrics together into a single operating-point calculation similar
to the worked engine-sizing examples in Rocket Propulsion Elements.
"""

from dataclasses import dataclass

from .constants import STANDARD_GRAVITY
from .guard import non_negative, positive
from .nozzle_geometry import diameter_from_area
from .nozzle_losses import ExpansionState, classify_expansion, is_flow_separation_likely
from .nozzle_theory import (
    characteristic_velocity,
    exit_mach_number,
    exit_pressure_from_area_ratio,
    mass_flow_rate as nozzle_mass_flow_rate,
    thrust_coefficient,
)


@dataclass(frozen=True)
class EngineOperatingPoint:
    """Collection of ideal rocket engine operating quantities."""

    chamber_pressure: float
    chamber_temperature: float
    gamma: float
    molecular_mass: float
    area_ratio: float
    throat_area: float
    exit_area: float
    throat_diameter: float
    exit_diameter: float
    ambient_pressure: float
    exit_pressure: float
    exit_mach: float
    characteristic_velocity: float
    thrust_coefficient: float
    mass_flow_rate: float
    thrust: float
    effective_exhaust_velocity: float
    specific_impulse: float
    expansion: ExpansionState
    flow_separation_likely: bool

    def __str__(self) -> str:
        """Render a readable engine report.

        Args:
            None.

        Returns:
            A multi-line report string.

        Raises:
            None.

        Physics:
            Grouping the derived engine quantities in one report mirrors the way
            engine designers review a candidate operating point.
        """
        return (
            "Engine Operating Point\n"
            f"  Chamber pressure: {self.chamber_pressure:.3f} Pa\n"
            f"  Chamber temperature: {self.chamber_temperature:.3f} K\n"
            f"  Gamma: {self.gamma:.5f}\n"
            f"  Molecular mass: {self.molecular_mass:.3f} kg/kmol\n"
            f"  Area ratio: {self.area_ratio:.5f}\n"
            f"  Throat area: {self.throat_area:.6f} m^2\n"
            f"  Exit area: {self.exit_area:.6f} m^2\n"
            f"  Throat diameter: {self.throat_diameter:.6f} m\n"
            f"  Exit diameter: {self.exit_diameter:.6f} m\n"
            f"  Ambient pressure: {self.ambient_pressure:.3f} Pa\n"
            f"  Exit pressure: {self.exit_pressure:.3f} Pa\n"
            f"  Exit Mach: {self.exit_mach:.5f}\n"
            f"  Characteristic velocity: {self.characteristic_velocity:.3f} m/s\n"
            f"  Thrust coefficient: {self.thrust_coefficient:.5f}\n"
            f"  Mass flow rate: {self.mass_flow_rate:.6f} kg/s\n"
            f"  Thrust: {self.thrust:.3f} N\n"
            f"  Effective exhaust velocity: {self.effective_exhaust_velocity:.3f} m/s\n"
            f"  Specific impulse: {self.specific_impulse:.3f} s\n"
            f"  Expansion state: {self.expansion.name}\n"
            f"  Flow separation likely: {self.flow_separation_likely}"
        )


def analyse(
    gamma: float,
    chamber_temperature: float,
    molecular_mass: float,
    chamber_pressure: float,
    area_ratio: float,
    throat_area: float,
    ambient_pressure: float,
) -> EngineOperatingPoint:
    """Analyse an ideal operating point for a given chamber and nozzle.

    Args:
        gamma: Ratio of specific heats.
        chamber_temperature: Chamber temperature in kelvin.
        molecular_mass: Mean molecular mass in kilograms per kilomole.
        chamber_pressure: Chamber pressure in pascals.
        area_ratio: Exit area divided by throat area.
        throat_area: Throat area in square metres.
        ambient_pressure: Ambient pressure in pascals.

    Returns:
        A fully populated ``EngineOperatingPoint`` instance.

    Raises:
        ValueError: If the nozzle is not actually expanding supersonically or any
            required input is outside its physical range.

    Physics:
        The calculation chains together isentropic expansion, choked mass flow,
        and classical thrust relations to summarize a complete engine state.
    """
    positive(chamber_temperature, "chamber_temperature")
    positive(molecular_mass, "molecular_mass")
    positive(chamber_pressure, "chamber_pressure")
    positive(throat_area, "throat_area")
    non_negative(ambient_pressure, "ambient_pressure")
    if area_ratio <= 1.0:
        raise ValueError("area_ratio must be greater than 1.0.")
    exit_pressure = exit_pressure_from_area_ratio(gamma, chamber_pressure, area_ratio)
    exit_mach = exit_mach_number(gamma, chamber_pressure, exit_pressure)
    c_star = characteristic_velocity(gamma, chamber_temperature, molecular_mass)
    cf = thrust_coefficient(gamma, chamber_pressure, exit_pressure, ambient_pressure, area_ratio)
    mdot = nozzle_mass_flow_rate(gamma, chamber_pressure, chamber_temperature, molecular_mass, throat_area)
    thrust_value = cf * chamber_pressure * throat_area
    effective_velocity = thrust_value / mdot
    specific_impulse_value = effective_velocity / STANDARD_GRAVITY
    exit_area = throat_area * area_ratio
    return EngineOperatingPoint(
        chamber_pressure=chamber_pressure,
        chamber_temperature=chamber_temperature,
        gamma=gamma,
        molecular_mass=molecular_mass,
        area_ratio=area_ratio,
        throat_area=throat_area,
        exit_area=exit_area,
        throat_diameter=diameter_from_area(throat_area),
        exit_diameter=diameter_from_area(exit_area),
        ambient_pressure=ambient_pressure,
        exit_pressure=exit_pressure,
        exit_mach=exit_mach,
        characteristic_velocity=c_star,
        thrust_coefficient=cf,
        mass_flow_rate=mdot,
        thrust=thrust_value,
        effective_exhaust_velocity=effective_velocity,
        specific_impulse=specific_impulse_value,
        expansion=classify_expansion(exit_pressure, max(ambient_pressure, 1e-12)),
        flow_separation_likely=is_flow_separation_likely(exit_pressure, max(ambient_pressure, 1e-12)),
    )


def design_for_thrust(
    gamma: float,
    chamber_temperature: float,
    molecular_mass: float,
    chamber_pressure: float,
    area_ratio: float,
    required_thrust: float,
    ambient_pressure: float,
) -> EngineOperatingPoint:
    """Size throat area to meet a thrust requirement and analyse the result.

    Args:
        gamma: Ratio of specific heats.
        chamber_temperature: Chamber temperature in kelvin.
        molecular_mass: Mean molecular mass in kilograms per kilomole.
        chamber_pressure: Chamber pressure in pascals.
        area_ratio: Exit area divided by throat area.
        required_thrust: Desired thrust in newtons.
        ambient_pressure: Ambient pressure in pascals.

    Returns:
        The resulting ideal operating point for the required thrust.

    Raises:
        ValueError: If the request is physically invalid.

    Physics:
        The thrust coefficient converts the desired thrust directly into a
        throat area, after which the full operating point can be reconstructed.
    """
    positive(required_thrust, "required_thrust")
    exit_pressure = exit_pressure_from_area_ratio(gamma, chamber_pressure, area_ratio)
    cf = thrust_coefficient(gamma, chamber_pressure, exit_pressure, ambient_pressure, area_ratio)
    throat_area = required_thrust / (cf * chamber_pressure)
    return analyse(
        gamma=gamma,
        chamber_temperature=chamber_temperature,
        molecular_mass=molecular_mass,
        chamber_pressure=chamber_pressure,
        area_ratio=area_ratio,
        throat_area=throat_area,
        ambient_pressure=ambient_pressure,
    )


__all__ = ["EngineOperatingPoint", "analyse", "design_for_thrust", "ExpansionState"]
