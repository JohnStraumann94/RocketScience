"""Sample: Electric Thruster Power Balance
Chapter: Chapter 19
Demonstrates: Jet power, electrical power, power-plant mass, long-duration propellant usage, and the high-Isp thrust trade.
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
    return f"{label:<40} = {rendered}{(' ' + unit) if unit else ''}"


# =============================================================================
# EDIT THESE VALUES AND RE-RUN
# =============================================================================
THRUST = 0.25   # N - thruster thrust
ISP = 1800.0    # s - specific impulse
ETA = 0.55      # - thruster efficiency
ALPHA = 120.0   # W/kg - power-system specific power
HOURS = 8000.0  # h - firing duration


def main() -> None:
    c = performance.exhaust_velocity_from_isp(ISP)
    jet_power = electric_propulsion.jet_power(THRUST, c)
    elec_power = electric_propulsion.required_electrical_power(THRUST, c, ETA)
    power_plant_mass = electric_propulsion.power_plant_mass(elec_power, ALPHA)
    mass_flow = performance.mass_flow_rate(THRUST, c)
    seconds = HOURS * 3600.0
    prop_used = mass_flow * seconds
    total_impulse = performance.total_impulse(THRUST, seconds)

    print(heading("THRUSTER"))
    print(line("Thrust", THRUST, "N", ".5f"))
    print(line("Thrust", THRUST * 1000.0, "mN", ".2f"))
    print(line("Specific impulse", ISP, "s", ".1f"))
    print(line("Exhaust velocity", c, "m/s", ".1f"))
    print(line("Mass flow rate", mass_flow * 1.0e6, "mg/s", ".3f"))
    print()

    print(heading("POWER BALANCE"))
    print(line("Jet power  0.5*F*c", jet_power, "W", ".2f"))
    print(line("Thruster efficiency", ETA, "", ".2f"))
    print(line("Required electrical power", elec_power, "W", ".2f"))
    print(line("Required electrical power", elec_power / 1000.0, "kW", ".4f"))
    print(line("Power dissipated as heat", elec_power - jet_power, "W", ".2f"))
    print()

    print(heading("POWER PLANT"))
    print(line("Specific power alpha", ALPHA, "W/kg", ".2f"))
    print(line("Power plant mass", power_plant_mass, "kg", ".3f"))
    print(line("Power plant mass per newton", power_plant_mass / THRUST, "kg/N", ".1f"))
    print()

    print(heading(f"OVER {HOURS:.0f} HOURS"))
    print(line("Firing duration", seconds / 86400.0, "days", ".2f"))
    print(line("Propellant consumed", prop_used, "kg", ".4f"))
    print(line("Total impulse", total_impulse, "N.s", ".1f"))
    print(line("Energy consumed", elec_power * seconds / 3.6e9, "MWh", ".4f"))
    print()

    print(heading("ISP-VS-THRUST TRADE"))
    print(f"  Holding electrical power at {elec_power / 1000.0:.3f} kW")
    print("    Isp(s)    c(km/s)   Thrust(mN)   m_dot(mg/s)")
    print("  " + "-" * 52)
    for test_isp in (800.0, 1200.0, 1600.0, 2000.0, 3000.0, 4000.0, 6000.0, 10000.0):
        test_c = performance.exhaust_velocity_from_isp(test_isp)
        test_thrust = electric_propulsion.thrust_from_power(elec_power, test_c, ETA)
        test_flow = performance.mass_flow_rate(test_thrust, test_c)
        print(f"  {test_isp:8.0f}  {test_c / 1000.0:9.2f}  {test_thrust * 1000.0:11.3f}  {test_flow * 1.0e6:12.4f}")
    print()

    print(heading("CHEMICAL COMPARISON"))
    print(line("This thruster", THRUST, "N", ".4f"))
    print(line("A satellite hydrazine thruster", 22.0, "N", ".1f"))
    print(line("An RL10 upper stage engine", 110000.0, "N", ".0f"))
    print(line("A Merlin 1D first stage engine", 845000.0, "N", ".0f"))


if __name__ == "__main__":
    main()
