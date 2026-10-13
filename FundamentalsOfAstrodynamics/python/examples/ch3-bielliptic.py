"""
ch3-bielliptic.py — Sample: Bi-elliptic vs Hohmann transfer.
=============================================================

Chapter 3 of Bate, Mueller & White, *Fundamentals of Astrodynamics*.

Demonstrates:
    When flying further actually costs less — the three-burn transfer that
    beats Hohmann for large radius ratios.

Physics background:
    Instead of transferring directly, fling the vehicle out to a very high
    intermediate apoapsis r_b, do the shape change out there where it is
    moving slowly and therefore cheaply, then drop down to the target radius.

    This is the Oberth effect running in reverse.  Because a burn's effect
    on orbital energy is proportional to the speed at which it is performed,
    changing the orbit far away — where speed is low — costs very little Δv.

    Rules of thumb for R = r₂/r₁:
      * R < 11.94  ⟹ Hohmann always wins
      * R > 15.58  ⟹ bi-elliptic always wins (given a large enough r_b)
      * In between it depends on how far out you are willing to go, and the
        price is always time: bi-elliptic transfers can take many times longer.
"""

# ---------------------------------------------------------------------------
# Path setup — allows running this script from its own directory.
# ---------------------------------------------------------------------------
import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

from astro_core.maneuvers import Maneuvers, BI_ELLIPTIC_BREAK_EVEN_RATIO

# ===========================================================================
# EDIT THESE VALUES AND RE-RUN
# ===========================================================================
R1_KM  =   7000.0   # km — initial circular radius
RB_KM  = 210000.0   # km — intermediate apoapsis (must be > r1 and r2)
R2_KM  = 140000.0   # km — final circular radius (ratio 20, above break-even)
# ===========================================================================

def main() -> None:
    hohmann    = Maneuvers.hohmann_transfer(R1_KM, R2_KM)
    bi_elliptic = Maneuvers.bi_elliptic_transfer(R1_KM, RB_KM, R2_KM)

    print(
        f"Radius ratio r2/r1 = {R2_KM / R1_KM:.4f}   "
        f"(break-even is {BI_ELLIPTIC_BREAK_EVEN_RATIO:.3f})"
    )
    print()

    print("HOHMANN (2 burns)")
    print(f"  burn 1                  = {hohmann.burns_km_per_sec[0]:12.6f} km/s")
    print(f"  burn 2                  = {hohmann.burns_km_per_sec[1]:12.6f} km/s")
    print(f"  TOTAL                   = {hohmann.total_delta_v_km_per_sec:12.6f} km/s")
    print(
        f"  time                    = {hohmann.transfer_time_hours:12.3f} hours "
        f"({hohmann.transfer_time_hours / 24.0:.3f} days)"
    )
    print()

    print("BI-ELLIPTIC (3 burns)")
    print(f"  burn 1 (at r1)          = {bi_elliptic.burns_km_per_sec[0]:12.6f} km/s")
    print(f"  burn 2 (at rb)          = {bi_elliptic.burns_km_per_sec[1]:12.6f} km/s")
    print(f"  burn 3 (at r2)          = {bi_elliptic.burns_km_per_sec[2]:12.6f} km/s")
    print(f"  TOTAL                   = {bi_elliptic.total_delta_v_km_per_sec:12.6f} km/s")
    print(
        f"  time                    = {bi_elliptic.transfer_time_hours:12.3f} hours "
        f"({bi_elliptic.transfer_time_hours / 24.0:.3f} days)"
    )
    print()

    saving   = hohmann.total_delta_v_km_per_sec - bi_elliptic.total_delta_v_km_per_sec
    time_cost = bi_elliptic.transfer_time_hours - hohmann.transfer_time_hours

    print("VERDICT")
    if saving > 0.0:
        pct = saving / hohmann.total_delta_v_km_per_sec * 100.0
        print(
            f"  Bi-elliptic WINS by {saving:.6f} km/s ({pct:.2f}% cheaper),"
        )
        print(
            f"  at a cost of {time_cost:.1f} extra hours "
            f"({time_cost / 24.0:.2f} days) of flight time."
        )
    else:
        print(
            f"  Hohmann WINS by {-saving:.6f} km/s, and is "
            f"{-time_cost / 24.0:.2f} days faster as well."
        )


if __name__ == "__main__":
    main()
