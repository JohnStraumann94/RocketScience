"""
ch8-gravity-assist.py — Sample: Gravity assist (planetary flyby).
=================================================================

Chapter 8 of Bate, Mueller & White, *Fundamentals of Astrodynamics*.

Demonstrates:
    Stealing momentum from a planet — turning angle and heliocentric
    velocity gain from a hyperbolic flyby.

Physics background:
    In the planet's own reference frame a flyby changes nothing but
    direction: the vehicle arrives with speed v_∞ and leaves with speed
    v_∞, rotated through a turning angle δ given by:
        sin(δ/2) = 1/e

    But the planet is itself moving around the Sun.  In the heliocentric
    frame that rotation adds vectorially to the planet's orbital velocity,
    and the spacecraft can gain up to 2 v_∞ sin(δ/2) of heliocentric
    speed — for free.

    The energy comes from the planet's orbital motion.  Jupiter lost an
    utterly immeasurable amount of orbital energy giving Voyager 2 about
    10 km/s.

    The eccentricity of the flyby hyperbola from periapsis and v_∞:
        e = 1 + r_p v_∞² / μ

    Closer periapsis ⟹ higher e ⟹ larger turning angle ⟹ more velocity
    gain, but you fight against atmospheric/radiation limits.

Default values: Jupiter flyby with v_∞ = 5.6 km/s.
"""

# ---------------------------------------------------------------------------
# Path setup — allows running this script from its own directory.
# ---------------------------------------------------------------------------
import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import math
import astro_core.angles as ang
import astro_core.constants as C
from astro_core.interplanetary import Interplanetary

# ===========================================================================
# EDIT THESE VALUES AND RE-RUN
# ===========================================================================
V_INF_KM_S   = 5.6          # km/s — hyperbolic excess speed relative to the planet
PERIAPSIS_KM = 100000.0     # km — radius of closest approach
PLANET_MU    = C.MU_JUPITER # km³/s² — Jupiter by default
# ===========================================================================

def main() -> None:
    v_inf = V_INF_KM_S
    rp    = PERIAPSIS_KM
    mu    = PLANET_MU

    # Eccentricity of the flyby hyperbola.
    e = 1.0 + rp * v_inf ** 2 / mu

    turning = Interplanetary.flyby_turning_angle(e)
    gain    = Interplanetary.gravity_assist_delta_v(v_inf, turning)
    v_peri  = math.sqrt(v_inf ** 2 + 2.0 * mu / rp)

    print("FLYBY HYPERBOLA")
    print(f"  v_infinity              = {v_inf:12.6f} km/s")
    print(f"  periapsis radius        = {rp:12.0f} km")
    print(f"  eccentricity e          = {e:12.6f}")
    print(f"  semi-major axis a       = {-mu / (v_inf ** 2):12.0f} km  (negative, as required)")
    print(f"  speed at periapsis      = {v_peri:12.6f} km/s")
    print()
    print("RESULT")
    print(f"  turning angle delta     = {ang.to_degrees(turning):12.4f} deg")
    print(f"  max heliocentric gain   = {gain:12.6f} km/s")
    print()
    print("  This is the magnitude of the change in the heliocentric velocity")
    print("  vector.  Whether it is a speed-up or a slow-down depends on which")
    print("  side of the planet you pass - trailing edge to gain, leading edge")
    print("  to lose (which is how missions to Mercury shed energy).")
    print()
    print("SENSITIVITY TO FLYBY ALTITUDE")
    print("    Periapsis (km)          e     Turning (deg)     Gain (km/s)")
    print("    ---------------------------------------------------------------")

    for factor in (0.25, 0.5, 1.0, 2.0, 4.0, 8.0):
        test_rp = rp * factor
        test_e  = 1.0 + test_rp * v_inf ** 2 / mu
        test_turn = Interplanetary.flyby_turning_angle(test_e)
        test_gain = Interplanetary.gravity_assist_delta_v(v_inf, test_turn)
        print(
            f"    {test_rp:13.0f}  {test_e:9.4f}  "
            f"{ang.to_degrees(test_turn):15.4f}  {test_gain:14.6f}"
        )

    print()
    print("  Closer is always better for turning - which is why flyby design is")
    print("  a fight between trajectory gain and atmospheric/radiation limits.")


if __name__ == "__main__":
    main()
