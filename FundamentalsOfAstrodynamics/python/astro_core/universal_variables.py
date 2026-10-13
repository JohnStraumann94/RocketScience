"""
universal_variables.py — Chapter 4 (advanced): Universal-variable propagation.
===============================================================================

Port of ``Astro.Core/UniversalVariables.cs``.

The classical approach needs a different equation for each conic: Kepler's
equation for ellipses, Barker's for parabolas, the hyperbolic Kepler equation
for hyperbolas.  Worse, all three degrade badly near e = 1, which is exactly
where interesting missions live (escape trajectories, transfer arcs, comet
orbits).

Sundman's **universal anomaly** χ removes the problem entirely.  By changing
the independent variable via dt = (r / √μ) dχ, a *single* equation covers
every conic section continuously, with no special cases and no loss of accuracy
at e = 1::

    √μ dt = χ³ C(ψ) + (r₀·v₀/√μ) χ² S(ψ) + r₀ χ (1 − ψ C(ψ))

where ψ = χ²/a and C, S are the Stumpff functions.  This is the formulation
used by essentially every production astrodynamics package, and it is what
makes :mod:`lambert` possible.
"""

import math
from .vector3 import Vector3
from .orbit_conversions import StateVector
from . import constants as _C, guard as _G


def stumpff_c(psi: float) -> float:
    """
    **Stumpff function C(ψ)**, defined by the entire series
    C(ψ) = 1/2! − ψ/4! + ψ²/6! − …

    Args:
        psi: The argument ψ = χ²/a.

    Returns:
        C(ψ).

    Closed forms exist for each sign of ψ:

    * ψ > 0 (ellipse):   C = (1 − cos √ψ) / ψ
    * ψ < 0 (hyperbola): C = (cosh √(−ψ) − 1) / (−ψ)
    * ψ = 0 (parabola):  C = 1/2

    Both closed forms are 0/0 indeterminate at ψ = 0, so near the origin we
    fall back on the truncated power series.  Without that guard the parabolic
    case — precisely the one universal variables were invented to handle —
    would produce NaN.
    """
    near_zero = 1e-6

    if psi > near_zero:
        sqrt_psi = math.sqrt(psi)
        return (1.0 - math.cos(sqrt_psi)) / psi

    if psi < -near_zero:
        sqrt_neg_psi = math.sqrt(-psi)
        return (math.cosh(sqrt_neg_psi) - 1.0) / (-psi)

    # Series expansion around ψ = 0: 1/2 - ψ/24 + ψ²/720 - …
    return 0.5 - psi / 24.0 + psi * psi / 720.0


def stumpff_s(psi: float) -> float:
    """
    **Stumpff function S(ψ)**, defined by S(ψ) = 1/3! − ψ/5! + ψ²/7! − …

    Args:
        psi: The argument ψ = χ²/a.

    Returns:
        S(ψ).

    * ψ > 0: S = (√ψ − sin √ψ) / ψ^(3/2)
    * ψ < 0: S = (sinh √(−ψ) − √(−ψ)) / (−ψ)^(3/2)
    * ψ = 0: S = 1/6

    The same near-zero series guard applies, and here it matters even more: for
    small ψ the numerator is the difference of two nearly equal quantities, so
    the closed form suffers catastrophic cancellation well before ψ actually
    reaches zero.
    """
    near_zero = 1e-6

    if psi > near_zero:
        sqrt_psi = math.sqrt(psi)
        return (sqrt_psi - math.sin(sqrt_psi)) / psi ** 1.5

    if psi < -near_zero:
        sqrt_neg_psi = math.sqrt(-psi)
        return (math.sinh(sqrt_neg_psi) - sqrt_neg_psi) / (-psi) ** 1.5

    # Series expansion around ψ = 0: 1/6 - ψ/120 + ψ²/5040 - …
    return 1.0 / 6.0 - psi / 120.0 + psi * psi / 5040.0


class UniversalVariables:
    """
    Chapter 4 (advanced) — Universal-variable propagation.

    All methods are static; the class is a namespace, not an object.
    """

    @staticmethod
    def propagate(
        initial: StateVector,
        delta_time_sec: float,
        mu: float = _C.MU_EARTH,
        tolerance: float = 1e-10,
        max_iterations: int = 200,
    ) -> StateVector:
        """
        Propagate a state vector forward (or backward) in time by solving the
        universal Kepler equation — valid for *every* conic section.

        Args:
            initial:         The state at the initial epoch.
            delta_time_sec:  Elapsed time [s]; negative propagates into the past.
            mu:              Gravitational parameter [km³/s²].
            tolerance:       Convergence tolerance on the universal anomaly.
            max_iterations:  Iteration cap.

        Returns:
            The :class:`~orbit_conversions.StateVector` at the new epoch.

        Raises:
            RuntimeError: When the iteration does not converge.

        After χ is found, the new state is reconstructed with the **Lagrange f
        and g coefficients**::

            r = f r₀ + g v₀
            v = ḟ r₀ + ġ v₀

        These express the new state as a linear combination of the old position
        and velocity — which is possible because the motion is planar, so r₀
        and v₀ span the whole orbit plane.
        """
        _G.positive(mu, "mu")

        r0 = initial.position_km
        v0 = initial.velocity_km_per_sec

        r0_mag = r0.magnitude
        v0_mag = v0.magnitude
        sqrt_mu = math.sqrt(mu)

        # α = 1/a, obtained from the energy.  Its sign is the conic classifier:
        # α > 0 ⟹ ellipse, α = 0 ⟹ parabola, α < 0 ⟹ hyperbola.
        alpha = 2.0 / r0_mag - v0_mag * v0_mag / mu

        r_dot_v = Vector3.dot(r0, v0)

        # ---- Initial guess for χ, chosen by conic type (Vallado, Algorithm 8) ----
        if alpha > 1e-12:
            # Elliptical: χ advances roughly linearly with time.
            chi = sqrt_mu * delta_time_sec * alpha
        elif abs(alpha) <= 1e-12:
            # Parabolic: solve Barker's equation for the starting value.
            h = Vector3.cross(r0, v0)
            p = h.magnitude_squared / mu
            s = 0.5 * math.atan(
                1.0 / (3.0 * math.sqrt(mu / (p ** 3)) * delta_time_sec)
            )
            w = math.atan(math.cbrt(math.tan(s)))
            chi = math.sqrt(p) * 2.0 / math.tan(2.0 * w)
        else:
            # Hyperbolic: a logarithmic guess, because χ grows only
            # logarithmically with dt.
            a = 1.0 / alpha   # negative
            chi = (
                math.copysign(1.0, delta_time_sec)
                * math.sqrt(-a)
                * math.log(
                    -2.0 * mu * alpha * delta_time_sec
                    / (
                        r_dot_v
                        + math.copysign(1.0, delta_time_sec)
                        * math.sqrt(-mu * a)
                        * (1.0 - r0_mag * alpha)
                    )
                )
            )

        # ---- Newton iteration on the universal Kepler equation ----
        psi = 0.0
        c2 = 0.5
        c3 = 1.0 / 6.0
        r = r0_mag

        for iteration in range(max_iterations):
            psi = chi * chi * alpha
            c2 = stumpff_c(psi)
            c3 = stumpff_s(psi)

            # r(χ) — needed both as the derivative dt/dχ and for the final f, g terms.
            r = (
                chi * chi * c2
                + (r_dot_v / sqrt_mu) * chi * (1.0 - psi * c3)
                + r0_mag * (1.0 - psi * c2)
            )

            # The universal Kepler equation residual, in units of √μ · time.
            residual = (
                sqrt_mu * delta_time_sec
                - chi ** 3 * c3
                - (r_dot_v / sqrt_mu) * chi * chi * c2
                - r0_mag * chi * (1.0 - psi * c3)
            )

            chi_new = chi + residual / r

            if abs(chi_new - chi) < tolerance:
                chi = chi_new
                # Recompute auxiliaries at converged χ so f/g are consistent.
                psi = chi * chi * alpha
                c2 = stumpff_c(psi)
                c3 = stumpff_s(psi)
                r = (
                    chi * chi * c2
                    + (r_dot_v / sqrt_mu) * chi * (1.0 - psi * c3)
                    + r0_mag * (1.0 - psi * c2)
                )
                break

            chi = chi_new
        else:
            raise RuntimeError(
                f"Universal Kepler propagation failed to converge in "
                f"{max_iterations} iterations."
            )

        # ---- Lagrange f and g coefficients ----
        f = 1.0 - (chi * chi / r0_mag) * c2
        g = delta_time_sec - (chi ** 3 / sqrt_mu) * c3
        g_dot = 1.0 - (chi * chi / r) * c2
        f_dot = (sqrt_mu / (r * r0_mag)) * chi * (psi * c3 - 1.0)

        r_new = f * r0 + g * v0
        v_new = f_dot * r0 + g_dot * v0

        return StateVector(r_new, v_new)
