## Background

A nozzle is a fixed piece of hardware. The atmosphere it flies through varies by three
orders of magnitude between sea level and orbit. This sample holds the engine constant and
asks what happens to its performance as the ambient pressure falls — a question every launch
vehicle designer must answer.

The key insight is that exit pressure p₂ does not change with altitude. It is set by the
area ratio and gamma, which are geometric and thermodynamic properties of the engine itself.
What changes is p₃, the ambient. As p₃ falls, the pressure thrust term `(p₂ − p₃) × A₂`
climbs from a small or negative value at sea level toward its vacuum ceiling of `p₂ × A₂`.
The result is that every engine gains thrust and Isp continuously as it climbs, without any
hardware change at all.

For a typical first-stage engine with an area ratio of 20–30, the thrust gain from pad to
vacuum is on the order of 10–15%. That gain is free — the engine does no additional work. It
is simply recovering performance that was being suppressed by ambient back-pressure.

The design compromise is the area ratio. A large area ratio extracts maximum performance in
vacuum but over-expands at sea level, potentially triggering flow separation. A small area
ratio avoids separation at sea level but leaves vacuum performance on the table. Multi-stage
vehicles resolve this by using different engines on different stages: a small-nozzle first
stage optimised for sea level, and a large-nozzle upper stage that only ever fires in near-
vacuum.

## Experiments to try

1. Change `eps` from 22 to 10. The sea-level engine is now closer to optimum expansion at
   the pad. Notice the separation warning disappears (if it was present). Compare the sea-
   level versus vacuum thrust gain column: a smaller nozzle gains proportionally less by
   climbing, because it was less under-expanded to begin with.

2. Set `eps` to 60 (a vacuum-class nozzle). At sea level the exit pressure is far below
   ambient — deeply over-expanded. Look for the separation flag in the table. The vacuum Isp
   is significantly higher than the sea-level figure, showing what this nozzle is designed for.

3. Reduce `p1` from 100 to 30 bar, keeping `eps` at 22. The exit pressure p2 is fixed by
   the area ratio, but the absolute value of p1 determines how over- or under-expanded the
   engine is relative to ambient. A lower chamber pressure over-expands more easily.

4. Set `hmax` to 20 km and observe how quickly the engine performance converges once the
   atmosphere thins. The atmosphere has a scale height of about 7200 m, so by 50 km it is
   effectively vacuum. The last few rows of the table change very little.

5. Find the crossover altitude where the expansion state switches from "OverExpanded" to
   "UnderExpanded". This is the altitude where the engine is perfectly matched to its
   surroundings — the instantaneous optimum point for this nozzle.

## Watch out for

- **The optimum area ratio at sea level is printed at the bottom of the report.** If your
  `eps` is higher than this value, the nozzle is over-expanded at sea level; if it is lower,
  the nozzle is under-expanded. There is no single correct choice — it is a trade.

- **The exponential atmosphere model** used here (`p3 = 101325 × exp(−h/7200)`) is a simple
  one-scale-height approximation. It is adequate for performance estimates to a few percent
  but does not capture the stratospheric temperature inversion or high-altitude composition
  changes. Real trajectory tools use a multi-layer standard atmosphere table.

> Flow separation occurs roughly when `p₂ < 0.4 × p₃`. At sea level with a large area
> ratio, this condition can be met, and the table will flag it with "YES" in the Sep? column.
> Do not fly a nozzle in that condition. Separation produces random asymmetric side loads.

- **`hmax` is entered in km, not metres.** The default of 60 km is above the point where the
  atmosphere becomes negligible. Entering 60 where 60000 is intended gives a table that only
  covers the first 60 metres of flight, which is not particularly interesting.
