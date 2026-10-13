"""Sample: Altitude Sweep - One Nozzle, Every Altitude
Chapter: Chapter 3
Demonstrates: How one fixed nozzle gains thrust and Isp as ambient pressure falls with altitude.
"""

import math
import os
import sys

# Add the parent directory (python\) to the module search path so that
# 'import propulsion_core' works when running this script directly from
# its own directory or from anywhere else.
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

from propulsion_core import constants, performance, nozzle_theory, nozzle_geometry, nozzle_losses, engine_analysis, rocket_equation, staging, flight_performance, gravity_turn, thermochemistry, propellants, liquid_engine, solid_motor, electric_propulsion

LABEL_WIDTH = 34


def heading(title: str) -> str:
    return f"{title}\n{'-' * len(title)}"


def line(label: str, value, unit: str = "", fmt_spec: str = "") -> str:
    rendered = value if isinstance(value, str) else (format(value, fmt_spec) if fmt_spec else str(value))
    return f"{label:<{LABEL_WIDTH}} = {rendered}{(' ' + unit) if unit else ''}"


# =============================================================================
# EDIT THESE VALUES AND RE-RUN
# =============================================================================
GAMMA = 1.22    # - ratio of specific heats
T1 = 3600.0     # K - chamber temperature
MM = 21.5       # kg/kmol - exhaust molecular mass
P1_BAR = 100.0  # bar - chamber pressure
EPS = 22.0      # - nozzle area ratio A2/At
AT_CM2 = 300.0  # cm^2 - throat area
H_MAX_KM = 60.0 # km - highest altitude to sample


def main() -> None:
    p1 = constants.bar_to_pa(P1_BAR)
    at = AT_CM2 * 1.0e-4

    print("  Alt(km)   p3(bar)    Thrust(kN)   Isp(s)   Expansion       Sep?")
    print("  " + "-" * 68)

    # Sweep 12 equal altitude intervals and then add a pure-vacuum row at the end.
    steps = 12
    for i in range(steps + 1):
        alt_m = (H_MAX_KM * 1000.0) * i / steps
        p3 = nozzle_losses.ambient_pressure_at_altitude(alt_m)
        point = engine_analysis.analyse(GAMMA, T1, MM, p1, EPS, at, p3)
        print(
            f"  {alt_m / 1000.0:6.1f}  {constants.pa_to_bar(p3):9.5f}  {point.thrust / 1000.0:11.2f}  "
            f"{point.specific_impulse:7.1f}   {point.expansion.name:<14}  {'YES' if point.flow_separation_likely else '-'}"
        )

    vacuum = engine_analysis.analyse(GAMMA, T1, MM, p1, EPS, at, 0.0)
    print(
        f"  {'vac':>6}  {0.0:9.5f}  {vacuum.thrust / 1000.0:11.2f}  {vacuum.specific_impulse:7.1f}   "
        f"{'VACUUM':<14}  -"
    )
    print()

    sea_level = engine_analysis.analyse(GAMMA, T1, MM, p1, EPS, at, constants.SEA_LEVEL_PRESSURE)
    print(heading("SEA LEVEL versus VACUUM"))
    print(line("Sea-level thrust", sea_level.thrust / 1000.0, "kN", ".2f"))
    print(line("Vacuum thrust", vacuum.thrust / 1000.0, "kN", ".2f"))
    print(line("Thrust gain", 100.0 * (vacuum.thrust / sea_level.thrust - 1.0), "%", ".2f"))
    print(line("Sea-level Isp", sea_level.specific_impulse, "s", ".2f"))
    print(line("Vacuum Isp", vacuum.specific_impulse, "s", ".2f"))
    print(line("Isp gain", vacuum.specific_impulse - sea_level.specific_impulse, "s", ".2f"))
    print()
    print(line("Fixed exit pressure p2", constants.pa_to_bar(vacuum.exit_pressure), "bar", ".5f"))
    print("  Note that p2 stays fixed: the nozzle geometry did not change, only the atmosphere did.")


if __name__ == "__main__":
    main()
