## Background

Specific impulse is not the only figure of merit for a propellant combination, and treating
it as such leads to poor vehicle designs. Density matters too, and it matters differently for
different stages.

Density impulse `I_d = Isp × ρ_bulk` captures the impulse available per unit tank volume
rather than per unit propellant weight. LOX/LH₂ leads all practical combinations on Isp —
around 450 s in vacuum — but its bulk density at the optimum mixture ratio is near
350 kg/m³. LOX/RP-1 has about 340 s vacuum Isp but a bulk density near 1030 kg/m³. On
density impulse, LOX/RP-1 comfortably beats LOX/LH₂. Three times the propellant mass fits
in the same tank volume.

Those larger hydrogen tanks need structure, insulation (LH₂ boils at 20 K), pressurisation,
and feed lines — all of which worsen the structural coefficient and partially offset the Isp
advantage. The tank volume comparison at the bottom of the report makes the size difference
impossible to miss.

Storables — N₂O₄/UDMH, N₂O₄/MMH, and their relatives — sacrifice 20–30 s of Isp for room-
temperature liquids and hypergolic ignition (no ignition system, reliable restart after any
time in space). Every deep-space probe and most satellites use storables for exactly that
reason: they can sit fuelled for years. The toxicity and carcinogenic properties of these
propellants are why modern designs are moving toward LOX/methane and green alternatives.

## Experiments to try

1. Raise `p1` from 100 to 200 bar and `eps` from 40 to 80. Every Isp figure in the table
   rises, but the relative ranking does not change much. The advantage of LOX/LH₂ on Isp
   is robust across a wide range of operating conditions.

2. Look at the two ranked lists — by Isp and by density impulse. Find the combinations that
   appear at different positions in each ranking. LOX/RP-1 typically rises significantly in
   the density impulse ranking. That reordering is the central propellant trade of launch
   vehicle design.

3. Change `mass` from 100000 to 10000 kg (a smaller vehicle). The tank volumes scale
   proportionally, but the relative sizes of the tanks remain the same. Confirm that the
   hydrogen tank is roughly three times the volume of the kerosene tank for the same mass.

4. Lower `eps` to 8 (a sea-level first-stage expansion ratio). Compare how the Isp figures
   change for each combination versus the vacuum expansion case. High-performance cryogenic
   combinations lose less of their advantage at low expansion ratios than storables do.

5. Look at the notes block at the bottom. Each combination's entry describes its practical
   advantages and disadvantages. Cross-reference the note for storables with the density
   impulse figure — the density advantage partly explains their choice for in-space missions
   where tank volume on a tight spacecraft budget matters.

## Watch out for

- **The Isp figures are from ideal frozen-flow theory with representative gas properties.**
  They are accurate to a few percent but are not CEA-derived equilibrium values. Do not
  treat them as design-critical data — they are for comparison and insight.

- **The Isp comparison uses the same chamber pressure and area ratio for every combination.**
  This is intentional: the comparison would be unfair if each combination were evaluated at
  its own optimum operating point. The inputs `p1` and `eps` are the fixed reference
  conditions.

> Bulk density is computed at the optimum mixture ratio for each combination. Running off-
> optimum — which real engines often do for cooling or structural reasons — changes the bulk
> density. See the ch7-mixture-ratio sample to explore how the density and tank volumes shift
> with mixture ratio.

- **The "5% ullage" note in the tank volume table** means each tank is sized 5% larger than
  the propellant volume. Ullage provides space for thermal expansion, pressurisation gas, and
  propellant management hardware. The actual ullage fraction varies by design; 3–6% is typical.
