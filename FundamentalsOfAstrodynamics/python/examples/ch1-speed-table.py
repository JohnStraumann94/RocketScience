"""
ch1-speed-table.py — Sample: Circular speed and period vs altitude.
====================================================================

Chapter 1 of Bate, Mueller & White, *Fundamentals of Astrodynamics*.

Demonstrates:
    A sweep from LEO to beyond geostationary orbit, tabulating circular
    speed and period at each altitude, to build intuition for how slowly
    high orbits move and how dramatically the period grows.

Physics background:
    For a circular orbit the vis-viva equation collapses to v = √(μ/r),
    and Kepler's third law gives T = 2π √(a³/μ).
    Both are worth internalising as a table rather than a formula:
      * Speed falls only as the square root of radius — a satellite six
        times further out still moves at 40% of its original speed.
      * Period grows as radius to the 3/2 power, so it climbs dramatically.
      * Geostationary altitude (35,786 km) is exactly where the period
        reaches one sidereal day, 23 h 56 m 04 s — not 24 hours.
"""

# ---------------------------------------------------------------------------
# Path setup — allows running this script from its own directory.
# ---------------------------------------------------------------------------
import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import astro_core.constants as C
import astro_core.two_body as tb

# ===========================================================================
# EDIT THESE VALUES AND RE-RUN
# ===========================================================================
MIN_ALTITUDE_KM = 200.0    # km — lowest altitude in the table
MAX_ALTITUDE_KM = 40000.0  # km — highest altitude in the table
NUM_STEPS       = 12       # number of rows in the table (integer, 2–60)
# ===========================================================================

def main() -> None:
    steps = max(2, min(60, int(NUM_STEPS)))

    print("  Altitude       Radius        Speed        Period       Period")
    print("      (km)         (km)       (km/s)         (min)      (hours)")
    print("  ---------------------------------------------------------------")

    for i in range(steps):
        # Linearly interpolate altitude between MIN and MAX.
        altitude = MIN_ALTITUDE_KM + (MAX_ALTITUDE_KM - MIN_ALTITUDE_KM) * i / (steps - 1)
        radius = C.EARTH_RADIUS_KM + altitude

        speed  = tb.circular_speed(radius)
        T      = tb.period(radius)

        print(
            f"  {altitude:9.0f}  {radius:11.1f}  {speed:11.4f}  "
            f"{T / 60.0:11.2f}  {T / 3600.0:11.3f}"
        )

    print()
    gso_radius = 42164.17
    print(
        f"  For reference, geostationary radius = {gso_radius:.2f} km "
        f"(altitude {gso_radius - C.EARTH_RADIUS_KM:.0f} km),"
    )
    print(
        f"  where the period equals one sidereal day = "
        f"{C.SIDEREAL_DAY_SECONDS / 3600.0:.4f} hours."
    )


if __name__ == "__main__":
    main()
