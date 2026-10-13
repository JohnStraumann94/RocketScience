"""
ch3-phasing.py — Sample: Phasing manoeuvre (rendezvous).
==========================================================

Chapter 3 of Bate, Mueller & White, *Fundamentals of Astrodynamics*.

Demonstrates:
    The great paradox of orbital mechanics — to catch up with something
    ahead of you in the same orbit, slow down.

Physics background:
    Suppose a target is 30° ahead of you in the same circular orbit.
    Firing forwards to chase it is exactly wrong: thrusting prograde raises
    your orbit, lengthens your period, and you fall further behind.

    The correct answer is to brake.  That drops you into a smaller, faster-
    period orbit; after one lap you arrive back at the same point earlier
    than the target, having gained on it.  A second burn, equal and
    opposite, restores the original orbit — now in the right place.

    This is the fundamental manoeuvre of every rendezvous ever flown, from
    Gemini VI to routine ISS resupply.

    Period of the phasing orbit:
        T_phasing = T_circular − Δφ / (n · k)
    where Δφ is the phase to make up and k is the number of revolutions.
"""

# ---------------------------------------------------------------------------
# Path setup — allows running this script from its own directory.
# ---------------------------------------------------------------------------
import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import astro_core.two_body as tb
import astro_core.angles as ang
from astro_core.maneuvers import Maneuvers

# ===========================================================================
# EDIT THESE VALUES AND RE-RUN
# ===========================================================================
ORBIT_RADIUS_KM = 6778.137  # km  — circular orbit radius (≈ ISS orbit, 400 km alt)
PHASE_ANGLE_DEG = 30.0      # deg — phase angle to make up (positive = target ahead)
REVOLUTIONS     = 1         # integer — revolutions to spread the manoeuvre over
# ===========================================================================

def main() -> None:
    phase_rad = ang.to_radians(PHASE_ANGLE_DEG)
    revs = max(1, int(REVOLUTIONS))

    result = Maneuvers.phasing_maneuver(ORBIT_RADIUS_KM, phase_rad, revs)

    print(result.description)
    print()
    T = tb.period(ORBIT_RADIUS_KM)
    print(f"Original circular period  = {T / 60.0:12.4f} min")
    print(f"Original circular speed   = {tb.circular_speed(ORBIT_RADIUS_KM):12.6f} km/s")
    print()
    print(f"Burn 1 (enter phasing)    = {result.burns_km_per_sec[0]:12.6f} km/s")
    print(f"Burn 2 (restore orbit)    = {result.burns_km_per_sec[1]:12.6f} km/s")
    print(f"TOTAL delta-v             = {result.total_delta_v_km_per_sec:12.6f} km/s")
    print()
    print(f"Total manoeuvre time      = {result.transfer_time_hours:12.4f} hours")
    print()
    if result.burns_km_per_sec[0] < 0.0:
        print("The first burn is RETROGRADE: you slow down to catch up with a target ahead.")
    else:
        print("The first burn is PROGRADE: you speed up to drop back towards a target behind.")


if __name__ == "__main__":
    main()
