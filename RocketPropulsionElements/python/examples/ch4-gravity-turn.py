"""Sample: Gravity Turn Ascent Trajectory
Chapter: Chapter 4
Demonstrates: A two-dimensional gravity-turn ascent with burnout, max-Q, apogee, and orbital-context reporting.
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
    return f"{label:<42} = {rendered}{(' ' + unit) if unit else ''}"


def sampled_indices(count: int, target_rows: int) -> list[int]:
    if count <= target_rows:
        return list(range(count))
    stride = max(1, count // target_rows)
    indices = list(range(0, count, stride))
    if indices[-1] != count - 1:
        indices.append(count - 1)
    return indices


def to_display_degrees(angle: float) -> float:
    # The C# library exposes degrees. The current Python port stores radians in the
    # trajectory object, so convert small-angle values for display to preserve the
    # original sample's user-facing output.
    return angle * constants.RAD_TO_DEG if abs(angle) <= 2.0 * math.pi + 1e-9 else angle


# =============================================================================
# EDIT THESE VALUES AND RE-RUN
# =============================================================================
M0 = 50_000.0              # kg - gross lift-off mass
MP = 42_000.0              # kg - propellant mass
VACUUM_THRUST = 900_000.0  # N - vacuum thrust
ISP = 310.0                # s - engine specific impulse
EXIT_AREA = 0.8            # m^2 - nozzle exit area
EXIT_PRESSURE = 50_000.0   # Pa - nozzle exit pressure
CD = 0.3                   # - drag coefficient
REF_AREA = 7.0             # m^2 - aerodynamic reference area
PITCH_OVER_TIME = 12.0     # s - time to apply the pitch-over kick
PITCH_OVER_ANGLE = 3.5     # deg - pitch-over angle away from vertical
COAST_SECONDS = 200.0      # s - post-burn ballistic coast
TIME_STEP = 0.1            # s - integration step size


def main() -> None:
    ascent = gravity_turn.ascend(M0, MP, VACUUM_THRUST, ISP, EXIT_AREA, EXIT_PRESSURE, CD, REF_AREA, PITCH_OVER_TIME, PITCH_OVER_ANGLE, COAST_SECONDS, TIME_STEP)

    print(heading("VEHICLE AND STEERING"))
    print(line("Gross lift-off mass", M0, "kg", ".1f"))
    print(line("Propellant mass", MP, "kg", ".1f"))
    print(line("Vacuum thrust", VACUUM_THRUST / 1000.0, "kN", ".2f"))
    print(line("Pitch-over at", PITCH_OVER_TIME, "s", ".2f"))
    print(line("Pitch-over angle", PITCH_OVER_ANGLE, "deg", ".2f"))
    print()

    print(heading("FLIGHT PATH"))
    print("   t(s)  Downrng(km)   Alt(km)   Vel(m/s)  Gamma(deg)   q(kPa)")
    print("  " + "-" * 66)
    for index in sampled_indices(len(ascent.samples), 18):
        sample = ascent.samples[index]
        gamma_deg = to_display_degrees(sample.flight_path_angle)
        coast_flag = "  (coast)" if not sample.powered else ""
        print(f"  {sample.time:5.1f}  {sample.downrange / 1000.0:10.3f}  {sample.altitude / 1000.0:8.3f}  {sample.velocity:9.1f}  {gamma_deg:10.2f}  {sample.dynamic_pressure / 1000.0:7.2f}{coast_flag}")
    print()

    burnout_gamma_deg = to_display_degrees(ascent.burnout_flight_path_angle)
    gamma_rad = math.radians(burnout_gamma_deg)
    horizontal = ascent.burnout_velocity * math.cos(gamma_rad)
    vertical = ascent.burnout_velocity * math.sin(gamma_rad)

    print(heading("BURNOUT STATE"))
    print(line("Burnout time", ascent.burnout_time, "s", ".2f"))
    print(line("Burnout altitude", ascent.burnout_altitude / 1000.0, "km", ".3f"))
    print(line("Burnout downrange", ascent.burnout_downrange / 1000.0, "km", ".3f"))
    print(line("Burnout speed", ascent.burnout_velocity, "m/s", ".2f"))
    print(line("Burnout flight path angle", burnout_gamma_deg, "deg", ".2f"))
    print(line("Horizontal component", horizontal, "m/s", ".2f"))
    print(line("Vertical component", vertical, "m/s", ".2f"))
    print()

    vertical_flight_gravity_loss = constants.STANDARD_GRAVITY * ascent.burnout_time
    print(heading("LOSS ACCOUNTING"))
    print(line("Ideal delta-v", ascent.ideal_delta_v, "m/s", ".2f"))
    print(line("Gravity loss", ascent.gravity_loss, "m/s", ".2f"))
    print(line("Drag loss", ascent.drag_loss, "m/s", ".2f"))
    print(line("Speed achieved", ascent.burnout_velocity, "m/s", ".2f"))
    print(line("Efficiency", 100.0 * ascent.burnout_velocity / ascent.ideal_delta_v, "%", ".2f"))
    print(line("Vertical-flight gravity loss (g*tb)", vertical_flight_gravity_loss, "m/s", ".2f"))
    print(line("Saved by turning over", vertical_flight_gravity_loss - ascent.gravity_loss, "m/s", ".2f"))
    print()

    print(heading("MAX-Q AND APOGEE"))
    print(line("Max dynamic pressure", ascent.max_dynamic_pressure / 1000.0, "kPa", ".3f"))
    print(line("Time of max-Q", ascent.max_dynamic_pressure_time, "s", ".2f"))
    print(line("Apogee altitude", ascent.apogee_altitude / 1000.0, "km", ".3f"))
    print(line("Time of apogee", ascent.apogee_time, "s", ".2f"))
    print()

    circular = flight_performance.circular_orbit_velocity(ascent.burnout_altitude)
    print(heading("ORBITAL CONTEXT"))
    print(line("Circular speed at burnout altitude", circular, "m/s", ".2f"))
    print(line("Horizontal speed achieved", horizontal, "m/s", ".2f"))
    print(line("Shortfall", circular - horizontal, "m/s", ".2f"))


if __name__ == "__main__":
    main()
