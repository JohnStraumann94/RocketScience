## Background

Chapter 3's nozzle equations rest entirely on the perfect-gas model, and this sample exposes
the thermodynamic foundation. The specific heats cp and cv, the speed of sound, and the
stagnation relations all follow from two numbers: the specific heat ratio γ and the specific
gas constant `R = R_universal / M`.

The isentropic stagnation relations at the throat are particularly useful as sanity checks.
At Mach 1 — which the throat always is, in a choked nozzle — the static temperature is
`T_t = T₁ × 2/(γ+1)` and the static pressure is `p_t = p₁ × [2/(γ+1)]^(γ/(γ−1))`. For
γ = 1.2, these give 91% of chamber temperature and 56% of chamber pressure, regardless of
what the engine is or how large it is. Those ratios are the same for a 1 N thruster and an
F-1. When someone's engine data sheet reports a throat temperature or pressure that deviates
significantly from these ratios, something is wrong.

The expansion profile table is the physical picture of how the gas accelerates down the
nozzle. Speed rises monotonically, but the local sound speed *falls* because the gas cools.
The Mach number climbs sharply because velocity grows while the speed of sound shrinks. An
exit Mach of 3–4 does not mean the gas is going four times faster than at the throat; it
means the local reference speed has fallen dramatically.

The adiabatic flame temperature estimate is intentionally simple and intentionally noted as
an overestimate. Above roughly 2500 K, dissociation absorbs energy and caps the actual
temperature below the simple formula's prediction.

## Experiments to try

1. Verify the identities explicitly: confirm that `cp − cv = R` and `cp / cv = gamma`. The
   report computes both and prints them as checks. Any discrepancy larger than 0.0001 would
   indicate a numerical error in the library.

2. Change `mach` from 3.5 to 1.0 (the throat). Read off the static temperature and pressure
   ratios and confirm they match the formulas `T_t/T₁ = 2/(γ+1)` and `p_t/p₁ = [2/(γ+1)]^...`.
   The report prints both the formula value and the calculated ratio for the throat separately.

3. Set `gamma` from 1.22 to 1.40 (diatomic gas like N₂). Both cp and cv change; the ratio
   stays at gamma. The speed of sound in the chamber changes. Watch the throat temperature
   and pressure ratios move — they depend only on gamma, not on T₁ or M.

4. Raise `q` (heat of reaction) to 8.0e6 J/kg and observe the flame temperature estimate.
   Then compare it against the `t1` you entered. If `t1 < flame_temperature`, the estimate
   is self-consistent; if `t1 > flame_temperature`, either the real cp is lower or the
   estimate is wrong (dissociation at high temperature). The report flags this distinction.

5. Vary `mach` from 0.1 to 4.5 and read the expansion profile table each time — particularly
   the area ratio column. Confirm that the area ratio has a minimum at Mach 1 and rises on
   both sides, which is the converging–diverging shape of the nozzle.

## Watch out for

- **`p1` is in bar; `q` is in J/kg.** The default heat of reaction (5.5×10⁶ J/kg) is
  representative of a hydrocarbon propellant. For a cryogenic hydrogen combination,
  typical values are 13–16 MJ/kg. Enter 5.5e6, not 5.5.

- **The flame temperature estimate ignores dissociation.** A real exhaust at 3000+ K is
  significantly dissociated: H₂O → H + OH, CO₂ → CO + O, and so on. Each bond broken
  consumes energy and reduces the temperature below the simple estimate. Use NASA CEA for
  any result that matters.

> The report prints two checks at the top: `cp − cv` should equal `R`, and `cp / cv` should
> equal `gamma`. These are algebraic identities that must hold exactly given the definitions.
> They are included as explicit confidence checks, not because there is any doubt about the
> arithmetic, but because they make it easy to verify that you entered consistent inputs.

- **The Mach number `mach` is the quantity at which static conditions are computed.** It is
  not the exit Mach number of the nozzle you are analysing; it is a free parameter you can
  set to any value. The area ratio column tells you what expansion ratio would produce that
  Mach number.
