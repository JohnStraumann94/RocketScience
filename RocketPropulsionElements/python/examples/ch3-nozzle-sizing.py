"""Sample: Nozzle Sizing and Geometry
Chapter: Chapter 3
Demonstrates: Starting from a thrust requirement and turning it into throat, exit, chamber, and nozzle dimensions.
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
REQUIRED_THRUST_KN = 500.0  # kN - thrust requirement at the design ambient pressure
GAMMA = 1.22                # - ratio of specific heats
T1 = 3600.0                 # K - chamber temperature
MM = 21.5                   # kg/kmol - exhaust molecular mass
P1_BAR = 100.0              # bar - chamber pressure
EPS = 22.0                  # - nozzle area ratio A2/At
P3_BAR = 1.01325            # bar - design ambient pressure
L_STAR = 1.1                # m - chamber characteristic length
HALF_ANGLE_DEG = 15.0       # deg - reference conical half-angle


def main() -> None:
    required_thrust = REQUIRED_THRUST_KN * 1000.0
    p1 = constants.bar_to_pa(P1_BAR)
    p3 = constants.bar_to_pa(P3_BAR)

    # Design the engine scale from the thrust requirement, then derive the metal geometry.
    point = engine_analysis.design_for_thrust(GAMMA, T1, MM, p1, EPS, required_thrust, p3)
    throat_radius = point.throat_diameter / 2.0
    chamber_volume = nozzle_geometry.chamber_volume(L_STAR, point.throat_area)
    conical_length = nozzle_geometry.conical_nozzle_length(throat_radius, EPS, HALF_ANGLE_DEG)
    bell_length = nozzle_geometry.bell_nozzle_length(throat_radius, EPS, 0.8)
    divergence = nozzle_geometry.conical_divergence_factor(HALF_ANGLE_DEG)

    # Use a representative contraction ratio to infer one plausible chamber diameter.
    contraction_ratio = 3.0
    chamber_area = contraction_ratio * point.throat_area
    chamber_diameter = nozzle_geometry.diameter_from_area(chamber_area)
    chamber_length = chamber_volume / chamber_area

    print(heading("REQUIREMENT"))
    print(line("Required thrust", required_thrust / 1000.0, "kN", ".2f"))
    print(line("At ambient pressure", constants.pa_to_bar(p3), "bar", ".5f"))
    print(line("Delivered thrust (check)", point.thrust / 1000.0, "kN", ".2f"))
    print()

    print(heading("PERFORMANCE"))
    print(line("Thrust coefficient C_F", point.thrust_coefficient, "", ".5f"))
    print(line("Characteristic velocity c*", point.characteristic_velocity, "m/s", ".2f"))
    print(line("Specific impulse", point.specific_impulse, "s", ".2f"))
    print(line("Mass flow rate", point.mass_flow_rate, "kg/s", ".3f"))
    print(line("Expansion state", point.expansion.name))
    print()

    print(heading("THROAT AND EXIT"))
    print(line("Throat area At", point.throat_area * 1.0e4, "cm^2", ".3f"))
    print(line("Throat diameter", point.throat_diameter * 1000.0, "mm", ".2f"))
    print(line("Exit area A2", point.exit_area * 1.0e4, "cm^2", ".3f"))
    print(line("Exit diameter", point.exit_diameter * 1000.0, "mm", ".2f"))
    print()

    print(heading("COMBUSTION CHAMBER"))
    print(line("Characteristic length L*", L_STAR, "m", ".3f"))
    print(line("Chamber volume Vc", chamber_volume * 1.0e6, "cm^3", ".1f"))
    print(line("Assumed contraction ratio Ac/At", contraction_ratio, "", ".2f"))
    print(line("Chamber diameter", chamber_diameter * 1000.0, "mm", ".2f"))
    print(line("Chamber length (cylindrical equivalent)", chamber_length * 1000.0, "mm", ".2f"))
    print()

    print(heading("NOZZLE LENGTH"))
    print(line(f"Conical length at {HALF_ANGLE_DEG:.1f} deg", conical_length * 1000.0, "mm", ".2f"))
    print(line("Conical divergence factor lambda", divergence, "", ".5f"))
    print(line("Divergence velocity loss", 100.0 * (1.0 - divergence), "%", ".3f"))
    print(line("80% bell length", bell_length * 1000.0, "mm", ".2f"))
    print(line("Length saved by using a bell", (conical_length - bell_length) * 1000.0, "mm", ".2f"))
    print(line("Length saving", 100.0 * (1.0 - bell_length / conical_length), "%", ".2f"))
    print("  A bell contour buys almost the same expansion performance at a much shorter length.")


if __name__ == "__main__":
    main()
