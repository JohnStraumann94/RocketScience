"""Sample: Feed System Pressure Budget
Chapter: Chapter 6
Demonstrates: Working backward from chamber pressure to required tank pressure, and comparing pressure-fed versus pump-fed tank mass.
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
P1_BAR = 20.0        # bar - chamber pressure
DP_INJ_BAR = 4.0     # bar - injector pressure drop
DP_LINE_BAR = 3.0    # bar - line and valve pressure drop
DP_DYN_BAR = 1.0     # bar - dynamic allowance
TANK_VOLUME = 12.0   # m^3 - total tank volume
TANK_RADIUS = 1.2    # m - tank radius
SIGMA_MPA = 350.0    # MPa - allowable wall stress
RHO_MAT = 2800.0     # kg/m^3 - tank material density


def main() -> None:
    p1 = constants.bar_to_pa(P1_BAR)
    injector_drop = constants.bar_to_pa(DP_INJ_BAR)
    line_drop = constants.bar_to_pa(DP_LINE_BAR)
    dynamic_drop = constants.bar_to_pa(DP_DYN_BAR)
    tank_pressure = liquid_engine.required_tank_pressure(p1, injector_drop, line_drop, dynamic_drop)

    cylinder_length = max(0.0, (TANK_VOLUME - 4.0 / 3.0 * math.pi * TANK_RADIUS ** 3) / (math.pi * TANK_RADIUS ** 2))
    surface_area = 2.0 * math.pi * TANK_RADIUS * cylinder_length + 4.0 * math.pi * TANK_RADIUS ** 2
    wall_thickness = tank_pressure * TANK_RADIUS / (SIGMA_MPA * 1.0e6)
    tank_mass = surface_area * wall_thickness * RHO_MAT

    pump_tank_pressure = 3.0e5
    pump_wall_thickness = pump_tank_pressure * TANK_RADIUS / (SIGMA_MPA * 1.0e6)
    pump_tank_mass = surface_area * pump_wall_thickness * RHO_MAT

    print(heading("PRESSURE BUDGET"))
    print(line("Chamber pressure", constants.pa_to_bar(p1), "bar", ".3f"))
    print(line("+ Injector drop", constants.pa_to_bar(injector_drop), "bar", ".3f"))
    print(line("+ Line / valve drop", constants.pa_to_bar(line_drop), "bar", ".3f"))
    print(line("+ Dynamic allowance", constants.pa_to_bar(dynamic_drop), "bar", ".3f"))
    print(line("= Required source pressure", constants.pa_to_bar(tank_pressure), "bar", ".3f"))
    print(line("= Required source pressure", constants.pa_to_psi(tank_pressure), "psi", ".1f"))
    print()

    print(heading("OPTION A - PRESSURE-FED"))
    print(line("Tank volume", TANK_VOLUME, "m^3", ".3f"))
    print(line("Tank radius", TANK_RADIUS, "m", ".3f"))
    print(line("Cylindrical section length", cylinder_length, "m", ".3f"))
    print(line("Tank surface area", surface_area, "m^2", ".3f"))
    print(line("Required wall thickness", wall_thickness * 1000.0, "mm", ".3f"))
    print(line("Tank structural mass", tank_mass, "kg", ".1f"))
    print()

    print(heading("OPTION B - PUMP-FED"))
    print(line("Tank pressure needed (NPSH only)", constants.pa_to_bar(pump_tank_pressure), "bar", ".3f"))
    print(line("Pressure rise the pump must supply", constants.pa_to_bar(tank_pressure - pump_tank_pressure), "bar", ".3f"))
    print(line("Required wall thickness", pump_wall_thickness * 1000.0, "mm", ".3f"))
    print(line("Tank structural mass", pump_tank_mass, "kg", ".1f"))
    print(line("Tank mass saved", tank_mass - pump_tank_mass, "kg", ".1f"))
    print(line("Tank mass saving", 100.0 * (1.0 - pump_tank_mass / tank_mass), "%", ".2f"))
    print()

    print(heading("VERDICT"))
    if P1_BAR < 15.0:
        print("  At this low chamber pressure a pressure-fed system is probably the right answer.")
    elif P1_BAR < 30.0:
        print("  This is the cross-over region: either architecture can be defensible.")
    else:
        print("  At this chamber pressure the tank penalty of pressure feed becomes prohibitive.")


if __name__ == "__main__":
    main()
