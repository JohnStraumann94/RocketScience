"""
ch4-universal.py — Sample: Universal variable propagation.
===========================================================

Chapter 4 of Bate, Mueller & White, *Fundamentals of Astrodynamics*.

Demonstrates:
    One equation that propagates any conic — circle, ellipse, parabola or
    hyperbola — without special cases, using Sundman's universal anomaly χ
    and the Stumpff functions C(ψ) and S(ψ).

Physics background:
    The classical approach needs a different equation for each conic type,
    and all of them degrade badly near e = 1 — exactly where escape
    trajectories and transfer arcs live.

    Sundman's universal anomaly χ fixes this.  By changing the independent
    variable via dt = (r / √μ) dχ, a single equation covers every conic::

        √μ dt = χ³ S(ψ) + (r₀·v₀/√μ) χ² C(ψ) + r₀ χ (1 − ψ C(ψ))

    where ψ = χ²/a.  Once χ is found, the new state comes from the Lagrange
    f and g coefficients:
        r = f r₀ + g v₀
        v = ḟ r₀ + ġ v₀

    Two-body motion conserves energy and angular momentum exactly; the
    output of this sample verifies both, providing confidence in the
    numerical propagator.
"""

# ---------------------------------------------------------------------------
# Path setup — allows running this script from its own directory.
# ---------------------------------------------------------------------------
import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import astro_core.two_body as tb
from astro_core.vector3 import Vector3
from astro_core.orbit_conversions import StateVector, OrbitConversions
from astro_core.universal_variables import UniversalVariables

# ===========================================================================
# EDIT THESE VALUES AND RE-RUN
# ===========================================================================
RX_KM =  -6045.0   # km
RY_KM =  -3490.0   # km
RZ_KM =   2500.0   # km
VX_KMS =  -3.457   # km/s
VY_KMS =   6.618   # km/s
VZ_KMS =   2.533   # km/s
DT_SEC =  3600.0   # s — propagation time (may be negative for backwards)
# ===========================================================================

def main() -> None:
    start = StateVector(
        position_km=Vector3(RX_KM, RY_KM, RZ_KM),
        velocity_km_per_sec=Vector3(VX_KMS, VY_KMS, VZ_KMS),
    )

    end = UniversalVariables.propagate(start, DT_SEC)

    elements = OrbitConversions.to_orbital_elements(start)

    print(f"ORBIT TYPE: {elements.orbit_type.name}   (e = {elements.eccentricity:.6f}, a = {elements.semi_major_axis_km:.3f} km)")
    print()
    print("INITIAL STATE")
    print(start)
    print()
    print(f"PROPAGATED BY {DT_SEC:.1f} s ({DT_SEC / 60.0:.3f} min)")
    print(end)
    print()

    # Energy must be conserved exactly under two-body motion.
    energy_start = tb.specific_energy(start.radius_km, start.speed_km_per_sec)
    energy_end   = tb.specific_energy(end.radius_km, end.speed_km_per_sec)
    print("CONSERVATION CHECKS (these must not change under two-body motion)")
    print(f"  energy before           = {energy_start:14.9f} km^2/s^2")
    print(f"  energy after            = {energy_end:14.9f} km^2/s^2")
    print(f"  drift                   = {abs(energy_end - energy_start):14.4E}")

    h_start = Vector3.cross(start.position_km, start.velocity_km_per_sec)
    h_end   = Vector3.cross(end.position_km, end.velocity_km_per_sec)
    print(f"  |h| before              = {h_start.magnitude:14.9f} km^2/s")
    print(f"  |h| after               = {h_end.magnitude:14.9f} km^2/s")

    # Round-trip: propagate forward then back.
    back = UniversalVariables.propagate(end, -DT_SEC)
    print()
    print("REVERSIBILITY CHECK (propagate forward then back)")
    print(f"  position error          = {(back.position_km - start.position_km).magnitude:14.4E} km")


if __name__ == "__main__":
    main()
