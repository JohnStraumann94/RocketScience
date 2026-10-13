"""
ch8-interplanetary.py — Sample: Interplanetary Hohmann transfer.
=================================================================

Chapter 8 of Bate, Mueller & White, *Fundamentals of Astrodynamics*.

Demonstrates:
    Full Earth-to-Mars budget: C3, injection delta-v, capture delta-v,
    heliocentric transfer time, required departure phase angle, and synodic
    period (launch window spacing).

Physics background:
    Patched conics applied on the scale of the solar system, with three
    conics instead of two:
      1. Departure hyperbola relative to Earth.
      2. Heliocentric transfer ellipse.
      3. Arrival hyperbola at Mars.

    The planetary spheres of influence are so small compared with 1 AU
    (Earth's SOI is 0.6% of 1 AU) that the patch points can be treated as
    coincident with the planets themselves.

    Two numbers dominate mission planning:
      * C3 = v_∞² [km²/s²] — the characteristic energy that launch providers
        quote in their performance curves.
      * Synodic period ≈ 780 days for Earth–Mars — why Mars missions cluster
        every 26 months.

    The departure phase angle (≈44° for Earth–Mars) is the other half of
    the window: Mars must be that far ahead of Earth at launch.
"""

# ---------------------------------------------------------------------------
# Path setup — allows running this script from its own directory.
# ---------------------------------------------------------------------------
import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import astro_core.constants as C
from astro_core.interplanetary import Interplanetary

# ===========================================================================
# EDIT THESE VALUES AND RE-RUN
# ===========================================================================
DEPARTURE_ORBIT_AU    = 1.0          # AU — Earth's orbital radius
ARRIVAL_ORBIT_AU      = 1.523679     # AU — Mars (1.5237), Venus (0.7233), Jupiter (5.2026)
DEPARTURE_PARKING_ALT = 200.0        # km — parking altitude above Earth
TARGET_MU             = C.MU_MARS   # km³/s² — Mars
TARGET_RADIUS         = C.MARS_RADIUS_KM  # km — Mars surface radius
ARRIVAL_PARKING_ALT   = 500.0        # km — parking altitude above target planet
# ===========================================================================

def main() -> None:
    transfer = Interplanetary.compute_hohmann_transfer(
        departure_orbit_radius_km=DEPARTURE_ORBIT_AU * C.ASTRONOMICAL_UNIT_KM,
        arrival_orbit_radius_km=ARRIVAL_ORBIT_AU * C.ASTRONOMICAL_UNIT_KM,
        departure_planet_mu=C.MU_EARTH,
        departure_parking_radius_km=C.EARTH_RADIUS_KM + DEPARTURE_PARKING_ALT,
        arrival_planet_mu=TARGET_MU,
        arrival_parking_radius_km=TARGET_RADIUS + ARRIVAL_PARKING_ALT,
    )

    print("HELIOCENTRIC TRANSFER ELLIPSE")
    print(f"  departure radius        = {DEPARTURE_ORBIT_AU:12.6f} AU")
    print(f"  arrival radius          = {ARRIVAL_ORBIT_AU:12.6f} AU")
    print(
        f"  transfer speed at departure = {transfer.heliocentric_departure_speed_km_per_sec:8.6f} km/s"
    )
    print(
        f"  flight time             = {transfer.transfer_time_days:12.2f} days  "
        f"({transfer.transfer_time_days / 365.25:.3f} years)"
    )
    print()
    print("DEPARTURE (Earth escape hyperbola)")
    print(f"  v_infinity required     = {transfer.departure_v_infinity_km_per_sec:12.6f} km/s")
    print(f"  C3 (= v_inf^2)          = {transfer.departure_c3:12.4f} km^2/s^2")
    print(f"  injection delta-v       = {transfer.injection_delta_v_km_per_sec:12.6f} km/s")
    print()
    print("ARRIVAL (capture hyperbola)")
    print(f"  v_infinity on arrival   = {transfer.arrival_v_infinity_km_per_sec:12.6f} km/s")
    print(f"  capture delta-v         = {transfer.capture_delta_v_km_per_sec:12.6f} km/s")
    print()
    print("MISSION TOTAL")
    print(f"  total delta-v           = {transfer.total_delta_v_km_per_sec:12.6f} km/s")
    print()
    print("LAUNCH WINDOW")
    print(f"  required phase angle    = {transfer.departure_phase_angle_deg:12.3f} deg")
    if transfer.departure_phase_angle_deg > 0.0:
        print("    (the target must LEAD the departure planet by this angle at launch)")
    else:
        print("    (the target must TRAIL the departure planet by this angle at launch)")
    print(
        f"  synodic period          = {transfer.synodic_period_days:12.2f} days  "
        f"({transfer.synodic_period_days / 365.25:.3f} years between windows)"
    )


if __name__ == "__main__":
    main()
