"""Sample: Optimum Number of Stages
Chapter: Chapter 4
Demonstrates: The idealised payload-fraction sweep versus stage count and the diminishing-return pattern behind staging.
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
ISP = 340.0   # s - specific impulse for each idealised stage
EPS = 0.08    # - structural coefficient epsilon for every stage
DV = 9_400.0  # m/s - total mission delta-v
NMAX = 8      # - maximum number of stages to evaluate


def main() -> None:
    c = performance.exhaust_velocity_from_isp(ISP)

    print(heading("INPUTS"))
    print(line("Effective exhaust velocity c", c, "m/s", ".2f"))
    print(line("Structural coefficient", EPS, "", ".4f"))
    print(line("Total mission delta-v", DV, "m/s", ".2f"))
    print(line("Single-stage ceiling dv_max", rocket_equation.maximum_single_stage_delta_v(c, EPS), "m/s", ".2f"))
    print()

    print(heading("SWEEP"))
    print("   n   dv/stage    lambda         gain       payload/100t")
    print("  " + "-" * 66)
    previous = 0.0
    for n in range(1, NMAX + 1):
        lam = staging.ideal_payload_fraction(DV, c, EPS, n)
        gain = lam - previous if n > 1 else None
        lambda_text = format(lam, ".4%") if lam > 0.0 else "IMPOSSIBLE"
        gain_text = format(gain, ".4%") if gain is not None else "-"
        payload_text = f"{100000.0 * lam:.0f} kg" if lam > 0.0 else "-"
        print(f"  {n:2d}   {DV / n:8.1f}   {lambda_text:>11}   {gain_text:>10}   {payload_text:>12}")
        previous = lam
    print()

    best_count, best_frac = staging.optimum_stage_count(DV, c, EPS, NMAX)
    print(heading("WHAT THE OPTIMISER SAYS"))
    print(line("Recommended stage count", str(best_count)))
    print(line("Payload fraction there", best_frac, "", ".6f"))
    print()

    print(heading("MARGINAL GAIN"))
    lambda_prev = staging.ideal_payload_fraction(DV, c, EPS, 1)
    for n in range(2, NMAX + 1):
        lambda_now = staging.ideal_payload_fraction(DV, c, EPS, n)
        print(line(f"Adding stage {n}", 100000.0 * (lambda_now - lambda_prev), "kg", ".1f"))
        lambda_prev = lambda_now


if __name__ == "__main__":
    main()
