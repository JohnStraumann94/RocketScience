"""
ch1-energy.py — Sample: Vis-viva and orbital energy.
=====================================================

Chapter 1 of Bate, Mueller & White, *Fundamentals of Astrodynamics*.

Demonstrates:
    The vis-viva equation, specific mechanical energy, orbital period,
    mean motion, escape speed, and orbit classification (ellipse /
    parabola / hyperbola) for any point on any orbit.

Physics background:
    The two-body equation of motion has two conserved quantities:
      * h = r × v  (specific angular momentum)
      * ε = v²/2 − μ/r  (specific mechanical energy)
    Because energy is constant, the speed at any radius depends only on that
    radius and on the size of the orbit — the vis-viva equation:
        v = √(μ (2/r − 1/a))
    The sign of ε classifies the orbit: ε < 0 ⟹ bound, ε = 0 ⟹ parabolic,
    ε > 0 ⟹ hyperbolic.
"""

# ---------------------------------------------------------------------------
# Path setup — allows running this script from its own directory.
# The astro_core package lives one level up (in python\).
# ---------------------------------------------------------------------------
import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import math
import astro_core.constants as C
import astro_core.two_body as tb

# ===========================================================================
# EDIT THESE VALUES AND RE-RUN
# ===========================================================================
ALTITUDE_KM      = 400.0      # km — altitude above Earth's surface
SEMI_MAJOR_AXIS_KM = 6778.137  # km — set equal to radius for a circular orbit
# ===========================================================================

def main() -> None:
    # Geocentric radius: Earth's equatorial radius plus the altitude.
    radius_km = C.EARTH_RADIUS_KM + ALTITUDE_KM
    sma = SEMI_MAJOR_AXIS_KM

    # Compute speed from the vis-viva equation.
    speed = tb.vis_viva(radius_km, sma)

    # Derived quantities.
    energy  = tb.specific_energy(radius_km, speed)
    circular = tb.circular_speed(radius_km)
    escape  = tb.escape_speed(radius_km)

    print(f"Radius r                  = {radius_km:12.3f} km")
    print(f"Semi-major axis a         = {sma:12.3f} km")
    print()
    print(f"Speed (vis-viva)          = {speed:12.6f} km/s")
    print(f"Circular speed at r       = {circular:12.6f} km/s")
    print(f"Escape speed at r         = {escape:12.6f} km/s")
    print()
    print(f"Specific energy eps       = {energy:12.6f} km^2/s^2")

    if energy < 0.0:
        # Bound orbit: period and mean motion are defined.
        T = tb.period(sma)
        n = tb.mean_motion(sma)
        print("Orbit is BOUND (elliptical or circular).")
        print(f"Period T                  = {T:12.2f} s  ({T / 60.0:.2f} min)")
        print(f"Mean motion n             = {n:12.6E} rad/s")
    elif abs(energy) < 1e-9:
        print("Orbit is PARABOLIC - exactly at escape speed.")
    else:
        v_inf = math.sqrt(2.0 * energy)
        print("Orbit is HYPERBOLIC - the vehicle escapes with energy to spare.")
        print(f"v_infinity                = {v_inf:12.6f} km/s")


if __name__ == "__main__":
    main()
