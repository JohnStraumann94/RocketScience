"""
ch7-lunar.py — Sample: Lunar transfer by patched conics.
=========================================================

Chapter 7 of Bate, Mueller & White, *Fundamentals of Astrodynamics*.

Demonstrates:
    Trans-lunar injection (TLI) delta-v, sphere of influence, coast time,
    and lunar orbit insertion (LOI) delta-v, using the patched-conic method.

Physics background:
    The Earth–Moon–spacecraft system is a three-body problem with no closed-
    form solution.  The patched-conic approximation divides space into regions
    where one body dominates, solves a two-body problem in each, and stitches
    the answers together at the boundary — the Moon's sphere of influence:

        r_SOI = R (μ_Moon / μ_Earth)^(2/5)  ≈ 66,183 km

    Outside the SOI: geocentric ellipse (trans-lunar injection).
    Inside the SOI: selenocentric hyperbola.
    At periapsis: lunar orbit insertion burn (Oberth effect).

    Every Apollo mission was first sketched exactly this way.
"""

# ---------------------------------------------------------------------------
# Path setup — allows running this script from its own directory.
# ---------------------------------------------------------------------------
import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import math
import astro_core.constants as C
import astro_core.two_body as tb
from astro_core.lunar_trajectory import LunarTrajectory

# ===========================================================================
# EDIT THESE VALUES AND RE-RUN
# ===========================================================================
PARKING_ALTITUDE_KM = 185.0  # km — parking orbit altitude (Apollo was 185 km)
V_INF_KM_S          = 0.8    # km/s — arrival v_infinity at the Moon's SOI
LUNAR_ORBIT_ALT_KM  = 110.0  # km — target lunar orbit altitude (Apollo ~110 km)
# ===========================================================================

def main() -> None:
    parking_radius = C.EARTH_RADIUS_KM + PARKING_ALTITUDE_KM
    soi = LunarTrajectory.moon_sphere_of_influence_km()
    arrival_radius = C.EARTH_MOON_DISTANCE_KM - soi

    leg = LunarTrajectory.compute_geocentric_leg(parking_radius, arrival_radius)

    lunar_orbit_radius = C.MOON_RADIUS_KM + LUNAR_ORBIT_ALT_KM
    loi = LunarTrajectory.lunar_orbit_insertion_delta_v(V_INF_KM_S, lunar_orbit_radius)

    print("GEOMETRY")
    print(f"  Earth-Moon distance     = {C.EARTH_MOON_DISTANCE_KM:12.0f} km")
    print(
        f"  Moon's sphere of influence = {soi:9.0f} km  "
        f"({soi / C.EARTH_MOON_DISTANCE_KM * 100.0:.1f}% of the distance)"
    )
    print(f"  patch point radius      = {arrival_radius:12.0f} km")
    print()
    print("PHASE 1 - GEOCENTRIC ELLIPSE (trans-lunar injection)")
    print(f"  parking orbit radius    = {parking_radius:12.1f} km")
    print(f"  parking orbit speed     = {tb.circular_speed(parking_radius):12.6f} km/s")
    print(f"  injection speed         = {leg.injection_speed_km_per_sec:12.6f} km/s")
    print(f"  TLI delta-v             = {leg.injection_delta_v_km_per_sec:12.6f} km/s")
    print(f"  transfer a              = {leg.transfer_semi_major_axis_km:12.0f} km")
    print(f"  transfer e              = {leg.transfer_eccentricity:12.6f}")
    print(f"  arrival speed (geo)     = {leg.arrival_speed_km_per_sec:12.6f} km/s")
    print(f"  coast time              = {leg.time_of_flight_days:12.4f} days")
    print()
    print("PHASE 2 - SELENOCENTRIC HYPERBOLA (lunar orbit insertion)")
    print(f"  arrival v_infinity      = {V_INF_KM_S:12.6f} km/s")
    print(f"  target orbit radius     = {lunar_orbit_radius:12.1f} km  (altitude {LUNAR_ORBIT_ALT_KM:.0f} km)")
    v_peri = math.sqrt(V_INF_KM_S ** 2 + 2.0 * C.MU_MOON / lunar_orbit_radius)
    print(f"  speed at periapsis      = {v_peri:12.6f} km/s")
    print(f"  lunar circular speed    = {tb.circular_speed(lunar_orbit_radius, C.MU_MOON):12.6f} km/s")
    print(f"  LOI delta-v             = {loi:12.6f} km/s")
    print()
    total = leg.injection_delta_v_km_per_sec + loi
    print("MISSION TOTAL")
    print(f"  TLI + LOI               = {total:12.6f} km/s")
    print()
    print("  For comparison, Apollo's actual TLI was about 3.05-3.10 km/s and")
    print("  its LOI about 0.9 km/s, over a 3-day (higher energy) trajectory.")


if __name__ == "__main__":
    main()
