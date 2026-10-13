"""Sample: Nozzle Loss Budget
Chapter: Chapter 3
Demonstrates: How divergence, friction, chemistry, and c* efficiency reduce theoretical Isp to delivered Isp.
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
THEORETICAL_ISP = 350.0     # s - ideal specific impulse from Chapter 3 theory
HALF_ANGLE_DEG = 15.0       # deg - equivalent divergence half-angle
FRICTION = 0.99             # - friction / boundary-layer efficiency factor
TWO_PHASE = 1.0             # - condensed-phase efficiency factor
CHEMICAL = 0.985            # - chemistry / kinetic efficiency factor
MEASURED_CSTAR = 1780.0     # m/s - measured characteristic velocity
THEORETICAL_CSTAR = 1830.0  # m/s - theoretical characteristic velocity


def main() -> None:
    divergence = nozzle_geometry.conical_divergence_factor(HALF_ANGLE_DEG)
    nozzle_eff = nozzle_losses.combined_nozzle_efficiency(divergence, FRICTION, TWO_PHASE, CHEMICAL)
    cstar_eff = nozzle_losses.cstar_efficiency(MEASURED_CSTAR, THEORETICAL_CSTAR)
    overall = nozzle_eff * cstar_eff
    delivered_isp = THEORETICAL_ISP * overall
    lost_total = THEORETICAL_ISP - delivered_isp

    print(heading("EFFICIENCY FACTORS"))
    print(line(f"Divergence lambda at {HALF_ANGLE_DEG:.1f} deg", divergence, "", ".5f"))
    print(line("Friction / boundary layer", FRICTION, "", ".5f"))
    print(line("Two-phase flow", TWO_PHASE, "", ".5f"))
    print(line("Chemical / kinetic", CHEMICAL, "", ".5f"))
    print(line("Combined nozzle efficiency", nozzle_eff, "", ".5f"))
    print()
    print(line("Measured c*", MEASURED_CSTAR, "m/s", ".2f"))
    print(line("Theoretical c*", THEORETICAL_CSTAR, "m/s", ".2f"))
    print(line("c* efficiency", cstar_eff, "", ".5f"))
    print()
    print(line("Overall efficiency", overall, "", ".5f"))
    print(line("Overall efficiency", 100.0 * overall, "%", ".3f"))
    print()

    print(heading("SPECIFIC IMPULSE"))
    print(line("Theoretical Isp", THEORETICAL_ISP, "s", ".2f"))
    print(line("Delivered Isp", delivered_isp, "s", ".2f"))
    print(line("Total loss", lost_total, "s", ".2f"))
    print()

    print(heading("WHERE THE SECONDS WENT"))
    print(line("Divergence", THEORETICAL_ISP * (1.0 - divergence), "s", ".3f"))
    print(line("Friction / boundary layer", THEORETICAL_ISP * (1.0 - FRICTION), "s", ".3f"))
    print(line("Two-phase flow", THEORETICAL_ISP * (1.0 - TWO_PHASE), "s", ".3f"))
    print(line("Chemical / kinetic", THEORETICAL_ISP * (1.0 - CHEMICAL), "s", ".3f"))
    print(line("Incomplete combustion (c*)", THEORETICAL_ISP * (1.0 - cstar_eff), "s", ".3f"))
    print("  These add a bit high because the factors multiply rather than add.")
    print()

    print(heading("VERDICT"))
    if overall >= 0.96:
        print("  Excellent. This is flight-hardware territory.")
    elif overall >= 0.93:
        print("  Normal for a well-developed engine.")
    elif overall >= 0.90:
        print("  Acceptable for development hardware, with room still left to recover.")
    else:
        print("  Poor. Start by checking c* efficiency, because that points directly at the chamber or injector.")


if __name__ == "__main__":
    main()
