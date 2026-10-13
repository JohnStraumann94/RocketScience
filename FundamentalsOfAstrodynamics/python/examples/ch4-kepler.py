"""
ch4-kepler.py — Sample: Kepler's equation solver.
==================================================

Chapter 4 of Bate, Mueller & White, *Fundamentals of Astrodynamics*.

Demonstrates:
    Solving M = E − e sin E for the eccentric anomaly by Newton's method,
    watching convergence degrade as eccentricity increases.

Physics background:
    The true anomaly ν does not advance uniformly — a satellite races
    through periapsis and crawls through apoapsis.  Kepler introduced the
    mean anomaly M, which DOES advance uniformly, and tied it to the
    geometry through M = E − e sin E.

    Going from E to M is trivial.  Going from M to E is not: the equation
    is transcendental and has occupied numerical analysts since 1650.

    Newton's method:
        E_{k+1} = E_k + (M − E_k + e sin E_k) / (1 − e cos E_k)
    converges quickly, but the derivative 1 − e cos E approaches zero near
    periapsis of a highly eccentric orbit, which is why the solver switches
    its starting guess to E₀ = π once e exceeds 0.8.

    Push the eccentricity towards 0.99 and watch the iteration count climb.

Cross-check:
    With M = 60° and e = 0.8 the solver must converge (in ≤ 100 iterations).
    The expected eccentric anomaly is approximately E ≈ 90.44°.
"""

# ---------------------------------------------------------------------------
# Path setup — allows running this script from its own directory.
# ---------------------------------------------------------------------------
import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import astro_core.two_body as tb
import astro_core.angles as ang
from astro_core.kepler_solver import KeplerSolver

# ===========================================================================
# EDIT THESE VALUES AND RE-RUN
# ===========================================================================
MEAN_ANOMALY_DEG   = 60.0     # deg — mean anomaly M
ECCENTRICITY       = 0.8      # dimensionless — try 0.99 to stress the solver
SEMI_MAJOR_AXIS_KM = 26600.0  # km — used to convert to time since periapsis
# ===========================================================================

def main() -> None:
    m_rad = ang.to_radians(MEAN_ANOMALY_DEG)
    ecc   = ECCENTRICITY
    sma   = SEMI_MAJOR_AXIS_KM

    solution = KeplerSolver.solve_eccentric_anomaly(m_rad, ecc)
    nu       = KeplerSolver.eccentric_to_true_anomaly(solution.anomaly_rad, ecc)

    # Radius from the eccentric anomaly: r = a(1 − e cos E).
    import math
    radius = sma * (1.0 - ecc * math.cos(solution.anomaly_rad))

    time_since_periapsis = KeplerSolver.time_since_periapsis(nu, ecc, sma)

    print(f"Mean anomaly M            = {MEAN_ANOMALY_DEG:12.6f} deg")
    print(f"Eccentricity e            = {ecc:12.6f}")
    print()
    print("SOLUTION")
    print(f"  Eccentric anomaly E     = {ang.to_degrees(solution.anomaly_rad):12.6f} deg")
    print(f"  True anomaly nu         = {ang.to_degrees(nu):12.6f} deg")
    print()
    print("CONVERGENCE")
    print(f"  Newton iterations       = {solution.iterations:12d}")
    print(f"  Final residual          = {solution.final_residual:12.4E} rad")
    guess = "E0 = M (low eccentricity)" if ecc < 0.8 else "E0 = pi (high eccentricity)"
    print(f"  Starting guess used     = {guess}")
    print()
    print("DERIVED QUANTITIES")
    print(f"  Radius r = a(1-e cos E) = {radius:12.3f} km")
    print(f"  Time since periapsis    = {time_since_periapsis:12.2f} s  ({time_since_periapsis / 60.0:.3f} min)")
    T = tb.period(sma)
    print(f"  Orbital period          = {T:12.2f} s  ({T / 60.0:.3f} min)")
    print()
    print("VERIFICATION (substituting E back into Kepler's equation)")
    m_check = KeplerSolver.eccentric_to_mean_anomaly(solution.anomaly_rad, ecc)
    print(f"  E - e sin(E)            = {ang.to_degrees(m_check):12.9f} deg")


if __name__ == "__main__":
    main()
