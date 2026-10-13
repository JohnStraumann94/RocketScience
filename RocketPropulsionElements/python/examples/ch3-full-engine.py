"""Sample: Complete Engine Operating Point
Chapter: Chapter 3
Demonstrates: A one-call integrated engine analysis, including expansion state and flow-separation interpretation.
"""

import math
import os
import sys

# Add the parent directory (python\) to the module search path so that
# 'import propulsion_core' works when running this script directly from
# its own directory or from anywhere else.
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

from propulsion_core import constants, performance, nozzle_theory, nozzle_geometry, nozzle_losses, engine_analysis, rocket_equation, staging, flight_performance, gravity_turn, thermochemistry, propellants, liquid_engine, solid_motor, electric_propulsion

LABEL_WIDTH = 40


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
P3_BAR = 1.01325  # bar - ambient pressure


def main() -> None:
    # Convert the top-of-file engineering inputs into SI units.
    p1 = constants.bar_to_pa(P1_BAR)
    at = AT_CM2 * 1.0e-4
    p3 = constants.bar_to_pa(P3_BAR)

    # The core library assembles the entire ideal operating point for us.
    point = engine_analysis.analyse(GAMMA, T1, MM, p1, EPS, at, p3)

    # Print the detailed built-in operating-point report first because it mirrors
    # the C# object's ToString() content closely and already includes every field.
    print(str(point))
    print()
    print(heading("INTERPRETATION"))

    # The optimum area ratio is finite only while an atmosphere exists. In vacuum,
    # the perfect-expansion area ratio tends toward infinity.
    optimum_eps = nozzle_theory.optimum_area_ratio(GAMMA, p1, p3) if p3 > 0.0 else math.inf

    if point.expansion == nozzle_losses.ExpansionState.OPTIMUM:
        print("  OPTIMUM: p2 = p3. This nozzle is perfectly matched to this altitude.")
    elif point.expansion == nozzle_losses.ExpansionState.UNDER_EXPANDED:
        print("  UNDER-EXPANDED: p2 > p3. The gas still wants more nozzle area.")
        print("  A larger area ratio would recover some performance at this altitude.")
    else:
        print("  OVER-EXPANDED: p2 < p3. The pressure thrust term is subtracting from thrust.")
        print("  This is common for vacuum-biased nozzles operated too low in the atmosphere.")

    if point.flow_separation_likely:
        print()
        print("  *** FLOW SEPARATION WARNING ***")
        print("  The exit pressure has fallen below roughly 0.4 times ambient.")
        print("  That makes internal separation and side loads likely in a real engine.")

    print()
    print(line("Optimum area ratio at this altitude", "infinite (vacuum)" if math.isinf(optimum_eps) else format(optimum_eps, ".3f")))
    print(line("Area ratio you specified", EPS, "", ".3f"))


if __name__ == "__main__":
    main()
