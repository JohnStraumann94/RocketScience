"""Sample: Solid Motor Equilibrium Pressure and Stability
Chapter: Chapter 11
Demonstrates: The n < 1 stability criterion, equilibrium chamber pressure, and sensitivity to burning area and pressure exponent.
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
AB = 2.5        # m^2 - burning surface area
AT_CM2 = 60.0   # cm^2 - throat area
RHO = 1770.0    # kg/m^3 - propellant density
CSTAR = 1550.0  # m/s - characteristic velocity
A = 5.0e-5      # SI - burn-rate coefficient in r = a * p^n
N = 0.35        # - pressure exponent


def main() -> None:
    at = AT_CM2 * 1.0e-4
    stable = solid_motor.is_stable(N)
    klemmung = solid_motor.klemmung(AB, at)
    pressure = solid_motor.equilibrium_chamber_pressure(AB, at, RHO, CSTAR, A, N)
    burn_rate = solid_motor.burn_rate(A, pressure, N)
    generated = AB * RHO * burn_rate
    discharged = pressure * at / CSTAR
    sensitivity_exponent = 1.0 / (1.0 - N)

    print(heading("STABILITY CHECK"))
    print(line("Pressure exponent n", N, "", ".4f"))
    print(line("Stable?", "YES" if stable else "NO"))
    print()

    print(heading("EQUILIBRIUM"))
    print(line("Burning surface area Ab", AB, "m^2", ".4f"))
    print(line("Throat area At", AT_CM2, "cm^2", ".3f"))
    print(line("Klemmung K = Ab / At", klemmung, "", ".2f"))
    print(line("Sensitivity exponent 1/(1-n)", sensitivity_exponent, "", ".4f"))
    print(line("Equilibrium pressure", constants.pa_to_bar(pressure), "bar", ".3f"))
    print(line("Burn rate at that pressure", burn_rate * 1000.0, "mm/s", ".4f"))
    print()

    print(heading("MASS BALANCE VERIFICATION"))
    print(line("Generated  Ab * rho * r", generated, "kg/s", ".5f"))
    print(line("Discharged  p * At / c*", discharged, "kg/s", ".5f"))
    print(line("Residual", abs(generated - discharged), "kg/s", ".3e"))
    print()

    print(heading("SENSITIVITY TO BURNING AREA"))
    print("   Ab factor   New Ab(m2)   Pressure(bar)   Pressure change")
    print("  " + "-" * 64)
    for factor in (0.90, 0.95, 1.00, 1.05, 1.10, 1.25, 1.50, 2.00):
        new_ab = AB * factor
        new_pressure = solid_motor.equilibrium_chamber_pressure(new_ab, at, RHO, CSTAR, A, N)
        print(f"  {factor:10.2f}   {new_ab:10.4f}   {constants.pa_to_bar(new_pressure):13.3f}   {new_pressure / pressure - 1.0:15.2%}")
    print()

    print(heading("SENSITIVITY TO n"))
    print("     n     1/(1-n)   Pressure(bar)   dP for +10% area")
    print("  " + "-" * 58)
    for test_n in (0.20, 0.35, 0.50, 0.65, 0.80, 0.90, 0.95, 0.98):
        p = solid_motor.equilibrium_chamber_pressure(AB, at, RHO, CSTAR, A, test_n)
        p_up = solid_motor.equilibrium_chamber_pressure(AB * 1.1, at, RHO, CSTAR, A, test_n)
        print(f"  {test_n:5.2f}   {1.0 / (1.0 - test_n):8.3f}   {constants.pa_to_bar(p):13.2f}   {p_up / p - 1.0:16.1%}")
    print()

    print(heading("KLEMMUNG SWEEP"))
    print("   At(cm2)   Klemmung K   Pressure(bar)   Burn rate(mm/s)")
    print("  " + "-" * 62)
    for factor in (0.6, 0.8, 1.0, 1.2, 1.5, 2.0):
        test_at = at * factor
        p = solid_motor.equilibrium_chamber_pressure(AB, test_at, RHO, CSTAR, A, N)
        print(f"  {test_at * 1.0e4:8.2f}   {solid_motor.klemmung(AB, test_at):10.2f}   {constants.pa_to_bar(p):13.3f}   {solid_motor.burn_rate(A, p, N) * 1000.0:16.4f}")
    print()

    print(heading("TEMPERATURE SENSITIVITY"))
    p_hot = pressure * 1.20
    p_cold = pressure * 0.85
    print(line("Pressure at +60 C", constants.pa_to_bar(p_hot), "bar", ".3f"))
    print(line("Pressure at -40 C", constants.pa_to_bar(p_cold), "bar", ".3f"))
    print(line("Sensitivity pi_K", 100.0 * solid_motor.temperature_sensitivity(p_hot, p_cold, 100.0), "%/K", ".4f"))


if __name__ == "__main__":
    main()
