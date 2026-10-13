"""
ch2-rv2coe.py — Sample: State vector to orbital elements (rv2coe).
===================================================================

Chapter 2 of Bate, Mueller & White, *Fundamentals of Astrodynamics*.

Demonstrates:
    Converting a position + velocity measurement (state vector) into the
    six classical Keplerian orbital elements.

Physics background:
    Radar and GPS produce position and velocity.  Catalogues and mission
    plans work in orbital elements.  This conversion is the bridge.

    The algorithm builds three vectors and then reads the angles off them:
      h = r × v            (angular momentum, fixes the orbital plane)
      n = K × h            (node vector, points at the ascending node)
      e = [(v² − μ/r)r − (r·v)v] / μ   (eccentricity vector, points at periapsis)

    Each angle comes from an arccosine plus a quadrant test, because arccos
    alone cannot distinguish 60° from 300°.

Default values:
    Vallado Example 2-5.  Expected output (approximately):
      a = 36127.343 km, e = 0.832853, i = 87.870°,
      RAAN = 227.898°, argp = 53.38°, ν = 92.335°.
"""

# ---------------------------------------------------------------------------
# Path setup — allows running this script from its own directory.
# ---------------------------------------------------------------------------
import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import astro_core.constants as C
import astro_core.two_body as tb
from astro_core.vector3 import Vector3
from astro_core.orbit_conversions import StateVector, OrbitConversions

# ===========================================================================
# EDIT THESE VALUES AND RE-RUN
# ===========================================================================
# Position components [km]
RX = 6524.834
RY = 6862.875
RZ = 6448.296
# Velocity components [km/s]
VX =  4.901327
VY =  5.533756
VZ = -1.976341
# ===========================================================================

def main() -> None:
    state = StateVector(
        position_km=Vector3(RX, RY, RZ),
        velocity_km_per_sec=Vector3(VX, VY, VZ),
    )

    elements = OrbitConversions.to_orbital_elements(state)

    print("INPUT STATE")
    print(state)
    print()
    print("CLASSICAL ORBITAL ELEMENTS")
    print(elements)

    if elements.eccentricity < 1.0:
        print()
        T = tb.period(elements.semi_major_axis_km)
        print(f"Period                    = {T / 60.0:14.3f} min")
        print(
            f"Perigee altitude          = "
            f"{elements.periapsis_radius_km - C.EARTH_RADIUS_KM:14.3f} km"
        )
        print(
            f"Apogee altitude           = "
            f"{elements.apoapsis_radius_km - C.EARTH_RADIUS_KM:14.3f} km"
        )

    # Round-trip check: convert straight back and see if we recover the input.
    recovered = OrbitConversions.to_state_vector(elements)
    pos_error = (recovered.position_km - state.position_km).magnitude
    vel_error = (recovered.velocity_km_per_sec - state.velocity_km_per_sec).magnitude

    print()
    print("ROUND-TRIP VERIFICATION (elements converted back to a state vector)")
    print(f"  position error            = {pos_error:.3E} km")
    print(f"  velocity error            = {vel_error:.3E} km/s")


if __name__ == "__main__":
    main()
