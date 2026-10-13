"""Sample: Electric Mission Optimisation
Chapter: Chapter 19
Demonstrates: The optimum exhaust velocity for a power-limited mission, coupled mass breakdown, and a brute-force Isp sweep.
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
PAYLOAD = 500.0     # kg - useful spacecraft payload mass
DV = 6_000.0        # m/s - mission delta-v requirement
DAYS = 300.0        # days - available firing time
ALPHA = 120.0       # W/kg - power-system specific power
ETA = 0.60          # - thruster efficiency
FORCED_ISP = 0.0    # s - 0 means use the computed optimum


def main() -> None:
    burn_time = DAYS * 86400.0
    c_opt = electric_propulsion.optimum_exhaust_velocity(DV, burn_time, ALPHA, ETA)
    isp_opt = performance.specific_impulse(c_opt)
    c = performance.exhaust_velocity_from_isp(FORCED_ISP) if FORCED_ISP > 0.0 else c_opt
    isp = performance.specific_impulse(c)
    prop_mass, ppm, thrust, elec_power, initial_mass = electric_propulsion.mission_mass_breakdown(PAYLOAD, DV, c, burn_time, ALPHA, ETA)

    print(heading("MISSION"))
    print(line("Payload mass", PAYLOAD, "kg", ".2f"))
    print(line("Mission delta-v", DV, "m/s", ".1f"))
    print(line("Available firing time", DAYS, "days", ".1f"))
    print(line("Power plant specific power", ALPHA, "W/kg", ".2f"))
    print(line("Thruster efficiency", ETA, "", ".2f"))
    print()

    print(heading("OPTIMUM EXHAUST VELOCITY"))
    print(line("c_opt = sqrt(2*alpha*eta*t)", c_opt, "m/s", ".2f"))
    print(line("Optimum specific impulse", isp_opt, "s", ".2f"))
    print(line("Specific impulse in use", isp, "s", ".2f"))
    print("  (using the computed optimum)" if FORCED_ISP <= 0.0 else "  (you forced this value for comparison against the optimum)")
    print()

    print(heading("MASS BREAKDOWN"))
    print(line("Payload", PAYLOAD, "kg", ".3f"))
    print(line("Propellant", prop_mass, "kg", ".3f"))
    print(line("Power plant", ppm, "kg", ".3f"))
    print(line("Initial mass", initial_mass, "kg", ".3f"))
    print(line("Payload fraction", PAYLOAD / initial_mass, "", ".3%"))
    print(line("Propellant fraction", prop_mass / initial_mass, "", ".3%"))
    print(line("Power plant fraction", ppm / initial_mass, "", ".3%"))
    print()

    print(heading("PROPULSION SYSTEM"))
    print(line("Required thrust", thrust * 1000.0, "mN", ".4f"))
    print(line("Required electrical power", elec_power, "W", ".2f"))
    print(line("Required electrical power", elec_power / 1000.0, "kW", ".4f"))
    print(line("Initial acceleration", thrust / initial_mass, "m/s^2", ".6f"))
    print(line("Mass flow rate", performance.mass_flow_rate(thrust, c) * 1.0e6, "mg/s", ".4f"))
    print()

    print(heading("ISP SWEEP"))
    print("    Isp(s)   Propellant(kg)   Power plant(kg)   Initial mass(kg)   Power(kW)")
    print("  " + "-" * 84)
    best_isp = None
    best_mass = float('inf')
    test_isp = max(300.0, isp_opt * 0.25)
    while test_isp <= isp_opt * 2.6 + 1e-9:
        test_c = performance.exhaust_velocity_from_isp(test_isp)
        sweep_prop, sweep_ppm, _, sweep_power, sweep_initial = electric_propulsion.mission_mass_breakdown(PAYLOAD, DV, test_c, burn_time, ALPHA, ETA)
        marker = ""
        if sweep_initial < best_mass:
            best_mass = sweep_initial
            best_isp = test_isp
            marker = " <-- best"
        print(f"  {test_isp:8.0f}   {sweep_prop:14.2f}   {sweep_ppm:15.2f}   {sweep_initial:16.2f}   {sweep_power / 1000.0:9.3f}{marker}")
        test_isp += isp_opt * 0.15
    print()
    print(line("Lowest initial mass found at Isp", best_isp, "s", ".1f"))
    print(line("Analytical optimum Isp", isp_opt, "s", ".1f"))


if __name__ == "__main__":
    main()
