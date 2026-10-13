"""
ch9-sunsync.py — Sample: Sun-synchronous orbit design.
=======================================================

Chapter 9 of Bate, Mueller & White, *Fundamentals of Astrodynamics*.

Demonstrates:
    Choose an altitude, get the inclination that keeps the local solar time
    at the equator crossing constant on every pass — the design formula for
    every Earth-observation and weather satellite.

Physics background:
    A sun-synchronous orbit precesses its node eastward at exactly the rate
    the Earth orbits the Sun: 360° per tropical year, or ≈1.99106×10⁻⁷ rad/s.
    The orbital plane then holds a constant angle to the Sun, so the satellite
    crosses the equator at the same local solar time on every single pass.

    That consistency is everything for Earth observation: images taken weeks
    apart have the same shadows and can be compared directly.  Landsat,
    Sentinel, and every operational weather satellite in polar orbit uses it.

    Setting the nodal regression formula equal to the required rate and
    solving for inclination:
        cos(i) = −requiredRate / (1.5 n J2 (Rₑ/p)²)

    The result is always retrograde (i > 90°), because cos(i) must be
    negative to make the node drift eastward.

    Notice the altitude constraint: above roughly 5,975 km no sun-synchronous
    orbit exists, because J2 cannot supply enough precession at higher orbits.
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
ALTITUDE_KM  = 800.0    # km — orbit altitude
ECCENTRICITY = 0.0      # dimensionless (SSO orbits are usually near-circular)
# Sweep table bounds
MIN_ALT_KM   = 300.0    # km
MAX_ALT_KM   = 1500.0   # km
# ===========================================================================

def main() -> None:
    altitude = ALTITUDE_KM
    ecc      = ECCENTRICITY
    sma      = C.EARTH_RADIUS_KM + altitude

    inclination = Perturbations.sun_synchronous_inclination(sma, ecc)
    T = tb.period(sma)
    node_rate = Perturbations.nodal_regression_rate(sma, ecc, inclination)

    print("DESIGN POINT")
    print(f"  altitude                = {altitude:12.1f} km")
    print(f"  semi-major axis a       = {sma:12.3f} km")
    print(f"  eccentricity e          = {ecc:12.6f}")
    print()
    print(f"  REQUIRED INCLINATION    = {ang.to_degrees(inclination):12.5f} deg  (retrograde)")
    print()
    print(f"  orbital period          = {T / 60.0:12.4f} min")
    print(f"  orbits per day          = {C.SOLAR_DAY_SECONDS / T:12.4f}")
    print(f"  resulting node drift    = {ang.to_degrees(node_rate) * C.SOLAR_DAY_SECONDS:12.6f} deg/day")
    print(f"  required for sun-sync   = {360.0 / 365.2422:12.6f} deg/day")
    print()
    print("ALTITUDE SWEEP")
    print("     Altitude    Inclination      Period    Orbits/day")
    print("         (km)          (deg)       (min)")
    print("     ------------------------------------------------")

    for i in range(13):
        h = MIN_ALT_KM + (MAX_ALT_KM - MIN_ALT_KM) * i / 12.0
        a = C.EARTH_RADIUS_KM + h
        try:
            inc = ang.to_degrees(Perturbations.sun_synchronous_inclination(a, ecc))
            t   = tb.period(a)
            print(
                f"     {h:8.0f}  {inc:13.5f}  {t / 60.0:10.3f}  {C.SOLAR_DAY_SECONDS / t:12.4f}"
            )
        except ValueError:
            # Above roughly 5975 km no solution exists.
            print(f"     {h:8.0f}   -- no sun-synchronous solution exists at this altitude --")


if __name__ == "__main__":
    main()
