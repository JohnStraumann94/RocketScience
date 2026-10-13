"""
ch9-j2.py — Sample: J2 nodal regression and apsidal rotation.
=============================================================

Chapter 9 of Bate, Mueller & White, *Fundamentals of Astrodynamics*.

Demonstrates:
    How the Earth's equatorial bulge makes every orbit precess — the secular
    (steadily accumulating) drifts of the right ascension of the ascending
    node (RAAN) and the argument of perigee.

Physics background:
    The Earth is not a sphere: centrifugal flattening makes it about 21 km
    wider at the equator than at the poles.  That bulge exerts a torque on
    an inclined orbit, and the orbit responds like a gyroscope — it precesses.

    The effect is captured by J2 = 1.0826×10⁻³ and produces two secular drifts:

        dΩ/dt = −(3/2) n J2 (Rₑ/p)² cos(i)
        dω/dt =  (3/4) n J2 (Rₑ/p)² (5 cos²i − 1)

    Both are exploited rather than merely tolerated:
      * Set i so the node drifts at exactly 360°/year ⟹ sun-synchronous orbit.
      * Set cos²i = 1/5 (i.e. i = 63.435°) ⟹ perigee frozen (Molniya trick).

    Default orbit: a Molniya satellite at i = 63.4349° (critical inclination),
    e = 0.74, where apsidal rotation should be essentially zero.
"""

# ---------------------------------------------------------------------------
# Path setup — allows running this script from its own directory.
# ---------------------------------------------------------------------------
import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import astro_core.constants as C
import astro_core.two_body as tb
import astro_core.angles as ang
from astro_core.perturbations import Perturbations

# ===========================================================================
# EDIT THESE VALUES AND RE-RUN
# ===========================================================================
SEMI_MAJOR_AXIS_KM = 26600.0   # km — semi-major axis   (Molniya default)
ECCENTRICITY       = 0.74      # dimensionless
INCLINATION_DEG    = 63.4349   # deg  (try 51.6 for ISS, 98.6 for SSO)
TIME_SPAN_DAYS     = 365.0     # days — how far to project the drifts
# ===========================================================================

def main() -> None:
    sma  = SEMI_MAJOR_AXIS_KM
    ecc  = ECCENTRICITY
    inc  = ang.to_radians(INCLINATION_DEG)
    days = TIME_SPAN_DAYS

    node_rate  = Perturbations.nodal_regression_rate(sma, ecc, inc)
    apsis_rate = Perturbations.apsidal_rotation_rate(sma, ecc, inc)

    node_per_day  = ang.to_degrees(node_rate)  * C.SOLAR_DAY_SECONDS
    apsis_per_day = ang.to_degrees(apsis_rate) * C.SOLAR_DAY_SECONDS

    print("ORBIT")
    print(f"  semi-major axis a       = {sma:12.1f} km")
    print(f"  eccentricity e          = {ecc:12.6f}")
    print(f"  inclination i           = {INCLINATION_DEG:12.4f} deg")
    print(f"  perigee altitude        = {tb.periapsis_radius(sma, ecc) - C.EARTH_RADIUS_KM:12.1f} km")
    print(f"  apogee altitude         = {tb.apoapsis_radius(sma, ecc) - C.EARTH_RADIUS_KM:12.1f} km")
    print(f"  period                  = {tb.period(sma) / 3600.0:12.4f} hours")
    print()
    print("SECULAR J2 DRIFT RATES")
    print(f"  nodal regression        = {node_per_day:12.6f} deg/day")
    print(f"                          = {node_per_day * 365.25:12.3f} deg/year")
    print(f"  apsidal rotation        = {apsis_per_day:12.6f} deg/day")
    print(f"                          = {apsis_per_day * 365.25:12.3f} deg/year")
    print()
    print(f"OVER {days:.0f} DAYS")
    print(f"  RAAN moves by           = {node_per_day * days:12.3f} deg")
    print(f"  argument of perigee by  = {apsis_per_day * days:12.3f} deg")
    print()
    print("INTERPRETATION")
    if node_per_day < -0.001:
        print("  The node drifts WESTWARD (prograde orbit).")
    elif node_per_day > 0.001:
        print("  The node drifts EASTWARD (retrograde orbit).")
    else:
        print("  The node is essentially STATIONARY (polar orbit).")

    crit_deg = ang.to_degrees(Perturbations.critical_inclination_rad())
    at_crit = (
        abs(abs(INCLINATION_DEG) - crit_deg) < 0.01
        or abs(abs(INCLINATION_DEG) - (180.0 - crit_deg)) < 0.01
    )
    if at_crit:
        print(
            f"  You are at the CRITICAL INCLINATION ({crit_deg:.4f} deg): perigee is frozen.\n"
            "  This is the Molniya trick - apogee stays parked over the same hemisphere."
        )
    else:
        print(
            f"  Perigee is drifting.  The critical inclination that would freeze it is\n"
            f"  {crit_deg:.4f} deg (or {180.0 - crit_deg:.4f} deg retrograde)."
        )


if __name__ == "__main__":
    main()
