"""
kepler_solver.py — Chapter 4: Kepler's equation and anomaly conversions.
========================================================================

Port of ``Astro.Core/KeplerSolver.cs``.

The orbit equation gives r as a function of the true anomaly ν, but ν does
not advance uniformly — a satellite races through periapsis and crawls through
apoapsis.  Kepler solved this by introducing two auxiliary angles:

* **Eccentric anomaly E** — a geometric construction that projects the
  ellipse onto its circumscribing circle.

* **Mean anomaly M** — a fictitious angle that *does* advance uniformly:
  M = n(t − t_p), where n is the mean motion and t_p the time of periapsis
  passage.

They are tied together by **Kepler's equation**: M = E − e sin E.  Going from
E to M is trivial.  Going the other way is not: the equation is transcendental
and has no closed-form solution, which is why the iterative solver below
exists and why it has occupied numerical analysts since 1650.
"""

from __future__ import annotations
import math
from dataclasses import dataclass
from . import constants as _C, guard as _G, angles as _A
from .two_body import mean_motion as _mean_motion


DEFAULT_TOLERANCE: float = 1e-12
"""Default convergence tolerance on the Kepler residual [rad]."""

DEFAULT_MAX_ITERATIONS: int = 100
"""Default iteration cap; Newton's method normally converges in 3–6 steps."""


@dataclass(frozen=True, slots=True)
class KeplerSolution:
    """
    The outcome of a Kepler-equation solve, including diagnostics.

    Attributes:
        anomaly_rad:     The eccentric anomaly E (elliptic) or H (hyperbolic)
                         [rad].
        iterations:      How many Newton steps were actually taken.
        final_residual:  |M_computed − M_target| at the returned solution [rad].
    """

    anomaly_rad: float
    iterations: int
    final_residual: float


class KeplerSolver:
    """
    Chapter 4 — Kepler's equation and anomaly/time conversions.

    All methods are static; the class is a namespace, not an object.
    """

    # ------------------------------------------------------------------
    # Kepler's equation solvers
    # ------------------------------------------------------------------

    @staticmethod
    def solve_eccentric_anomaly(
        mean_anomaly_rad: float,
        eccentricity: float,
        tolerance: float = DEFAULT_TOLERANCE,
        max_iterations: int = DEFAULT_MAX_ITERATIONS,
    ) -> KeplerSolution:
        """
        Solve **M = E − e sin E** for the eccentric anomaly E.

        Args:
            mean_anomaly_rad: M — mean anomaly [rad].  Any value; it is
                              wrapped to [0, 2π) internally.
            eccentricity:     e — eccentricity, 0 ≤ e < 1.
            tolerance:        Absolute convergence tolerance on the residual [rad].
            max_iterations:   Safety cap on the iteration count.

        Returns:
            :class:`KeplerSolution` containing E and convergence diagnostics.

        Raises:
            ValueError:        When e is outside [0, 1).
            RuntimeError:      When the solver does not converge.

        **Method: Newton–Raphson.**  Define f(E) = E − e sin E − M.  Then
        f′(E) = 1 − e cos E, and the update is::

            E_{k+1} = E_k − f(E_k) / f′(E_k)
                    = E_k + (M − E_k + e sin E_k) / (1 − e cos E_k)

        **Why the starting guess matters.**  f′(E) = 1 − e cos E approaches
        zero near periapsis of a highly eccentric orbit, so a careless initial
        guess can send the first Newton step wildly off.  The classic remedy,
        used here, is due to Smith: start from E₀ = M for moderate
        eccentricity, but from E₀ = π (the far side) when e > 0.8, so that
        the iteration approaches the root from the well-conditioned direction.
        """
        _G.in_range(eccentricity, 0.0, 1.0 - 1e-12, "eccentricity")

        # Wrap M into [0, 2π) to make the starting guess sensible.
        m = _A.normalize(mean_anomaly_rad)

        # Smith's starting guess (see docstring above).
        e_anom = m if eccentricity < 0.8 else math.pi

        iteration = 0
        residual = float("inf")

        while iteration < max_iterations:
            # Kepler residual and its derivative.
            f = e_anom - eccentricity * math.sin(e_anom) - m
            f_prime = 1.0 - eccentricity * math.cos(e_anom)

            residual = abs(f)
            if residual < tolerance:
                break

            # f_prime can only vanish if e == 1, which the guard above already
            # excluded, but a defensive check costs nothing.
            if abs(f_prime) < 1e-15:
                raise RuntimeError(
                    "Kepler solver stalled: derivative vanished "
                    "(eccentricity too close to 1)."
                )

            e_anom -= f / f_prime
            iteration += 1

        if residual > tolerance:
            raise RuntimeError(
                f"Kepler's equation failed to converge in {max_iterations} "
                f"iterations (residual {residual:.3E})."
            )

        return KeplerSolution(_A.normalize(e_anom), iteration, residual)

    @staticmethod
    def solve_hyperbolic_anomaly(
        mean_anomaly_rad: float,
        eccentricity: float,
        tolerance: float = DEFAULT_TOLERANCE,
        max_iterations: int = DEFAULT_MAX_ITERATIONS,
    ) -> KeplerSolution:
        """
        Solve the **hyperbolic** Kepler equation M = e sinh H − H for H.

        Args:
            mean_anomaly_rad: M — hyperbolic mean anomaly [rad].  NOT wrapped,
                              since a hyperbola never repeats.
            eccentricity:     e — eccentricity, must be > 1.
            tolerance:        Absolute convergence tolerance.
            max_iterations:   Safety cap.

        Returns:
            :class:`KeplerSolution` containing H and convergence diagnostics.

        Raises:
            ValueError:   When e ≤ 1.
            RuntimeError: When the solver does not converge.

        Same Newton scheme, with f(H) = e sinh H − H − M and
        f′(H) = e cosh H − 1.  The starting guess uses the asymptotic inverse
        of sinh: H₀ ≈ asinh(M/e) — sinh grows exponentially, so starting at M
        itself would overshoot catastrophically for large M.
        """
        if eccentricity <= 1.0:
            raise ValueError(
                f"The hyperbolic Kepler equation requires e > 1; got {eccentricity!r}."
            )

        # Asymptotic starting guess: for large |M|, e sinh H dominates ⟹ H ~ asinh(M/e).
        h = math.asinh(mean_anomaly_rad / eccentricity)

        iteration = 0
        residual = float("inf")

        while iteration < max_iterations:
            f = eccentricity * math.sinh(h) - h - mean_anomaly_rad
            f_prime = eccentricity * math.cosh(h) - 1.0

            residual = abs(f)
            if residual < tolerance:
                break

            h -= f / f_prime
            iteration += 1

        if residual > tolerance:
            raise RuntimeError(
                f"Hyperbolic Kepler equation failed to converge in "
                f"{max_iterations} iterations (residual {residual:.3E})."
            )

        return KeplerSolution(h, iteration, residual)

    # ------------------------------------------------------------------
    # Anomaly conversions — elliptical orbits
    # ------------------------------------------------------------------

    @staticmethod
    def eccentric_to_true_anomaly(
        eccentric_anomaly_rad: float,
        eccentricity: float,
    ) -> float:
        """
        Eccentric anomaly E → true anomaly ν, via the half-angle relation::

            tan(ν/2) = √((1+e)/(1−e)) · tan(E/2)

        Args:
            eccentric_anomaly_rad: E [rad].
            eccentricity:          e, 0 ≤ e < 1.

        Returns:
            ν in [0, 2π).

        The half-angle form is used rather than
        ``cos ν = (cos E − e)/(1 − e cos E)`` because ``math.atan2`` resolves
        the quadrant automatically, removing the need for an error-prone sign
        test.
        """
        _G.in_range(eccentricity, 0.0, 1.0 - 1e-12, "eccentricity")
        factor = math.sqrt((1.0 + eccentricity) / (1.0 - eccentricity))
        nu = 2.0 * math.atan2(
            factor * math.sin(eccentric_anomaly_rad / 2.0),
            math.cos(eccentric_anomaly_rad / 2.0),
        )
        return _A.normalize(nu)

    @staticmethod
    def true_to_eccentric_anomaly(
        true_anomaly_rad: float,
        eccentricity: float,
    ) -> float:
        """
        True anomaly ν → eccentric anomaly E (inverse of
        :meth:`eccentric_to_true_anomaly`).

        Args:
            true_anomaly_rad: ν [rad].
            eccentricity:     e, 0 ≤ e < 1.

        Returns:
            E in [0, 2π).
        """
        _G.in_range(eccentricity, 0.0, 1.0 - 1e-12, "eccentricity")
        factor = math.sqrt((1.0 - eccentricity) / (1.0 + eccentricity))
        e = 2.0 * math.atan2(
            factor * math.sin(true_anomaly_rad / 2.0),
            math.cos(true_anomaly_rad / 2.0),
        )
        return _A.normalize(e)

    @staticmethod
    def eccentric_to_mean_anomaly(
        eccentric_anomaly_rad: float,
        eccentricity: float,
    ) -> float:
        """
        Eccentric anomaly E → mean anomaly M via Kepler's equation (forward
        direction, trivial).

        Args:
            eccentric_anomaly_rad: E [rad].
            eccentricity:          e.

        Returns:
            M in [0, 2π).
        """
        return _A.normalize(
            eccentric_anomaly_rad - eccentricity * math.sin(eccentric_anomaly_rad)
        )

    @staticmethod
    def true_to_mean_anomaly(
        true_anomaly_rad: float,
        eccentricity: float,
    ) -> float:
        """
        True anomaly ν → mean anomaly M (composition of the two conversions
        above).

        Args:
            true_anomaly_rad: ν [rad].
            eccentricity:     e, 0 ≤ e < 1.

        Returns:
            M in [0, 2π).
        """
        return KeplerSolver.eccentric_to_mean_anomaly(
            KeplerSolver.true_to_eccentric_anomaly(true_anomaly_rad, eccentricity),
            eccentricity,
        )

    @staticmethod
    def mean_to_true_anomaly(
        mean_anomaly_rad: float,
        eccentricity: float,
    ) -> float:
        """
        Mean anomaly M → true anomaly ν.  Solves Kepler's equation, then
        converts.

        This is the operation you need to answer "where will the satellite be
        in 40 minutes?".

        Args:
            mean_anomaly_rad: M [rad].
            eccentricity:     e, 0 ≤ e < 1.

        Returns:
            ν in [0, 2π).
        """
        solution = KeplerSolver.solve_eccentric_anomaly(mean_anomaly_rad, eccentricity)
        return KeplerSolver.eccentric_to_true_anomaly(solution.anomaly_rad, eccentricity)

    # ------------------------------------------------------------------
    # Anomaly conversions — hyperbolic orbits
    # ------------------------------------------------------------------

    @staticmethod
    def hyperbolic_to_true_anomaly(
        hyperbolic_anomaly_rad: float,
        eccentricity: float,
    ) -> float:
        """
        Hyperbolic anomaly H → true anomaly ν, via::

            tan(ν/2) = √((e+1)/(e−1)) · tanh(H/2)

        Args:
            hyperbolic_anomaly_rad: H [rad].
            eccentricity:           e > 1.

        Returns:
            ν [rad].
        """
        if eccentricity <= 1.0:
            raise ValueError(f"Requires e > 1; got {eccentricity!r}.")
        factor = math.sqrt((eccentricity + 1.0) / (eccentricity - 1.0))
        return 2.0 * math.atan(factor * math.tanh(hyperbolic_anomaly_rad / 2.0))

    @staticmethod
    def true_to_hyperbolic_anomaly(
        true_anomaly_rad: float,
        eccentricity: float,
    ) -> float:
        """
        True anomaly ν → hyperbolic anomaly H.

        Args:
            true_anomaly_rad: ν [rad].
            eccentricity:     e > 1.

        Returns:
            H [rad].
        """
        if eccentricity <= 1.0:
            raise ValueError(f"Requires e > 1; got {eccentricity!r}.")
        factor = math.sqrt((eccentricity - 1.0) / (eccentricity + 1.0))
        return 2.0 * math.atanh(factor * math.tan(true_anomaly_rad / 2.0))

    # ------------------------------------------------------------------
    # Time ↔ anomaly
    # ------------------------------------------------------------------

    @staticmethod
    def time_since_periapsis(
        true_anomaly_rad: float,
        eccentricity: float,
        semi_major_axis_km: float,
        mu: float = _C.MU_EARTH,
    ) -> float:
        """
        Time since periapsis passage for a given true anomaly on an elliptical
        orbit [s].

        Formula: t − t_p = M / n, with M from Kepler's equation and
        n = √(μ/a³).

        Args:
            true_anomaly_rad:   ν [rad].
            eccentricity:       e, 0 ≤ e < 1.
            semi_major_axis_km: a [km].
            mu:                 Gravitational parameter [km³/s²].

        Returns:
            Time since periapsis [s].
        """
        m = KeplerSolver.true_to_mean_anomaly(true_anomaly_rad, eccentricity)
        n = _mean_motion(semi_major_axis_km, mu)
        return m / n

    @staticmethod
    def true_anomaly_after_time(
        time_since_periapsis_sec: float,
        eccentricity: float,
        semi_major_axis_km: float,
        mu: float = _C.MU_EARTH,
    ) -> float:
        """
        True anomaly reached a given time after periapsis passage [rad].

        Args:
            time_since_periapsis_sec: t − t_p [s].
            eccentricity:             e, 0 ≤ e < 1.
            semi_major_axis_km:       a [km].
            mu:                       Gravitational parameter [km³/s²].

        Returns:
            ν in [0, 2π).
        """
        n = _mean_motion(semi_major_axis_km, mu)
        m = n * time_since_periapsis_sec
        return KeplerSolver.mean_to_true_anomaly(m, eccentricity)

    @staticmethod
    def propagate(
        elements,
        delta_time_sec: float,
        mu: float = _C.MU_EARTH,
    ):
        """
        Propagate a full element set forward (or backward) in time by advancing
        only the true anomaly.

        This is legitimate because, in the unperturbed two-body problem, the
        other five elements are constant.

        Args:
            elements:        Starting :class:`~orbital_elements.OrbitalElements`.
            delta_time_sec:  Elapsed time [s]; may be negative.
            mu:              Gravitational parameter [km³/s²].

        Returns:
            Elements at the new epoch.

        Raises:
            NotImplementedError: For open orbits (e ≥ 1); use
                :meth:`~universal_variables.UniversalVariables.propagate`
                instead.
        """
        if elements.eccentricity >= 1.0:
            raise NotImplementedError(
                "Use UniversalVariables.propagate for open "
                "(parabolic/hyperbolic) trajectories."
            )
        n = _mean_motion(elements.semi_major_axis_km, mu)
        m0 = KeplerSolver.true_to_mean_anomaly(
            elements.true_anomaly_rad, elements.eccentricity
        )
        m1 = m0 + n * delta_time_sec   # M advances linearly — that is the whole point
        nu1 = KeplerSolver.mean_to_true_anomaly(m1, elements.eccentricity)
        return elements.at_true_anomaly(nu1)
