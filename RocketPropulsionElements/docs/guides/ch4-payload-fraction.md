## Background

The payload fraction formula makes the case for staging more sharply than any verbal
argument. For a single stage flying a delta-v of Δv with structural coefficient ε and
effective exhaust velocity c, the fraction of initial mass available for payload is:

    λ = [exp(−Δv/c) − ε] / (1 − ε)

When the exponential term `exp(−Δv/c)` falls to ε, the payload fraction reaches zero — the
stage can just barely accelerate its own empty structure and nothing else. That gives the
hard ceiling on what a single stage can ever achieve at a given ε and c:

    Δv_max = −c × ln(ε)

For a LOX/RP-1 engine (c ≈ 3400 m/s) with ε = 0.08, Δv_max is about 8600 m/s. A LEO
trajectory requires roughly 9400 m/s including losses. The gap is not negotiable with
chemistry or structures: kerosene SSTO is *impossible* at realistic structural coefficients.

For LOX/LH₂ at c ≈ 4410 m/s and ε = 0.08, the ceiling rises to about 11100 m/s — above
the LEO requirement — so hydrogen SSTO is arithmetically possible. In practice, hydrogen's
low density (71 kg/m³) demands large, heavy tanks that push the effective ε back up, and the
payload fraction collapses to a few percent or less. Every SSTO programme has demonstrated
this cliff experimentally.

The "cliff" table in the output shows the full picture: payload fraction plunges long before
Δv_max is reached, so a vehicle designed near the ceiling has no mass growth margin at all.

## Experiments to try

1. Set `isp` to 340 s (LOX/RP-1, ε = 0.08, Δv = 9400 m/s). The payload fraction is zero
   or negative — the mission is impossible for this single stage. The report says so.

2. Change `isp` to 450 s (LOX/LH₂) at the same ε and Δv. Payload fraction becomes positive.
   Note how small it is. Now raise ε to 0.12 (realistic for an LH₂ stage with large tanks)
   and observe how quickly the payload fraction collapses again.

3. Reduce `eps` to 0.05 (outstanding structure) while keeping 340 s Isp. Compare the ceiling
   Δv against the default case. Then try ε = 0.05 with 450 s Isp: the "What would fix it?"
   block shows these improvements in combination.

4. Set `dv` to 5000 m/s (a modest upper-stage manoeuvre). The payload fraction is large and
   comfortable. This shows why single-stage upper stages are routine — the delta-v budget for
   GTO injection is a fraction of the total launch vehicle requirement.

5. Look at the cliff table. Find the delta-v where the payload fraction first drops below 1%.
   That is the practical limit for a realistic vehicle — below that point, mass growth during
   development would wipe out the payload entirely.

## Watch out for

- **`eps` (structural coefficient) is not the structural mass fraction.** The structural
  coefficient ε = m_structure / (m_structure + m_propellant) is defined as the fraction of
  non-payload mass that is structure. A very good stage achieves ε = 0.06–0.08. Do not
  enter the propellant mass fraction zeta (which is typically 0.85–0.92) by mistake.

- **The ceiling Δv_max assumes a *payload of zero*.** It is not the practical limit. A
  vehicle designed with λ = 0.5% has essentially no margin and will certainly fail if any
  mass growth occurs during development. Practical design targets λ above about 2–3%.

> When the payload fraction is zero or negative, the formula returns a non-positive value and
> the report states "PAYLOAD FRACTION IS ZERO OR NEGATIVE." This is correct physics, not a
> numerical error. The stage genuinely cannot reach the requested delta-v with any payload.

- **The delta-v `dv` does not include losses.** To reach LEO, a real vehicle needs about
  9200–9500 m/s of total budget to cover gravity and drag losses, not just the 7800 m/s of
  circular orbit speed. Enter the full budget including losses, not the orbital speed alone.
