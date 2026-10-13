"""Simple one-dimensional ascent-performance approximations.

Chapter reference: these utilities correspond to early trajectory-estimation
methods in Rocket Propulsion Elements, where atmospheric effects and gravity
losses are estimated before moving to more detailed flight simulation.
"""

import math
from dataclasses import dataclass

from .constants import ATMOSPHERIC_SCALE_HEIGHT, EARTH_MU, EARTH_RADIUS, SEA_LEVEL_DENSITY, STANDARD_GRAVITY
from .guard import non_negative, positive
from .nozzle_losses import ambient_pressure_at_altitude
from .rocket_equation import delta_v_from_isp


@dataclass(frozen=True)
class AscentSample:
    """One sample from the vertical ascent integration."""

    time: float
    altitude: float
    velocity: float
    mass: float
    acceleration: float
    dynamic_pressure: float


@dataclass(frozen=True)
class AscentResult:
    """Summary of a simple explicit-Euler vertical ascent simulation."""

    samples: list[AscentSample]
    burnout_time: float
    burnout_altitude: float
    burnout_velocity: float
    ideal_delta_v: float
    gravity_loss: float
    drag_loss: float
    max_dynamic_pressure: float
    max_dynamic_pressure_time: float


def gravity_loss(burn_time_seconds: float, flight_path_angle_degrees: float) -> float:
    """Estimate idealized gravity loss during powered flight.

    Args:
        burn_time_seconds: Duration of powered flight in seconds.
        flight_path_angle_degrees: Flight-path angle above local horizontal.

    Returns:
        Gravity loss in metres per second.

    Raises:
        ValueError: If burn time is negative.

    Physics:
        Only the component of gravity aligned opposite the flight path drains
        delta-v, giving ``g_0 t sin(theta)`` in this simple approximation.
    """
    non_negative(burn_time_seconds, "burn_time_seconds")
    return STANDARD_GRAVITY * burn_time_seconds * math.sin(math.radians(flight_path_angle_degrees))


def drag(drag_coefficient: float, reference_area: float, density: float, velocity: float) -> float:
    """Compute aerodynamic drag magnitude.

    Args:
        drag_coefficient: Dimensionless drag coefficient.
        reference_area: Reference area in square metres.
        density: Atmospheric density in kilograms per cubic metre.
        velocity: Vehicle speed in metres per second.

    Returns:
        Drag force in newtons.

    Raises:
        ValueError: If any input that must be non-negative is invalid.

    Physics:
        The standard quadratic drag model approximates pressure drag and skin
        friction using dynamic pressure times area and coefficient.
    """
    non_negative(drag_coefficient, "drag_coefficient")
    non_negative(reference_area, "reference_area")
    non_negative(density, "density")
    non_negative(velocity, "velocity")
    return 0.5 * drag_coefficient * reference_area * density * velocity * velocity


def dynamic_pressure(density: float, velocity: float) -> float:
    """Compute dynamic pressure.

    Args:
        density: Atmospheric density in kilograms per cubic metre.
        velocity: Vehicle speed in metres per second.

    Returns:
        Dynamic pressure in pascals.

    Raises:
        ValueError: If density or velocity is negative.

    Physics:
        Dynamic pressure ``q = 1/2 rho v^2`` is the aerodynamic loading scale
        that drives drag and many structural design constraints.
    """
    non_negative(density, "density")
    non_negative(velocity, "velocity")
    return 0.5 * density * velocity * velocity


def density_at_altitude(altitude_metres: float) -> float:
    """Approximate atmospheric density with a single exponential atmosphere.

    Args:
        altitude_metres: Altitude in metres.

    Returns:
        Density in kilograms per cubic metre.

    Raises:
        ValueError: If altitude is negative.

    Physics:
        A one-scale-height density model is crude but useful for quick launch
        estimates when a full standard atmosphere is unnecessary.
    """
    non_negative(altitude_metres, "altitude_metres")
    return SEA_LEVEL_DENSITY * math.exp(-altitude_metres / ATMOSPHERIC_SCALE_HEIGHT)


def circular_orbit_velocity(altitude_metres: float) -> float:
    """Compute circular-orbit speed around Earth.

    Args:
        altitude_metres: Orbit altitude above Earth's reference radius in metres.

    Returns:
        Circular-orbit velocity in metres per second.

    Raises:
        ValueError: If altitude is negative.

    Physics:
        Circular orbital speed follows directly from balancing centripetal and
        gravitational acceleration at radius ``R_earth + h``.
    """
    non_negative(altitude_metres, "altitude_metres")
    return math.sqrt(EARTH_MU / (EARTH_RADIUS + altitude_metres))


def vertical_ascent(
    initial_mass: float,
    propellant_mass: float,
    vacuum_thrust: float,
    specific_impulse_seconds: float,
    exit_area: float,
    exit_pressure: float,
    drag_coefficient: float,
    reference_area: float,
    time_step: float = 0.1,
) -> AscentResult:
    """Simulate a simple vertical ascent using explicit Euler integration.

    Args:
        initial_mass: Liftoff mass in kilograms.
        propellant_mass: Propellant mass consumed during the burn in kilograms.
        vacuum_thrust: Vacuum thrust in newtons.
        specific_impulse_seconds: Effective specific impulse in seconds.
        exit_area: Nozzle exit area in square metres.
        exit_pressure: Exit pressure in pascals.
        drag_coefficient: Aerodynamic drag coefficient.
        reference_area: Aerodynamic reference area in square metres.
        time_step: Integration time step in seconds.

    Returns:
        An ``AscentResult`` containing the sampled trajectory and summary values.

    Raises:
        ValueError: If masses, propulsion inputs, or time step are invalid.

    Physics:
        The model assumes purely vertical motion, constant mass flow, and a
        simple exponential atmosphere. It is a first-order performance estimate.
    """
    positive(initial_mass, "initial_mass")
    non_negative(propellant_mass, "propellant_mass")
    positive(vacuum_thrust, "vacuum_thrust")
    positive(specific_impulse_seconds, "specific_impulse_seconds")
    non_negative(exit_area, "exit_area")
    non_negative(exit_pressure, "exit_pressure")
    non_negative(drag_coefficient, "drag_coefficient")
    non_negative(reference_area, "reference_area")
    positive(time_step, "time_step")
    if propellant_mass > initial_mass:
        raise ValueError("propellant_mass cannot exceed initial_mass.")
    # The source signature includes exit pressure for interface symmetry with
    # more detailed engine models even though vacuum thrust already embeds it.
    _ = exit_pressure
    exhaust_velocity = specific_impulse_seconds * STANDARD_GRAVITY
    mdot = vacuum_thrust / exhaust_velocity
    burn_time = propellant_mass / mdot if propellant_mass > 0.0 else 0.0
    final_mass = initial_mass - propellant_mass
    samples: list[AscentSample] = []
    gravity_loss_value = 0.0
    drag_loss_value = 0.0
    max_q = 0.0
    max_q_time = 0.0
    time = 0.0
    altitude = 0.0
    velocity = 0.0
    while time <= burn_time + 1e-12:
        mass = max(final_mass, initial_mass - mdot * time)
        ambient_pressure = ambient_pressure_at_altitude(altitude)
        density = density_at_altitude(altitude)
        # Vacuum thrust already includes the internal pressure term; subtracting
        # ambient pressure times exit area recovers the actual external thrust.
        actual_thrust = vacuum_thrust - ambient_pressure * exit_area
        drag_force = drag(drag_coefficient, reference_area, density, velocity)
        q_value = dynamic_pressure(density, velocity)
        acceleration_value = (actual_thrust - drag_force) / mass - STANDARD_GRAVITY
        samples.append(
            AscentSample(
                time=time,
                altitude=altitude,
                velocity=velocity,
                mass=mass,
                acceleration=acceleration_value,
                dynamic_pressure=q_value,
            )
        )
        if q_value > max_q:
            max_q = q_value
            max_q_time = time
        gravity_loss_value += STANDARD_GRAVITY * time_step
        drag_loss_value += (drag_force / mass) * time_step
        old_velocity = velocity
        velocity = velocity + acceleration_value * time_step
        altitude = altitude + old_velocity * time_step
        if altitude < 0.0:
            altitude = 0.0
        time += time_step
    burnout_sample = samples[-1] if samples else AscentSample(0.0, 0.0, 0.0, initial_mass, 0.0, 0.0)
    return AscentResult(
        samples=samples,
        burnout_time=burn_time,
        burnout_altitude=burnout_sample.altitude,
        burnout_velocity=burnout_sample.velocity,
        ideal_delta_v=delta_v_from_isp(specific_impulse_seconds, initial_mass, final_mass),
        gravity_loss=gravity_loss_value,
        drag_loss=drag_loss_value,
        max_dynamic_pressure=max_q,
        max_dynamic_pressure_time=max_q_time,
    )


__all__ = [
    "AscentSample",
    "AscentResult",
    "gravity_loss",
    "drag",
    "dynamic_pressure",
    "density_at_altitude",
    "circular_orbit_velocity",
    "vertical_ascent",
]
