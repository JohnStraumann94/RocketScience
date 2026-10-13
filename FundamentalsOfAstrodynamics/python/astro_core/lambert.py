"""
lambert.py — Chapter 5: Lambert's problem.
==========================================

Port of ``Astro.Core/Lambert.cs``.

**The question.**  Given a departure point r₁, an arrival point r₂, and the
time of flight between them, what orbit connects the two?  Equivalently: what
velocity must I have at r₁ to arrive at r₂ exactly dt seconds later?

**Why it matters.**  Lambert's problem is the targeting engine of spaceflight.
Every interplanetary porkchop plot is thousands of Lambert solutions on a grid
of departure and arrival dates.  Every rendezvous burn, intercept, and
trajectory correction is posed as a Lambert problem.

**Lambert's theorem** (1761) states that the transfer time depends only on the
semi-major axis, the chord length between the two points, and the sum r₁ + r₂
— remarkably, not on the shape of the arc in any other way.

The implementation below is the universal-variable formulation with bisection
on ψ (Bate/Mueller/White Chapter 5, refined by Vallado).  Bisection is used
in preference to Newton's method because the time-of-flight function is
monotonic in ψ but has an awkward derivative near the parabolic boundary;
bisection is slower but cannot diverge.
"""

from __future__ import annotations
import math
from dataclasses import dataclass
from enum import Enum, auto
from .vector3 import Vector3
from .universal_variables import stumpff_c, stumpff_s
from . import constants as _C, guard as _G


class TransferDirection(Enum):
    """Which way round the central body the transfer arc travels."""

    SHORT_WAY = auto()
    """Swept angle < 180°.  Prograde in the sense that the arc follows r₁ × r₂."""

    LONG_WAY = auto()
    """Swept angle > 180°.  Occasionally cheaper; sometimes the only option."""


@dataclass(frozen=True, slots=True)
class LambertSolution:
    """
    The solution of Lambert's problem.

    Attributes:
        departure_velocity_km_per_sec: v₁ — the velocity required at r₁ [km/s].
        arrival_velocity_km_per_sec:   v₂ — the velocity at arrival at r₂ [km/s].
        iterations:                    Number of bisection steps used.
    """

    departure_velocity_km_per_sec: Vector3
    arrival_velocity_km_per_sec: Vector3
    iterations: int


class Lambert:
    """
    Chapter 5 — Orbit Determination from Two Positions and Time (Lambert's
    problem).

    All methods are static.
    """

    @staticmethod
    def solve(
        position1_km: Vector3,
        position2_km: Vector3,
        time_of_flight_sec: float,
        direction: TransferDirection = TransferDirection.SHORT_WAY,
        mu: float = _C.MU_EARTH,
        tolerance: float = 1e-6,
        max_iterations: int = 200,
    ) -> LambertSolution:
        """
        Solve Lambert's problem for a single-revolution transfer.

        Args:
            position1_km:        r₁ — departure position vector [km].
            position2_km:        r₂ — arrival position vector [km].
            time_of_flight_sec:  dt — time of flight [s]; must be positive.
            direction:           Short way (< 180°) or long way (> 180°).
            mu:                  Gravitational parameter [km³/s²].
            tolerance:           Convergence tolerance on the time of flight [s].
            max_iterations:      Bisection iteration cap.

        Returns:
            :class:`LambertSolution` with the departure and arrival velocities.

        Raises:
            ValueError:   When the geometry is degenerate (r₁ and r₂ collinear).
            RuntimeError: When the bisection does not converge.

        **Algorithm.** The geometry constant A combines the chord and radii::

            A = sin(Δν) √(r₁ r₂ / (1 − cos(Δν)))

        Then we bisect on ψ = χ²/a.  For each trial ψ the Stumpff functions
        give an effective chord y, which gives χ, which gives the implied
        time of flight.  The TOF is monotonic in ψ, so bisection always
        converges.

        Finally the Lagrange coefficients f, g, ġ recover the velocities::

            v₁ = (r₂ − f r₁) / g
            v₂ = (ġ r₂ − r₁) / g
        """
        _G.positive(time_of_flight_sec, "time_of_flight_sec")
        _G.positive(mu, "mu")

        r1 = position1_km.magnitude
        r2 = position2_km.magnitude
        _G.positive(r1, "position1_km")
        _G.positive(r2, "position2_km")

        # Swept angle between the two position vectors.
        cos_delta_nu = max(-1.0, min(1.0,
            Vector3.dot(position1_km, position2_km) / (r1 * r2)
        ))
        delta_nu = math.acos(cos_delta_nu)

        # The long-way solution sweeps the reflex angle instead.
        if direction == TransferDirection.LONG_WAY:
            delta_nu = _C.TWO_PI - delta_nu

        # 'A' is the geometry constant that couples the two ends of the arc.
        sin_delta_nu = math.sin(delta_nu)
        a = sin_delta_nu * math.sqrt(r1 * r2 / (1.0 - math.cos(delta_nu)))

        if math.isnan(a) or abs(a) < 1e-12:
            raise ValueError(
                "Degenerate Lambert geometry: the position vectors are collinear "
                "(swept angle of 0 or 180 degrees), so the transfer plane is "
                "undefined.  Perturb one of the positions slightly."
            )

        # ---- Bisection bracket on ψ = χ²/a ----
        # Upper bound 4π² corresponds to one full revolution; the lower bound
        # is pushed down as needed to keep y positive.
        psi = 0.0
        psi_upper = 4.0 * math.pi ** 2
        psi_lower = -4.0 * math.pi

        c2 = 0.5
        c3 = 1.0 / 6.0
        y = 0.0

        for iteration in range(max_iterations):
            c2 = stumpff_c(psi)
            c3 = stumpff_s(psi)

            # y is the "effective chord" term; must stay positive for √y to be real.
            y = r1 + r2 + a * (psi * c3 - 1.0) / math.sqrt(c2)

            # If A > 0 the geometry can still drive y negative for very small ψ.
            # Raise the lower bracket until y becomes positive again.
            if a > 0.0 and y < 0.0:
                for _ in range(100):
                    psi_lower += 0.1
                    psi = psi_lower
                    c2 = stumpff_c(psi)
                    c3 = stumpff_s(psi)
                    y = r1 + r2 + a * (psi * c3 - 1.0) / math.sqrt(c2)
                    if y >= 0.0:
                        break

            chi = math.sqrt(y / c2)

            # Time of flight implied by the current ψ.
            tof_computed = (chi ** 3 * c3 + a * math.sqrt(y)) / math.sqrt(mu)

            if abs(tof_computed - time_of_flight_sec) < tolerance:
                break

            # TOF is monotonically increasing in ψ ⟹ plain bisection.
            if tof_computed <= time_of_flight_sec:
                psi_lower = psi
            else:
                psi_upper = psi

            psi = (psi_upper + psi_lower) / 2.0
        else:
            raise RuntimeError(
                f"Lambert solver failed to converge in {max_iterations} "
                "iterations.  The requested time of flight may be physically "
                "impossible for this geometry."
            )

        # ---- Recover velocities from Lagrange coefficients ----
        f = 1.0 - y / r1
        g = a * math.sqrt(y / mu)
        g_dot = 1.0 - y / r2

        v1 = (position2_km - f * position1_km) / g
        v2 = (g_dot * position2_km - position1_km) / g

        return LambertSolution(v1, v2, iteration)
