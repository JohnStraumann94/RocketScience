"""Classical rocket performance relations.

Chapter reference: these equations correspond to the introductory performance
metrics in Rocket Propulsion Elements, including thrust, exhaust velocity,
specific impulse, propellant fraction, and thrust-to-weight ratio.
"""

from .constants import STANDARD_GRAVITY
from .guard import non_negative, positive


def thrust(
    mass_flow_rate: float,
    exit_velocity: float,
    exit_pressure: float,
    ambient_pressure: float,
    exit_area: float,
) -> float:
    """Compute nozzle thrust from momentum and pressure terms.

    Args:
        mass_flow_rate: Propellant mass flow rate in kilograms per second.
        exit_velocity: Exhaust velocity at the nozzle exit in metres per second.
        exit_pressure: Static pressure at the nozzle exit plane in pascals.
        ambient_pressure: Surrounding pressure acting on the exit plane.
        exit_area: Nozzle exit area in square metres.

    Returns:
        Total thrust in newtons.

    Raises:
        ValueError: If any quantity that must be non-negative is invalid.

    Physics:
        Rocket thrust is the sum of the momentum flux carried by the exhaust and
        the net pressure force on the exit plane, ``F = m_dot * v_e + (p_e - p_a)A_e``.
    """
    non_negative(mass_flow_rate, "mass_flow_rate")
    non_negative(exit_velocity, "exit_velocity")
    non_negative(exit_pressure, "exit_pressure")
    non_negative(ambient_pressure, "ambient_pressure")
    non_negative(exit_area, "exit_area")
    return mass_flow_rate * exit_velocity + (exit_pressure - ambient_pressure) * exit_area


def effective_exhaust_velocity(thrust_value: float, mass_flow_rate: float) -> float:
    """Reduce thrust and mass flow to a single effective exhaust velocity.

    Args:
        thrust_value: Produced thrust in newtons.
        mass_flow_rate: Propellant mass flow rate in kilograms per second.

    Returns:
        Effective exhaust velocity in metres per second.

    Raises:
        ValueError: If thrust is negative or mass flow is not strictly positive.

    Physics:
        Effective exhaust velocity is defined so the full thrust can be written
        as ``F = m_dot * c`` even when exit-pressure thrust is present.
    """
    non_negative(thrust_value, "thrust")
    positive(mass_flow_rate, "mass_flow_rate")
    return thrust_value / mass_flow_rate


def specific_impulse(effective_exhaust_velocity_value: float) -> float:
    """Convert effective exhaust velocity into specific impulse.

    Args:
        effective_exhaust_velocity_value: Effective exhaust velocity in metres per second.

    Returns:
        Specific impulse in seconds.

    Raises:
        ValueError: If the effective exhaust velocity is negative.

    Physics:
        Specific impulse is simply exhaust velocity normalized by standard
        gravity, which turns a velocity measure into the familiar "seconds" form.
    """
    non_negative(effective_exhaust_velocity_value, "effective_exhaust_velocity")
    return effective_exhaust_velocity_value / STANDARD_GRAVITY


def exhaust_velocity_from_isp(specific_impulse_seconds: float) -> float:
    """Convert specific impulse back into effective exhaust velocity.

    Args:
        specific_impulse_seconds: Specific impulse in seconds.

    Returns:
        Effective exhaust velocity in metres per second.

    Raises:
        ValueError: If specific impulse is negative.

    Physics:
        The conversion reverses the normalization used for specific impulse:
        ``c = I_sp * g_0``.
    """
    non_negative(specific_impulse_seconds, "specific_impulse_seconds")
    return specific_impulse_seconds * STANDARD_GRAVITY


def total_impulse(thrust_value: float, burn_time_seconds: float) -> float:
    """Compute the time-integrated thrust for a constant-thrust burn.

    Args:
        thrust_value: Thrust in newtons.
        burn_time_seconds: Burn duration in seconds.

    Returns:
        Total impulse in newton-seconds.

    Raises:
        ValueError: If thrust or burn time is negative.

    Physics:
        Total impulse is the area under the thrust-time curve. For a constant
        thrust approximation it reduces to ``I_t = F * t``.
    """
    non_negative(thrust_value, "thrust")
    non_negative(burn_time_seconds, "burn_time_seconds")
    return thrust_value * burn_time_seconds


def specific_impulse_from_total_impulse(total_impulse_value: float, propellant_mass: float) -> float:
    """Infer specific impulse from delivered impulse and consumed propellant.

    Args:
        total_impulse_value: Total impulse in newton-seconds.
        propellant_mass: Propellant mass expended in kilograms.

    Returns:
        Specific impulse in seconds.

    Raises:
        ValueError: If total impulse is negative or propellant mass is not positive.

    Physics:
        Rearranging ``I_t = m_p * g_0 * I_sp`` gives the average specific
        impulse over the burn.
    """
    non_negative(total_impulse_value, "total_impulse")
    positive(propellant_mass, "propellant_mass")
    return total_impulse_value / (propellant_mass * STANDARD_GRAVITY)


def mass_flow_rate(thrust_value: float, effective_exhaust_velocity_value: float) -> float:
    """Compute propellant mass flow rate from thrust and exhaust velocity.

    Args:
        thrust_value: Thrust in newtons.
        effective_exhaust_velocity_value: Effective exhaust velocity in metres per second.

    Returns:
        Mass flow rate in kilograms per second.

    Raises:
        ValueError: If thrust is negative or effective exhaust velocity is not positive.

    Physics:
        The ideal one-dimensional thrust model reduces to ``m_dot = F / c`` when
        the effective exhaust velocity is already known.
    """
    non_negative(thrust_value, "thrust")
    positive(effective_exhaust_velocity_value, "effective_exhaust_velocity")
    return thrust_value / effective_exhaust_velocity_value


def propellant_mass_fraction(propellant_mass: float, initial_mass: float) -> float:
    """Compute the fraction of the initial mass that is propellant.

    Args:
        propellant_mass: Propellant carried or consumed in kilograms.
        initial_mass: Initial gross mass in kilograms.

    Returns:
        Propellant mass fraction ``m_p / m_0``.

    Raises:
        ValueError: If masses are invalid or propellant mass exceeds initial mass.

    Physics:
        This fraction measures how much of the launch mass is available to do
        useful propulsive work rather than remain as structure or payload.
    """
    non_negative(propellant_mass, "propellant_mass")
    positive(initial_mass, "initial_mass")
    if propellant_mass > initial_mass:
        raise ValueError("propellant_mass cannot exceed initial_mass.")
    return propellant_mass / initial_mass


def mass_ratio(initial_mass: float, final_mass: float) -> float:
    """Return Sutton's mass-ratio convention used by the source C# helper.

    Args:
        initial_mass: Vehicle mass before propellant is expended.
        final_mass: Vehicle mass after propellant is expended.

    Returns:
        The ratio ``m_f / m_0``, which is less than one for a real burn.

    Raises:
        ValueError: If masses are invalid or final mass exceeds initial mass.

    Physics:
        This helper intentionally follows the Sutton convention quoted in the
        request: it returns final mass divided by initial mass. That is the
        opposite of the ``m_0 / m_f`` ratio used inside the Tsiolkovsky equation.
    """
    positive(initial_mass, "initial_mass")
    positive(final_mass, "final_mass")
    if final_mass > initial_mass:
        raise ValueError("final_mass cannot exceed initial_mass.")
    # MASS-RATIO CONVENTION TRAP: this function returns mf / m0, not m0 / mf.
    return final_mass / initial_mass


def thrust_to_weight_ratio(thrust_value: float, mass: float) -> float:
    """Compute thrust-to-weight ratio at standard gravity.

    Args:
        thrust_value: Thrust in newtons.
        mass: Vehicle mass in kilograms.

    Returns:
        Dimensionless thrust-to-weight ratio.

    Raises:
        ValueError: If thrust is negative or mass is not positive.

    Physics:
        ``T/W = F / (m g_0)`` compares engine force to weight. Values above one
        indicate an engine can overcome static weight at sea level in this simple model.
    """
    non_negative(thrust_value, "thrust")
    positive(mass, "mass")
    return thrust_value / (mass * STANDARD_GRAVITY)


__all__ = [
    "thrust",
    "effective_exhaust_velocity",
    "specific_impulse",
    "exhaust_velocity_from_isp",
    "total_impulse",
    "specific_impulse_from_total_impulse",
    "mass_flow_rate",
    "propellant_mass_fraction",
    "mass_ratio",
    "thrust_to_weight_ratio",
]
