"""
ch2-coe2rv.py — Sample: Orbital elements to state vector (coe2rv).
===================================================================

Chapter 2 of Bate, Mueller & White, *Fundamentals of Astrodynamics*.

Demonstrates:
    The inverse conversion: classical orbital elements → Cartesian state
    vector, via the perifocal frame and a 3-1-3 rotation matrix.

Physics background:
    Going the other way is a two-stage process.

    Stage 1 — write the state in the perifocal (PQW) frame, where the P
    axis points at periapsis.  In that frame the algebra is almost trivial:
        r_PQW = (p / (1 + e cos ν)) · [cos ν, sin ν, 0]
        v_PQW = √(μ/p)             · [−sin ν, e + cos ν, 0]

    Stage 2 — rotate into the inertial frame with the classical 3-1-3 Euler
    sequence R3(−Ω) R1(−i) R3(−ω).  Those three rotations undo exactly how
    the three orienting elements were defined.

Default values match the Vallado Example 2-5 answer, so rv2coe and coe2rv
should round-trip back to the state vector from the rv2coe sample.
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
from astro_core.vector3 import Vector3
from astro_core.orbital_elements import OrbitalElements
from astro_core.orbit_conversions import OrbitConversions

# ===========================================================================
# EDIT THESE VALUES AND RE-RUN
# ===========================================================================
SEMI_MAJOR_AXIS_KM     = 36127.343  # km — semi-major axis
ECCENTRICITY           = 0.832853   # dimensionless
INCLINATION_DEG        = 87.870     # deg
RAAN_DEG               = 227.898    # deg
ARG_PERIAPSIS_DEG      = 53.38      # deg
TRUE_ANOMALY_DEG       = 92.335     # deg
# ===========================================================================

def main() -> None:
    elements = OrbitalElements(
        semi_major_axis_km=SEMI_MAJOR_AXIS_KM,
        eccentricity=ECCENTRICITY,
        inclination_rad=ang.to_radians(INCLINATION_DEG),
        raan_rad=ang.to_radians(RAAN_DEG),
        argument_of_periapsis_rad=ang.to_radians(ARG_PERIAPSIS_DEG),
        true_anomaly_rad=ang.to_radians(TRUE_ANOMALY_DEG),
    )

    state = OrbitConversions.to_state_vector(elements)

    print("INPUT ELEMENTS")
    print(elements)
    print()
    print("RESULTING STATE VECTOR")
    print(state)
    print()

    # Angular momentum vector — should be perpendicular to both r and v.
    h = Vector3.cross(state.position_km, state.velocity_km_per_sec)
    print(
        f"Angular momentum h        = {h.to_string(4)}  |h| = {h.magnitude:.4f} km^2/s"
    )
    gamma = ang.to_degrees(
        tb.flight_path_angle(elements.eccentricity, elements.true_anomaly_rad)
    )
    print(f"Flight-path angle         = {gamma:.5f} deg")
    print(
        f"Specific energy           = "
        f"{tb.specific_energy(state.radius_km, state.speed_km_per_sec):.6f} km^2/s^2"
    )


if __name__ == "__main__":
    main()
