## Background

The idealised model for n identical stages, each carrying the same structural coefficient
and the same exhaust velocity and each providing Δv/n, gives a payload fraction that rises
monotonically with stage count. It never turns over. That is the honest answer to "how many
stages does the idealised model recommend?" — it always recommends more.

The important message is in the "Gain over n−1" column, not in the absolute payload fraction.
The marginal gain from each additional stage collapses quickly. In the default setup with
Isp = 340 s and ε = 0.08, going from one stage to two is often the difference between
"impossible" and "possible". Going from two to three adds a real but smaller amount. Going
from three to four adds very little. Beyond four, the marginal gain in this idealised model
is smaller than the real cost of adding a stage to hardware: the interstage, the separation
system, the extra engine, and the additional integration and qualification programme.

The formula charges nothing for those fixed per-stage costs. That is the model's deliberate
limitation, and it is stated explicitly in the Theory section and in the report's OPTIMUM
block. If the optimiser saturates at your `nmax`, the report says so, and it directs you to
the marginal gain column as the practical decision tool.

The real design insight is not "use as many stages as possible" but rather "use as many
stages as gives a meaningful marginal gain — and then one more is not worth it".

## Experiments to try

1. Set `dv` to 5000 m/s. The one-stage case is now trivially achievable and the marginal
   gain from each additional stage is small from the start. Compare this against the 9400 m/s
   default to see how the trade changes with mission difficulty.

2. Raise `eps` from 0.08 to 0.15 (heavier structure). The single-stage ceiling drops. More
   stages are needed to reach the same delta-v, and the marginal gain column shows the gain
   persisting to higher stage counts. Poor structures benefit more from staging.

3. Set `isp` to 450 s (LOX/LH₂) and ε = 0.08. The single-stage ceiling rises above 9400 m/s
   and two stages gives a comfortable payload fraction. The optimiser recommends a low stage
   count in this idealised model.

4. Reduce `nmax` to 4 and re-run. The optimiser saturates at 4, which is typically the last
   stage count with a meaningful marginal gain in the real world. Anything the model adds
   beyond that is a consequence of the free-staging assumption.

5. Look at the "marginal gain expressed as payload per 100 t" table. Pick the row where the
   gain drops below about 500 kg per 100 t of gross mass. That is roughly where the fixed
   cost of a real staging event (typically 500–1500 kg for a medium-class vehicle) begins to
   exceed the benefit.

## Watch out for

> The optimiser in this model always saturates at `nmax` for realistic missions, because the
> idealised payload fraction is strictly monotone increasing in n. This is **not a bug**. The
> model is correct within its assumptions; the assumptions are what limit it. Read the
> marginal gain column, not the optimiser recommendation.

- **`nmax` is entered as a decimal number but treated as an integer.** The sample clamps it
  to between 1 and 12. Entering 3.7 gives the same result as 3; the decimal part is ignored.

- The sample assumes **identical stages**: same ε, same c. Real vehicles use stages of
  different sizes, different propellants, and different structural coefficients. See
  ch4-multistage for the full stage-by-stage accounting of a heterogeneous vehicle.

- **Delta-v is split evenly between stages** in this idealisation. For heterogeneous stages
  with different Isp values, equal Δv split is not necessarily optimum — the higher-Isp
  stage should ideally do more of the work. The ch4-multistage sample handles this correctly
  by computing each stage's contribution from its own actual mass ratio.
