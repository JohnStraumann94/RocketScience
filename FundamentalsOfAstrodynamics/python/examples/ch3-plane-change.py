"""
ch3-plane-change.py — Sample: Plane changes, simple and combined.
=================================================================

Chapter 3 of Bate, Mueller & White, *Fundamentals of Astrodynamics*.

Demonstrates:
    Why changing inclination is the most expensive thing you can do in
    orbit, and how folding a plane change into another burn saves delta-v.

Physics background:
    Rotating an orbital plane without changing the orbit's size requires:
        Δv = 2 v sin(Δi / 2)
    because the before and after velocity vectors form an isosceles triangle.
    The speed v appears as a direct multiplier, and that is devastating: in
    LEO (v ≈ 7.7 km/s) even a 30° plane change costs ≈4 km/s.

    There are two escapes:
    1. Do it where v is small (at apoapsis of a highly elliptical orbit).
    2. Fold the plane change into a burn you were going to make anyway,
       using the law of cosines:
           Δv = √(v_i² + v_f² − 2 v_i v_f cos(Δi))
    This is exactly what a GEO apogee kick motor does: circularise AND
    remove the launch inclination in a single burn.

Default values use the GEO transfer orbit apogee, where a 28.5° plane
change (Cape Canaveral latitude) is cheapest.
"""

# ---------------------------------------------------------------------------
# Path setup — allows running this script from its own directory.
# ---------------------------------------------------------------------------
import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import astro_core.angles as ang
from astro_core.maneuvers import Maneuvers

# ===========================================================================
# EDIT THESE VALUES AND RE-RUN
# ===========================================================================
VI_KM_S    = 1.6079   # km/s — speed before the burn (GEO transfer apogee speed)
VF_KM_S    = 3.0747   # km/s — speed after the burn  (GEO circular speed)
DI_DEG     = 28.5     # deg  — plane change angle     (Cape Canaveral latitude)
VLEO_KM_S  = 7.7258   # km/s — LEO speed for comparison
# ===========================================================================

def main() -> None:
    di_rad = ang.to_radians(DI_DEG)

    # Combined single burn (law of cosines).
    combined = Maneuvers.combined_maneuver(VI_KM_S, VF_KM_S, di_rad)

    # Separate burns: speed change then plane change.
    speed_only = abs(VF_KM_S - VI_KM_S)
    plane_only  = Maneuvers.simple_plane_change(VF_KM_S, di_rad)
    separate    = speed_only + plane_only

    print(f"Plane change angle        = {DI_DEG:12.4f} deg")
    print()
    print("SEPARATE BURNS")
    print(f"  speed change only       = {speed_only:12.6f} km/s")
    print(f"  plane change only       = {plane_only:12.6f} km/s  (at v = {VF_KM_S:.4f} km/s)")
    print(f"  TOTAL                   = {separate:12.6f} km/s")
    print()
    print("COMBINED SINGLE BURN")
    print(f"  TOTAL                   = {combined:12.6f} km/s")
    saving = separate - combined
    print(f"  saving                  = {saving:12.6f} km/s ({saving / separate * 100.0:.2f}%)")
    print()
    print("WHERE YOU DO IT MATTERS ENORMOUSLY")
    leo_plane = Maneuvers.simple_plane_change(VLEO_KM_S, di_rad)
    apo_plane = Maneuvers.simple_plane_change(VI_KM_S, di_rad)
    print(f"  same plane change in LEO (v = {VLEO_KM_S:.4f} km/s)")
    print(f"                            = {leo_plane:12.6f} km/s")
    print(f"  same plane change at apogee (v = {VI_KM_S:.4f} km/s)")
    print(f"                            = {apo_plane:12.6f} km/s")
    print()
    print(f"  ... a factor of {leo_plane / apo_plane:.2f} difference for exactly the same geometric result.")


if __name__ == "__main__":
    main()
