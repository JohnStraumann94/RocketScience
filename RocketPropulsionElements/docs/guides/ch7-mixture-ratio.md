## Background

Given a total propellant mass flow, the mixture ratio determines how it is split between
the oxidiser and fuel circuits. The split matters for three independent reasons: it controls
the combustion temperature and products (and therefore Isp), it determines the relative
sizes of the two tanks, and it sets the thermal environment the chamber must survive.

The optimum mixture ratio for performance is not the stoichiometric ratio. Real engines run
fuel-rich, and the reason is the `√(T/M)` dependence of exhaust velocity. Running fuel-rich
lowers the flame temperature (bad for Isp through T) but leaves light unburnt fuel molecules
in the products (very good for Isp through M). For LOX/LH₂, the stoichiometric ratio is 8:1
by mass but flight engines run around 5.5:1 — the molecular mass reduction from excess
hydrogen wins over the temperature reduction. For LOX/RP-1, the optimum is near 2.5:1
compared with a stoichiometric of about 3.4:1.

Fuel-rich operation also protects the engine: an oxidiser-rich environment at 3500 K attacks
metals aggressively, while a fuel-rich environment is relatively benign. Oxidiser-rich staged
combustion — used by the Russian RD-180 and NK-33 families — is a deliberate exception made
for thermodynamic efficiency, and it requires special alloys and tight contamination controls.

Enter 0 for `mr` to use the catalogued optimum mixture ratio for whichever combination you
selected. Enter a non-zero value to override it and explore the off-optimum performance.

## Experiments to try

1. Select combination 1 (LOX/LH₂, `choice` = 1) and leave `mr` at 0. The oxidiser tank
   volume is much smaller than the fuel tank despite the LOX/LH₂ mass ratio near 5.5 —
   because liquid oxygen is 16 times denser than liquid hydrogen. The fuel tank dominates
   the vehicle length.

2. Select combination 2 (LOX/RP-1) and compare the tank volumes to combination 1 for the
   same propellant mass. The LOX/RP-1 tanks are dramatically smaller. This is the density
   difference that drives first-stage design toward kerosene.

3. Select combination 2 (LOX/RP-1) and override `mr` to 1.5, 2.5, 3.5, and 4.5 in
   successive runs. Watch the oxidiser volume grow and the fuel volume shrink monotonically.
   The mixture ratio sweep table at the bottom of the report shows this systematically.

4. Select combination 4 (N₂O₄/UDMH) and observe that the tank volumes are similar in size
   — the mixture ratio is close to 2, and the densities of the two propellants are
   comparable. This makes for a more balanced stage geometry than hydrogen combinations.

5. Change `ullage` from 0.05 to 0.10. Both tank volumes grow proportionally. Compare the
   burn time at the same mass flow rate — it does not change, because burn time depends on
   total propellant mass and flow rate, not on the tank volume.

## Watch out for

- **`choice` selects from the catalogue in order: 1 = LOX/LH₂, 2 = LOX/RP-1, 3 = LOX/CH₄,
  4 = N₂O₄/UDMH, 5 = H₂O₂/RP-1.** The names are printed in the output header. The total
  number of combinations is fixed by the `Propellants.All` catalogue; entering a value
  outside 1–5 is clamped to the nearest valid choice.

- **Setting `mr` to 0 uses the catalogued optimum**, which is the stoichiometric peak for
  Isp after the molecular mass trade is taken into account. This is not the same as the
  stoichiometric mass ratio, which is always higher. The catalogued values come from the
  propellant combination record and are representative values, not CEA-derived optima.

> The mixture ratio sweep at the bottom uses the optimum as the reference point and sweeps
> from 60% to 145% of it. The real optimum may shift slightly if you are operating at a
> chamber pressure or area ratio very different from the catalogue's representative conditions,
> because dissociation alters the product composition.

- **Tank volumes are in m³, not litres.** The default propellant mass of 100000 kg at
  LOX/RP-1 bulk density gives total volumes of order 100 m³ — a large first stage. For a
  small thruster or a test article, reduce `mass` accordingly.
