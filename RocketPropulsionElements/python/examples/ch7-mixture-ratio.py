"""Sample: Mixture Ratio, Flow Split and Tank Sizing
Chapter: Chapter 7
Demonstrates: Splitting total propellant flow into oxidiser and fuel streams, estimating masses and tank volumes, and exploring mixture-ratio sensitivity.
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
TOTAL_FLOW = 250.0     # kg/s - total propellant flow rate
MIXTURE_RATIO = 2.56   # - oxidiser-to-fuel mass ratio for LOX/RP-1
RHO_OX = 1141.0        # kg/m^3 - liquid oxygen density
RHO_FUEL = 810.0       # kg/m^3 - RP-1 density
BURN_TIME = 160.0      # s - engine burn time
P1_BAR = 70.0          # bar - chamber pressure used for representative Isp reporting


def main() -> None:
    ox_flow, fuel_flow = propellants.split_flow(TOTAL_FLOW, MIXTURE_RATIO)
    ox_mass = ox_flow * BURN_TIME
    fuel_mass = fuel_flow * BURN_TIME
    total_prop_mass = TOTAL_FLOW * BURN_TIME
    ox_vol, fuel_vol = propellants.tank_volumes(total_prop_mass, propellants.LOX_RP1)
    bulk_density = propellants.LOX_RP1.bulk_density
    isp_vacuum = propellants.LOX_RP1.ideal_vacuum_isp(constants.bar_to_pa(P1_BAR), 40.0)

    print(heading("FLOW SPLIT"))
    print(line("Total mass flow", TOTAL_FLOW, "kg/s", ".3f"))
    print(line("Mixture ratio O/F", MIXTURE_RATIO, "", ".3f"))
    print(line("Oxidiser flow", ox_flow, "kg/s", ".3f"))
    print(line("Fuel flow", fuel_flow, "kg/s", ".3f"))
    print(line("Check  ox / fuel", ox_flow / fuel_flow, "", ".4f"))
    print()

    print(heading("PROPELLANT MASSES"))
    print(line("Burn time", BURN_TIME, "s", ".2f"))
    print(line("Oxidiser mass", ox_mass, "kg", ".1f"))
    print(line("Fuel mass", fuel_mass, "kg", ".1f"))
    print(line("Total propellant mass", total_prop_mass, "kg", ".1f"))
    print()

    print(heading("TANK VOLUMES"))
    print(line("Representative LOX/RP-1 bulk density", bulk_density, "kg/m^3", ".2f"))
    print(line("Oxidiser tank volume", ox_vol, "m^3", ".3f"))
    print(line("Fuel tank volume", fuel_vol, "m^3", ".3f"))
    print(line("Total tank volume", ox_vol + fuel_vol, "m^3", ".3f"))
    print(line("Representative vacuum Isp", isp_vacuum, "s", ".2f"))
    print()

    print(heading("MIXTURE RATIO SWEEP"))
    print("    MR      Ox flow     Fuel flow    Isp(s)")
    print("  " + "-" * 44)
    sweep = 1.5
    while sweep <= 4.5 + 1e-9:
        sweep_ox, sweep_fuel = propellants.split_flow(TOTAL_FLOW, sweep)
        # The lightweight catalogue exposes one representative LOX/RP-1 performance point,
        # not a full chemistry solver over mixture ratio, so the Isp column is anchored to
        # that catalogue value while the flow split varies exactly with MR.
        print(f"  {sweep:5.2f}   {sweep_ox:9.3f}   {sweep_fuel:11.3f}   {isp_vacuum:6.2f}")
        sweep += 0.5


if __name__ == "__main__":
    main()
