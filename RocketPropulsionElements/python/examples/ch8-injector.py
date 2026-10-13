"""Sample: Injector Orifice Sizing
Chapter: Chapter 8
Demonstrates: Sizing oxidiser and fuel injector circuits from the incompressible orifice equation.
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
TOTAL_FLOW = 250.0  # kg/s - total propellant flow rate
MIXTURE_RATIO = 2.3 # - oxidiser-to-fuel mass ratio
RHO_OX = 1141.0     # kg/m^3 - liquid oxygen density
RHO_FUEL = 810.0    # kg/m^3 - RP-1 density
P1_BAR = 70.0       # bar - chamber pressure
DROP_FRAC = 0.20    # - injector pressure drop as a fraction of chamber pressure
CD = 0.75           # - discharge coefficient
D_OX_MM = 1.5       # mm - oxidiser orifice diameter
D_FUEL_MM = 1.2     # mm - fuel orifice diameter


def main() -> None:
    p1 = constants.bar_to_pa(P1_BAR)
    delta_p = DROP_FRAC * p1
    ox_flow, fuel_flow = propellants.split_flow(TOTAL_FLOW, MIXTURE_RATIO)
    d_ox = D_OX_MM / 1000.0
    d_fuel = D_FUEL_MM / 1000.0
    ox_area = liquid_engine.required_orifice_area(ox_flow, CD, RHO_OX, delta_p)
    fuel_area = liquid_engine.required_orifice_area(fuel_flow, CD, RHO_FUEL, delta_p)
    ox_holes = liquid_engine.orifice_count(ox_area, d_ox)
    fuel_holes = liquid_engine.orifice_count(fuel_area, d_fuel)
    ox_check = liquid_engine.injector_mass_flow(CD, ox_area, RHO_OX, delta_p)
    fuel_check = liquid_engine.injector_mass_flow(CD, fuel_area, RHO_FUEL, delta_p)
    ox_vel = ox_flow / (RHO_OX * ox_area)
    fuel_vel = fuel_flow / (RHO_FUEL * fuel_area)

    print(heading("DESIGN POINT"))
    print(line("Chamber pressure", P1_BAR, "bar", ".3f"))
    print(line("Injector pressure drop", constants.pa_to_bar(delta_p), "bar", ".3f"))
    print(line("Drop as fraction of p1", DROP_FRAC, "", ".3f"))
    print(line("Discharge coefficient", CD, "", ".3f"))
    if DROP_FRAC < 0.10:
        print("  WARNING: this injector is hydraulically soft and instability risk is high.")
    elif DROP_FRAC < 0.15:
        print("  Marginal stiffness: expect limited decoupling from chamber oscillations.")
    elif DROP_FRAC <= 0.25:
        print("  Normal design range: a healthy stability-versus-pressure-loss compromise.")
    else:
        print("  Very stiff injector, but expensive in pump power or tank pressure.")
    print()

    print(heading("OXIDISER CIRCUIT"))
    print(line("Flow rate", ox_flow, "kg/s", ".3f"))
    print(line("Density", RHO_OX, "kg/m^3", ".1f"))
    print(line("Required total orifice area", ox_area * 1.0e6, "mm^2", ".3f"))
    print(line("Orifice diameter", D_OX_MM, "mm", ".3f"))
    print(line("Area of one orifice", nozzle_geometry.area_from_diameter(d_ox) * 1.0e6, "mm^2", ".4f"))
    print(line("Number of orifices", str(ox_holes)))
    print(line("Injection velocity", ox_vel, "m/s", ".2f"))
    print(line("Round-trip flow check", ox_check, "kg/s", ".4f"))
    print()

    print(heading("FUEL CIRCUIT"))
    print(line("Flow rate", fuel_flow, "kg/s", ".3f"))
    print(line("Density", RHO_FUEL, "kg/m^3", ".1f"))
    print(line("Required total orifice area", fuel_area * 1.0e6, "mm^2", ".3f"))
    print(line("Orifice diameter", D_FUEL_MM, "mm", ".3f"))
    print(line("Area of one orifice", nozzle_geometry.area_from_diameter(d_fuel) * 1.0e6, "mm^2", ".4f"))
    print(line("Number of orifices", str(fuel_holes)))
    print(line("Injection velocity", fuel_vel, "m/s", ".2f"))
    print(line("Round-trip flow check", fuel_check, "kg/s", ".4f"))
    print()

    print(heading("ELEMENT COUNT"))
    print(line("Total orifices", str(ox_holes + fuel_holes)))
    print(line("Momentum ratio (fuel / ox)", (fuel_flow * fuel_vel) / (ox_flow * ox_vel), "", ".4f"))
    print()

    print(heading("THROTTLING TRADE"))
    print("   Throttle   Required drop (bar)   Drop as % of full p1")
    print("  " + "-" * 60)
    for throttle in (1.0, 0.8, 0.6, 0.4, 0.3, 0.2):
        required_drop = delta_p * throttle * throttle
        print(f"  {throttle:8.0%}   {constants.pa_to_bar(required_drop):19.3f}   {required_drop / p1:20.2%}")


if __name__ == "__main__":
    main()
