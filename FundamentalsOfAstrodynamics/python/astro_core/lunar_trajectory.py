"""
lunar_trajectory.py — Chapter 7: Lunar Trajectories by Patched Conics.
=======================================================================

Port of ``Astro.Core/LunarTrajectory.cs``.

The Earth–Moon–spacecraft system is a three-body problem with no closed-form
solution.  The **patched-conic approximation** divides space into regions where
a single body dominates, solves a simple two-body problem in each, and stitches
("patches") the answers together at the boundary:

1. Outside the Moon's **sphere of influence** (SOI), ignore the Moon: the
   vehicle flies a geocentric ellipse.
2. At the SOI boundary, subtract the Moon's orbital velocity to convert the
   state into the Moon-centred frame.
3. Inside the SOI, ignore the Earth: the vehicle flies a selenocentric
   hyperbola.

The result is typically accurate to a fraction of a percent in delta-v — good
enough for mission design, after which a numerically integrated trajectory takes
over.  Every Apollo mission was first sketched exactly this way.
"""

from __future__ import annotations
import math
from dataclasses import dataclass
from . import constants as _C, guard as _G
from .two_body import vis_viva, circular_speed


@dataclass(frozen=True, slots=True)
class GeocentricLeg:
    """
    The result of the geocentric (phase 1) leg of a patched-conic lunar
    transfer.

    Attributes:
        injection_speed_km_per_sec:    v₀ — speed required at trans-lunar
                                       injection [km/s].
        injection_delta_v_km_per_sec:  Delta-v from the circular parking orbit
                                       [km/s].
        transfer_semi_major_axis_km:   a — semi-major axis of the geocentric
                                       transfer ellipse [km].
        transfer_eccentricity:         e — eccentricity of the transfer ellipse.
        arrival_speed_km_per_sec:      v₁ — geocentric speed on reaching the
                                       arrival radius [km/s].
        time_of_flight_sec:            Coast time from injection to arrival [s].
    """

    injection_speed_km_per_sec: float
    injection_delta_v_km_per_sec: float
    transfer_semi_major_axis_km: float
    transfer_eccentricity: float
    arrival_speed_km_per_sec: float
    time_of_flight_sec: float

    @property
    def time_of_flight_days(self) -> float:
        """Time of flight in days, the usual unit for lunar transfers."""
        return self.time_of_flight_sec / _C.SOLAR_DAY_SECONDS


class LunarTrajectory:
    """
    Chapter 7 — Lunar Trajectories (patched-conic method).

    All methods are static; the class is a namespace, not an object.
    """

    @staticmethod
    def sphere_of_influence_radius(
        orbital_radius_km: float,
        smaller_body_mu: float,
        larger_body_mu: float,
    ) -> float:
        """
        Radius of the **sphere of influence** of a smaller body orbiting a
        larger one: r_SOI = R (m_small / m_large)^(2/5)  [km].

        Args:
            orbital_radius_km:  R — distance between the two bodies [km].
            smaller_body_mu:    μ of the smaller body [km³/s²].
            larger_body_mu:     μ of the larger body [km³/s²].

        Returns:
            The SOI radius [km].

        **Laplace's definition.**  The surface on which the perturbing
        acceleration of the distant body, relative to the near body's own
        attraction, is equal in the two possible framings of the problem.  The
        2/5 exponent is what falls out of that comparison.

        Because μ is proportional to mass, the mass ratio can be replaced by
        the μ ratio directly — which is convenient, since μ values are far
        better determined than masses.

        For the Earth–Moon system this yields about 66,200 km, roughly one
        sixth of the Earth–Moon distance.
        """
        _G.positive(orbital_radius_km, "orbital_radius_km")
        _G.positive(smaller_body_mu, "smaller_body_mu")
        _G.positive(larger_body_mu, "larger_body_mu")
        return orbital_radius_km * (smaller_body_mu / larger_body_mu) ** 0.4

    @staticmethod
    def moon_sphere_of_influence_km() -> float:
        """
        The radius of the Moon's sphere of influence [km] — approximately
        66,183 km.

        Computed from Laplace's formula with the IAU constants.
        """
        return LunarTrajectory.sphere_of_influence_radius(
            _C.EARTH_MOON_DISTANCE_KM,
            _C.MU_MOON,
            _C.MU_EARTH,
        )

    @staticmethod
    def compute_geocentric_leg(
        parking_orbit_radius_km: float,
        arrival_radius_km: float,
        mu: float = _C.MU_EARTH,
    ) -> GeocentricLeg:
        """
        Compute the geocentric leg of a simplified patched-conic lunar transfer.

        This is a Hohmann-like ellipse from the circular parking orbit out to
        the Moon's SOI arrival radius.

        Args:
            parking_orbit_radius_km: Radius of the circular Earth parking orbit
                                     [km].
            arrival_radius_km:       Geocentric radius at which the vehicle meets
                                     the Moon's SOI [km].  For the simplest model
                                     use Earth–Moon distance minus the SOI radius.
            mu:                      Earth's gravitational parameter [km³/s²].

        Returns:
            :class:`GeocentricLeg` with injection Δv and coast time.

        Raises:
            ValueError: When arrival_radius_km ≤ parking_orbit_radius_km.

        This deliberately assumes the arrival point lies on the transfer
        ellipse's line of apsides (pure Hohmann geometry).  Real lunar
        transfers arrive with a "sweep angle" λ that must be chosen to match
        the Moon's motion; that refinement turns this into a targeting problem.
        """
        _G.positive(parking_orbit_radius_km, "parking_orbit_radius_km")
        _G.positive(arrival_radius_km, "arrival_radius_km")

        if arrival_radius_km <= parking_orbit_radius_km:
            raise ValueError(
                "The arrival radius must be greater than the parking-orbit radius."
            )

        # Transfer ellipse with perigee at the parking orbit and apogee at arrival.
        a_transfer = (parking_orbit_radius_km + arrival_radius_km) / 2.0
        e_transfer = (arrival_radius_km - parking_orbit_radius_km) / (
            arrival_radius_km + parking_orbit_radius_km
        )

        injection_speed = vis_viva(parking_orbit_radius_km, a_transfer, mu)
        parking_speed = circular_speed(parking_orbit_radius_km, mu)
        injection_delta_v = injection_speed - parking_speed

        arrival_speed = vis_viva(arrival_radius_km, a_transfer, mu)

        # Half the period of the transfer ellipse (perigee to apogee).
        time_of_flight = math.pi * math.sqrt(a_transfer ** 3 / mu)

        return GeocentricLeg(
            injection_speed,
            injection_delta_v,
            a_transfer,
            e_transfer,
            arrival_speed,
            time_of_flight,
        )

    @staticmethod
    def lunar_orbit_insertion_delta_v(
        hyperbolic_excess_speed_km_per_sec: float,
        periapsis_radius_km: float,
        mu_moon: float = _C.MU_MOON,
    ) -> float:
        """
        The delta-v required to brake from a hyperbolic arrival at the Moon
        into a circular lunar orbit ("lunar orbit insertion", LOI) [km/s].

        Args:
            hyperbolic_excess_speed_km_per_sec: v_∞ — the selenocentric speed
                                                on entering the SOI [km/s].
            periapsis_radius_km:                Radius of the target circular
                                                lunar orbit [km].
            mu_moon:                            The Moon's gravitational
                                                parameter [km³/s²].

        Returns:
            The LOI delta-v [km/s].

        Energy is conserved on the incoming hyperbola, so the speed at
        periapsis follows from vis-viva with v_∞² = μ(−1/a)::

            v_periapsis = √(v_∞² + 2μ/r_p)
            ΔvLOI       = v_periapsis − v_circular(r_p)

        This is the **Oberth effect** in action, and it explains why every
        orbit-insertion burn is performed at periapsis: the deeper you dive
        before burning, the larger v_periapsis, and the smaller the fraction
        of it you must shed.
        """
        _G.non_negative(
            hyperbolic_excess_speed_km_per_sec, "hyperbolic_excess_speed_km_per_sec"
        )
        _G.positive(periapsis_radius_km, "periapsis_radius_km")

        v_periapsis = math.sqrt(
            hyperbolic_excess_speed_km_per_sec ** 2
            + 2.0 * mu_moon / periapsis_radius_km
        )
        v_circular = circular_speed(periapsis_radius_km, mu_moon)
        return v_periapsis - v_circular
