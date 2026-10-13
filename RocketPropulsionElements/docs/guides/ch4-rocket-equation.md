## Background

The Tsiolkovsky rocket equation, derived in 1903 by integrating Newton's second law with a
continuously burning engine, is the most important equation in spaceflight. It states that
the achievable velocity change is proportional to the effective exhaust velocity and to the
natural logarithm of the initial-to-final mass ratio:

    Δv = c × ln(m₀/mf)

The logarithm is the source of both the power and the frustration of rocketry. It means that
Isp improvements are linear in their delta-v contribution — ten percent more Isp buys ten
percent more delta-v. Mass ratio improvements, by contrast, are logarithmic: to double the
delta-v from mass ratio alone you must square the mass ratio, which means going from 90%
propellant to 99%. That is the structural wall every SSTO design runs into.

The inverse problem — sizing propellant for a required delta-v — reveals the exponential
penalty of large missions. The required initial mass grows as `mf × exp(Δv/c)`. For a
9400 m/s LEO mission with c = 3430 m/s (Isp = 350 s), the exponential is about 15.5. That
means the vehicle must be 15.5 times heavier than its empty structure plus payload. Adding
1000 m/s of delta-v margin costs far more propellant than the first 1000 m/s did.

## Experiments to try

1. Change `isp` from 350 to 450 s (LOX/LH2) and observe the delta-v increase and the
   reduction in required propellant. Then change it to 250 s (solid rocket) to see the
   opposite direction. Note that the "+10% Isp buys" line always moves in direct proportion
   to Isp, while the "-10% dry mass" line varies.

2. Set `dvreq` to 9400 m/s (LEO) with `isp` = 340 and observe the shortfall. Then switch
   to 450 s — the shortfall changes. This is the comparison between kerosene and hydrogen
   upper stages in a nutshell.

3. Look at "The exponential cost of margin" table. Each row adds the same 100–2000 m/s
   increment to the required delta-v but costs more propellant than the previous row. The
   2000 m/s row costs more than all the smaller rows combined for typical parameters.

4. Set `mf` equal to `m0`. The sample returns a validation error: the final mass must be
   less than the initial mass. This guards against the physically impossible mass ratio of
   exactly 1 (no propellant).

5. Raise `m0` to 500000 kg and `mf` to 50000 kg (mass ratio of 10). Note the delta-v, then
   double the mass ratio to 20 by halving `mf` to 25000 kg. The extra delta-v is exactly
   `c × ln(2)`, regardless of where you started.

## Watch out for

> **Two opposite mass ratio conventions exist in this library.** `Performance.MassRatio(m0, mf)`
> returns `mf/m0` — Sutton's convention, **less than one**. The report labels it "Mass ratio
> MR = mf/m0 (Sutton)". `RocketEquation.RequiredMassRatio` returns `m0/mf` — the Tsiolkovsky
> convention, **greater than one**, the value that appears inside the logarithm. The report
> shows both with their labels. If a result is wrong by the square of what you expected, this
> is almost certainly the cause.

- **The delta-v computed here is ideal** — no gravity loss, no drag loss, no staging. Real
  launch vehicles need 1000–2000 m/s more than the orbital speed to account for these losses.
  A LEO mission budget is typically 9200–9500 m/s rather than the 7800 m/s of the circular
  orbit speed itself.

- **`dvreq` is entirely independent of `m0` and `mf`.** It drives only the inverse problem:
  given the dry mass `mf` and the required delta-v, how much propellant is needed? The
  forward problem uses `m0` and `mf` only.

- The sensitivity rows "+10% Isp" and "-10% dry mass" use the forward problem values, not the
  inverse problem inputs. They answer: if this vehicle's Isp or dry mass changed, how would
  its capability change?
