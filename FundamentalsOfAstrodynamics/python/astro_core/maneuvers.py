"""
maneuvers.py — Chapter 3: Basic Orbital Manoeuvres.
====================================================

Port of ``Astro.Core/Maneuvers.cs``.

Every routine here assumes the **impulsive approximation**: the engine fires
for an interval so short that the spacecraft's position does not change during
the burn — only its velocity does.  Real chemical burns last minutes rather
than instants, which incurs a "gravity loss" of a few percent, but the
impulsive model is accurate enough for mission design and is what makes
closed-form answers possible at all.

**Why delta-v is the currency of spaceflight.**  Delta-v is independent of
the vehicle: it is a property of the trajectory.  Convert it to propellant
mass only at the end, via Tsiolkovsky's rocket equation.
"""

from __future__ import annotations
import math
from dataclasses import dataclass, field
from typing import List
from . import constants as _C, guard as _G, angles as _A
from .two_body import (
    circular_speed, vis_viva, period, mean_motion
)


# Break-even radius ratio at which bi-elliptic beats Hohmann (r_b → ∞).
# This is the root of a transcendental equation; the numerical value appears
# in every astrodynamics textbook without further comment.
BI_ELLIPTIC_BREAK_EVEN_RATIO: float = 11.93876
"""
The radius ratio r₂/r₁ above which a bi-elliptic transfer (with a
sufficiently large intermediate apoapsis) is cheaper than Hohmann.
"""


@dataclass(frozen=True)
class TransferResult:
    """
    The result of a multi-burn orbital transfer.

    Attributes:
        burns_km_per_sec:   Individual impulsive velocity changes [km/s], in
                            execution order.  Negative values are retrograde.
        transfer_time_sec:  Total coast time from the first burn to the last [s].
        description:        A human-readable narrative of the manoeuvre.
    """

    burns_km_per_sec: List[float]
    transfer_time_sec: float
    description: str

    @property
    def total_delta_v_km_per_sec(self) -> float:
        """
        Total delta-v — the sum of the *magnitudes* of the individual burns [km/s].

        Magnitudes, not a vector sum: every burn costs propellant regardless of
        direction, and it is this scalar total that feeds the rocket equation.
        """
        return sum(abs(dv) for dv in self.burns_km_per_sec)

    @property
    def transfer_time_hours(self) -> float:
        """Transfer time expressed in hours, for readability."""
        return self.transfer_time_sec / 3600.0


class Maneuvers:
    """
    Chapter 3 — Basic Orbital Manoeuvres.

    All methods are static; the class is a namespace, not an object.
    """

    # ------------------------------------------------------------------
    # Hohmann transfer
    # ------------------------------------------------------------------

    @staticmethod
    def hohmann_transfer(
        initial_radius_km: float,
        final_radius_km: float,
        mu: float = _C.MU_EARTH,
    ) -> TransferResult:
        """
        The **Hohmann transfer**: the classic two-burn minimum-energy transfer
        between coplanar circular orbits, using an ellipse tangent to both.

        Args:
            initial_radius_km: r₁ — radius of the starting circular orbit [km].
            final_radius_km:   r₂ — radius of the target circular orbit [km].
            mu:                Gravitational parameter [km³/s²].

        Returns:
            :class:`TransferResult` with two burns and the coast time.

        The transfer ellipse has its periapsis on the inner circle and its
        apoapsis on the outer one, so its semi-major axis is simply
        a_t = (r₁ + r₂)/2.  Both burns are purely tangential — no direction
        change — which is exactly why it is efficient::

            Δv₁ = v_transfer_periapsis − v_circular(r₁)   (raise apoapsis to r₂)
            Δv₂ = v_circular(r₂) − v_transfer_apoapsis    (circularise at r₂)
            t   = T_transfer / 2 = π √(aₜ³ / μ)

        **Optimality caveat.**  Hohmann is minimum-Δv for two impulses only when
        the radius ratio r₂/r₁ is below about 11.94.  Above that the three-burn
        bi-elliptic transfer wins — see :meth:`bi_elliptic_transfer`.

        The routine works equally well for lowering an orbit (r₂ < r₁); both
        burns simply come out negative (retrograde).
        """
        _G.positive(initial_radius_km, "initial_radius_km")
        _G.positive(final_radius_km, "final_radius_km")

        # Semi-major axis of the transfer ellipse spans from r₁ to r₂.
        a_transfer = (initial_radius_km + final_radius_km) / 2.0

        # Circular speeds on the two parking orbits.
        v1_circular = circular_speed(initial_radius_km, mu)
        v2_circular = circular_speed(final_radius_km, mu)

        # Speeds on the transfer ellipse at each end, from vis-viva.
        v_transfer_at_r1 = vis_viva(initial_radius_km, a_transfer, mu)
        v_transfer_at_r2 = vis_viva(final_radius_km, a_transfer, mu)

        delta_v1 = v_transfer_at_r1 - v1_circular   # departure burn
        delta_v2 = v2_circular - v_transfer_at_r2   # arrival / circularisation burn

        # Half the period of the transfer ellipse: you fly exactly one half revolution.
        transfer_time = math.pi * math.sqrt(a_transfer ** 3 / mu)

        direction = "raising" if final_radius_km > initial_radius_km else "lowering"
        ecc = abs(final_radius_km - initial_radius_km) / (final_radius_km + initial_radius_km)
        description = (
            f"Hohmann transfer {direction} the orbit from r = {initial_radius_km:.1f} km "
            f"to r = {final_radius_km:.1f} km.  "
            f"Transfer ellipse: a = {a_transfer:.1f} km, e = {ecc:.5f}."
        )

        return TransferResult([delta_v1, delta_v2], transfer_time, description)

    # ------------------------------------------------------------------
    # Bi-elliptic transfer
    # ------------------------------------------------------------------

    @staticmethod
    def bi_elliptic_transfer(
        initial_radius_km: float,
        intermediate_radius_km: float,
        final_radius_km: float,
        mu: float = _C.MU_EARTH,
    ) -> TransferResult:
        """
        The **bi-elliptic transfer**: a three-burn transfer that first flings
        the vehicle out to a high intermediate apoapsis r_b, changes the orbit
        cheaply out there, then drops back down to the target radius.

        Args:
            initial_radius_km:      r₁ — starting circular radius [km].
            intermediate_radius_km: r_b — intermediate apoapsis radius [km].
                                    Must exceed both r₁ and r₂.
            final_radius_km:        r₂ — target circular radius [km].
            mu:                     Gravitational parameter [km³/s²].

        Returns:
            :class:`TransferResult` with three burns and the coast time.

        Raises:
            ValueError: When r_b ≤ max(r₁, r₂).

        **Counter-intuitive insight.**  Going *further* can cost *less*.  The
        reason is the Oberth effect running in reverse: at a very high apoapsis
        the orbital speed is tiny, so the burn that changes the orbit's shape
        is correspondingly cheap.  The penalty is time.

        Rules of thumb for R = r₂/r₁:

        * R < 11.94 ⟹ Hohmann always wins
        * R > 15.58 ⟹ bi-elliptic always wins (for large enough r_b)
        * In between it depends on how far out you are willing to go
        """
        _G.positive(initial_radius_km, "initial_radius_km")
        _G.positive(intermediate_radius_km, "intermediate_radius_km")
        _G.positive(final_radius_km, "final_radius_km")

        if intermediate_radius_km <= max(initial_radius_km, final_radius_km):
            raise ValueError(
                "The intermediate apoapsis must be larger than both the initial "
                "and the final radius, otherwise the manoeuvre is not bi-elliptic."
            )

        # First transfer ellipse: periapsis r₁, apoapsis r_b.
        a1 = (initial_radius_km + intermediate_radius_km) / 2.0

        # Second transfer ellipse: apoapsis r_b, periapsis r₂.
        a2 = (intermediate_radius_km + final_radius_km) / 2.0

        # Burn 1 at r₁: leave the circular orbit onto the first ellipse.
        delta_v1 = vis_viva(initial_radius_km, a1, mu) - circular_speed(initial_radius_km, mu)

        # Burn 2 at r_b: the cheap one, done where the vehicle is moving slowest.
        delta_v2 = vis_viva(intermediate_radius_km, a2, mu) - vis_viva(intermediate_radius_km, a1, mu)

        # Burn 3 at r₂: circularise.
        delta_v3 = circular_speed(final_radius_km, mu) - vis_viva(final_radius_km, a2, mu)

        # Two half-ellipses, so half of each period.
        transfer_time = (
            math.pi * math.sqrt(a1 ** 3 / mu)
            + math.pi * math.sqrt(a2 ** 3 / mu)
        )

        description = (
            f"Bi-elliptic transfer r1 = {initial_radius_km:.1f} km -> "
            f"rb = {intermediate_radius_km:.1f} km -> "
            f"r2 = {final_radius_km:.1f} km.  "
            f"Radius ratio r2/r1 = {final_radius_km / initial_radius_km:.2f}."
        )

        return TransferResult([delta_v1, delta_v2, delta_v3], transfer_time, description)

    # ------------------------------------------------------------------
    # Simple plane change
    # ------------------------------------------------------------------

    @staticmethod
    def simple_plane_change(
        orbital_speed_km_per_sec: float,
        plane_change_rad: float,
    ) -> float:
        """
        A **simple plane change**: rotate the orbital plane by
        *plane_change_rad* without altering the orbit's size or shape.

        Args:
            orbital_speed_km_per_sec: v — the speed at the burn point [km/s].
            plane_change_rad:         The angle between the old and new orbital
                                      planes [rad].

        Returns:
            The required delta-v [km/s].

        Since the speed is unchanged, the velocity vectors form an isosceles
        triangle::

            Δv = 2 v sin(Δi / 2)

        **This is brutally expensive.**  In LEO (v ≈ 7.7 km/s) even a 30°
        plane change costs ≈4 km/s — more than the entire Hohmann transfer to
        GEO.  Always perform plane changes where v is small (at apoapsis) or
        fold them into another burn with :meth:`combined_maneuver`.
        """
        _G.positive(orbital_speed_km_per_sec, "orbital_speed_km_per_sec")
        return 2.0 * orbital_speed_km_per_sec * math.sin(abs(plane_change_rad) / 2.0)

    # ------------------------------------------------------------------
    # Combined manoeuvre
    # ------------------------------------------------------------------

    @staticmethod
    def combined_maneuver(
        initial_speed_km_per_sec: float,
        final_speed_km_per_sec: float,
        plane_change_rad: float,
    ) -> float:
        """
        A **combined manoeuvre**: change speed and plane in a single burn.

        Args:
            initial_speed_km_per_sec: v_i — speed before the burn [km/s].
            final_speed_km_per_sec:   v_f — desired speed after the burn [km/s].
            plane_change_rad:         Angle between the two velocity vectors [rad].

        Returns:
            The required delta-v [km/s].

        Uses the law of cosines on the velocity triangle::

            Δv = √(v_i² + v_f² − 2 v_i v_f cos(Δi))

        Always cheaper than doing the two operations separately, because vector
        addition beats scalar addition.  This is precisely how a geostationary
        transfer orbit's apogee kick motor works: it circularises AND removes
        the launch-site inclination in one burn.
        """
        _G.positive(initial_speed_km_per_sec, "initial_speed_km_per_sec")
        _G.positive(final_speed_km_per_sec, "final_speed_km_per_sec")

        dv2 = (
            initial_speed_km_per_sec ** 2
            + final_speed_km_per_sec ** 2
            - 2.0
            * initial_speed_km_per_sec
            * final_speed_km_per_sec
            * math.cos(plane_change_rad)
        )
        # Rounding can drive a near-zero result microscopically negative.
        return math.sqrt(max(dv2, 0.0))

    # ------------------------------------------------------------------
    # Phasing manoeuvre
    # ------------------------------------------------------------------

    @staticmethod
    def phasing_maneuver(
        orbit_radius_km: float,
        phase_angle_rad: float,
        number_of_revolutions: int = 1,
        mu: float = _C.MU_EARTH,
    ) -> TransferResult:
        """
        A **phasing manoeuvre**: change a spacecraft's position along its own
        circular orbit without changing the orbit permanently.

        Args:
            orbit_radius_km:       Radius of the circular orbit [km].
            phase_angle_rad:       The angle to make up [rad].  Positive means
                                   the chaser must *gain* on a target that is
                                   ahead of it.
            number_of_revolutions: How many revolutions of the phasing orbit to
                                   spread the change over.
            mu:                    Gravitational parameter [km³/s²].

        Returns:
            :class:`TransferResult` with two equal-and-opposite burns.

        Raises:
            ValueError: When *number_of_revolutions* < 1 or the phase angle
                        is too large for the chosen number of revolutions.

        **The great paradox of orbital mechanics: to catch up, slow down.**
        Braking drops you into a smaller, faster-period orbit; after one lap
        you arrive back at the same point earlier than the target, having
        gained on it.  Two burns of equal magnitude are needed — one to enter
        the phasing orbit and one to leave it.
        """
        _G.positive(orbit_radius_km, "orbit_radius_km")
        if number_of_revolutions < 1:
            raise ValueError(
                f"At least one revolution is required; got {number_of_revolutions}."
            )

        T = period(orbit_radius_km, mu)

        # To gain a lead angle of φ, the phasing orbit's period must be
        # SHORTER by the time the target needs to cover φ:
        # ΔT = φ / n, spread over k revolutions.
        n = mean_motion(orbit_radius_km, mu)
        # normalize_signed returns the "shortest" angle, handling the ±360° wrap.
        phasing_period = T - _A.normalize_signed(phase_angle_rad) / (n * number_of_revolutions)

        if phasing_period <= 0.0:
            raise ValueError(
                "The requested phase change is too large for the given number "
                "of revolutions."
            )

        # Invert Kepler's third law: a = (μ (T/2π)²)^(1/3).
        a_phasing = (mu * (phasing_period / _C.TWO_PI) ** 2) ** (1.0 / 3.0)

        v_circular = circular_speed(orbit_radius_km, mu)
        v_phasing = vis_viva(orbit_radius_km, a_phasing, mu)

        delta_v = v_phasing - v_circular   # burn 1: enter phasing orbit
        # Burn 2 is exactly the reverse, restoring the original circular orbit.

        description = (
            f"Phasing manoeuvre to change position by "
            f"{_A.to_degrees(phase_angle_rad):.2f} deg "
            f"over {number_of_revolutions} revolution(s).  "
            f"Phasing orbit a = {a_phasing:.1f} km "
            f"(period {phasing_period / 60.0:.2f} min vs {T / 60.0:.2f} min)."
        )

        return TransferResult(
            [delta_v, -delta_v],
            phasing_period * number_of_revolutions,
            description,
        )
