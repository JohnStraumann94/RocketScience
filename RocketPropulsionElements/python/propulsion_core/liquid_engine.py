"""Liquid-engine feed-system and cooling helper relations.

Chapter reference: these formulas support injector sizing, tank-pressure
budgeting, turbopump estimates, and regenerative-cooling calculations from
the liquid-propulsion chapters of Rocket Propulsion Elements.
"""

import math

from .constants import STANDARD_GRAVITY
from .guard import non_negative, positive


def injector_mass_flow(
    discharge_coefficient: float,
    orifice_area: float,
    propellant_density: float,
    pressure_drop: float,
) -> float:
    """Compute incompressible injector-orifice mass flow rate.

    Args:
        discharge_coefficient: Orifice discharge coefficient.
        orifice_area: Total orifice area in square metres.
        propellant_density: Liquid density in kilograms per cubic metre.
        pressure_drop: Pressure drop across the injector in pascals.

    Returns:
        Mass flow rate in kilograms per second.

    Raises:
        ValueError: If any input is outside its physical range.

    Physics:
        The standard incompressible orifice relation gives ``m_dot = C_d A sqrt(2 rho delta_p)``.
    """
    positive(discharge_coefficient, "discharge_coefficient")
    positive(orifice_area, "orifice_area")
    positive(propellant_density, "propellant_density")
    positive(pressure_drop, "pressure_drop")
    return discharge_coefficient * orifice_area * math.sqrt(2.0 * propellant_density * pressure_drop)


def required_orifice_area(
    mass_flow_rate: float,
    discharge_coefficient: float,
    propellant_density: float,
    pressure_drop: float,
) -> float:
    """Compute required injector area for a target liquid mass flow rate.

    Args:
        mass_flow_rate: Required propellant mass flow in kilograms per second.
        discharge_coefficient: Orifice discharge coefficient.
        propellant_density: Liquid density in kilograms per cubic metre.
        pressure_drop: Pressure drop across the injector in pascals.

    Returns:
        Required total orifice area in square metres.

    Raises:
        ValueError: If any input is outside its physical range.

    Physics:
        This is the direct rearrangement of the incompressible injector flow equation.
    """
    positive(mass_flow_rate, "mass_flow_rate")
    positive(discharge_coefficient, "discharge_coefficient")
    positive(propellant_density, "propellant_density")
    positive(pressure_drop, "pressure_drop")
    return mass_flow_rate / (discharge_coefficient * math.sqrt(2.0 * propellant_density * pressure_drop))


def orifice_count(total_area: float, orifice_diameter: float) -> int:
    """Compute the integer number of equal circular injector orifices required.

    Args:
        total_area: Required total flow area in square metres.
        orifice_diameter: Diameter of one circular orifice in metres.

    Returns:
        Number of identical orifices, rounded up to the next integer.

    Raises:
        ValueError: If either input is not strictly positive.

    Physics:
        Injector elements are discrete, so the required count must be rounded up
        once the area of one orifice is known.
    """
    positive(total_area, "total_area")
    positive(orifice_diameter, "orifice_diameter")
    single_orifice_area = math.pi * orifice_diameter * orifice_diameter / 4.0
    return math.ceil(total_area / single_orifice_area)


def required_tank_pressure(
    chamber_pressure: float,
    injector_pressure_drop: float,
    line_and_valve_pressure_drop: float,
    dynamic_pressure_allowance: float = 0.0,
) -> float:
    """Compute required tank pressure for a pressure-fed liquid engine.

    Args:
        chamber_pressure: Chamber pressure in pascals.
        injector_pressure_drop: Required pressure drop across the injector.
        line_and_valve_pressure_drop: Feed-system losses upstream of the injector.
        dynamic_pressure_allowance: Additional margin for acceleration or transients.

    Returns:
        Minimum required tank pressure in pascals.

    Raises:
        ValueError: If any component pressure is negative.

    Physics:
        Pressure-fed systems must overcome chamber pressure plus every planned
        pressure loss between the tank and the combustion chamber.
    """
    positive(chamber_pressure, "chamber_pressure")
    non_negative(injector_pressure_drop, "injector_pressure_drop")
    non_negative(line_and_valve_pressure_drop, "line_and_valve_pressure_drop")
    non_negative(dynamic_pressure_allowance, "dynamic_pressure_allowance")
    return chamber_pressure + injector_pressure_drop + line_and_valve_pressure_drop + dynamic_pressure_allowance


def pump_power(mass_flow_rate: float, pressure_rise: float, density: float, pump_efficiency: float) -> float:
    """Estimate hydraulic shaft power for a liquid-propellant pump.

    Args:
        mass_flow_rate: Pumped mass flow rate in kilograms per second.
        pressure_rise: Pump pressure rise in pascals.
        density: Pumped liquid density in kilograms per cubic metre.
        pump_efficiency: Pump efficiency as a fraction.

    Returns:
        Required shaft power in watts.

    Raises:
        ValueError: If inputs are invalid.

    Physics:
        Hydraulic power is volumetric flow times pressure rise; dividing by
        efficiency accounts for losses in the real pump.
    """
    positive(mass_flow_rate, "mass_flow_rate")
    positive(pressure_rise, "pressure_rise")
    positive(density, "density")
    positive(pump_efficiency, "pump_efficiency")
    return mass_flow_rate * pressure_rise / (density * pump_efficiency)


def pump_head(pressure_rise: float, density: float) -> float:
    """Convert pump pressure rise into equivalent liquid head.

    Args:
        pressure_rise: Pump pressure rise in pascals.
        density: Liquid density in kilograms per cubic metre.

    Returns:
        Head in metres of fluid.

    Raises:
        ValueError: If pressure rise or density is not strictly positive.

    Physics:
        Pump head is pressure rise normalized by fluid specific weight ``rho g``.
    """
    positive(pressure_rise, "pressure_rise")
    positive(density, "density")
    return pressure_rise / (density * STANDARD_GRAVITY)


def pump_specific_speed(rotational_speed_rad_per_sec: float, volume_flow_rate: float, head_metres: float) -> float:
    """Compute pump specific speed using the supplied source formula.

    Args:
        rotational_speed_rad_per_sec: Pump shaft speed in radians per second.
        volume_flow_rate: Volume flow rate in cubic metres per second.
        head_metres: Pump head in metres.

    Returns:
        Dimensionless specific-speed-style metric from the source relation.

    Raises:
        ValueError: If any input is not strictly positive.

    Physics:
        Specific speed compares speed, flow, and head to indicate the family of
        pump geometry best suited to the required operating point.
    """
    positive(rotational_speed_rad_per_sec, "rotational_speed_rad_per_sec")
    positive(volume_flow_rate, "volume_flow_rate")
    positive(head_metres, "head_metres")
    return rotational_speed_rad_per_sec * math.sqrt(volume_flow_rate) / (STANDARD_GRAVITY * head_metres) ** 0.75


def convective_heat_transfer(
    heat_transfer_coefficient: float,
    surface_area: float,
    gas_temperature: float,
    wall_temperature: float,
) -> float:
    """Compute convective heat-transfer rate between gas and wall.

    Args:
        heat_transfer_coefficient: Convective coefficient in watts per square metre-kelvin.
        surface_area: Exposed area in square metres.
        gas_temperature: Gas-side temperature in kelvin.
        wall_temperature: Wall temperature in kelvin.

    Returns:
        Heat-transfer rate in watts, positive when heat flows into the wall.

    Raises:
        ValueError: If the coefficient or area is not strictly positive.

    Physics:
        Newton's law of cooling models the heat flux as proportional to the
        temperature difference between the hot gas and the wall.
    """
    positive(heat_transfer_coefficient, "heat_transfer_coefficient")
    positive(surface_area, "surface_area")
    return heat_transfer_coefficient * surface_area * (gas_temperature - wall_temperature)


def coolant_temperature_rise(heat_rate: float, coolant_mass_flow: float, specific_heat: float) -> float:
    """Compute coolant temperature rise from absorbed heat.

    Args:
        heat_rate: Heat absorbed by the coolant in watts.
        coolant_mass_flow: Coolant mass flow rate in kilograms per second.
        specific_heat: Coolant specific heat in joules per kilogram-kelvin.

    Returns:
        Temperature rise in kelvin.

    Raises:
        ValueError: If coolant flow rate or specific heat is not strictly positive.

    Physics:
        The energy balance ``Q_dot = m_dot c_p delta_T`` maps absorbed wall heat
        directly into coolant temperature rise.
    """
    positive(coolant_mass_flow, "coolant_mass_flow")
    positive(specific_heat, "specific_heat")
    return heat_rate / (coolant_mass_flow * specific_heat)


__all__ = [
    "injector_mass_flow",
    "required_orifice_area",
    "orifice_count",
    "required_tank_pressure",
    "pump_power",
    "pump_head",
    "pump_specific_speed",
    "convective_heat_transfer",
    "coolant_temperature_rise",
]
