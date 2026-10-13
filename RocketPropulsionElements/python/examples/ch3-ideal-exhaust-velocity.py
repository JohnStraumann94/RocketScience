"""Sample: Ideal Exhaust Velocity
Chapter: Chapter 3
Demonstrates: Sutton Equation 3-15 term by term, the theoretical ceiling, and first-order sensitivity.
"""

import math
import os
import sys

# Add the parent directory (python\) to the module search path so that
# 'import propulsion_core' works when running this script directly from
# its own directory or from anywhere else.
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

from propulsion_core import constants, performance, nozzle_theory, nozzle_geometry, nozzle_losses, engine_analysis, rocket_equation, staging, flight_performance, gravity_turn, thermochemistry, propellants, liquid_engine, solid_motor, electric_propulsion

LABEL_WIDTH = 42


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
P2_BAR = 1.0   # bar - exit pressure


def main() -> None:
    # Convert the user-friendly bar inputs to SI pascals before calling the core library.
    p1 = constants.bar_to_pa(P1_BAR)
    p2 = constants.bar_to_pa(P2_BAR)
    if p2 >= p1:
        raise ValueError("P2 must be below P1 for a meaningful expansion.")

    # Ask the library for the ideal exhaust velocity, then unpack the textbook equation
    # manually so the report shows which term is doing the real work.
    r = nozzle_theory.specific_gas_constant(MM)
    v2 = nozzle_theory.ideal_exhaust_velocity(GAMMA, T1, MM, p1, p2)
    gamma_group = 2.0 * GAMMA / (GAMMA - 1.0)
    thermal_group = r * T1
    pressure_group = 1.0 - (p2 / p1) ** ((GAMMA - 1.0) / GAMMA)
    v_max = math.sqrt(gamma_group * thermal_group)

    print(heading("EQUATION 3-15 TERM BY TERM"))
    print(line("Specific gas constant R = R'/M", r, "J/(kg.K)", ".2f"))
    print(line("Gamma group 2g/(g-1)", gamma_group, "", ".4f"))
    print(line("Thermal group R*T1", thermal_group, "J/kg", ".0f"))
    print(line("Pressure ratio p1/p2", p1 / p2, "", ".2f"))
    print(line("Pressure group [1-(p2/p1)^((g-1)/g)]", pressure_group, "", ".5f"))
    print()

    print(heading("RESULT"))
    print(line("Ideal exhaust velocity v2", v2, "m/s", ".1f"))
    print(line("Equivalent specific impulse", performance.specific_impulse(v2), "s", ".2f"))
    print("  (that Isp assumes perfect expansion, so p2 = p3 and c = v2)")
    print()

    print(heading("HOW CLOSE TO THE CEILING"))
    print(line("Maximum possible v2 (p2 -> 0)", v_max, "m/s", ".1f"))
    print(line("Fraction of ceiling achieved", 100.0 * v2 / v_max, "%", ".2f"))
    print(line("Velocity still on the table", v_max - v2, "m/s", ".1f"))
    print()

    print(heading("SENSITIVITY"))
    print(line("+10% chamber temperature", nozzle_theory.ideal_exhaust_velocity(GAMMA, T1 * 1.1, MM, p1, p2) - v2, "m/s", ".1f"))
    print(line("-10% molecular mass", nozzle_theory.ideal_exhaust_velocity(GAMMA, T1, MM * 0.9, p1, p2) - v2, "m/s", ".1f"))
    print(line("+10% chamber pressure", nozzle_theory.ideal_exhaust_velocity(GAMMA, T1, MM, p1 * 1.1, p2) - v2, "m/s", ".1f"))
    print(line("-10% exit pressure", nozzle_theory.ideal_exhaust_velocity(GAMMA, T1, MM, p1, p2 * 0.9) - v2, "m/s", ".1f"))
    print()
    print("  Compare the temperature and molecular-mass rows: that is the sqrt(T/M) term")
    print("  revealing itself. Chamber pressure matters much less than people expect.")


if __name__ == "__main__":
    main()
