"""Sample: BATES Grain Thrust-Time Curve
Chapter: Chapter 12
Demonstrates: Marching a BATES grain through time, solving equilibrium pressure at each web depth, and summarising the resulting thrust history.
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
    return f"{label:<44} = {rendered}{(' ' + unit) if unit else ''}"


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
OD_MM = 200.0   # mm - grain outer diameter
ID_MM = 70.0    # mm - grain core diameter
LEN_MM = 500.0  # mm - grain length
AT_CM2 = 12.0   # cm^2 - throat area
RHO = 1770.0    # kg/m^3 - propellant density
CSTAR = 1550.0  # m/s - characteristic velocity
A = 5.0e-5      # SI - burn-rate coefficient
N = 0.35        # - pressure exponent
CF = 1.55       # - thrust coefficient
DT = 0.02       # s - integration time step


def main() -> None:
    od = OD_MM / 1000.0
    core = ID_MM / 1000.0
    length = LEN_MM / 1000.0
    at = AT_CM2 * 1.0e-4
    max_web = (od - core) / 2.0
    prop_mass = solid_motor.bates_grain_mass(od, core, length, RHO)
    case_volume = nozzle_geometry.area_from_diameter(od) * length
    prop_volume = prop_mass / RHO
    loading = solid_motor.volumetric_loading_fraction(prop_volume, case_volume)

    history = []
    web = 0.0
    time = 0.0
    total_impulse = 0.0
    burned_mass = 0.0
    max_steps = 200000
    steps = 0

    # March the internal-ballistics problem forward one small web increment at a time.
    while web < max_web and steps < max_steps:
        steps += 1
        burning_area = solid_motor.bates_grain_burning_area(od, core, length, web)
        if burning_area <= 0.0:
            break
        pressure = solid_motor.equilibrium_chamber_pressure(burning_area, at, RHO, CSTAR, A, N)
        burn_rate = solid_motor.burn_rate(A, pressure, N)
        thrust = CF * pressure * at
        history.append((time, web, burning_area, pressure, thrust, burn_rate))
        total_impulse += thrust * DT
        burned_mass += burning_area * RHO * burn_rate * DT
        web += burn_rate * DT
        time += DT

    final = history[-1]
    burn_time = final[0] + DT
    peak_pressure = max(item[3] for item in history)
    avg_pressure = sum(item[3] for item in history) / len(history)
    peak_thrust = max(item[4] for item in history)
    avg_thrust = sum(item[4] for item in history) / len(history)

    print(heading("GRAIN GEOMETRY"))
    print(line("Outer diameter", OD_MM, "mm", ".2f"))
    print(line("Initial core diameter", ID_MM, "mm", ".2f"))
    print(line("Grain length", LEN_MM, "mm", ".2f"))
    print(line("Web thickness", max_web * 1000.0, "mm", ".2f"))
    print(line("Propellant mass", prop_mass, "kg", ".4f"))
    print(line("Volumetric loading fraction", loading, "", ".2%"))
    print()

    print(heading("THRUST-TIME CURVE"))
    print("    t(s)   Web(mm)   Ab(cm2)   p(bar)   Thrust(N)   r(mm/s)")
    print("  " + "-" * 64)
    for index in sampled_indices(len(history), 18):
        t, w, area, pressure, thrust, rate = history[index]
        print(f"  {t:6.2f}   {w * 1000.0:7.2f}   {area * 1.0e4:7.1f}   {constants.pa_to_bar(pressure):6.2f}   {thrust:9.1f}   {rate * 1000.0:8.3f}")
    print()

    print(heading("SUMMARY"))
    print(line("Action time", burn_time, "s", ".3f"))
    print(line("Peak pressure", constants.pa_to_bar(peak_pressure), "bar", ".3f"))
    print(line("Average pressure", constants.pa_to_bar(avg_pressure), "bar", ".3f"))
    print(line("Peak thrust", peak_thrust, "N", ".2f"))
    print(line("Average thrust", avg_thrust, "N", ".2f"))
    print(line("Total impulse", total_impulse, "N.s", ".2f"))
    print(line("Isp by total impulse", performance.specific_impulse_from_total_impulse(total_impulse, prop_mass), "s", ".2f"))
    print(line("Propellant burned fraction", min(burned_mass / prop_mass, 1.0), "", ".2%"))


if __name__ == "__main__":
    main()
