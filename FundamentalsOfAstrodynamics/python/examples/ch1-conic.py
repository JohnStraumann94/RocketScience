"""
ch1-conic.py — Sample: The orbit equation and conic geometry.
==============================================================

Chapter 1 of Bate, Mueller & White, *Fundamentals of Astrodynamics*.

Demonstrates:
    The polar orbit equation r = p / (1 + e cos ν) swept around a full
    revolution, showing how radius, speed, and flight-path angle vary with
    true anomaly for any conic section.

Physics background:
    All four conic sections are contained in one polar equation:
        r = p / (1 + e cos ν)
    where p = a(1 − e²) is the semi-latus rectum and ν is the true anomaly
    measured from periapsis.  Eccentricity alone decides the shape:
      e = 0  ⟹ circle
      0 < e < 1  ⟹ ellipse
      e = 1  ⟹ parabola
      e > 1  ⟹ hyperbola
    The flight-path angle γ = atan2(e sin ν, 1 + e cos ν) is zero at both
    apsides, which is precisely why tangential burns there are so efficient.
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

# ===========================================================================
# EDIT THESE VALUES AND RE-RUN
# ===========================================================================
SEMI_MAJOR_AXIS_KM = 26600.0  # km — semi-major axis  (Molniya-like orbit)
ECCENTRICITY       = 0.74     # dimensionless  (try 0 for a circle)
NUM_STEPS          = 13       # number of rows in the table
# ===========================================================================

def main() -> None:
    sma = SEMI_MAJOR_AXIS_KM
    ecc = ECCENTRICITY
    steps = max(2, min(73, int(NUM_STEPS)))

    # Semi-latus rectum.
    p = sma * (1.0 - ecc ** 2)

    print(f"Semi-latus rectum p = {p:.3f} km")
    rp = tb.periapsis_radius(sma, ecc)
    print(
        f"Periapsis radius    = {rp:.3f} km "
        f"(altitude {rp - C.EARTH_RADIUS_KM:.1f} km)"
    )

    if ecc < 1.0:
        ra = tb.apoapsis_radius(sma, ecc)
        print(
            f"Apoapsis radius     = {ra:.3f} km "
            f"(altitude {ra - C.EARTH_RADIUS_KM:.1f} km)"
        )
        T = tb.period(sma)
        print(f"Period              = {T / 3600.0:.4f} hours")

    print()
    print("       nu        Radius        Speed   Flight-path")
    print("    (deg)          (km)       (km/s)     angle (deg)")
    print("    ------------------------------------------------")

    for i in range(steps):
        nu_deg = 360.0 * i / (steps - 1)
        nu_rad = ang.to_radians(nu_deg)

        radius = tb.radius_at_true_anomaly(p, ecc, nu_rad)
        speed  = tb.vis_viva(radius, sma)
        gamma  = ang.to_degrees(tb.flight_path_angle(ecc, nu_rad))

        print(
            f"    {nu_deg:6.1f}  {radius:12.1f}  {speed:11.5f}  {gamma:14.4f}"
        )


if __name__ == "__main__":
    main()
