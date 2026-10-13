"""
ballistic_missile.py — Chapter 6: Ballistic Missile Trajectories.
==================================================================

Port of ``Astro.Core/BallisticMissile.cs``.

An ICBM flight has three phases: a powered **boost** phase, a **free-flight**
phase that is pure two-body Keplerian motion, and a **re-entry** phase dominated
by drag.  Only the middle phase is analytic, and it is by far the longest, so the
standard treatment is to characterise everything by the state at *burnout*: the
radius r_bo, the speed v_bo and the flight-path angle φ_bo.

The key insight is that a ballistic missile trajectory is simply an **elliptical
orbit whose perigee happens to lie inside the Earth**.  All the machinery of
Chapters 1–4 applies unchanged; the only new element is the "free-flight range
equation" that relates the burnout conditions to the ground range achieved.
"""

from __future__ import annotations
import math
from dataclasses import dataclass
from . import constants as _C, guard as _G, angles as _A
from .two_body import specific_energy, mean_motion, periapsis_radius
from .kepler_solver import KeplerSolver


@dataclass(frozen=True, slots=True)
class BallisticTrajectory:
    """
    The complete free-flight solution for a ballistic missile trajectory.

    Attributes:
        q_parameter:              Q = v_bo² r_bo / μ — dimensionless "energy"
                                  parameter at burnout.  Q = 1 (horizontal) is
                                  a circular orbit; Q = 2 is escape speed.
        eccentricity:             e — eccentricity of the free-flight ellipse.
        semi_major_axis_km:       a — semi-major axis [km].
        free_flight_range_angle_rad: Ψ — geocentric sweep from burnout to
                                  re-entry [rad].
        surface_range_km:         Ground range during free flight [km].
        maximum_altitude_km:      Apogee altitude above the surface [km].
        time_of_flight_sec:       Free-flight duration burnout → re-entry [s].
    """

    q_parameter: float
    eccentricity: float
    semi_major_axis_km: float
    free_flight_range_angle_rad: float
    surface_range_km: float
    maximum_altitude_km: float
    time_of_flight_sec: float

    @property
    def free_flight_range_angle_deg(self) -> float:
        """Ψ expressed in degrees, for display."""
        return self.free_flight_range_angle_rad * _C.RAD_TO_DEG

    @property
    def time_of_flight_minutes(self) -> float:
        """Free-flight duration in minutes, for display."""
        return self.time_of_flight_sec / 60.0


class BallisticMissile:
    """
    Chapter 6 — Ballistic Missile Trajectories.

    All methods are static; the class is a namespace, not an object.
    """

    @staticmethod
    def compute(
        burnout_radius_km: float,
        burnout_speed_km_per_sec: float,
        burnout_flight_path_angle_rad: float,
        mu: float = _C.MU_EARTH,
        planet_radius_km: float = _C.EARTH_RADIUS_KM,
    ) -> BallisticTrajectory:
        """
        Compute the complete free-flight trajectory from burnout conditions.

        Args:
            burnout_radius_km:              r_bo — geocentric radius at burnout [km]
                                            (Earth radius + burnout altitude).
            burnout_speed_km_per_sec:       v_bo — inertial speed at burnout [km/s].
            burnout_flight_path_angle_rad:  φ_bo — flight-path angle at burnout
                                            [rad], measured from the local horizontal.
            mu:                             Gravitational parameter [km³/s²].
            planet_radius_km:               Surface radius [km], used for the
                                            ground-range conversion.

        Returns:
            :class:`BallisticTrajectory` with all derived quantities.

        Raises:
            ValueError: When the burnout speed equals or exceeds escape speed
                        (Q ≥ 2 ⟹ this is a spacecraft, not a missile).

        **The free-flight range equation** (BMW eq. 6.2-4)::

            cos(Ψ/2) = (1 − Q cos²φ) / √(1 + Q(Q − 2) cos²φ)

        where Q = v_bo² r_bo / μ.

        **Symmetry assumption.**  Burnout and re-entry occur at the same radius,
        making the trajectory symmetric about apogee.  This is the standard
        textbook idealisation and is accurate to a few percent for real systems.
        """
        _G.positive(burnout_radius_km, "burnout_radius_km")
        _G.positive(burnout_speed_km_per_sec, "burnout_speed_km_per_sec")

        # Q = v_bo² r_bo / μ — the dimensionless energy parameter.
        q = burnout_speed_km_per_sec ** 2 * burnout_radius_km / mu

        if q >= 2.0:
            raise ValueError(
                f"Q = {q:.4f} >= 2 means the vehicle has reached escape speed — "
                "this is no longer a ballistic trajectory."
            )

        cos_phi = math.cos(burnout_flight_path_angle_rad)
        cos_phi_sq = cos_phi ** 2

        # Eccentricity of the free-flight ellipse (BMW eq. 6.2-3).
        e = math.sqrt(1.0 + q * (q - 2.0) * cos_phi_sq)

        # Semi-major axis from the energy.
        energy = specific_energy(burnout_radius_km, burnout_speed_km_per_sec, mu)
        a = -mu / (2.0 * energy)

        # ---- Free-flight range equation -----------------------------------
        numerator = 1.0 - q * cos_phi_sq
        denominator = math.sqrt(1.0 + q * (q - 2.0) * cos_phi_sq)
        half_psi = _A.safe_acos(numerator / denominator)
        psi = 2.0 * half_psi

        # Ground range is the sweep angle times the planet's radius (great-circle arc).
        surface_range = psi * planet_radius_km

        # Apogee of the ellipse, converted to an altitude above the surface.
        apogee_radius = a * (1.0 + e)
        maximum_altitude = apogee_radius - planet_radius_km

        # ---- Time of free flight ------------------------------------------
        # True anomaly at burnout (BMW eq. 6.3-1).  The trajectory is symmetric
        # about apogee, so re-entry occurs at ν = 2π − ν_bo.
        nu_burnout = math.atan2(
            q * cos_phi * math.sin(burnout_flight_path_angle_rad),
            q * cos_phi_sq - 1.0,
        )
        nu_burnout = _A.normalize(nu_burnout)

        nu_reentry = _C.TWO_PI - nu_burnout

        m_burnout = KeplerSolver.true_to_mean_anomaly(nu_burnout, e)
        m_reentry = KeplerSolver.true_to_mean_anomaly(nu_reentry, e)
        n = mean_motion(a, mu)

        time_of_flight = (m_reentry - m_burnout) / n
        if time_of_flight < 0.0:
            # Wrap-around safety net: add one full period if the anomalies
            # straddled 2π.
            time_of_flight += _C.TWO_PI / n

        return BallisticTrajectory(
            q, e, a, psi, surface_range, maximum_altitude, time_of_flight
        )

    @staticmethod
    def optimal_flight_path_angle(
        free_flight_range_angle_rad: float,
    ) -> float:
        """
        The burnout flight-path angle that maximises ground range for a given
        burnout speed: φ_optimal = (π − Ψ) / 4.

        Args:
            free_flight_range_angle_rad: Ψ — the desired free-flight range angle
                                         [rad].

        Returns:
            The optimal flight-path angle [rad].

        A beautifully simple result.  For a short-range shot (Ψ → 0) it reduces
        to 45° — the familiar flat-Earth artillery answer.  As the range grows
        towards half the globe (Ψ → π) the optimal angle falls towards zero:
        fire almost horizontally and let the ellipse do the work.

        This is also the origin of the "high road / low road" pairing: for any
        range shorter than the maximum, two flight-path angles produce the same
        range — one lofted and one depressed — bracketing the optimum.
        """
        return (math.pi - free_flight_range_angle_rad) / 4.0

    @staticmethod
    def minimum_burnout_speed(
        free_flight_range_angle_rad: float,
        burnout_radius_km: float,
        mu: float = _C.MU_EARTH,
    ) -> float:
        """
        The minimum burnout speed capable of achieving a given free-flight range
        angle, flown at the optimal flight-path angle [km/s].

        Args:
            free_flight_range_angle_rad: Ψ — required range angle [rad].
            burnout_radius_km:           r_bo — burnout radius [km].
            mu:                          Gravitational parameter [km³/s²].

        Returns:
            Minimum burnout speed [km/s].

        Obtained by substituting the optimal angle into the range equation and
        solving for Q::

            Q_min = 2 sin(Ψ/2) / (1 + sin(Ψ/2))
            v = √(Q_min μ / r)
        """
        _G.positive(burnout_radius_km, "burnout_radius_km")
        sin_half_psi = math.sin(free_flight_range_angle_rad / 2.0)
        q_min = 2.0 * sin_half_psi / (1.0 + sin_half_psi)
        return math.sqrt(q_min * mu / burnout_radius_km)
