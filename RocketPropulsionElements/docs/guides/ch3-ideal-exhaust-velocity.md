## Background

Sutton's Equation 3-15 is the single most important result in the book. It gives the velocity
of exhaust gas leaving a nozzle under isentropic, one-dimensional, frozen-composition
assumptions — and it is the starting point for every performance calculation in liquid and
solid propulsion.

The equation has three physically distinct groups that behave very differently. The thermal
group `R × T1` (where `R = R_universal / M`) is under the square root and contributes
strongly: exhaust velocity scales as `√(T1/M)`. Raising chamber temperature and reducing
exhaust molecular mass are therefore the two most powerful levers a propellant chemist has.
Note that they enter with equal weight — a 10% improvement in `T1/M` gives the same velocity
gain whether it comes from temperature or molecular mass.

The pressure ratio group `[1 − (p2/p1)^((γ−1)/γ)]` saturates rapidly. Going from a pressure
ratio of 10 to 100 is a large gain; going from 100 to 1000 is much smaller. The maximum
theoretically achievable velocity, at p2 = 0, is only a few percent above what a real nozzle
achieves at a modest pressure ratio. This is why chamber pressure matters far less to
performance than propellant selection.

The sensitivity table at the bottom of the report makes the relative importance of each term
explicit by applying a 10% perturbation to one variable at a time. The T1 and M lines are
nearly equal and dominant; the chamber pressure line is much smaller.

## Experiments to try

1. Change `mm` from 22 to 10 kg/kmol (approximately LOX/LH2 products). With the same T1 of
   3500 K the velocity rises substantially, demonstrating why hydrogen is so attractive.
   Compare the result against LOX/RP-1 (M ≈ 22, T1 ≈ 3600 K) — hydrogen wins despite its
   *lower* flame temperature, because `3000/10 > 3600/22`.

2. Raise `p1` from 70 to 700 bar (keeping `p2` at 1 bar). The velocity increases, but by
   much less than you might expect. Compare the pressure group values before and after to see
   the saturation directly. Then look at the "velocity still on the table" line — most of the
   available velocity is already captured at 70 bar.

3. Set `p2` equal to `p1` (e.g. both to 70 bar). The sample returns a validation error
   because exit pressure must be below chamber pressure for expansion to occur. This is the
   guard that prevents a physically impossible operating point.

4. Reduce `gamma` from 1.24 to 1.15 (complex polyatomic exhaust) and observe the small
   velocity gain. Compare it against the molecular mass sensitivity. Chasing a lower gamma is
   real but minor; chasing lower M is the main game.

5. Set `p2` to 0.1 bar (expanding to near-vacuum). The fraction of ceiling achieved rises
   noticeably. Now compute the optimum area ratio needed to achieve this exit pressure from
   the full operating point in the ch3-cstar-cf sample.

## Watch out for

- **`p1` and `p2` are entered in bar, not pascals.** The sample converts internally using
  `Constants.BarToPa`. The default of 70 bar is a reasonable medium-thrust engine; 1.0 bar
  exit is sea-level ambient. Entering 70 where 7000000 is needed would give a pressure ratio
  below 1, triggering the validation error.

- **`mm` is in kg/kmol, not g/mol.** The values are numerically identical (both equal the
  molar mass in the common sense), but the label says kg/kmol. LOX/LH2 products are near
  10, hydrocarbons 22–24, APCP solids 25–30.

> The velocity ceiling `v_max = √(2γ/(γ−1) × R × T1)` is reached only when `p2 → 0`,
> which requires an infinitely large exit area. No real nozzle achieves it. The report shows
> how close the specified operating point comes.

- **The Isp printed assumes perfect expansion** — that is, `p2 = p3` so that `c = v2`. If
  the ambient pressure differs from the exit pressure, the delivered Isp will be different
  from what this calculation shows. Use the ch3-cstar-cf or ch3-full-engine samples for a
  complete operating point including the ambient correction.
