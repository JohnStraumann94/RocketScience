"""Sample: Vertical Ascent with Gravity and Drag Losses
Chapter: Chapter 4
Demonstrates: A one-dimensional ascent integration, burnout state, loss accounting, gravity-loss comparisons, and max-Q.
"""

import math
import os
import sys

# Add the parent directory (python\) to the module search path so that
# 'import propulsion_core' works when running this script directly from
# its own directory or from anywhere else.
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

from propulsion_core import constants, performance, nozzle_theory, nozzle_geometry, nozzle_losses, engine_analysis, rocket_equation, staging, flight_performance, gravity_turn, thermochemistry, propellants, liquid_engine, solid_motor, electric_propulsion


def heading(title: str) -> str:
    return f"{title}\n{'-' * len(title)}"


def line(label: str, value, unit: str = "", fmt_spec: str = "") -> str:
    rendered = value if isinstance(value, str) else (format(value, fmt_spec) if fmt_spec else str(value))
    return f"{label:<40} = {rendered}{(' ' + unit) if unit else ''}"


def sampled_indices(count: int, target_rows: int) -> list[int]:
    if count <= target_rows:
        return list(range(count))
    stride = max(1, count // target_rows)
    indices = list(range(0, count, stride))
    if indices[-1] != count - 1:
        indices.append(count - 1)
    return indices


# =============================================================================
# EDIT THESE VALUES AND RE-RUN
# =============================================================================
M0 = 50_000.0            # kg - gross lift-off mass
MP = 42_000.0            # kg - propellant mass
VACUUM_THRUST = 900_000.0  # N - vacuum thrust
ISP = 310.0              # s - engine specific impulse
EXIT_AREA = 0.8          # m^2 - nozzle exit area
EXIT_PRESSURE = 50_000.0 # Pa - nozzle exit pressure
CD = 0.3                 # - drag coefficient
REF_AREA = 7.0           # m^2 - aerodynamic reference area
TIME_STEP = 0.05         # s - integration time step


def main() -> None:
    sea_level_thrust = VACUUM_THRUST - constants.SEA_LEVEL_PRESSURE * EXIT_AREA
    liftoff_twr = performance.thrust_to_weight_ratio(sea_level_thrust, M0)
    ascent = flight_performance.vertical_ascent(M0, MP, VACUUM_THRUST, ISP, EXIT_AREA, EXIT_PRESSURE, CD, REF_AREA, TIME_STEP)

    print(heading("VEHICLE"))
    print(line("Gross lift-off mass", M0, "kg", ".1f"))
    print(line("Propellant mass", MP, "kg", ".1f"))
    print(line("Vacuum thrust", VACUUM_THRUST / 1000.0, "kN", ".2f"))
    print(line("Sea-level thrust", sea_level_thrust / 1000.0, "kN", ".2f"))
    print(line("Lift-off thrust-to-weight", liftoff_twr, "", ".3f"))
    print()

    print(heading("TRAJECTORY"))
    print("   t(s)   Alt(km)   Vel(m/s)   Mass(kg)   Accel(g)    q(kPa)")
    print("  " + "-" * 62)
    for index in sampled_indices(len(ascent.samples), 15):
        sample = ascent.samples[index]
        print(f"  {sample.time:5.1f}   {sample.altitude / 1000.0:7.3f}   {sample.velocity:8.1f}   {sample.mass:8.0f}   {sample.acceleration / constants.STANDARD_GRAVITY:8.2f}   {sample.dynamic_pressure / 1000.0:8.2f}")
    print()

    print(heading("BURNOUT STATE"))
    print(line("Burnout time", ascent.burnout_time, "s", ".2f"))
    print(line("Burnout altitude", ascent.burnout_altitude / 1000.0, "km", ".3f"))
    print(line("Burnout velocity", ascent.burnout_velocity, "m/s", ".2f"))
    print()

    back_pressure_residual = ascent.ideal_delta_v - ascent.gravity_loss - ascent.drag_loss - ascent.burnout_velocity
    print(heading("LOSS ACCOUNTING"))
    print(line("Ideal delta-v", ascent.ideal_delta_v, "m/s", ".2f"))
    print(line("Gravity loss", ascent.gravity_loss, "m/s", ".2f"))
    print(line("Drag loss", ascent.drag_loss, "m/s", ".2f"))
    print(line("Back-pressure residual", back_pressure_residual, "m/s", ".2f"))
    print(line("Velocity achieved", ascent.burnout_velocity, "m/s", ".2f"))
    print(line("Efficiency", 100.0 * ascent.burnout_velocity / ascent.ideal_delta_v, "%", ".2f"))
    print(line("Gravity loss as g * tb", constants.STANDARD_GRAVITY * ascent.burnout_time, "m/s", ".2f"))
    for angle in (90.0, 60.0, 45.0, 30.0, 10.0):
        print(line(f"Loss at flight path angle {angle:4.0f} deg", flight_performance.gravity_loss(ascent.burnout_time, angle), "m/s", ".2f"))
    print()

    max_q_sample = max(ascent.samples, key=lambda s: s.dynamic_pressure)
    print(heading("MAX-Q"))
    print(line("Maximum dynamic pressure", ascent.max_dynamic_pressure / 1000.0, "kPa", ".3f"))
    print(line("Time of max-Q", ascent.max_dynamic_pressure_time, "s", ".2f"))
    print(line("Altitude at max-Q", max_q_sample.altitude / 1000.0, "km", ".3f"))
    print(line("Velocity at max-Q", max_q_sample.velocity, "m/s", ".2f"))
    print(line("Density at max-Q", flight_performance.density_at_altitude(max_q_sample.altitude), "kg/m^3", ".5f"))
    print()

    orbital = flight_performance.circular_orbit_velocity(200000.0)
    print(heading("ORBITAL CONTEXT"))
    print(line("Circular orbit velocity at 200 km", orbital, "m/s", ".2f"))
    print(line("Still needed", orbital - ascent.burnout_velocity, "m/s", ".2f"))


if __name__ == "__main__":
    main()
