"""Sample: Propellant Comparison
Chapter: Chapter 7
Demonstrates: Ranking representative propellant combinations by vacuum Isp, density impulse, and tank-volume consequences.
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


# =============================================================================
# EDIT THESE VALUES AND RE-RUN
# =============================================================================
P1_BAR = 100.0        # bar - chamber pressure for the comparison
EPS = 40.0            # - area ratio used for the comparison
PROP_MASS = 100_000.0 # kg - propellant mass used for tank sizing


def main() -> None:
    p1 = constants.bar_to_pa(P1_BAR)
    rows = []
    for combo in propellants.ALL:
        isp = combo.ideal_vacuum_isp(p1, EPS)
        bulk_density = combo.bulk_density
        density_impulse = combo.density_impulse(p1, EPS)
        ox_vol, fuel_vol = propellants.tank_volumes(PROP_MASS, combo)
        rows.append((combo, isp, bulk_density, density_impulse, ox_vol, fuel_vol))

    print(heading(f"AT {P1_BAR:.0f} bar CHAMBER PRESSURE, AREA RATIO {EPS:.0f}"))
    print("  Combination            O/F    T1(K)   M     Isp(s)  rho(kg/m3)  Id(s.kg/m3)")
    print("  " + "-" * 78)
    for combo, isp, bulk_density, density_impulse, _, _ in rows:
        print(f"  {combo.name:<20} {combo.optimum_mixture_ratio:5.2f} {combo.chamber_temperature:8.0f} {combo.molecular_mass:6.1f} {isp:8.1f} {bulk_density:10.0f} {density_impulse:12.0f}")
    print()

    print(heading("RANKED BY ISP"))
    for rank, row in enumerate(sorted(rows, key=lambda item: item[1], reverse=True), start=1):
        print(f"  {rank}. {row[0].name:<22} {row[1]:7.1f} s")
    print()

    print(heading("RANKED BY DENSITY IMPULSE"))
    for rank, row in enumerate(sorted(rows, key=lambda item: item[3], reverse=True), start=1):
        print(f"  {rank}. {row[0].name:<22} {row[3]:9.0f} s.kg/m^3")
    print()

    print(heading("TANK VOLUME"))
    print("  Combination            Ox vol(m3)  Fuel vol(m3)  Total(m3)  Sphere dia(m)")
    print("  " + "-" * 74)
    for combo, _, _, _, ox_vol, fuel_vol in rows:
        total = ox_vol + fuel_vol
        sphere_diameter = 2.0 * (3.0 * total / (4.0 * math.pi)) ** (1.0 / 3.0)
        print(f"  {combo.name:<20} {ox_vol:11.2f} {fuel_vol:13.2f} {total:10.2f} {sphere_diameter:14.2f}")
    print()

    print(heading("NOTES"))
    for combo, *_ in rows:
        print(f"  {combo.name}:")
        print(f"      {combo.oxidizer} / {combo.fuel}")
        print(f"      {combo.notes}")


if __name__ == "__main__":
    main()
