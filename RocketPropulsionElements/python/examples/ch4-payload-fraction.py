"""Sample: Payload Fraction and the Single-Stage Limit
Chapter: Chapter 4
Demonstrates: The single-stage payload-fraction cliff, the hard delta-v ceiling, and why SSTO is so marginal.
"""

import math
import os
import sys

# Add the parent directory (python\) to the module search path so that
# 'import propulsion_core' works when running this script directly from
# its own directory or from anywhere else.
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

from propulsion_core import constants, performance, nozzle_theory, nozzle_geometry, nozzle_losses, engine_analysis, rocket_equation, staging, flight_performance, gravity_turn, thermochemistry, propellants, liquid_engine, solid_motor, electric_propulsion

LABEL_WIDTH = 38


def heading(title: str) -> str:
    return f"{title}\n{'-' * len(title)}"


def line(label: str, value, unit: str = "", fmt_spec: str = "") -> str:
    rendered = value if isinstance(value, str) else (format(value, fmt_spec) if fmt_spec else str(value))
    return f"{label:<{LABEL_WIDTH}} = {rendered}{(' ' + unit) if unit else ''}"


# =============================================================================
# EDIT THESE VALUES AND RE-RUN
# =============================================================================
ISP = 340.0  # s - stage specific impulse
EPS = 0.08   # - structural coefficient epsilon
DV = 9_400.0 # m/s - mission delta-v


def main() -> None:
    c = performance.exhaust_velocity_from_isp(ISP)
    max_dv = rocket_equation.maximum_single_stage_delta_v(c, EPS)
    payload_frac = rocket_equation.payload_fraction(DV, c, EPS)

    print(heading("THE STAGE"))
    print(line("Specific impulse", ISP, "s", ".2f"))
    print(line("Effective exhaust velocity c", c, "m/s", ".2f"))
    print(line("Structural coefficient epsilon", EPS, "", ".4f"))
    print()

    print(heading("THE HARD CEILING"))
    print(line("dv_max = -c * ln(epsilon)", max_dv, "m/s", ".2f"))
    print(line("Requested mission delta-v", DV, "m/s", ".2f"))
    print(line("Margin against the ceiling", max_dv - DV, "m/s", ".2f"))
    print()

    if payload_frac <= 0.0:
        print("PAYLOAD")
        print("-------")
        print("  IMPOSSIBLE: this single stage cannot reach the requested delta-v even with zero payload.")
    else:
        print(heading("PAYLOAD"))
        print(line("Payload fraction lambda", payload_frac, "", ".5f"))
        print(line("Payload fraction", 100.0 * payload_frac, "%", ".3f"))
        print(line("Payload per 100 t gross mass", 100000.0 * payload_frac, "kg", ".1f"))
    print()

    print(heading("THE CLIFF"))
    print("    dv (m/s)   Payload fraction   Payload per 100 t")
    print("  " + "-" * 52)
    upper = max(DV, max_dv) + 1000.0
    dv_value = 5000.0
    while dv_value <= upper + 1e-9:
        lam = rocket_equation.payload_fraction(dv_value, c, EPS)
        if lam > 0.0:
            print(f"   {dv_value:9.0f}   {lam:16.3%}   {100000.0 * lam:15.0f} kg")
        else:
            print(f"   {dv_value:9.0f}   {'IMPOSSIBLE':>16}   {'-':>15}")
        dv_value += 500.0
    print()

    print(heading("WHAT WOULD FIX IT?"))
    print(line("dv_max with epsilon reduced to 0.05", rocket_equation.maximum_single_stage_delta_v(c, 0.05), "m/s", ".2f"))
    print(line("dv_max with LOX/LH2 (Isp 450 s)", rocket_equation.maximum_single_stage_delta_v(performance.exhaust_velocity_from_isp(450.0), EPS), "m/s", ".2f"))
    print(line("dv_max with both improvements", rocket_equation.maximum_single_stage_delta_v(performance.exhaust_velocity_from_isp(450.0), 0.05), "m/s", ".2f"))


if __name__ == "__main__":
    main()
