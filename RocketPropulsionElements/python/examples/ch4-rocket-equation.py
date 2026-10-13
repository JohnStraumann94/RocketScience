"""Sample: The Rocket Equation
Chapter: Chapter 4
Demonstrates: The forward delta-v calculation, sensitivity to mass ratio and Isp, and the inverse propellant-sizing problem.
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
ISP = 350.0       # s - engine specific impulse
M0 = 50_000.0     # kg - initial mass
MF = 8_000.0      # kg - final mass
DV_REQ = 9_500.0  # m/s - required mission delta-v for the inverse problem


def main() -> None:
    if MF >= M0:
        raise ValueError("MF must be less than M0.")

    c = performance.exhaust_velocity_from_isp(ISP)
    mass_ratio = performance.mass_ratio(M0, MF)
    inverse_mass_ratio = 1.0 / mass_ratio
    delta_v = rocket_equation.delta_v(c, M0, MF)
    required_mr = rocket_equation.required_mass_ratio(DV_REQ, c)
    required_prop = rocket_equation.required_propellant_mass(DV_REQ, c, MF)

    print(heading("FORWARD PROBLEM"))
    print(line("Specific impulse", ISP, "s", ".2f"))
    print(line("Effective exhaust velocity c", c, "m/s", ".2f"))
    print(line("Initial mass m0", M0, "kg", ".1f"))
    print(line("Final mass mf", MF, "kg", ".1f"))
    print(line("Propellant carried", M0 - MF, "kg", ".1f"))
    print(line("Mass ratio MR = mf/m0 (Sutton)", mass_ratio, "", ".4f"))
    print(line("Reciprocal m0/mf", inverse_mass_ratio, "", ".4f"))
    print(line("ln(m0/mf)", math.log(inverse_mass_ratio), "", ".5f"))
    print(line("Ideal delta-v", delta_v, "m/s", ".2f"))
    print()

    print(heading("SENSITIVITY"))
    dv_isp_up = rocket_equation.delta_v_from_isp(ISP * 1.1, M0, MF)
    dv_dry_down = rocket_equation.delta_v(c, M0, MF * 0.9)
    doubled_mass_ratio_delta_v = rocket_equation.delta_v(c, M0, M0 / (2.0 * inverse_mass_ratio))
    print(line("+10% Isp buys", dv_isp_up - delta_v, "m/s", ".2f"))
    print(line("-10% dry mass buys", dv_dry_down - delta_v, "m/s", ".2f"))
    print(line("Doubling the mass ratio buys", doubled_mass_ratio_delta_v - delta_v, "m/s", ".2f"))
    print(line("  c * ln(2)", c * math.log(2.0), "m/s", ".2f"))
    print()

    print(heading("INVERSE PROBLEM"))
    print(line("Required delta-v", DV_REQ, "m/s", ".2f"))
    print(line("Required mass ratio m0/mf", required_mr, "", ".4f"))
    print(line("Required propellant (same dry mass)", required_prop, "kg", ".1f"))
    print(line("Required initial mass", MF + required_prop, "kg", ".1f"))
    print(line("Propellant you actually carry", M0 - MF, "kg", ".1f"))
    print(line("Shortfall or surplus", (M0 - MF) - required_prop, "kg", ".1f"))
    print("  This vehicle MEETS the requirement." if delta_v >= DV_REQ else "  This vehicle FALLS SHORT of the requirement.")
    print()

    print(heading("EXPONENTIAL COST OF MARGIN"))
    print("   Extra dv (m/s)   Extra propellant (kg)")
    print("  " + "-" * 44)
    for extra in (100.0, 250.0, 500.0, 1000.0, 2000.0):
        with_margin = rocket_equation.required_propellant_mass(DV_REQ + extra, c, MF)
        print(f"   {extra:12.0f}   {with_margin - required_prop:20.1f}")


if __name__ == "__main__":
    main()
