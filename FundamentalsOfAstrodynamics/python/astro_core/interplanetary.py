"""
interplanetary.py — Chapter 8: Interplanetary Trajectories.
============================================================

Port of ``Astro.Core/Interplanetary.cs``.

Patched conics applied on the scale of the solar system.  The structure mirrors
the lunar case but with three conics instead of two:

1. A departure **hyperbola** relative to the launch planet.
2. A heliocentric **transfer ellipse** between the two planetary orbits.
3. An arrival **hyperbola** relative to the target planet.

The planets' spheres of influence are so tiny compared with the astronomical
unit (Earth's SOI is 0.6% of 1 AU) that the patch points can be treated as
coincident with the planets themselves — a simplification that makes the whole
calculation tractable by hand.

Every routine here assumes **coplanar circular** planetary orbits.  Real
trajectory design replaces the Hohmann ellipse with a Lambert solve on a grid
of departure and arrival dates (a "porkchop plot"), but this model gets the
delta-v budget right to within a few percent and, crucially, explains *why*
the numbers are what they are.
"""

from __future__ import annotations
import math
from dataclasses import dataclass
from . import constants as _C, guard as _G, angles as _A
from .two_body import circular_speed, vis_viva, mean_motion, period as _period


@dataclass(frozen=True, slots=True)
class InterplanetaryTransfer:
    """
    A complete interplanetary Hohmann transfer budget.

    Attributes:
        heliocentric_departure_speed_km_per_sec: Speed required on the
            heliocentric transfer ellipse at departure [km/s].
        departure_v_infinity_km_per_sec:  v_∞ relative to the departure planet
            [km/s].
        departure_c3:                     C3 = v_∞² — the "characteristic energy"
            quoted by launch providers [km²/s²].
        injection_delta_v_km_per_sec:     Δv for trans-planetary injection from
            the departure parking orbit [km/s].
        arrival_v_infinity_km_per_sec:    v_∞ relative to the arrival planet [km/s].
        capture_delta_v_km_per_sec:       Δv to capture into the arrival parking
            orbit [km/s].
        transfer_time_sec:                Heliocentric coast time [s].
        departure_phase_angle_rad:        Required phase angle of the target planet
            at departure [rad].
        synodic_period_sec:               Time between successive launch
            opportunities [s].
    """

    heliocentric_departure_speed_km_per_sec: float
    departure_v_infinity_km_per_sec: float
    departure_c3: float
    injection_delta_v_km_per_sec: float
    arrival_v_infinity_km_per_sec: float
    capture_delta_v_km_per_sec: float
    transfer_time_sec: float
    departure_phase_angle_rad: float
    synodic_period_sec: float

    @property
    def total_delta_v_km_per_sec(self) -> float:
        """Total mission delta-v (injection plus capture) [km/s]."""
        return self.injection_delta_v_km_per_sec + self.capture_delta_v_km_per_sec

    @property
    def transfer_time_days(self) -> float:
        """Heliocentric transfer time in days."""
        return self.transfer_time_sec / _C.SOLAR_DAY_SECONDS

    @property
    def departure_phase_angle_deg(self) -> float:
        """Required departure phase angle in degrees."""
        return self.departure_phase_angle_rad * _C.RAD_TO_DEG

    @property
    def synodic_period_days(self) -> float:
        """Synodic period in days — how long you wait for the next launch window."""
        return self.synodic_period_sec / _C.SOLAR_DAY_SECONDS


class Interplanetary:
    """
    Chapter 8 — Interplanetary Trajectories.

    All methods are static; the class is a namespace, not an object.
    """

    @staticmethod
    def synodic_period(
        period1_sec: float,
        period2_sec: float,
    ) -> float:
        """
        **Synodic period**: the interval between successive identical alignments
        of two planets, and therefore the spacing of launch windows [s].

        Args:
            period1_sec: Orbital period of the first planet [s].
            period2_sec: Orbital period of the second planet [s].

        Returns:
            The synodic period [s], or ``math.inf`` for co-orbital bodies.

        The synodic period follows from::

            1 / T_syn = | 1/T₁ − 1/T₂ |

        For Earth and Mars this gives about 780 days — which is precisely why
        Mars missions launch in clusters roughly every 26 months and why missing
        a window is so expensive.
        """
        _G.positive(period1_sec, "period1_sec")
        _G.positive(period2_sec, "period2_sec")

        difference = abs(1.0 / period1_sec - 1.0 / period2_sec)
        if difference < 1e-20:
            # Co-orbital bodies never change their relative geometry.
            return math.inf

        return 1.0 / difference

    @staticmethod
    def hyperbolic_excess_speed(
        periapsis_speed_km_per_sec: float,
        periapsis_radius_km: float,
        mu: float,
    ) -> float:
        """
        **Hyperbolic excess speed** v_∞: the speed the vehicle retains "at
        infinity" after climbing out of a planet's gravity well [km/s].

        Args:
            periapsis_speed_km_per_sec: Speed at the departure hyperbola's
                                        periapsis [km/s].
            periapsis_radius_km:        Radius of that periapsis [km].
            mu:                         Gravitational parameter of the planet
                                        [km³/s²].

        Returns:
            v_∞ [km/s].

        Raises:
            ValueError: When the vehicle is below escape speed (closed orbit).

        From the energy equation: v_∞² = v_p² − 2μ/r_p.  Note that v_∞ is
        *not* simply v_p minus the escape speed: energies add, speeds do not.
        That fact is exactly the Oberth effect.
        """
        _G.positive(periapsis_radius_km, "periapsis_radius_km")

        v_inf_sq = periapsis_speed_km_per_sec ** 2 - 2.0 * mu / periapsis_radius_km
        if v_inf_sq < 0.0:
            raise ValueError(
                "The vehicle is below escape speed at that radius — it is on a "
                "closed orbit and has no v_∞."
            )
        return math.sqrt(v_inf_sq)

    @staticmethod
    def injection_delta_v(
        hyperbolic_excess_speed_km_per_sec: float,
        parking_orbit_radius_km: float,
        mu: float,
    ) -> float:
        """
        The delta-v needed to depart a circular parking orbit onto a hyperbola
        with the required v_∞ [km/s].

        Args:
            hyperbolic_excess_speed_km_per_sec: v_∞ required [km/s].
            parking_orbit_radius_km:            Radius of the circular parking
                                                orbit [km].
            mu:                                 Gravitational parameter of the
                                                departure planet [km³/s²].

        Returns:
            The injection Δv [km/s].

        The same expression (with opposite sign convention) gives the capture
        burn at arrival::

            Δv = √(v_∞² + 2μ/r_p) − √(μ/r_p)
        """
        _G.non_negative(
            hyperbolic_excess_speed_km_per_sec, "hyperbolic_excess_speed_km_per_sec"
        )
        _G.positive(parking_orbit_radius_km, "parking_orbit_radius_km")

        v_periapsis = math.sqrt(
            hyperbolic_excess_speed_km_per_sec ** 2
            + 2.0 * mu / parking_orbit_radius_km
        )
        return v_periapsis - circular_speed(parking_orbit_radius_km, mu)

    @staticmethod
    def compute_hohmann_transfer(
        departure_orbit_radius_km: float,
        arrival_orbit_radius_km: float,
        departure_planet_mu: float,
        departure_parking_radius_km: float,
        arrival_planet_mu: float,
        arrival_parking_radius_km: float,
        mu_sun: float = _C.MU_SUN,
    ) -> InterplanetaryTransfer:
        """
        Compute a complete planet-to-planet Hohmann transfer budget.

        Args:
            departure_orbit_radius_km:    Heliocentric radius of the departure
                                          planet's orbit [km].
            arrival_orbit_radius_km:      Heliocentric radius of the target
                                          planet's orbit [km].
            departure_planet_mu:          μ of the departure planet [km³/s²].
            departure_parking_radius_km:  Radius of the parking orbit at
                                          departure [km].
            arrival_planet_mu:            μ of the arrival planet [km³/s²].
            arrival_parking_radius_km:    Radius of the parking orbit at arrival
                                          [km].
            mu_sun:                       μ of the Sun [km³/s²].

        Returns:
            :class:`InterplanetaryTransfer` with the full budget.

        **The departure phase angle** is what makes a launch window.  By the
        time the spacecraft reaches the target's orbital radius, the target must
        be there too.  Since the transfer takes half the period of the transfer
        ellipse, the target must lead the departure point by::

            φ = π − n_target · t_transfer

        For Earth to Mars this works out at roughly 44° — Mars must be 44°
        ahead of Earth at launch.
        """
        _G.positive(departure_orbit_radius_km, "departure_orbit_radius_km")
        _G.positive(arrival_orbit_radius_km, "arrival_orbit_radius_km")

        # ---- The heliocentric transfer ellipse --------------------------------
        a_transfer = (departure_orbit_radius_km + arrival_orbit_radius_km) / 2.0

        v_depart_planet_helio = circular_speed(departure_orbit_radius_km, mu_sun)
        v_arrive_planet_helio = circular_speed(arrival_orbit_radius_km, mu_sun)

        v_transfer_at_departure = vis_viva(departure_orbit_radius_km, a_transfer, mu_sun)
        v_transfer_at_arrival = vis_viva(arrival_orbit_radius_km, a_transfer, mu_sun)

        # v_∞ is the *difference* between the spacecraft's heliocentric speed and
        # the planet's own heliocentric speed — i.e. the excess from the launcher.
        v_inf_departure = abs(v_transfer_at_departure - v_depart_planet_helio)
        v_inf_arrival = abs(v_arrive_planet_helio - v_transfer_at_arrival)

        # ---- Planetocentric hyperbolas (departure and arrival) -----------------
        injection_dv = Interplanetary.injection_delta_v(
            v_inf_departure, departure_parking_radius_km, departure_planet_mu
        )
        capture_dv = Interplanetary.injection_delta_v(
            v_inf_arrival, arrival_parking_radius_km, arrival_planet_mu
        )

        # ---- Timing -----------------------------------------------------------
        transfer_time = math.pi * math.sqrt(a_transfer ** 3 / mu_sun)

        n_target = mean_motion(arrival_orbit_radius_km, mu_sun)
        phase_angle = _A.normalize_signed(math.pi - n_target * transfer_time)

        synodic = Interplanetary.synodic_period(
            _period(departure_orbit_radius_km, mu_sun),
            _period(arrival_orbit_radius_km, mu_sun),
        )

        return InterplanetaryTransfer(
            v_transfer_at_departure,
            v_inf_departure,
            v_inf_departure ** 2,
            injection_dv,
            v_inf_arrival,
            capture_dv,
            transfer_time,
            phase_angle,
            synodic,
        )

    @staticmethod
    def flyby_turning_angle(eccentricity: float) -> float:
        """
        The turning angle of a hyperbolic flyby ("gravity assist"), δ [rad].

        Args:
            eccentricity: Eccentricity of the flyby hyperbola (> 1).

        Returns:
            δ [rad].

        Raises:
            ValueError: When e ≤ 1 (not a hyperbola).

        The flyby rotates the v_∞ vector by δ without changing its magnitude
        — in the planet's frame.  In the *Sun's* frame that rotation adds
        vectorially to the planet's orbital velocity, which is how a spacecraft
        steals energy from a planet.  Voyager 2 gained roughly 10 km/s at
        Jupiter this way::

            sin(δ / 2) = 1 / e
        """
        if eccentricity <= 1.0:
            raise ValueError(
                f"A flyby requires a hyperbolic trajectory (e > 1); got {eccentricity!r}."
            )
        return 2.0 * math.asin(1.0 / eccentricity)

    @staticmethod
    def gravity_assist_delta_v(
        hyperbolic_excess_speed_km_per_sec: float,
        turning_angle_rad: float,
    ) -> float:
        """
        The maximum heliocentric speed change obtainable from a single gravity
        assist [km/s].

        Args:
            hyperbolic_excess_speed_km_per_sec: v_∞ relative to the planet [km/s].
            turning_angle_rad:                  The achieved turning angle δ [rad].

        Returns:
            The magnitude of the velocity change [km/s].

        The v_∞ vector is rotated, so the change in heliocentric velocity has
        magnitude 2 v_∞ sin(δ/2) — structurally identical to the plane-change
        formula, except that here the manoeuvre is *free*.
        """
        return 2.0 * hyperbolic_excess_speed_km_per_sec * math.sin(turning_angle_rad / 2.0)
