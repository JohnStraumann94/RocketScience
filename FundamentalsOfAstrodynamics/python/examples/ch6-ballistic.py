"""
ch6-ballistic.py — Sample: Ballistic missile free-flight trajectory.
=====================================================================

Chapter 6 of Bate, Mueller & White, *Fundamentals of Astrodynamics*.

Demonstrates:
    Range, apogee and flight time from burnout conditions, using the
    free-flight range equation and Kepler's equation for the time.

Physics background:
    A ballistic missile trajectory is simply an elliptical orbit whose
    perigee happens to lie inside the Earth.  Everything from Chapters 1–4
    applies unchanged.

    The free-flight range equation (BMW eq. 6.2-4):
        cos(Ψ/2) = (1 − Q cos²φ) / √(1 + Q(Q − 2) cos²φ)
    where Q = v_bo² r_bo / μ.  Q = 1 with φ = 0 is a circular orbit;
    Q = 2 is escape speed, so a missile always has Q < 2.

    Optimal loft angle for maximum range: φ_opt = (π − Ψ)/4.
    For short range → 45°; for half-globe range → near-horizontal.
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
from astro_core.ballistic_missile import BallisticMissile

# ===========================================================================
# EDIT THESE VALUES AND RE-RUN
# ===========================================================================
ALTITUDE_KM    = 100.0  # km — burnout altitude above Earth's surface
SPEED_KM_S     = 6.5    # km/s — burnout speed (must give Q < 2)
FLIGHT_PATH_DEG = 25.0  # deg — burnout flight-path angle from local horizontal
# ===========================================================================

def main() -> None:
    radius = C.EARTH_RADIUS_KM + ALTITUDE_KM
    speed  = SPEED_KM_S
    phi    = ang.to_radians(FLIGHT_PATH_DEG)

    result = BallisticMissile.compute(radius, speed, phi)

    print("BURNOUT CONDITIONS")
    print(f"  radius r_bo             = {radius:12.3f} km  (altitude {ALTITUDE_KM:.1f} km)")
    print(f"  speed  v_bo             = {speed:12.4f} km/s")
    print(f"  flight-path angle phi   = {FLIGHT_PATH_DEG:12.4f} deg")
    print()
    print("FREE-FLIGHT TRAJECTORY")
    print(f"  Q parameter             = {result.q_parameter:12.6f}   (Q = 2 is escape speed)")
    print(f"  eccentricity e          = {result.eccentricity:12.6f}")
    print(f"  semi-major axis a       = {result.semi_major_axis_km:12.3f} km")
    perigee = tb.periapsis_radius(result.semi_major_axis_km, result.eccentricity)
    print(
        f"  perigee radius          = {perigee:12.3f} km "
        "(inside the Earth, as expected)"
    )
    print()
    print("PERFORMANCE")
    print(f"  range angle Psi         = {result.free_flight_range_angle_deg:12.4f} deg")
    print(f"  ground range            = {result.surface_range_km:12.1f} km")
    print(f"  maximum altitude        = {result.maximum_altitude_km:12.1f} km")
    print(f"  free-flight time        = {result.time_of_flight_sec:12.1f} s  ({result.time_of_flight_minutes:.2f} min)")
    print()

    # Compare with the optimal trajectory for the same achieved range.
    optimal_angle = BallisticMissile.optimal_flight_path_angle(result.free_flight_range_angle_rad)
    minimum_speed = BallisticMissile.minimum_burnout_speed(result.free_flight_range_angle_rad, radius)

    print("OPTIMALITY")
    print(f"  optimal angle for this range = {ang.to_degrees(optimal_angle):8.4f} deg")
    print(f"  minimum speed needed         = {minimum_speed:8.4f} km/s")
    print(f"  you are using                = {speed:8.4f} km/s")
    if speed > minimum_speed + 1e-6:
        print(
            f"  ... {speed - minimum_speed:.4f} km/s more than the minimum.  Two flight-path angles\n"
            "      (a lofted 'high road' and a depressed 'low road') reach this same range."
        )
    else:
        print("  ... this is essentially the minimum-energy trajectory for this range.")


if __name__ == "__main__":
    main()
