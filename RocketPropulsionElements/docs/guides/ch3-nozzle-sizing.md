## Background

Analysis runs from the propellant and the geometry to performance. Design runs the other
direction: you are given a thrust requirement and you must produce metal dimensions. This
sample implements that inversion.

The fundamental sizing equation is `At = F / (C_F × p1)`. The thrust coefficient is
computed first from the gas properties and area ratio, and then the throat area is the only
unknown. Everything else — exit area, diameters, chamber volume, nozzle length — follows
from the throat by scaling rules that depend only on geometry.

Increasing chamber pressure is the primary lever for reducing engine size. For a fixed thrust
and area ratio, doubling p1 halves the required throat area and roughly halves every linear
dimension. That is why high-pressure engines (the SSME at 207 bar, the RD-180 at 267 bar)
are compact relative to their thrust. The price is paid in structural mass, pump power, and
manufacturing difficulty.

Characteristic length L* is an empirical residence time parameter: it is the length of a
hypothetical cylinder of throat-area cross-section that has the same volume as the actual
chamber. Typical ranges are 0.8–1.0 m for LOX/LH₂, 1.0–1.3 m for LOX/RP-1, and 0.6–0.9 m
for storable hypergolics. Too small and combustion is incomplete (c* efficiency falls); too
large and you carry dead wall area and weight.

The divergence loss factor `lambda = (1 + cos α) / 2` at the standard 15° cone gives 0.983
— a 1.7% velocity reduction because the exhaust is not perfectly axial. A bell nozzle
recovers most of that loss at 60–80% of the cone's length.

## Experiments to try

1. Raise `p1` from 100 to 200 bar (holding `thrust` at 500 kN). The throat diameter
   shrinks by approximately `1/√2`, and the chamber volume halves. Compare the bell nozzle
   length before and after — high pressure buys a compact engine.

2. Change `lstar` from 1.1 to 0.6 m (hypergolic range). The chamber volume shrinks and the
   chamber length shortens. Then raise it to 1.5 m to see the opposite extreme. Note that
   L* does not appear in any performance figure — it affects only the physical length.

3. Change `halfangle` from 15° to 25°. The divergence factor drops and the velocity loss
   rises. The conical nozzle grows shorter, but the bell nozzle length changes very little —
   it is defined as 80% of the 15° cone, regardless of the angle you enter for the cone.

4. Set `p3` to 0 (vacuum sizing). The thrust coefficient changes because the pressure thrust
   term is now `p2 × A2 / (p1 × At)`. The required throat area for the same vacuum thrust is
   slightly different from the sea-level case.

5. Change `eps` from 22 to 8 (a very compact sea-level nozzle). The exit diameter shrinks
   dramatically, and C_F falls. Compare the resulting Isp against the wide-nozzle case to
   see the performance penalty of a low expansion ratio at sea level.

## Watch out for

- **`thrust` is entered in kN**, converted to N internally. **`at` in the c*/C_F sample is
  in cm²**, but this sample does not take throat area as an input — it is the output. The
  delivered throat area is reported in both cm² and mm diameter.

- **The contraction ratio of 3.0 is assumed, not entered.** It is used only to estimate the
  chamber diameter and length. Real engines use lower contraction ratios as they grow larger,
  because the entry flow velocity into the chamber affects heat transfer and mixing. For a
  preliminary sizing this assumption is adequate.

> The chamber length printed is the cylindrical equivalent — a cylinder of the computed
> chamber area and volume. Real chambers taper and have hemispherical or torospherical ends.
> Add roughly 20–30% to get a realistic total chamber-plus-convergent-section length.

- **Bell nozzle length is hardwired at 80% of the 15° conical reference**, regardless of the
  half-angle you enter for the cone. The 80% figure is the standard Rao-bell approximation.
  A detailed contoured bell designed by the method of characteristics would be slightly
  different, but for sizing purposes the 80% rule is industry standard.
