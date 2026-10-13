"""Sample: Regenerative Cooling Heat Balance
Chapter: Chapter 9
Demonstrates: Converting gas-side heat transfer into wall heat load and coolant temperature rise, plus a wall-temperature trade study.
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
H_COEFF = 50_000.0    # W/(m^2*K) - gas-side heat-transfer coefficient
SURFACE_AREA = 0.15   # m^2 - cooled wall area
T_GAS = 3000.0        # K - gas-side driving temperature
T_WALL = 800.0        # K - wall temperature
COOLANT_FLOW = 15.0   # kg/s - coolant mass flow rate
CP_COOLANT = 3500.0   # J/(kg*K) - coolant specific heat


def main() -> None:
    heat_rate = liquid_engine.convective_heat_transfer(H_COEFF, SURFACE_AREA, T_GAS, T_WALL)
    delta_t = liquid_engine.coolant_temperature_rise(heat_rate, COOLANT_FLOW, CP_COOLANT)
    heat_flux = heat_rate / SURFACE_AREA
    sun_surface_flux = 63.0e6

    print(heading("HEAT INPUT"))
    print(line("Heat rate", heat_rate, "W", ".2f"))
    print(line("Heat rate", heat_rate / 1.0e6, "MW", ".4f"))
    print(line("Heat flux", heat_flux, "W/m^2", ".2f"))
    print(line("Heat flux", heat_flux / 1.0e6, "MW/m^2", ".4f"))
    print(line("Fraction of 'surface of the Sun' flux", heat_flux / sun_surface_flux, "", ".4f"))
    print()

    print(heading("COOLANT"))
    print(line("Coolant mass flow", COOLANT_FLOW, "kg/s", ".3f"))
    print(line("Coolant specific heat", CP_COOLANT, "J/(kg*K)", ".1f"))
    print(line("Temperature rise", delta_t, "K", ".3f"))
    print("  A small rise means the coolant has comfortable thermal capacity margin.")
    print("  A large rise warns that the flow, coolant choice, or wall temperature assumption must change.")
    print()

    print(heading("TRADE STUDY"))
    print("  Wall T(K)   Heat rate(MW)   Coolant rise(K)")
    print("  " + "-" * 46)
    wall = 500.0
    while wall <= 1500.0 + 1e-9:
        trade_heat = liquid_engine.convective_heat_transfer(H_COEFF, SURFACE_AREA, T_GAS, wall)
        trade_delta = liquid_engine.coolant_temperature_rise(trade_heat, COOLANT_FLOW, CP_COOLANT)
        print(f"  {wall:8.0f}   {trade_heat / 1.0e6:12.4f}   {trade_delta:15.4f}")
        wall += 100.0


if __name__ == "__main__":
    main()
