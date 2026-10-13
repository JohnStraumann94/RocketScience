"""
ch3-hohmann.py — Sample: Hohmann transfer.
==========================================

Chapter 3 of Bate, Mueller & White, *Fundamentals of Astrodynamics*.

Demonstrates:
    The classic two-burn minimum-energy transfer between coplanar circular
    orbits, computing each burn, the total delta-v, and the coast time.

Physics background:
    Walter Hohmann worked this out in 1925, decades before anything flew.
    Connect two circular orbits with an ellipse tangent to both: its
    periapsis sits on the inner circle and its apoapsis on the outer one,
    so a_transfer = (r1 + r2)/2.

    Both burns are purely tangential — no direction change — which is
    exactly why the manoeuvre is so efficient.  Each burn is just the
    difference of two vis-viva evaluations:

        Δv₁ = v_transfer(r₁) − v_circular(r₁)   [raise apoapsis to r₂]
        Δv₂ = v_circular(r₂) − v_transfer(r₂)   [circularise at r₂]
        t   = π √(aₜ³ / μ)                       [half-period of transfer ellipse]

    The defaults transfer from a 300 km parking orbit to geostationary
    altitude, which is the single most-flown manoeuvre in the history of
    spaceflight.

Cross-check:
    With r1 = 6678 km and r2 = 42164 km the total delta-v must be
    approximately 3.8926 km/s.
"""

# ---------------------------------------------------------------------------
# Path setup — allows running this script from its own directory.
# ---------------------------------------------------------------------------
import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import astro_core.two_body as tb
from astro_core.maneuvers import Maneuvers, BI_ELLIPTIC_BREAK_EVEN_RATIO

# ===========================================================================
# EDIT THESE VALUES AND RE-RUN
# ===========================================================================
R1_KM = 6678.0    # km — initial circular radius  (≈ 300 km altitude)
R2_KM = 42164.0   # km — final circular radius    (geostationary)
# ===========================================================================

def main() -> None:
    result = Maneuvers.hohmann_transfer(R1_KM, R2_KM)
    a_transfer = (R1_KM + R2_KM) / 2.0

    print(result.description)
    print()
    print(f"Initial circular speed    = {tb.circular_speed(R1_KM):12.6f} km/s")
    print(f"Final circular speed      = {tb.circular_speed(R2_KM):12.6f} km/s")
    print()

    dv1 = result.burns_km_per_sec[0]
    dv2 = result.burns_km_per_sec[1]

    print(
        f"Burn 1 (at r1)            = {dv1:12.6f} km/s  "
        f"({'prograde' if dv1 >= 0 else 'retrograde'})"
    )
    print(
        f"Burn 2 (at r2)            = {dv2:12.6f} km/s  "
        f"({'prograde' if dv2 >= 0 else 'retrograde'})"
    )
    print(f"TOTAL delta-v             = {result.total_delta_v_km_per_sec:12.6f} km/s")
    print()
    print(f"Transfer ellipse a        = {a_transfer:12.3f} km")
    print(f"Transfer time             = {result.transfer_time_sec:12.1f} s")
    print(f"                          = {result.transfer_time_hours:12.4f} hours")

    ratio = R2_KM / R1_KM
    print()
    print(f"Radius ratio r2/r1        = {ratio:12.4f}")
    if ratio > BI_ELLIPTIC_BREAK_EVEN_RATIO:
        print(
            f"  This exceeds the break-even ratio of {BI_ELLIPTIC_BREAK_EVEN_RATIO:.3f},\n"
            "  so a three-burn bi-elliptic transfer may be cheaper.  See ch3-bielliptic.py."
        )
    else:
        print(
            f"  This is below the break-even ratio of {BI_ELLIPTIC_BREAK_EVEN_RATIO:.3f},\n"
            "  so Hohmann is the optimal two-impulse transfer here."
        )


if __name__ == "__main__":
    main()
