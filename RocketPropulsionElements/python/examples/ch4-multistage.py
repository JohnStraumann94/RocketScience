"""Sample: Multi-Stage Vehicle Analysis
Chapter: Chapter 4
Demonstrates: Stage-by-stage mass bookkeeping, total delta-v, and comparison against an equivalent single stage.
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
MP1 = 130_000.0  # kg - stage 1 propellant mass
MS1 = 12_000.0   # kg - stage 1 structural mass
ISP1 = 300.0     # s - stage 1 specific impulse
MP2 = 30_000.0   # kg - stage 2 propellant mass
MS2 = 3_000.0    # kg - stage 2 structural mass
ISP2 = 340.0     # s - stage 2 specific impulse
MP3 = 6_000.0    # kg - stage 3 propellant mass
MS3 = 800.0      # kg - stage 3 structural mass
ISP3 = 450.0     # s - stage 3 specific impulse
PAYLOAD = 4_000.0  # kg - delivered payload mass


def main() -> None:
    stages_list = [
        staging.Stage(name="Stage 1", propellant_mass=MP1, structural_mass=MS1, specific_impulse=ISP1),
        staging.Stage(name="Stage 2", propellant_mass=MP2, structural_mass=MS2, specific_impulse=ISP2),
        staging.Stage(name="Stage 3", propellant_mass=MP3, structural_mass=MS3, specific_impulse=ISP3),
    ]
    result = staging.analyse_vehicle(stages_list, PAYLOAD)

    print(str(result))
    print()
    print(heading("PER-STAGE STRUCTURAL COEFFICIENTS"))
    for stage in stages_list:
        print(line(f"{stage.name} epsilon", stage.structural_coefficient, "", ".4f"))
    print()

    total_propellant = sum(stage.propellant_mass for stage in stages_list)
    total_structure = sum(stage.structural_mass for stage in stages_list)
    average_isp = sum(stage.specific_impulse * stage.propellant_mass for stage in stages_list) / total_propellant
    average_c = performance.exhaust_velocity_from_isp(average_isp)
    single_stage_m0 = total_propellant + total_structure + PAYLOAD
    single_stage_mf = total_structure + PAYLOAD
    single_stage_delta_v = rocket_equation.delta_v(average_c, single_stage_m0, single_stage_mf)

    print(heading("COMPARISON AGAINST A SINGLE STAGE"))
    print(line("Total propellant", total_propellant, "kg", ".1f"))
    print(line("Total structure", total_structure, "kg", ".1f"))
    print(line("Propellant-weighted average Isp", average_isp, "s", ".2f"))
    print(line("Single-stage delta-v with same mass", single_stage_delta_v, "m/s", ".2f"))
    print(line("Staged delta-v", result.total_delta_v, "m/s", ".2f"))
    print(line("Delta-v gained by staging", result.total_delta_v - single_stage_delta_v, "m/s", ".2f"))
    print(line("Improvement", 100.0 * (result.total_delta_v / single_stage_delta_v - 1.0), "%", ".2f"))
    print()

    print(heading("MISSION ASSESSMENT"))
    leo_requirement = 9400.0
    print(line("Typical LEO requirement", leo_requirement, "m/s", ".0f"))
    print(line("Margin", result.total_delta_v - leo_requirement, "m/s", ".2f"))
    print("  This vehicle reaches low Earth orbit with the stated payload." if result.total_delta_v >= leo_requirement else "  This vehicle does NOT reach low Earth orbit with the stated payload.")


if __name__ == "__main__":
    main()
