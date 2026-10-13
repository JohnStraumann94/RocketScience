## Background

Ideal theory always over-predicts performance. A real engine typically delivers 92–96% of
its theoretical specific impulse, and the loss budget tells you exactly where the remaining
4–8% went. This matters because different losses respond to different engineering fixes; a
low c* efficiency points at the injector, while a high divergence loss points at the nozzle
shape.

The five correction factors multiply together. Four belong to the nozzle:

- **Divergence factor λ** accounts for the exhaust not leaving perfectly axially. For a
  cone it is `(1 + cos α) / 2`; for a well-designed bell it is 0.985–0.995.
- **Friction / boundary layer factor** covers viscous losses and wall heat transfer.
  It is smaller for large engines (where the boundary layer is a smaller fraction of the
  throat) and larger for small engines.
- **Two-phase flow factor** matters only for metallised propellants. Aluminium oxide
  condensate in an APCP solid cannot expand, cannot follow the gas, and drags on it. For
  clean gas-phase propellants this factor is exactly 1.0.
- **Chemical / kinetic factor** accounts for reactions that do not have time to reach
  equilibrium during the short transit through the nozzle.

The fifth factor, c* efficiency, measures the chamber. It is computed from a hot-fire test
(`η_c* = c*_measured / c*_theoretical`) and captures everything related to combustion
completeness — injector pattern, chamber L*, propellant vaporisation.

The final delivered Isp is `theoretical Isp × η_nozzle × η_c*`. The two multiplied factors
are measured independently, which is exactly why the c*/C_F decomposition from the earlier
sample is so useful in practice.

## Experiments to try

1. Set `twophase` to 0.96 (an aluminised solid) and observe the loss. Then restore it to 1.0
   and reduce `friction` to 0.985 to see an equivalent loss from a smaller engine. Both
   reduce delivered Isp by roughly 5 seconds, but they are caused by completely different
   physics and require different fixes.

2. Reduce `halfangle` from 15° to 8° (a bell nozzle equivalent angle). The divergence factor
   rises toward 1.0 and the divergence loss falls. Compare it against the "length saved by
   using a bell" figure in the ch3-nozzle-sizing sample to see what you get for what you pay.

3. Set `cstarmeas` to 1650 m/s while `cstartheo` remains at 1830 m/s. The c* efficiency
   drops to about 0.90 — a poor injector. The overall efficiency and delivered Isp both fall
   markedly. The verdict block labels this as "Poor" and directs attention to the injector.

4. Make the engine perfect: set all five factors to 1.0 and `cstarmeas` equal to `cstartheo`.
   The delivered Isp equals the theoretical value. This confirms the numerical identity.

5. Enter a realistic set of factors for a well-developed hydrolox engine: `halfangle` = 9,
   `friction` = 0.993, `twophase` = 1.0, `chemical` = 0.993, and c* efficiency from test
   data of about 0.985. The overall efficiency should come out near 0.96.

## Watch out for

- **The loss attribution at the bottom is approximate.** Because the factors multiply rather
  than add, the individual "seconds lost" entries sum to slightly more than the total. The
  cross terms are double-counted. The attribution is how loss budgets are presented in
  practice — as a first-order breakdown — not as exact arithmetic.

- **`halfangle` for a bell should be the equivalent cone angle, not the throat entry angle.**
  A bell has a varying wall angle; the equivalent cone angle that gives the same divergence
  factor is typically 8–12°, not the exit angle of the bell. Using the geometric exit angle
  of a bell (which can be 4–6°) would over-state the divergence correction.

> If the overall efficiency is below 0.90, the verdict block flags it as "Poor" and
> specifically recommends checking the c* efficiency first. A low c* points at the injector
> or chamber L* far more reliably than it points at the nozzle; it is the most actionable
> single diagnostic available.

- **The theoretical Isp entered here is from Chapter 3 ideal theory.** It is not the vacuum
  Isp from a manufacturer's data sheet, which already includes some of these losses. Using a
  published vacuum Isp as the "theoretical" input will give an artificially high efficiency.
