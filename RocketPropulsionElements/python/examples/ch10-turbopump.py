"""Sample: Turbopump Power and Specific Speed
Chapter: Chapter 10
Demonstrates: Pump power, head, specific speed, and the severe density penalty of hydrogen pumping.
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


# =============================================================================
# EDIT THESE VALUES AND RE-RUN
# =============================================================================
MDOT = 70.0      # kg/s - pump mass flow rate
DP_BAR = 200.0   # bar - pump pressure rise
RHO = 810.0      # kg/m^3 - liquid density (RP-1 by default)
ETA = 0.75       # - pump efficiency
RPM = 25_000.0   # rpm - shaft rotational speed


def main() -> None:
    dp = constants.bar_to_pa(DP_BAR)
    power = liquid_engine.pump_power(MDOT, dp, RHO, ETA)
    head = liquid_engine.pump_head(dp, RHO)
    volume_flow = MDOT / RHO
    omega = RPM * 2.0 * math.pi / 60.0
    ns = liquid_engine.pump_specific_speed(omega, volume_flow, head)
    lh2_power = liquid_engine.pump_power(MDOT, dp, 71.0, ETA)
    lh2_head = liquid_engine.pump_head(dp, 71.0)

    print(heading("PUMP PARAMETERS"))
    print(line("Mass flow rate", MDOT, "kg/s", ".3f"))
    print(line("Pressure rise", DP_BAR, "bar", ".2f"))
    print(line("Density", RHO, "kg/m^3", ".1f"))
    print(line("Pump efficiency", ETA, "", ".3f"))
    print(line("Rotational speed", RPM, "rpm", ".0f"))
    print(line("Volume flow rate", volume_flow, "m^3/s", ".5f"))
    print()

    print(heading("POWER"))
    print(line("Shaft power required", power / 1000.0, "kW", ".2f"))
    print(line("Shaft power required", power / 745.7, "hp", ".0f"))
    print(line("Torque at this speed", power / omega, "N.m", ".2f"))
    print()

    print(heading("HEAD AND SPECIFIC SPEED"))
    print(line("Pump head", head, "m", ".2f"))
    print(line("Angular velocity", omega, "rad/s", ".2f"))
    print(line("Specific speed Ns", ns, "", ".4f"))
    print()

    print(heading("LH2 COMPARISON"))
    print(line("Same mass flow, same pressure rise, rho=71", ""))
    print(line("LH2 pump head", lh2_head, "m", ".2f"))
    print(line("LH2 shaft power", lh2_power / 1000.0, "kW", ".2f"))
    print(line("Power ratio LH2 / baseline", lh2_power / power, "", ".3f"))
    print()

    print(heading("IMPELLER TYPE SUGGESTION"))
    if ns < 0.3:
        print("  Very low specific speed: multiple radial stages or higher shaft speed are likely.")
    elif ns < 0.6:
        print("  Radial centrifugal impeller: high head, low flow, classic rocket-pump territory.")
    elif ns < 1.5:
        print("  Mixed-flow impeller: a practical compromise between head and flow.")
    elif ns < 2.5:
        print("  Mixed-flow toward axial: the duty is shifting toward higher flow per stage.")
    else:
        print("  Axial impeller: high flow, low head per stage, often appropriate for inducers.")


if __name__ == "__main__":
    main()
