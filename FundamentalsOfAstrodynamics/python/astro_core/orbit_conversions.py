"""
orbit_conversions.py — Chapter 2: State vector ↔ classical orbital elements.
=============================================================================

Port of ``Astro.Core/OrbitConversions.cs``.

These two routines are the workhorses of any astrodynamics library.
Everything else (propagation, manoeuvre planning, targeting) tends to be a
sandwich: state → elements → do something clever → elements → state.
"""

from __future__ import annotations
import math
from dataclasses import dataclass
from .vector3 import Vector3
from .orbital_elements import OrbitalElements
from . import constants as _C, guard as _G, angles as _A


# Tolerance used to decide whether an orbit is "circular" or "equatorial".
# A value of 1e-8 corresponds to roughly 2 milli-arcseconds of inclination —
# far below the noise floor of any real orbit determination, but comfortably
# above double-precision fuzz.
_SINGULARITY_TOLERANCE: float = 1e-8


@dataclass(frozen=True, slots=True)
class StateVector:
    """
    A position/velocity pair expressed in an inertial Cartesian frame.

    For Earth orbits this is the ECI (Earth-centred inertial) / IJK frame.
    For heliocentric orbits it is the ecliptic frame.

    Attributes:
        position_km:          **r** — position vector [km].
        velocity_km_per_sec:  **v** — inertial velocity vector [km/s].
    """

    position_km: Vector3
    velocity_km_per_sec: Vector3

    @property
    def radius_km(self) -> float:
        """Scalar distance from the centre of the central body [km]."""
        return self.position_km.magnitude

    @property
    def speed_km_per_sec(self) -> float:
        """Scalar inertial speed [km/s]."""
        return self.velocity_km_per_sec.magnitude

    def __str__(self) -> str:
        r = self.position_km
        v = self.velocity_km_per_sec
        return (
            f"r = {r.to_string(4)} km   |r| = {self.radius_km:.4f} km\n"
            f"v = {v.to_string(6)} km/s |v| = {self.speed_km_per_sec:.6f} km/s"
        )


class OrbitConversions:
    """
    Chapter 2 — Orbit Determination from a Single Observation.

    Static methods for converting between a Cartesian state vector (r, v) and
    the classical orbital elements.
    """

    @staticmethod
    def to_orbital_elements(
        state: StateVector,
        mu: float = _C.MU_EARTH,
    ) -> OrbitalElements:
        """
        **State vector → classical orbital elements** (the "rv2coe" algorithm).

        Args:
            state: Inertial position and velocity.
            mu:    Gravitational parameter of the central body [km³/s²].

        Returns:
            The six classical elements (angles in radians).

        Raises:
            ValueError: When the position vector is zero, or when r and v are
                        parallel (rectilinear trajectory, no orbital plane).

        The derivation, step by step:

        1. **h = r × v** — specific angular momentum.  Normal to the orbit
           plane, so its direction fixes both *i* and *Ω*.

        2. **n = K × h** — node vector, pointing at the ascending node.
           K = (0, 0, 1) is the polar unit vector.

        3. **e = [(v²−μ/r)r − (r·v)v] / μ** — eccentricity vector, pointing
           from the focus towards periapsis.  Its magnitude is *e*.

        4. Each angle then comes from a dot product plus a *quadrant check*,
           because arccos only returns [0, π] and cannot tell 60° from 300°.

        **Degenerate cases.**  When the orbit is equatorial the node vector
        vanishes; when it is circular the eccentricity vector vanishes.  The
        standard substitutions (true longitude / argument of latitude /
        longitude of periapsis) are then used, collapsed into the same six
        slots so that :meth:`to_state_vector` still round-trips exactly.
        """
        _G.positive(mu, "mu")

        r = state.position_km
        v = state.velocity_km_per_sec

        r_mag = r.magnitude
        v_mag = v.magnitude
        if r_mag <= 0.0:
            raise ValueError("Position vector must be non-zero.")

        # ----- Step 1: angular momentum ----------------------------------------
        h = Vector3.cross(r, v)
        h_mag = h.magnitude
        if h_mag <= 0.0:
            # r and v are parallel => a purely radial "orbit" (straight up/down).
            # Legitimate degenerate conic but has no orbital plane, so no elements.
            raise ValueError(
                "Position and velocity are parallel (rectilinear trajectory); "
                "orbital elements are undefined."
            )

        # ----- Step 2: node vector, n = K × h ----------------------------------
        # Written by hand because K = (0, 0, 1) makes two terms vanish:
        #   K × h = (0·h_z - 1·h_y,  1·h_x - 0·h_z,  0·h_y - 0·h_x)
        #         = (-h_y, h_x, 0)
        n = Vector3(-h.y, h.x, 0.0)
        n_mag = n.magnitude

        # ----- Step 3: eccentricity vector -------------------------------------
        r_dot_v = Vector3.dot(r, v)
        # e_vec = [(v² - μ/r) r - (r·v) v] / μ
        e_vec = ((v_mag * v_mag - mu / r_mag) * r - r_dot_v * v) / mu
        e = e_vec.magnitude

        # ----- Step 4: size, from the energy -----------------------------------
        energy = v_mag * v_mag / 2.0 - mu / r_mag
        if abs(energy) < 1e-14:
            a = math.inf   # parabolic: a is infinite
        else:
            a = -mu / (2.0 * energy)

        # ----- Step 5: inclination ---------------------------------------------
        # i is the angle between h and the polar axis; always in [0, π],
        # so no quadrant check is needed.  i > 90° means the orbit is retrograde.
        i = _A.safe_acos(h.z / h_mag)

        is_equatorial = n_mag < _SINGULARITY_TOLERANCE
        is_circular = e < _SINGULARITY_TOLERANCE

        if not is_equatorial and not is_circular:
            # ---- General, well-behaved case ------------------------------------
            # Ω: angle from the X axis (vernal equinox) round to the node vector.
            # Quadrant: if n_y < 0 the node lies in the western half => Ω > 180°.
            raan = _A.safe_acos(n.x / n_mag)
            if n.y < 0.0:
                raan = _C.TWO_PI - raan

            # ω: angle from the node round to periapsis.
            # Quadrant: periapsis below the equator (e_z < 0) => ω > 180°.
            arg_peri = _A.safe_acos(Vector3.dot(n, e_vec) / (n_mag * e))
            if e_vec.z < 0.0:
                arg_peri = _C.TWO_PI - arg_peri

            # ν: angle from periapsis round to the satellite.
            # Quadrant: r·v > 0 means radius is growing => ν < 180°.
            nu = _A.safe_acos(Vector3.dot(e_vec, r) / (e * r_mag))
            if r_dot_v < 0.0:
                nu = _C.TWO_PI - nu

        elif is_equatorial and not is_circular:
            # ---- Elliptical equatorial: no node, Ω is undefined ---------------
            # Convention: set Ω = 0 and absorb everything into the "longitude of
            # periapsis" ω, measured from the X axis directly.
            raan = 0.0

            arg_peri = _A.safe_acos(e_vec.x / e)
            if e_vec.y < 0.0:
                arg_peri = _C.TWO_PI - arg_peri
            # For retrograde equatorial orbit the angle runs the other way.
            if h.z < 0.0:
                arg_peri = _C.TWO_PI - arg_peri

            nu = _A.safe_acos(Vector3.dot(e_vec, r) / (e * r_mag))
            if r_dot_v < 0.0:
                nu = _C.TWO_PI - nu

        elif not is_equatorial:
            # ---- Circular inclined: no periapsis, ω is undefined --------------
            # Convention: set ω = 0 and report the "argument of latitude" u —
            # the angle from the ascending node to the satellite — in the ν slot.
            raan = _A.safe_acos(n.x / n_mag)
            if n.y < 0.0:
                raan = _C.TWO_PI - raan

            arg_peri = 0.0

            nu = _A.safe_acos(Vector3.dot(n, r) / (n_mag * r_mag))
            if r.z < 0.0:
                nu = _C.TWO_PI - nu

        else:
            # ---- Circular equatorial: neither node nor periapsis --------------
            # Only one angle survives: the "true longitude" λ, from X to r.
            raan = 0.0
            arg_peri = 0.0

            nu = _A.safe_acos(r.x / r_mag)
            if r.y < 0.0:
                nu = _C.TWO_PI - nu
            if h.z < 0.0:
                nu = _C.TWO_PI - nu

        return OrbitalElements(
            a,
            e,
            i,
            _A.normalize(raan),
            _A.normalize(arg_peri),
            _A.normalize(nu),
        )

    @staticmethod
    def to_state_vector(
        elements: OrbitalElements,
        mu: float = _C.MU_EARTH,
    ) -> StateVector:
        """
        **Classical orbital elements → state vector** (the "coe2rv" algorithm).

        Args:
            elements: The six classical elements (angles in radians).
            mu:       Gravitational parameter of the central body [km³/s²].

        Returns:
            Inertial position [km] and velocity [km/s].

        Raises:
            ValueError: For a parabolic orbit (a = ∞) or invalid p.

        Two stages:

        1. Build **r** and **v** in the **perifocal (PQW) frame**, whose P axis
           points at periapsis and whose W axis is along **h**::

               r_pqw = (p / (1 + e cos ν)) · [cos ν, sin ν, 0]
               v_pqw = √(μ/p)              · [−sin ν, e + cos ν, 0]

        2. Rotate into the inertial frame with the classical 3-1-3 Euler
           sequence R3(−Ω) · R1(−i) · R3(−ω).  The composite matrix is written
           out explicitly below to avoid three separate matrix multiplications.
        """
        _G.positive(mu, "mu")

        e = elements.eccentricity
        _G.non_negative(e, "eccentricity")

        # p = a(1 - e²).  Correct for ellipses AND hyperbolas (a < 0 flips signs).
        p = elements.semi_latus_rectum_km
        if math.isinf(elements.semi_major_axis_km):
            raise ValueError(
                "For a parabolic orbit, specify the semi-latus rectum directly "
                "rather than a = infinity."
            )
        _G.positive(p, "semi-latus rectum p = a(1 - e^2)")

        nu = elements.true_anomaly_rad
        cos_nu = math.cos(nu)
        sin_nu = math.sin(nu)

        # ---- Stage 1: perifocal frame ------------------------------------------
        r_mag = p / (1.0 + e * cos_nu)
        r_pqw = Vector3(r_mag * cos_nu, r_mag * sin_nu, 0.0)

        sqrt_mu_over_p = math.sqrt(mu / p)
        v_pqw = Vector3(-sqrt_mu_over_p * sin_nu, sqrt_mu_over_p * (e + cos_nu), 0.0)

        # ---- Stage 2: rotate PQW → IJK -----------------------------------------
        cos_o = math.cos(elements.raan_rad)
        sin_o = math.sin(elements.raan_rad)
        cos_w = math.cos(elements.argument_of_periapsis_rad)
        sin_w = math.sin(elements.argument_of_periapsis_rad)
        cos_i = math.cos(elements.inclination_rad)
        sin_i = math.sin(elements.inclination_rad)

        # Rows of the composite rotation matrix R = R3(-Ω) R1(-i) R3(-ω).
        row0 = Vector3(
            cos_o * cos_w - sin_o * sin_w * cos_i,
            -cos_o * sin_w - sin_o * cos_w * cos_i,
            sin_o * sin_i,
        )
        row1 = Vector3(
            sin_o * cos_w + cos_o * sin_w * cos_i,
            -sin_o * sin_w + cos_o * cos_w * cos_i,
            -cos_o * sin_i,
        )
        row2 = Vector3(
            sin_w * sin_i,
            cos_w * sin_i,
            cos_i,
        )

        r = Vector3(
            Vector3.dot(row0, r_pqw),
            Vector3.dot(row1, r_pqw),
            Vector3.dot(row2, r_pqw),
        )
        v = Vector3(
            Vector3.dot(row0, v_pqw),
            Vector3.dot(row1, v_pqw),
            Vector3.dot(row2, v_pqw),
        )

        return StateVector(r, v)
