"""Sample: Total Impulse and Mass Fractions
Chapter: Chapter 2
Demonstrates: Total impulse, implied Isp, propellant fraction, mass ratio, thrust-to-weight, and ideal delta-v.
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
    if isinstance(value, str):
        rendered = value
    else:
        rendered = format(value, fmt_spec) if fmt_spec else str(value)
    return f"{label:<{LABEL_WIDTH}} = {rendered}{(' ' + unit) if unit else ''}"


# =============================================================================
# EDIT THESE VALUES AND RE-RUN
# =============================================================================
THRUST = 1_000_000.0  # N - thrust (1000 kN)
BURN_TIME = 160.0     # s - burn duration
M0 = 100_000.0        # kg - initial mass
MP = 88_000.0         # kg - propellant mass


def main() -> None:
    # The vehicle must retain some non-propellant mass at burnout.
    mf = M0 - MP
    if mf <= 0.0:
        raise ValueError("MP must be less than M0 so the vehicle has a final mass.")

    # Total impulse is the area under the thrust-time curve for a constant-thrust burn.
    total_impulse = performance.total_impulse(THRUST, BURN_TIME)
    isp = performance.specific_impulse_from_total_impulse(total_impulse, MP)
    c = performance.exhaust_velocity_from_isp(isp)
    mass_flow = performance.mass_flow_rate(THRUST, c)

    # These are the standard bookkeeping ratios used throughout Sutton.
    zeta = performance.propellant_mass_fraction(MP, M0)
    mass_ratio = performance.mass_ratio(M0, mf)
    twr = performance.thrust_to_weight_ratio(THRUST, M0)
    burnout_twr = performance.thrust_to_weight_ratio(THRUST, mf)
    delta_v = rocket_equation.delta_v(c, M0, mf)

    print(heading("IMPULSE"))
    print(line("Total impulse It", total_impulse / 1.0e6, "MN.s", ".3f"))
    print(line("Implied specific impulse", isp, "s", ".2f"))
    print(line("Implied effective exhaust velocity", c, "m/s", ".1f"))
    print(line("Implied mass flow rate", mass_flow, "kg/s", ".2f"))
    print(line("Consistency check  m_dot * tb", mass_flow * BURN_TIME, "kg", ".1f"))
    print("  (that check should reproduce the propellant mass you entered)")
    print()

    print(heading("MASS BOOKKEEPING"))
    print(line("Initial mass m0", M0, "kg", ".1f"))
    print(line("Propellant mass mp", MP, "kg", ".1f"))
    print(line("Final (burnout) mass mf", mf, "kg", ".1f"))
    print(line("Propellant mass fraction zeta", zeta, "", ".4f"))
    print(line("Mass ratio MR = mf/m0", mass_ratio, "", ".4f"))
    print(line("Check  1 - zeta", 1.0 - zeta, "", ".4f"))
    print("  Sutton defines MR as FINAL over INITIAL, so it is less than one.")
    print(line("Reciprocal  m0/mf", 1.0 / mass_ratio, "", ".4f"))
    print()

    print(heading("ACCELERATION"))
    print(line("Thrust-to-weight at ignition", twr, "", ".3f"))
    print(line("Thrust-to-weight at burnout", burnout_twr, "", ".3f"))
    print(line("Initial acceleration", rocket_equation.acceleration(THRUST, M0), "m/s^2", ".3f"))
    print(line("Burnout acceleration", rocket_equation.acceleration(THRUST, mf), "m/s^2", ".3f"))
    print(line("Burnout acceleration in g", rocket_equation.acceleration(THRUST, mf) / constants.STANDARD_GRAVITY, "g", ".2f"))
    print()
    print("  T/W > 1: this stage can lift itself off the pad." if twr > 1.0 else "  T/W < 1: this stage cannot lift off and must be an upper stage or in-space burn.")
    print()

    print(heading("RESULTING CAPABILITY"))
    print(line("Ideal delta-v (no losses)", delta_v, "m/s", ".1f"))


if __name__ == "__main__":
    main()
