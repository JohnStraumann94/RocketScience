"""Sample: Thermochemistry and Isentropic Relations
Chapter: Chapter 5
Demonstrates: Perfect-gas thermodynamic properties, throat conditions, stagnation/static relations, and a simple flame-temperature estimate.
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
GAMMA = 1.22   # - ratio of specific heats
MM = 21.5      # kg/kmol - exhaust molecular mass
T1 = 3600.0    # K - chamber stagnation temperature
P1_BAR = 100.0 # bar - chamber stagnation pressure
MACH = 3.5     # - Mach number of interest
Q = 5.5e6      # J/kg - heat of reaction for the simple flame-temperature estimate


def main() -> None:
    p1 = constants.bar_to_pa(P1_BAR)
    r = nozzle_theory.specific_gas_constant(MM)
    cp = thermochemistry.specific_heat_constant_pressure(GAMMA, r)
    cv = thermochemistry.specific_heat_constant_volume(GAMMA, r)
    throat_t = nozzle_theory.throat_temperature(GAMMA, T1)
    throat_p = nozzle_theory.throat_pressure(GAMMA, p1)
    throat_a = thermochemistry.speed_of_sound(GAMMA, r, throat_t)
    static_t = thermochemistry.static_temperature(GAMMA, T1, MACH)
    t_ratio = thermochemistry.stagnation_temperature_ratio(GAMMA, MACH)
    p_ratio = thermochemistry.stagnation_pressure_ratio(GAMMA, MACH)
    static_p = p1 / p_ratio
    local_a = thermochemistry.speed_of_sound(GAMMA, r, static_t)
    flame_t = thermochemistry.adiabatic_flame_temperature(Q, cp)

    print(heading("GAS PROPERTIES"))
    print(line("Universal gas constant R'", constants.UNIVERSAL_GAS_CONSTANT, "J/(kmol.K)", ".4f"))
    print(line("Molecular mass M", MM, "kg/kmol", ".3f"))
    print(line("Specific gas constant R = R'/M", r, "J/(kg.K)", ".3f"))
    print(line("cp", cp, "J/(kg.K)", ".3f"))
    print(line("cv", cv, "J/(kg.K)", ".3f"))
    print(line("Check  cp - cv", cp - cv, "J/(kg.K)", ".3f"))
    print(line("Check  cp / cv", cp / cv, "", ".6f"))
    print()

    print(heading("CHAMBER CONDITIONS"))
    print(line("Temperature T1", T1, "K", ".2f"))
    print(line("Pressure p1", constants.pa_to_bar(p1), "bar", ".3f"))
    print(line("Density", thermochemistry.density(p1, r, T1), "kg/m^3", ".5f"))
    print(line("Speed of sound", thermochemistry.speed_of_sound(GAMMA, r, T1), "m/s", ".2f"))
    print()

    print(heading("THROAT CONDITIONS"))
    print(line("Tt / T1", throat_t / T1, "", ".6f"))
    print(line("Check  2/(gamma+1)", 2.0 / (GAMMA + 1.0), "", ".6f"))
    print(line("Throat temperature Tt", throat_t, "K", ".2f"))
    print(line("pt / p1", throat_p / p1, "", ".6f"))
    print(line("Throat pressure pt", constants.pa_to_bar(throat_p), "bar", ".4f"))
    print(line("Throat velocity (= local sound speed)", throat_a, "m/s", ".2f"))
    print(line("Throat density", thermochemistry.density(throat_p, r, throat_t), "kg/m^3", ".5f"))
    print()

    print(heading(f"STATIC CONDITIONS AT MACH {MACH:.2f}"))
    print(line("T1 / T", t_ratio, "", ".5f"))
    print(line("Static temperature T", static_t, "K", ".2f"))
    print(line("p1 / p", p_ratio, "", ".3f"))
    print(line("Static pressure p", constants.pa_to_bar(static_p), "bar", ".6f"))
    print(line("Local speed of sound", local_a, "m/s", ".2f"))
    print(line("Gas velocity M * a", MACH * local_a, "m/s", ".2f"))
    print()

    print(heading("EXPANSION PROFILE"))
    print("   Mach    T(K)     p(bar)      a(m/s)    v(m/s)    A/At")
    print("  " + "-" * 62)
    for mach in (0.1, 0.3, 0.5, 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5):
        tm = thermochemistry.static_temperature(GAMMA, T1, mach)
        pm = p1 / thermochemistry.stagnation_pressure_ratio(GAMMA, mach)
        am = thermochemistry.speed_of_sound(GAMMA, r, tm)
        ar = nozzle_theory.area_ratio_from_mach(GAMMA, mach)
        print(f"  {mach:5.2f}  {tm:7.1f}  {constants.pa_to_bar(pm):9.4f}  {am:9.1f}  {mach * am:8.1f}  {ar:7.3f}")
    print()

    print(heading("ADIABATIC FLAME TEMPERATURE ESTIMATE"))
    print(line("Heat of reaction", Q / 1.0e6, "MJ/kg", ".3f"))
    print(line("Estimated flame temperature", flame_t, "K", ".1f"))
    print(line("Chamber temperature you entered", T1, "K", ".1f"))


if __name__ == "__main__":
    main()
