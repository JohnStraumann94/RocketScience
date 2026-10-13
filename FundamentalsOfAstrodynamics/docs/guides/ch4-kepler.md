## Background

A satellite does not advance through its orbit at a uniform rate. It races through periapsis
and crawls through apoapsis, sweeping equal areas in equal times (Kepler's second law). The
mean anomaly M is an angle that *does* advance uniformly — it is proportional to time elapsed
since periapsis — but it corresponds to no actual geometric angle on the ellipse. Connecting
M to the true geometric position requires solving Kepler's equation:

    M = E − e sin(E)

where E is the eccentric anomaly, an auxiliary angle defined by the eccentric circle
circumscribed about the ellipse. Going from E to M is a direct substitution. Going from M to
E is transcendental — no closed form exists. This sample solves it by Newton's method:

    E_{n+1} = E_n − (E_n − e sin(E_n) − M) / (1 − e cos(E_n))

The derivative 1 − e cos(E) approaches zero near periapsis of a highly eccentric orbit,
slowing convergence. The solver switches its initial guess to E₀ = π for eccentricities above
0.8, which avoids the troublesome neighbourhood. The iteration count in the output lets you
watch this happen.

## Experiments to try

1. Run with the defaults (M = 60°, e = 0.8). The solver should converge in a small number of
   Newton iterations. The eccentric and true anomalies will both be printed; compare them to
   see how much larger the true anomaly is — you are past periapsis and moving outward, so
   the geometric angle is greater than the mean angle.

2. Increase `ecc` to 0.95. The iteration count will climb and the final residual will still
   be very small — the solver converges, but takes more steps. Compare the true anomaly: at
   high eccentricity even a small M maps to a much larger nu near periapsis.

3. Set `ecc` to 0.99 — close to the parabolic limit. The iteration count rises noticeably.
   The orbit is barely bound; a tiny push would make it hyperbolic. The code still converges
   because of the high-eccentricity starting guess.

4. Set `M` to 180. You are at apoapsis in mean-anomaly terms. For any eccentricity the
   eccentric anomaly and true anomaly should both also be 180°, because apoapsis is the same
   in all three angle systems.

5. Set `M` to 0. You are at periapsis. E and nu should both be 0° regardless of eccentricity.
   The convergence should be immediate — E₀ = M = 0 is already the solution.

6. Press **Visualize Trajectory** after running. The animated orbit shows one full revolution
   sampled uniformly in time. Watch the vehicle race through periapsis and linger near
   apoapsis — the visual proof of Kepler's second law — and find the marker for the mean
   anomaly you entered.

## Watch out for

- **Eccentricity must be strictly below 1.** Kepler's equation in the elliptic form used here
  is undefined for e ≥ 1. For hyperbolic orbits a different form with the hyperbolic sine
  applies. The sample will refuse e ≥ 1.

- **`M` is in degrees at the input, radians internally.** Any value is valid — the code
  normalises it to [0, 2π). Entering 400° is the same as entering 40°.

- **`sma` affects only the derived quantities** (period, radius, time since periapsis). The
  solution for E and nu depends only on M and e.

> The residual printed after convergence is the absolute error in radians after substituting
> the solved E back into Kepler's equation. It is typically below 1e-10 rad, which corresponds
> to a position error far below a millimetre on any realistic orbit. The iteration count is
> always small — usually 3 to 6 — except at very high eccentricities near periapsis.
