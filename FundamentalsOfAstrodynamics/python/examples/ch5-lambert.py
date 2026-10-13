"""
ch5-lambert.py — Sample: Lambert's problem.
============================================

Chapter 5 of Bate, Mueller & White, *Fundamentals of Astrodynamics*.

Demonstrates:
    Given two position vectors and a time of flight, find the orbit and the
    velocities at each end — the targeting engine of spaceflight.

Physics background:
    Lambert's theorem (1761) states that the transfer time depends only on
    the semi-major axis, the chord between the two points, and the sum
    r₁ + r₂ — remarkably, nothing else about the arc's shape.

    The solver uses the universal-variable formulation with bisection on ψ.
    Bisection is slower than Newton's method but cannot diverge, which
    matters because the time-of-flight function has an awkward derivative
    near the parabolic boundary.

    Every interplanetary porkchop plot is thousands of these solves on a
    grid of departure and arrival dates.

    Set LONG_WAY = True to sweep the reflex angle instead (> 180°) —
    occasionally cheaper, and sometimes the only option when the short way
    would fly through the Earth.
"""

# ---------------------------------------------------------------------------
# Path setup — allows running this script from its own directory.
# ---------------------------------------------------------------------------
import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import astro_core.angles as ang
from astro_core.vector3 import Vector3
from astro_core.orbit_conversions import StateVector, OrbitConversions
from astro_core.lambert import Lambert, TransferDirection
from astro_core.universal_variables import UniversalVariables

# ===========================================================================
# EDIT THESE VALUES AND RE-RUN
# ===========================================================================
# Departure position [km]
R1X =   5000.0
R1Y =  10000.0
R1Z =   2100.0
# Arrival position [km]
R2X = -14600.0
R2Y =   2500.0
R2Z =   7000.0
# Time of flight [s]
TOF_SEC  = 3600.0
# Long way? (sweeps > 180°)
LONG_WAY = False
# ===========================================================================

def main() -> None:
    r1 = Vector3(R1X, R1Y, R1Z)
    r2 = Vector3(R2X, R2Y, R2Z)

    direction = TransferDirection.LONG_WAY if LONG_WAY else TransferDirection.SHORT_WAY
    solution = Lambert.solve(r1, r2, TOF_SEC, direction)

    # Swept angle for display.
    sweep_rad = Vector3.angle_between(r1, r2)
    if LONG_WAY:
        import math
        sweep_rad = 2 * math.pi - sweep_rad
    sweep_deg = ang.to_degrees(sweep_rad)

    print(f"Departure r1              = {r1.to_string(3)} km   |r1| = {r1.magnitude:.3f} km")
    print(f"Arrival   r2              = {r2.to_string(3)} km   |r2| = {r2.magnitude:.3f} km")
    print(f"Swept angle               = {sweep_deg:12.4f} deg  ({'long way' if LONG_WAY else 'short way'})")
    print(f"Time of flight            = {TOF_SEC:12.1f} s  ({TOF_SEC / 60.0:.3f} min)")
    print()
    print("SOLUTION")
    v1 = solution.departure_velocity_km_per_sec
    v2 = solution.arrival_velocity_km_per_sec
    print(f"  departure velocity v1   = {v1.to_string(6)} km/s")
    print(f"                    |v1|  = {v1.magnitude:12.6f} km/s")
    print(f"  arrival velocity   v2   = {v2.to_string(6)} km/s")
    print(f"                    |v2|  = {v2.magnitude:12.6f} km/s")
    print(f"  bisection iterations    = {solution.iterations:12d}")
    print()

    # Describe the transfer orbit.
    transfer = OrbitConversions.to_orbital_elements(StateVector(r1, v1))
    print("TRANSFER ORBIT")
    print(transfer)
    print()

    # Verify: propagate the solution forward and check we reach r2.
    propagated = UniversalVariables.propagate(StateVector(r1, v1), TOF_SEC)
    miss = (propagated.position_km - r2).magnitude
    print("VERIFICATION (propagating the solution forward by the time of flight)")
    print(f"  arrived at              = {propagated.position_km.to_string(3)} km")
    print(f"  target was              = {r2.to_string(3)} km")
    print(f"  miss distance           = {miss:12.4E} km")


if __name__ == "__main__":
    main()
