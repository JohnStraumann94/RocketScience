"""Sample: Characteristic Velocity c* and Thrust Coefficient C_F
Chapter: Chapter 3
Demonstrates: The chamber/nozzle split, the identity c = c* x C_F, and thrust sizing from C_F alone.
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
GAMMA = 1.24   # - ratio of specific heats
T1 = 3500.0    # K - chamber temperature
MM = 22.0      # kg/kmol - exhaust molecular mass
P1_BAR = 70.0  # bar - chamber pressure
EPS = 16.0     # - nozzle area ratio A2/At
AT_CM2 = 200.0 # cm^2 - throat area
P3_BAR = 1.01325  # bar - ambient pressure


def main() -> None:
    # Convert the dimensional inputs into SI before passing them to propulsion_core.
    p1 = constants.bar_to_pa(P1_BAR)
    at = AT_CM2 * 1.0e-4
    p3 = constants.bar_to_pa(P3_BAR)

    # Chamber-only quantities come first: capital Gamma and c* say nothing about the nozzle.
    capital_gamma = nozzle_theory.vandenkerckhove_function(GAMMA)
    c_star = nozzle_theory.characteristic_velocity(GAMMA, T1, MM)

    # Exit pressure follows from geometry, not from user choice once area ratio is fixed.
    p2 = nozzle_theory.exit_pressure_from_area_ratio(GAMMA, p1, EPS)
    cf = nozzle_theory.thrust_coefficient(GAMMA, p1, p2, p3, EPS)
    cf_max = nozzle_theory.maximum_thrust_coefficient(GAMMA)
    cf_vacuum = nozzle_theory.thrust_coefficient(GAMMA, p1, p2, 0.0, EPS)

    # The choked-flow relation gives mass flow directly from chamber state and throat area.
    mdot = nozzle_theory.mass_flow_rate(GAMMA, p1, T1, MM, at)
    thrust = cf * p1 * at

    # These two routes to c should agree to within floating-point round-off only.
    c_from_identity = c_star * cf
    c_from_thrust = performance.effective_exhaust_velocity(thrust, mdot)

    print(heading("CHAMBER: CHARACTERISTIC VELOCITY"))
    print(line("Vandenkerckhove function Gamma", capital_gamma, "", ".5f"))
    print(line("Characteristic velocity c*", c_star, "m/s", ".2f"))
    print(line("Mass flow rate m_dot", mdot, "kg/s", ".3f"))
    print(line("Check  c* = p1*At/m_dot", p1 * at / mdot, "m/s", ".2f"))
    print()

    print(heading("NOZZLE: THRUST COEFFICIENT"))
    print(line("Area ratio A2/At", EPS, "", ".3f"))
    print(line("Resulting exit pressure p2", constants.pa_to_bar(p2), "bar", ".5f"))
    print(line("Ambient pressure p3", constants.pa_to_bar(p3), "bar", ".5f"))
    print(line("Thrust coefficient C_F", cf, "", ".5f"))
    print(line("Vacuum thrust coefficient", cf_vacuum, "", ".5f"))
    print(line("Theoretical maximum C_F,max", cf_max, "", ".5f"))
    print(line("Nozzle quality  C_F / C_F,max", 100.0 * cf / cf_max, "%", ".2f"))
    print()

    print(heading("THE IDENTITY  c = c* x C_F"))
    print(line("c from c* x C_F", c_from_identity, "m/s", ".4f"))
    print(line("c from F / m_dot", c_from_thrust, "m/s", ".4f"))
    print(line("Absolute difference", abs(c_from_identity - c_from_thrust), "m/s", ".3e"))
    print("  The tiny difference is just floating-point round-off.")
    print()

    print(heading("THRUST FROM C_F ALONE"))
    print(line("F = C_F * p1 * At", thrust / 1000.0, "kN", ".3f"))
    print(line("F = m_dot * c", mdot * c_from_thrust / 1000.0, "kN", ".3f"))
    print(line("Specific impulse", performance.specific_impulse(c_from_thrust), "s", ".2f"))


if __name__ == "__main__":
    main()
