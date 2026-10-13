"""Tsiolkovsky rocket-equation utilities.

Chapter reference: these are the classical ideal velocity and mass-ratio
relations used throughout Rocket Propulsion Elements for vehicle sizing.
"""

import math

from .guard import in_range, non_negative, positive
from .performance import exhaust_velocity_from_isp


def delta_v(effective_exhaust_velocity: float, initial_mass: float, final_mass: float) -> float:
    """Compute ideal delta-v from the Tsiolkovsky rocket equation.

    Args:
        effective_exhaust_velocity: Effective exhaust velocity in metres per second.
        initial_mass: Initial mass before propellant consumption.
        final_mass: Final mass after propellant consumption.

    Returns:
        Ideal delta-v in metres per second.

    Raises:
        ValueError: If masses are invalid or final mass exceeds initial mass.

    Physics:
        The logarithmic form ``delta_v = c ln(m_0 / m_f)`` reflects the fact
        that each bit of propellant accelerates the remaining vehicle mass.
    """
    positive(effective_exhaust_velocity, "effective_exhaust_velocity")
    positive(initial_mass, "initial_mass")
    positive(final_mass, "final_mass")
    if final_mass > initial_mass:
        raise ValueError("final_mass cannot exceed initial_mass.")
    return effective_exhaust_velocity * math.log(initial_mass / final_mass)


def delta_v_from_isp(specific_impulse_seconds: float, initial_mass: float, final_mass: float) -> float:
    """Compute ideal delta-v using specific impulse instead of exhaust velocity.

    Args:
        specific_impulse_seconds: Specific impulse in seconds.
        initial_mass: Initial mass before propellant consumption.
        final_mass: Final mass after propellant consumption.

    Returns:
        Ideal delta-v in metres per second.

    Raises:
        ValueError: If the underlying inputs are invalid.

    Physics:
        Specific impulse is just another way of expressing exhaust velocity, so
        the same Tsiolkovsky relation applies after converting units.
    """
    return delta_v(exhaust_velocity_from_isp(specific_impulse_seconds), initial_mass, final_mass)


def required_mass_ratio(delta_v: float, effective_exhaust_velocity: float) -> float:
    """Compute the rocket-equation mass ratio needed for a target delta-v.

    Args:
        delta_v: Required ideal mission delta-v in metres per second.
        effective_exhaust_velocity: Effective exhaust velocity in metres per second.

    Returns:
        The ratio ``m_0 / m_f``, which is greater than one for any positive delta-v.

    Raises:
        ValueError: If delta-v is negative or exhaust velocity is not positive.

    Physics:
        Inverting the rocket equation gives ``m_0 / m_f = exp(delta_v / c)``.
        This is intentionally the opposite convention from ``performance.mass_ratio``.
    """
    non_negative(delta_v, "delta_v")
    positive(effective_exhaust_velocity, "effective_exhaust_velocity")
    # MASS-RATIO CONVENTION TRAP: this function returns m0 / mf, not mf / m0.
    return math.exp(delta_v / effective_exhaust_velocity)


def required_propellant_mass(delta_v: float, effective_exhaust_velocity: float, final_mass: float) -> float:
    """Compute propellant mass required to deliver a target ideal delta-v.

    Args:
        delta_v: Required delta-v in metres per second.
        effective_exhaust_velocity: Effective exhaust velocity in metres per second.
        final_mass: Final dry-plus-payload mass after burnout in kilograms.

    Returns:
        Required propellant mass in kilograms.

    Raises:
        ValueError: If any input is outside its valid range.

    Physics:
        If ``m_0 / m_f`` is known, then the propellant mass is simply the excess
        of the initial mass over the specified final mass.
    """
    positive(final_mass, "final_mass")
    return final_mass * (required_mass_ratio(delta_v, effective_exhaust_velocity) - 1.0)


def final_mass(delta_v: float, effective_exhaust_velocity: float, initial_mass: float) -> float:
    """Compute final mass remaining after a burn that supplies a target delta-v.

    Args:
        delta_v: Ideal delta-v in metres per second.
        effective_exhaust_velocity: Effective exhaust velocity in metres per second.
        initial_mass: Initial mass before the burn in kilograms.

    Returns:
        Final mass in kilograms.

    Raises:
        ValueError: If any input is outside its valid range.

    Physics:
        Rearranging the mass-ratio form of the rocket equation gives the final
        mass once the required ratio and initial mass are known.
    """
    positive(initial_mass, "initial_mass")
    return initial_mass / required_mass_ratio(delta_v, effective_exhaust_velocity)


def burn_time(propellant_mass: float, mass_flow_rate: float) -> float:
    """Compute burn duration from propellant load and mass flow rate.

    Args:
        propellant_mass: Propellant mass in kilograms.
        mass_flow_rate: Propellant flow rate in kilograms per second.

    Returns:
        Burn time in seconds.

    Raises:
        ValueError: If propellant mass is negative or mass flow rate is not positive.

    Physics:
        With constant mass flow, propellant depletion is linear and burn time is
        simply ``m_p / m_dot``.
    """
    non_negative(propellant_mass, "propellant_mass")
    positive(mass_flow_rate, "mass_flow_rate")
    return propellant_mass / mass_flow_rate


def mass_at_time(initial_mass: float, mass_flow_rate: float, time_seconds: float) -> float:
    """Compute remaining mass during a constant-flow burn.

    Args:
        initial_mass: Mass at ignition in kilograms.
        mass_flow_rate: Constant propellant flow rate in kilograms per second.
        time_seconds: Elapsed burn time in seconds.

    Returns:
        Remaining mass in kilograms.

    Raises:
        ValueError: If the resulting mass is zero or negative, or if inputs are invalid.

    Physics:
        Constant mass flow gives a linear mass history, which is often adequate
        for first-pass ascent calculations.
    """
    positive(initial_mass, "initial_mass")
    positive(mass_flow_rate, "mass_flow_rate")
    non_negative(time_seconds, "time_seconds")
    remaining_mass = initial_mass - mass_flow_rate * time_seconds
    if remaining_mass <= 0.0:
        raise ValueError("Mass at time must remain positive.")
    return remaining_mass


def acceleration(thrust: float, current_mass: float) -> float:
    """Compute translational acceleration from thrust and current mass.

    Args:
        thrust: Applied thrust in newtons.
        current_mass: Instantaneous vehicle mass in kilograms.

    Returns:
        Acceleration in metres per second squared.

    Raises:
        ValueError: If thrust is negative or mass is not positive.

    Physics:
        This is a direct application of Newton's second law in the thrust axis.
    """
    non_negative(thrust, "thrust")
    positive(current_mass, "current_mass")
    return thrust / current_mass


def payload_fraction(delta_v: float, effective_exhaust_velocity: float, structural_coefficient: float) -> float:
    """Compute ideal payload fraction for a single stage with structure.

    Args:
        delta_v: Required ideal delta-v in metres per second.
        effective_exhaust_velocity: Effective exhaust velocity in metres per second.
        structural_coefficient: Structural mass divided by stage total mass.

    Returns:
        Payload fraction, floored at zero.

    Raises:
        ValueError: If inputs are invalid or the structural coefficient is not in ``[0, 1)``.

    Physics:
        The ideal burnout mass fraction ``exp(-delta_v / c)`` must cover both
        structure and payload, leaving the remainder as payload fraction.
    """
    non_negative(delta_v, "delta_v")
    positive(effective_exhaust_velocity, "effective_exhaust_velocity")
    in_range(structural_coefficient, 0.0, 0.999999999999, "structural_coefficient")
    mass_fraction = math.exp(-delta_v / effective_exhaust_velocity)
    return max(0.0, (mass_fraction - structural_coefficient) / (1.0 - structural_coefficient))


def maximum_single_stage_delta_v(effective_exhaust_velocity: float, structural_coefficient: float) -> float:
    """Compute the limiting ideal delta-v of a single stage.

    Args:
        effective_exhaust_velocity: Effective exhaust velocity in metres per second.
        structural_coefficient: Structural mass divided by total stage mass.

    Returns:
        Maximum ideal single-stage delta-v in metres per second.

    Raises:
        ValueError: If exhaust velocity is invalid or the structural coefficient is not in ``(0, 1)``.

    Physics:
        The stage reaches its limit when all final mass is structure, giving the
        maximum delta-v ``-c ln(epsilon)``.
    """
    positive(effective_exhaust_velocity, "effective_exhaust_velocity")
    if structural_coefficient <= 0.0 or structural_coefficient >= 1.0:
        raise ValueError("structural_coefficient must be greater than 0 and less than 1.")
    return -effective_exhaust_velocity * math.log(structural_coefficient)


__all__ = [
    "delta_v",
    "delta_v_from_isp",
    "required_mass_ratio",
    "required_propellant_mass",
    "final_mass",
    "burn_time",
    "mass_at_time",
    "acceleration",
    "payload_fraction",
    "maximum_single_stage_delta_v",
]
