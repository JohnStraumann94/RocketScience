"""
constants.py — Physical, astronomical and gravitational constants.
==================================================================

Port of ``Astro.Core/Constants.cs``.  Referenced throughout the library.

Unit convention (same as the C# library)
-----------------------------------------
* length ......... kilometres (km)
* time ........... seconds (s)
* speed .......... km/s
* angles ......... radians internally, degrees at the API/UI boundary
* gravitational parameter mu .. km³/s²

Why km instead of SI metres?  Because the numbers stay in a comfortable
range — Earth's radius is 6 378 km, not 6 378 000 m — which helps avoid
silly scale errors when typing in constants.
"""

import math

# ---------------------------------------------------------------------------
# Gravitational parameters  μ = G·M  [km³/s²]
#
# NOTE: μ is known to far greater precision than either G or M alone, because
# it is what actually gets measured by tracking spacecraft.  That is why
# astrodynamics always works with the *product* and never with G and M
# separately.
# ---------------------------------------------------------------------------

MU_SUN: float = 1.32712440018e11
"""Gravitational parameter of the Sun, μ_Sun [km³/s²]."""

MU_EARTH: float = 398600.4418
"""Gravitational parameter of the Earth, μ_Earth [km³/s²]  (EGM-96 value)."""

MU_MOON: float = 4902.800066
"""Gravitational parameter of the Moon, μ_Moon [km³/s²]."""

MU_MARS: float = 42828.375214
"""Gravitational parameter of Mars, μ_Mars [km³/s²]."""

MU_VENUS: float = 324858.592
"""Gravitational parameter of Venus, μ_Venus [km³/s²]."""

MU_JUPITER: float = 1.26686534e8
"""Gravitational parameter of Jupiter (system), μ_Jupiter [km³/s²]."""

# ---------------------------------------------------------------------------
# Body radii [km]
# ---------------------------------------------------------------------------

EARTH_RADIUS_KM: float = 6378.137
"""Earth mean equatorial radius [km]  (WGS-84)."""

MOON_RADIUS_KM: float = 1737.4
"""Moon mean radius [km]."""

MARS_RADIUS_KM: float = 3396.19
"""Mars mean equatorial radius [km]."""

SUN_RADIUS_KM: float = 695700.0
"""Sun mean radius [km]."""

# ---------------------------------------------------------------------------
# Earth shape / rotation
# ---------------------------------------------------------------------------

J2_EARTH: float = 1.08262668e-3
"""
Second zonal harmonic (oblateness) coefficient J2 of the Earth
[dimensionless].

This single term dominates every other non-spherical gravity perturbation by
roughly three orders of magnitude and is the basis of the secular node and
perigee drift formulas in :mod:`perturbations` (BMW Chapter 9).
"""

EARTH_ROTATION_RATE_RAD_PER_SEC: float = 7.2921159e-5
"""Earth inertial rotation rate [rad/s]  (one sidereal day = 86164.0905 s)."""

SIDEREAL_DAY_SECONDS: float = 86164.0905
"""Length of a sidereal day [s]."""

SOLAR_DAY_SECONDS: float = 86400.0
"""Length of a mean solar day [s]."""

# ---------------------------------------------------------------------------
# Orbital radii / periods of the major bodies (circular-coplanar approximation)
#
# These are deliberately simple mean values.  BMW's interplanetary chapter
# models the planets as coplanar circular orbits, and so do we.
# ---------------------------------------------------------------------------

ASTRONOMICAL_UNIT_KM: float = 1.495978707e8
"""Astronomical unit [km] — the IAU 2012 exact definition."""

EARTH_MOON_DISTANCE_KM: float = 384400.0
"""Mean Earth–Moon distance [km]."""

MOON_SIDEREAL_PERIOD_SECONDS: float = 27.321661 * SOLAR_DAY_SECONDS
"""Moon sidereal orbital period about the Earth [s]  (27.321661 days)."""

# ---------------------------------------------------------------------------
# Canonical (BMW) unit conversions
# ---------------------------------------------------------------------------

EARTH_DISTANCE_UNIT_KM: float = EARTH_RADIUS_KM
"""
One Earth canonical distance unit (DU) expressed in km.

In BMW, 1 DU is defined as the Earth's equatorial radius, so a circular orbit
"at the surface" has r = 1 DU.
"""

EARTH_TIME_UNIT_SECONDS: float = math.sqrt(
    EARTH_RADIUS_KM ** 3 / MU_EARTH
)
"""
One Earth canonical time unit (TU) expressed in seconds.

Defined so that μ = 1 DU³/TU², i.e. TU = √(DU³/μ) ≈ 806.81 s.  A satellite
in a circular orbit of 1 DU therefore has a speed of exactly 1 DU/TU ≈ 7.905
km/s.
"""

# ---------------------------------------------------------------------------
# Angle conversions
# ---------------------------------------------------------------------------

DEG_TO_RAD: float = math.pi / 180.0
"""Multiply degrees by this to obtain radians."""

RAD_TO_DEG: float = 180.0 / math.pi
"""Multiply radians by this to obtain degrees."""

TWO_PI: float = 2.0 * math.pi
"""Two π — one full revolution in radians."""
