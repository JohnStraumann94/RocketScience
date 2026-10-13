"""Planar gravity-turn ascent approximation.

Chapter reference: this module implements a lightweight powered-ascent and
coast model consistent with the introductory trajectory treatment in Rocket
Propulsion Elements, including pitch-over, drag, gravity loss, and apogee.
"""

import math
from dataclasses import dataclass

from .constants import DEG_TO_RAD, EARTH_MU, EARTH_RADIUS, SEA_LEVEL_PRESSURE, STANDARD_GRAVITY
from .flight_performance import density_at_altitude, drag, dynamic_pressure
from .guard import non_negative, positive
from .nozzle_losses import ambient_pressure_at_altitude
from .rocket_equation import delta_v_from_isp


@dataclass(frozen=True)
class GravityTurnSample:
    """One sample from the gravity-turn trajectory integration."""

    time: float
    downrange: float
    altitude: float
    velocity: float
    flight_path_angle: float
    mass: float
    thrust: float
    drag: float
    acceleration: float
    dynamic_pressure: float
    powered: bool


@dataclass(frozen=True)
class GravityTurnResult:
    """Summary data for the gravity-turn ascent and coast simulation."""

    samples: list[GravityTurnSample]
    burnout_time: float
    burnout_altitude: float
    burnout_downrange: float
    burnout_velocity: float
    burnout_flight_path_angle: float
    ideal_delta_v: float
    gravity_loss: float
    drag_loss: float
    max_dynamic_pressure: float
    max_dynamic_pressure_time: float
    max_dynamic_pressure_index: int
    burnout_index: int
    apogee_altitude: float
    apogee_time: float
    apogee_index: int

    def __str__(self) -> str:
        """Render a readable gravity-turn summary report.

        Args:
            None.

        Returns:
            A multi-line report string.

        Raises:
            None.

        Physics:
            Trajectory studies are usually summarized by burnout conditions,
            cumulative losses, max-Q, and apogee, which are the key mission markers.
        """
        return (
            "Gravity Turn Result\n"
            f"  Burnout time: {self.burnout_time:.3f} s\n"
            f"  Burnout altitude: {self.burnout_altitude:.3f} m\n"
            f"  Burnout downrange: {self.burnout_downrange:.3f} m\n"
            f"  Burnout velocity: {self.burnout_velocity:.3f} m/s\n"
            f"  Burnout flight-path angle: {self.burnout_flight_path_angle:.3f} rad\n"
            f"  Ideal delta-v: {self.ideal_delta_v:.3f} m/s\n"
            f"  Gravity loss: {self.gravity_loss:.3f} m/s\n"
            f"  Drag loss: {self.drag_loss:.3f} m/s\n"
            f"  Max dynamic pressure: {self.max_dynamic_pressure:.3f} Pa at {self.max_dynamic_pressure_time:.3f} s\n"
            f"  Apogee altitude: {self.apogee_altitude:.3f} m at {self.apogee_time:.3f} s"
        )


def local_gravity(altitude_metres: float) -> float:
    """Compute local gravitational acceleration using an inverse-square model.

    Args:
        altitude_metres: Altitude above the Earth reference radius in metres.

    Returns:
        Local gravitational acceleration in metres per second squared.

    Raises:
        None.

    Physics:
        Gravity weakens with the square of distance from Earth's centre, so even
        a simple ascent model benefits from updating ``g`` with altitude.
    """
    clamped_altitude = max(0.0, altitude_metres)
    return EARTH_MU / (EARTH_RADIUS + clamped_altitude) ** 2


def ascend(
    initial_mass: float,
    propellant_mass: float,
    vacuum_thrust: float,
    specific_impulse_seconds: float,
    exit_area: float,
    exit_pressure: float,
    drag_coefficient: float,
    reference_area: float,
    pitch_over_time: float,
    pitch_over_angle_degrees: float,
    coast_seconds: float = 0.0,
    time_step: float = 0.1,
) -> GravityTurnResult:
    """Simulate a planar gravity-turn ascent with a single powered stage.

    Args:
        initial_mass: Liftoff mass in kilograms.
        propellant_mass: Propellant mass consumed during powered flight.
        vacuum_thrust: Vacuum thrust in newtons.
        specific_impulse_seconds: Specific impulse in seconds.
        exit_area: Nozzle exit area in square metres.
        exit_pressure: Exit pressure in pascals.
        drag_coefficient: Aerodynamic drag coefficient.
        reference_area: Aerodynamic reference area in square metres.
        pitch_over_time: Time at which to apply the pitch-over kick.
        pitch_over_angle_degrees: Instantaneous pitch-over angle in degrees.
        coast_seconds: Optional coast time after burnout.
        time_step: Integration time step in seconds.

    Returns:
        A ``GravityTurnResult`` with trajectory samples and mission markers.

    Raises:
        ValueError: If the rocket cannot lift off or if the inputs are invalid.

    Physics:
        The model uses a planar point-mass gravity-turn approximation with a
        one-time pitch kick, variable gravity, atmospheric drag, and RK4 stepping.
    """
    positive(initial_mass, "initial_mass")
    non_negative(propellant_mass, "propellant_mass")
    positive(vacuum_thrust, "vacuum_thrust")
    positive(specific_impulse_seconds, "specific_impulse_seconds")
    non_negative(exit_area, "exit_area")
    non_negative(exit_pressure, "exit_pressure")
    non_negative(drag_coefficient, "drag_coefficient")
    non_negative(reference_area, "reference_area")
    non_negative(pitch_over_time, "pitch_over_time")
    non_negative(pitch_over_angle_degrees, "pitch_over_angle_degrees")
    non_negative(coast_seconds, "coast_seconds")
    positive(time_step, "time_step")
    if propellant_mass > initial_mass:
        raise ValueError("propellant_mass cannot exceed initial_mass.")
    # The source signature keeps exit pressure available even though the actual
    # thrust is reconstructed from vacuum thrust minus ambient back pressure.
    _ = exit_pressure
    exhaust_velocity = specific_impulse_seconds * STANDARD_GRAVITY
    mdot = vacuum_thrust / exhaust_velocity
    burn_time = propellant_mass / mdot if propellant_mass > 0.0 else 0.0
    final_mass = initial_mass - propellant_mass
    sea_level_thrust = vacuum_thrust - SEA_LEVEL_PRESSURE * exit_area
    if sea_level_thrust <= initial_mass * STANDARD_GRAVITY:
        raise ValueError("Sea-level thrust must exceed initial weight for liftoff.")
    velocity = 0.1
    flight_path_angle = math.pi / 2.0
    altitude = 0.0
    downrange = 0.0
    time = 0.0
    pitched_over = False
    total_time = burn_time + coast_seconds
    maximum_steps = int(total_time / time_step) + 10
    samples: list[GravityTurnSample] = []
    gravity_loss_value = 0.0
    drag_loss_value = 0.0

    def derivatives(sample_time: float, sample_velocity: float, sample_gamma: float, sample_altitude: float) -> tuple[float, float, float, float]:
        """Return the state derivatives used by the RK4 integrator.

        Args:
            sample_time: Integration substep time in seconds.
            sample_velocity: Current speed in metres per second.
            sample_gamma: Current flight-path angle in radians.
            sample_altitude: Current altitude in metres.

        Returns:
            A tuple ``(dv_dt, dgamma_dt, dh_dt, dx_dt)``.

        Raises:
            None.

        Physics:
            The translational and kinematic equations couple thrust, drag,
            gravity-turn curvature, radial motion, and downrange motion.
        """
        clamped_altitude = max(0.0, sample_altitude)
        clamped_velocity = max(0.1, sample_velocity)
        burning = sample_time < burn_time
        mass = initial_mass - mdot * sample_time if burning else final_mass
        ambient_pressure = ambient_pressure_at_altitude(clamped_altitude)
        density = density_at_altitude(clamped_altitude)
        thrust = vacuum_thrust - ambient_pressure * exit_area if burning else 0.0
        drag_force = drag(drag_coefficient, reference_area, density, clamped_velocity)
        gravity_value = local_gravity(clamped_altitude)
        radius = EARTH_RADIUS + clamped_altitude
        dv_dt = (thrust - drag_force) / mass - gravity_value * math.sin(sample_gamma)
        dgamma_dt = -(gravity_value / clamped_velocity - clamped_velocity / radius) * math.cos(sample_gamma)
        dh_dt = clamped_velocity * math.sin(sample_gamma)
        dx_dt = clamped_velocity * math.cos(sample_gamma) * EARTH_RADIUS / radius
        return dv_dt, dgamma_dt, dh_dt, dx_dt

    for step in range(maximum_steps):
        powered = time < burn_time
        mass = initial_mass - mdot * time if powered else final_mass
        ambient_pressure = ambient_pressure_at_altitude(max(0.0, altitude))
        density = density_at_altitude(max(0.0, altitude))
        thrust = vacuum_thrust - ambient_pressure * exit_area if powered else 0.0
        q_value = dynamic_pressure(density, velocity)
        drag_force = drag(drag_coefficient, reference_area, density, velocity)
        gravity_value = local_gravity(altitude)
        acceleration_value = (thrust - drag_force) / mass - gravity_value * math.sin(flight_path_angle)
        samples.append(
            GravityTurnSample(
                time=time,
                downrange=downrange,
                altitude=altitude,
                velocity=velocity,
                flight_path_angle=flight_path_angle,
                mass=mass,
                thrust=thrust,
                drag=drag_force,
                acceleration=acceleration_value,
                dynamic_pressure=q_value,
                powered=powered,
            )
        )
        if altitude < 0.0 and step > 0:
            break
        if time >= total_time:
            break
        if powered:
            gravity_loss_value += gravity_value * math.sin(flight_path_angle) * time_step
            drag_loss_value += drag_force / mass * time_step
        if not pitched_over and time >= pitch_over_time:
            flight_path_angle -= pitch_over_angle_degrees * DEG_TO_RAD
            pitched_over = True
        k1_v, k1_g, k1_h, k1_x = derivatives(time, velocity, flight_path_angle, altitude)
        k2_v, k2_g, k2_h, k2_x = derivatives(
            time + 0.5 * time_step,
            velocity + 0.5 * time_step * k1_v,
            flight_path_angle + 0.5 * time_step * k1_g,
            altitude + 0.5 * time_step * k1_h,
        )
        k3_v, k3_g, k3_h, k3_x = derivatives(
            time + 0.5 * time_step,
            velocity + 0.5 * time_step * k2_v,
            flight_path_angle + 0.5 * time_step * k2_g,
            altitude + 0.5 * time_step * k2_h,
        )
        k4_v, k4_g, k4_h, k4_x = derivatives(
            time + time_step,
            velocity + time_step * k3_v,
            flight_path_angle + time_step * k3_g,
            altitude + time_step * k3_h,
        )
        velocity += (time_step / 6.0) * (k1_v + 2.0 * k2_v + 2.0 * k3_v + k4_v)
        flight_path_angle += (time_step / 6.0) * (k1_g + 2.0 * k2_g + 2.0 * k3_g + k4_g)
        altitude += (time_step / 6.0) * (k1_h + 2.0 * k2_h + 2.0 * k3_h + k4_h)
        downrange += (time_step / 6.0) * (k1_x + 2.0 * k2_x + 2.0 * k3_x + k4_x)
        time += time_step
        if velocity < 0.1:
            velocity = 0.1
    burnout_index = max((index for index, sample in enumerate(samples) if sample.powered), default=0)
    max_q_index = max(range(len(samples)), key=lambda index: samples[index].dynamic_pressure)
    apogee_index = max(range(len(samples)), key=lambda index: samples[index].altitude)
    burnout_sample = samples[burnout_index]
    apogee_sample = samples[apogee_index]
    max_q_sample = samples[max_q_index]
    return GravityTurnResult(
        samples=samples,
        burnout_time=burnout_sample.time,
        burnout_altitude=burnout_sample.altitude,
        burnout_downrange=burnout_sample.downrange,
        burnout_velocity=burnout_sample.velocity,
        burnout_flight_path_angle=burnout_sample.flight_path_angle,
        ideal_delta_v=delta_v_from_isp(specific_impulse_seconds, initial_mass, final_mass),
        gravity_loss=gravity_loss_value,
        drag_loss=drag_loss_value,
        max_dynamic_pressure=max_q_sample.dynamic_pressure,
        max_dynamic_pressure_time=max_q_sample.time,
        max_dynamic_pressure_index=max_q_index,
        burnout_index=burnout_index,
        apogee_altitude=apogee_sample.altitude,
        apogee_time=apogee_sample.time,
        apogee_index=apogee_index,
    )


__all__ = [
    "GravityTurnSample",
    "GravityTurnResult",
    "local_gravity",
    "ascend",
]
