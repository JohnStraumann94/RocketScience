"""Idealized electric-propulsion performance trades.

Chapter reference: these utilities reflect the electric-propulsion mass and
power trade relations used in high-Isp mission analysis discussions.
"""

import math

from .guard import non_negative, positive


def jet_power(thrust: float, exhaust_velocity: float) -> float:
    """Compute kinetic jet power for an electric thruster plume.

    Args:
        thrust: Thruster thrust in newtons.
        exhaust_velocity: Exhaust velocity in metres per second.

    Returns:
        Jet power in watts.

    Raises:
        ValueError: If thrust or exhaust velocity is negative.

    Physics:
        Using ``F = m_dot v_e`` in the kinetic-power expression
        ``0.5 m_dot v_e^2`` yields ``0.5 F v_e``.
    """
    non_negative(thrust, "thrust")
    non_negative(exhaust_velocity, "exhaust_velocity")
    return 0.5 * thrust * exhaust_velocity


def required_electrical_power(thrust: float, exhaust_velocity: float, thruster_efficiency: float) -> float:
    """Compute electrical input power from jet power and efficiency.

    Args:
        thrust: Thruster thrust in newtons.
        exhaust_velocity: Exhaust velocity in metres per second.
        thruster_efficiency: Overall thruster efficiency as a fraction.

    Returns:
        Required electrical power in watts.

    Raises:
        ValueError: If inputs are invalid.

    Physics:
        Thruster efficiency measures how much electrical power becomes directed
        exhaust kinetic power, so required input is jet power divided by efficiency.
    """
    positive(exhaust_velocity, "exhaust_velocity")
    positive(thruster_efficiency, "thruster_efficiency")
    return jet_power(thrust, exhaust_velocity) / thruster_efficiency


def thrust_from_power(electrical_power: float, exhaust_velocity: float, thruster_efficiency: float) -> float:
    """Compute thrust achievable from electrical power and exhaust velocity.

    Args:
        electrical_power: Available electrical power in watts.
        exhaust_velocity: Exhaust velocity in metres per second.
        thruster_efficiency: Overall thruster efficiency as a fraction.

    Returns:
        Thrust in newtons.

    Raises:
        ValueError: If inputs are invalid.

    Physics:
        Solving the jet-power relation for thrust gives ``F = 2 eta P / v_e``.
    """
    non_negative(electrical_power, "electrical_power")
    positive(exhaust_velocity, "exhaust_velocity")
    positive(thruster_efficiency, "thruster_efficiency")
    return 2.0 * thruster_efficiency * electrical_power / exhaust_velocity


def optimum_exhaust_velocity(
    delta_v: float,
    burn_time_seconds: float,
    specific_power: float,
    thruster_efficiency: float,
) -> float:
    """Compute the classical optimum exhaust velocity for power-limited flight.

    Args:
        delta_v: Mission delta-v in metres per second.
        burn_time_seconds: Total burn duration in seconds.
        specific_power: Power-system specific power in watts per kilogram.
        thruster_efficiency: Thruster efficiency as a fraction.

    Returns:
        Optimum exhaust velocity in metres per second.

    Raises:
        ValueError: If any input is outside its physical range.

    Physics:
        In power-limited electric propulsion, the optimum exhaust velocity from
        simple mass minimization scales with ``sqrt(2 eta alpha t)``.
    """
    non_negative(delta_v, "delta_v")
    positive(burn_time_seconds, "burn_time_seconds")
    positive(specific_power, "specific_power")
    positive(thruster_efficiency, "thruster_efficiency")
    return math.sqrt(2.0 * thruster_efficiency * specific_power * burn_time_seconds)


def power_plant_mass(electrical_power: float, specific_power: float) -> float:
    """Compute power-system mass from electrical power and specific power.

    Args:
        electrical_power: Required electrical power in watts.
        specific_power: Power-system specific power in watts per kilogram.

    Returns:
        Power-plant mass in kilograms.

    Raises:
        ValueError: If electrical power is negative or specific power is not positive.

    Physics:
        Specific power states how many watts each kilogram of power system can supply.
    """
    non_negative(electrical_power, "electrical_power")
    positive(specific_power, "specific_power")
    return electrical_power / specific_power


def mission_mass_breakdown(
    payload_mass: float,
    delta_v: float,
    exhaust_velocity: float,
    burn_time_seconds: float,
    specific_power: float,
    thruster_efficiency: float,
) -> tuple[float, float, float, float, float]:
    """Solve a simple electric-propulsion mass and power balance by iteration.

    Args:
        payload_mass: Delivered payload mass in kilograms.
        delta_v: Mission delta-v in metres per second.
        exhaust_velocity: Thruster exhaust velocity in metres per second.
        burn_time_seconds: Burn duration in seconds.
        specific_power: Power-system specific power in watts per kilogram.
        thruster_efficiency: Thruster efficiency as a fraction.

    Returns:
        A tuple ``(propellant_mass, power_plant_mass, thrust, electrical_power, initial_mass)``.

    Raises:
        ValueError: If any input is outside its physical range.

    Physics:
        The power-system mass depends on required thrust, but thrust depends on
        vehicle mass. Fixed-point iteration resolves that circular dependence.
    """
    positive(payload_mass, "payload_mass")
    non_negative(delta_v, "delta_v")
    positive(exhaust_velocity, "exhaust_velocity")
    positive(burn_time_seconds, "burn_time_seconds")
    positive(specific_power, "specific_power")
    positive(thruster_efficiency, "thruster_efficiency")
    mass_ratio = math.exp(delta_v / exhaust_velocity)
    current_power_plant_mass = 0.0
    propellant_mass = 0.0
    thrust = 0.0
    electrical_power = 0.0
    initial_mass = payload_mass
    for _ in range(100):
        dry_mass = payload_mass + current_power_plant_mass
        initial_mass = dry_mass * mass_ratio
        propellant_mass = initial_mass - dry_mass
        average_mass = 0.5 * (initial_mass + dry_mass)
        # Average acceleration times burn time is used as a first-order way to
        # enforce the mission delta-v requirement on a low-thrust transfer.
        thrust = average_mass * delta_v / burn_time_seconds
        electrical_power = required_electrical_power(thrust, exhaust_velocity, thruster_efficiency)
        new_power_plant_mass = power_plant_mass(electrical_power, specific_power)
        if abs(new_power_plant_mass - current_power_plant_mass) < 1e-3:
            current_power_plant_mass = new_power_plant_mass
            break
        current_power_plant_mass = new_power_plant_mass
    return propellant_mass, current_power_plant_mass, thrust, electrical_power, initial_mass


__all__ = [
    "jet_power",
    "required_electrical_power",
    "thrust_from_power",
    "optimum_exhaust_velocity",
    "power_plant_mass",
    "mission_mass_breakdown",
]
