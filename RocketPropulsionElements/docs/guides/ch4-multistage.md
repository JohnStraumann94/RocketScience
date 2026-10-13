## Background

Multi-stage analysis answers the question the single-stage payload fraction raises but cannot
resolve: when staging is necessary, how do you allocate mass between the stages? The
accounting rule is simple but powerful. Each stage is analysed as a single-stage problem
using only its own propellant mass and structural mass, but the "payload" that stage must
deliver is the entire upper vehicle — all subsequent stages plus the final payload.

Delta-v contributions add because the rocket equation is a logarithm of a ratio, and
logarithms of products add. That is a mathematically deep reason for a very practical result:
`Δv_total = Σ c_i × ln(m₀_i / mf_i)`.

Two design heuristics emerge reliably:

1. Put the highest Isp engine on the last stage. The upper stage's burn is in vacuum, where
   high-expansion nozzles work, and the stage is starting from a much lighter condition —
   so each additional second of Isp is relatively cheap.

2. Staging returns diminish quickly. The comparison against a single-stage equivalent at the
   bottom of the report shows the delta-v gain from staging. Going from one stage to two is
   transformative; three to four is barely noticeable, and every additional staging event
   adds a separation system that can fail.

The sample defaults are loosely representative of a medium-class launcher: a dense first
stage at 300 s Isp, a vacuum-optimised second stage at 340 s, and a cryogenic third stage
at 450 s.

## Experiments to try

1. Swap the Isp values: put 450 s on Stage 1 and 300 s on Stage 3. The total delta-v falls
   noticeably, illustrating why the highest Isp must go on the last stage. The propellant
   masses and structural masses are the same; only the ordering of exhaust velocities changes.

2. Zero out Stage 3 (set `mp3` and `ms3` to 1 kg and `isp3` to any valid value). The vehicle
   becomes effectively two-stage. Compare the two-stage total delta-v against the comparison
   section. Then compare it against the ch4-optimum-stages sample with n = 2.

3. Reduce `ms1` from 12000 to 6000 kg (better structures). The Stage 1 structural coefficient
   falls and the total delta-v rises. Compare the gain against raising `isp1` by 30 s to see
   which is worth more.

4. Increase the payload from 4000 to 12000 kg. The per-stage mass ratios all decrease because
   the upper vehicle is heavier. Observe how the Stage 1 mass ratio is most sensitive to
   payload changes — the bottom stage carries everything.

5. Set all three stages to the same Isp (say 340 s) and equal structural coefficients. The
   total delta-v should match the idealised formula from ch4-optimum-stages for n = 3.

## Watch out for

> **The per-stage delta-v** reported for each stage uses that stage's own initial mass —
> which includes all subsequent stages and the payload. Stage 1's "initial mass" is the full
> vehicle gross mass. These are correct numbers; they just look large because Stage 1 is
> accelerating the whole stack.

- **Structural mass `ms` does not include the interstage, separation system, or additional
  engine.** In a real vehicle those components add 5–15% of the stage dry mass. The sample
  charges nothing for staging events, which is why it slightly over-estimates the benefit of
  adding stages. See ch4-optimum-stages for a detailed discussion of this idealisation.

- **The single-stage comparison uses the propellant-weighted average Isp.** This is not the
  same as any individual stage's Isp. It is a theoretical baseline: what would this same mass
  achieve if it were a single un-staged vehicle?

- The per-stage structural coefficient printed at the bottom is `ε = ms / (ms + mp)`. Values
  above 0.15 are heavy; below 0.07 is very good. Real stages typically land between 0.07
  and 0.12.
