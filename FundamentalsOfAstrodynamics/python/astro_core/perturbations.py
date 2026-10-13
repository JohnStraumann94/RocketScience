"""
perturbations.py — Chapter 9: Secular J2 Perturbation Effects.
==============================================================

Port of ``Astro.Core/Perturbations.cs``.

Everything up to this point assumed a perfectly spherical central body.  The
Earth is not spherical: centrifugal flattening makes the equatorial radius about
21 km larger than the polar one.  That equatorial bulge exerts a torque on an
inclined orbit, and the orbit responds like a gyroscope — it precesses.

The bulge is captured by the coefficient **J2** ≈ 1.0826 × 10⁻³.  Every other
gravitational harmonic is at least a thousand times smaller, so J2 alone accounts
for the overwhelming majority of the deviation from Keplerian motion in LEO.

J2 produces two **secular** effects — drifts that accumulate without bound rather
than oscillating — on the node and on perigee.  Both are calculated below.  Far
from being a nuisance, they are exploited: sun-synchronous orbits and Molniya
orbits are both direct engineering applications of these two formulas.
"""

from __future__ import annotations
import math
from . import constants as _C, guard as _G, angles as _A
from .two_body import mean_motion
from .kepler_solver import KeplerSolver


class Perturbations:
    """
    Chapter 9 — Secular J2 perturbation effects.

    All methods are static; the class is a namespace, not an object.
    """

    # ------------------------------------------------------------------
    # Critical inclination constant
    # ------------------------------------------------------------------

    @staticmethod
    def critical_inclination_rad() -> float:
        """
        The **critical inclination** at which apsidal rotation vanishes:
        i = arccos(√(1/5)) ≈ 63.4349° [rad].

        This special inclination zeroes the bracket (5 cos²i − 1) in the
        apsidal rotation formula.  Russian Molniya satellites fly at exactly
        this inclination so that their high apogee stays parked over the
        northern hemisphere for the satellite's lifetime.
        """
        return math.acos(math.sqrt(0.2))

    # ------------------------------------------------------------------
    # Drift rates
    # ------------------------------------------------------------------

    @staticmethod
    def nodal_regression_rate(
        semi_major_axis_km: float,
        eccentricity: float,
        inclination_rad: float,
        j2: float = _C.J2_EARTH,
        body_radius_km: float = _C.EARTH_RADIUS_KM,
        mu: float = _C.MU_EARTH,
    ) -> float:
        """
        **Nodal regression**: the secular drift rate of the right ascension of
        the ascending node, dΩ/dt [rad/s].

        Args:
            semi_major_axis_km: a — semi-major axis [km].
            eccentricity:       e — eccentricity (< 1).
            inclination_rad:    i — inclination [rad].
            j2:                 Oblateness coefficient (defaults to Earth's J2).
            body_radius_km:     Equatorial radius of the central body [km].
            mu:                 Gravitational parameter [km³/s²].

        Returns:
            dΩ/dt [rad/s].  Negative means westward (prograde orbit); positive
            means eastward (retrograde orbit).

        The formula is::

            dΩ/dt = −(3/2) n J2 (Rₑ/p)² cos(i)

        with p = a(1 − e²) and n the mean motion.  Note the consequences of
        cos(i):

        * Prograde orbit (i < 90°): cos i > 0 ⟹ node drifts **westward**.
          A typical LEO regresses several degrees per day.
        * Polar orbit (i = 90°): cos i = 0 ⟹ node is **stationary**.
        * Retrograde orbit (i > 90°): cos i < 0 ⟹ node drifts **eastward**.
          Tune the inclination so the drift matches the Earth's motion around
          the Sun — that is a sun-synchronous orbit.
        """
        _G.positive(semi_major_axis_km, "semi_major_axis_km")
        _G.in_range(eccentricity, 0.0, 1.0 - 1e-12, "eccentricity")

        n = mean_motion(semi_major_axis_km, mu)
        p = semi_major_axis_km * (1.0 - eccentricity ** 2)
        factor = body_radius_km / p

        return -1.5 * n * j2 * factor ** 2 * math.cos(inclination_rad)

    @staticmethod
    def apsidal_rotation_rate(
        semi_major_axis_km: float,
        eccentricity: float,
        inclination_rad: float,
        j2: float = _C.J2_EARTH,
        body_radius_km: float = _C.EARTH_RADIUS_KM,
        mu: float = _C.MU_EARTH,
    ) -> float:
        """
        **Apsidal rotation**: the secular drift rate of the argument of perigee,
        dω/dt [rad/s].

        Args:
            semi_major_axis_km: a — semi-major axis [km].
            eccentricity:       e — eccentricity (< 1).
            inclination_rad:    i — inclination [rad].
            j2:                 Oblateness coefficient.
            body_radius_km:     Equatorial radius [km].
            mu:                 Gravitational parameter [km³/s²].

        Returns:
            dω/dt [rad/s].

        The formula is::

            dω/dt = (3/4) n J2 (Rₑ/p)² (5 cos²i − 1)

        The bracket changes sign at cos²i = 1/5, i.e. i = 63.435° (or its
        retrograde mirror 116.565°).  At that **critical inclination** perigee
        does not move at all — the Molniya trick.
        """
        _G.positive(semi_major_axis_km, "semi_major_axis_km")
        _G.in_range(eccentricity, 0.0, 1.0 - 1e-12, "eccentricity")

        n = mean_motion(semi_major_axis_km, mu)
        p = semi_major_axis_km * (1.0 - eccentricity ** 2)
        factor = body_radius_km / p
        cos_i = math.cos(inclination_rad)

        return 0.75 * n * j2 * factor ** 2 * (5.0 * cos_i ** 2 - 1.0)

    @staticmethod
    def sun_synchronous_inclination(
        semi_major_axis_km: float,
        eccentricity: float = 0.0,
        j2: float = _C.J2_EARTH,
        body_radius_km: float = _C.EARTH_RADIUS_KM,
        mu: float = _C.MU_EARTH,
    ) -> float:
        """
        The inclination required for a **sun-synchronous** orbit [rad].

        Args:
            semi_major_axis_km: a — semi-major axis [km].
            eccentricity:       e — eccentricity (usually 0 for SSO).
            j2:                 Oblateness coefficient.
            body_radius_km:     Equatorial radius [km].
            mu:                 Gravitational parameter [km³/s²].

        Returns:
            The required inclination [rad], which is always retrograde (> 90°).

        Raises:
            ValueError: When no sun-synchronous inclination exists for the
                        given altitude (orbit too high for J2 to supply the
                        required precession rate).

        A sun-synchronous orbit precesses its node eastward at exactly the rate
        the Earth orbits the Sun: 360° per tropical year, or ≈1.99106×10⁻⁷
        rad/s.  The orbital plane then holds a constant angle to the Sun, so the
        satellite crosses the equator at the same local solar time on every pass.

        Setting the nodal regression formula equal to that target rate and
        solving for i::

            cos(i) = − requiredRate / (1.5 n J2 (Rₑ/p)²)

        For a typical 800 km circular orbit the answer is about 98.6°.
        """
        _G.positive(semi_major_axis_km, "semi_major_axis_km")

        # Required precession rate: one revolution per tropical year (365.2422 days).
        tropical_year_sec = 365.2422 * _C.SOLAR_DAY_SECONDS
        required_rate = _C.TWO_PI / tropical_year_sec   # rad/s, positive (eastward)

        n = mean_motion(semi_major_axis_km, mu)
        p = semi_major_axis_km * (1.0 - eccentricity ** 2)
        factor = body_radius_km / p

        # Solve: required_rate = -1.5 n J2 (Rₑ/p)² cos(i)  ⟹  cos(i) = ...
        cos_i = -required_rate / (1.5 * n * j2 * factor ** 2)

        if abs(cos_i) > 1.0:
            raise ValueError(
                f"No sun-synchronous inclination exists for a = {semi_major_axis_km:.1f} km; "
                "the orbit is too high for J2 to supply the required precession rate."
            )

        return math.acos(cos_i)

    @staticmethod
    def propagate_with_j2(
        elements,
        delta_time_sec: float,
        j2: float = _C.J2_EARTH,
        body_radius_km: float = _C.EARTH_RADIUS_KM,
        mu: float = _C.MU_EARTH,
    ):
        """
        Propagate the node and perigee of an element set forward in time under
        secular J2 drift only, leaving a, e and i unchanged.

        Args:
            elements:        Initial osculating :class:`~orbital_elements.OrbitalElements`.
            delta_time_sec:  Elapsed time [s].
            j2:              Oblateness coefficient.
            body_radius_km:  Equatorial radius [km].
            mu:              Gravitational parameter [km³/s²].

        Returns:
            Elements with Ω, ω and ν all advanced.

        This is the classic "J2 mean element" propagator.  It is correct only for
        the secular terms: the short-period oscillations (of order J2 times the
        orbit radius, so tens of kilometres) are deliberately ignored, which is
        exactly the approximation that makes two-line element sets useful.
        """
        raan_rate = Perturbations.nodal_regression_rate(
            elements.semi_major_axis_km,
            elements.eccentricity,
            elements.inclination_rad,
            j2,
            body_radius_km,
            mu,
        )
        arg_perigee_rate = Perturbations.apsidal_rotation_rate(
            elements.semi_major_axis_km,
            elements.eccentricity,
            elements.inclination_rad,
            j2,
            body_radius_km,
            mu,
        )

        # Advance the in-plane position with the ordinary Keplerian propagator …
        from .orbital_elements import OrbitalElements
        advanced = KeplerSolver.propagate(elements, delta_time_sec, mu)

        # … then rotate the orbit plane and the line of apsides on top of it.
        return OrbitalElements(
            advanced.semi_major_axis_km,
            advanced.eccentricity,
            advanced.inclination_rad,
            _A.normalize(elements.raan_rad + raan_rate * delta_time_sec),
            _A.normalize(elements.argument_of_periapsis_rad + arg_perigee_rate * delta_time_sec),
            advanced.true_anomaly_rad,
        )
