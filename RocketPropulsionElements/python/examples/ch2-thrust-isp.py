"""Sample: Thrust, Exhaust Velocity and Specific Impulse
Chapter: Chapter 2
Demonstrates: The two-term thrust equation, effective exhaust velocity, and specific impulse.
"""

import math
import os
import sys

# Add the parent directory (python\) to the module search path so that
# 'import propulsion_core' works when running this script directly from
# its own directory or from anywhere else.
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

from propulsion_core import constants, performance, nozzle_theory, nozzle_geometry, nozzle_losses, engine_analysis, rocket_equation, staging, flight_performance, gravity_turn, thermochemistry, propellants, liquid_engine, solid_motor, electric_propulsion

LABEL_WIDTH = 36


def heading(title: str) -> str:
    """Return a simple section heading that reads well in a terminal."""
    return f"{title}\n{'-' * len(title)}"


def line(label: str, value, unit: str = "", fmt_spec: str = "") -> str:
    """Render one aligned report line.

    The helper keeps all example scripts readable and makes the printed output
    line up in the same engineering-report style as the C# samples.
    """
    if isinstance(value, str):
        rendered = value
    else:
        rendered = format(value, fmt_spec) if fmt_spec else str(value)
    suffix = f" {unit}" if unit else ""
    return f"{label:<{LABEL_WIDTH}} = {rendered}{suffix}"


# =============================================================================
# EDIT THESE VALUES AND RE-RUN
# =============================================================================
MDOT = 250.0   # kg/s - propellant mass flow rate
V2 = 2800.0    # m/s - nozzle exit velocity
P2 = 70000.0   # Pa - nozzle exit pressure
A2 = 1.5       # m^2 - nozzle exit area
P3 = 101325.0  # Pa - ambient pressure (sea level)


def main() -> None:
    # Break the thrust equation into its two physical contributions first.
    # Seeing these terms separately is the entire teaching value of the sample.
    momentum_thrust = MDOT * V2
    pressure_thrust = (P2 - P3) * A2

    # Now ask the propulsion_core library for the full textbook thrust result,
    # then collapse that thrust back into effective exhaust velocity and Isp.
    thrust = performance.thrust(MDOT, V2, P2, P3, A2)
    c = performance.effective_exhaust_velocity(thrust, MDOT)
    isp = performance.specific_impulse(c)

    # Re-run the identical engine in vacuum so the only changed quantity is the
    # atmosphere acting on the nozzle exit plane.
    vacuum_thrust = performance.thrust(MDOT, V2, P2, 0.0, A2)
    vacuum_isp = performance.specific_impulse(
        performance.effective_exhaust_velocity(vacuum_thrust, MDOT)
    )

    print(heading("THRUST BREAKDOWN"))
    print(line("Momentum thrust  m_dot * v2", momentum_thrust / 1000.0, "kN", ".3f"))
    print(line("Pressure thrust  (p2-p3) * A2", pressure_thrust / 1000.0, "kN", ".3f"))
    print(line("Total thrust F", thrust / 1000.0, "kN", ".3f"))
    print(line("Total thrust F", constants.newtons_to_pounds_force(thrust), "lbf", ".0f"))
    print()
    print(line("Pressure term as share of total", 100.0 * pressure_thrust / thrust, "%", ".2f"))
    print(line(
        "Pressure term sign",
        "POSITIVE (under-expanded)" if pressure_thrust > 0.0 else "NEGATIVE (over-expanded)" if pressure_thrust < 0.0 else "ZERO (perfectly expanded)",
    ))
    print()
    print(heading("DERIVED PERFORMANCE"))
    print(line("Exit velocity v2", V2, "m/s", ".1f"))
    print(line("Effective exhaust velocity c", c, "m/s", ".1f"))
    print(line("Specific impulse Isp", isp, "s", ".2f"))
    print()
    print(heading("THE SAME ENGINE IN VACUUM"))
    print(line("Vacuum thrust", vacuum_thrust / 1000.0, "kN", ".3f"))
    print(line("Vacuum Isp", vacuum_isp, "s", ".2f"))
    print(line("Thrust gained by leaving the atmosphere", (vacuum_thrust - thrust) / 1000.0, "kN", ".3f"))
    print()
    print("  The gain is exactly p3 * A2 - ambient pressure acting on the exit plane.")
    print(line("  Check: p3 * A2", P3 * A2 / 1000.0, "kN", ".3f"))


if __name__ == "__main__":
    main()
