## Background

The injector is the most critical single component in a liquid rocket engine. It determines
whether the engine runs smoothly, burns efficiently, and remains stable. The F-1 engine's
development history is the clearest illustration: approximately 2000 full-scale tests were
required to find an injector face design stable against combustion instability. No other
component required anything like that effort.

Each orifice obeys the incompressible flow equation `m_dot = Cd × A × √(2ρΔp)`. The
discharge coefficient Cd covers the vena contracta and viscous losses: a sharp-edged hole
gives about 0.6, a well-rounded entry gives 0.75–0.85.

The most important design rule is the pressure drop target: 15–25% of chamber pressure. This
is chosen deliberately large for a reason called hydraulic decoupling. A small injector drop
allows pressure oscillations in the chamber to feed back into the propellant feed lines,
modulating the flow, which modulates combustion, which reinforces the oscillation. That
positive feedback loop is a combustion instability, and it can go from nothing to engine
destruction in milliseconds. A stiff injector — a large pressure drop — makes the flow nearly
independent of chamber fluctuations.

The square-root throttling table at the bottom shows the consequence of this choice for
deep throttling: the required pressure drop falls as the *square* of the flow rate reduction,
so at 30% throttle the drop is only 9% of its full-power value. At that point the injector
is hydraulically soft and instability is a real risk. This is why deeply throttleable engines
use pintle injectors or other variable-area schemes.

## Experiments to try

1. Set `dpfrac` to 0.08 (8% of p1). The report labels this as below the stability threshold
   and warns explicitly. Then raise it to 0.20 and read the "Normal design range" assessment.
   The warning text is the engineering judgement encoded in the library.

2. Reduce `dox` from 1.5 mm to 0.8 mm. The area of one orifice falls as d², so the hole
   count grows as approximately d⁻². Watch the orifice count column jump. Very small holes
   are difficult to drill consistently; 0.5 mm is near the practical lower limit.

3. Change `cd` from 0.75 to 0.60 (sharp-edged holes). The required total orifice area grows
   to pass the same flow at the same pressure drop, and the hole count increases. A lower Cd
   means each hole is less efficient.

4. Raise `mr` (mixture ratio) from 2.3 to 4.0. The oxidiser flow fraction grows and the
   fuel flow fraction shrinks. The two circuits are sized independently; the oxidiser side
   gains holes and the fuel side loses them. The momentum ratio at the bottom changes.

5. Look at the throttling table. Find the throttle setting where the injector drop falls below
   10% of p1. At that point the design is in the instability-risk zone. A pintle injector
   maintains a nearly constant pressure drop fraction by continuously varying its geometry.

## Watch out for

- **All pressures are in bar; orifice diameters are in mm.** The sample converts bar to Pa
  and mm to m internally before calling the library. Entering 70 for p1 means 70 bar = 7 MPa.

- **Injection velocity is a secondary output**, not a design input. High injection velocity
  improves atomisation quality (smaller droplets, faster vaporisation, more uniform mixing),
  but too high means the propellant passes through the chamber before it has burned — just
  as too low means poor mixing. Typical targets are 10–30 m/s for a doublet element.

> **The hole count is rounded up.** The sample computes the minimum number of whole orifices
> to meet the required area. The actual area provided is slightly larger than requested, and
> the round-trip flow check confirms the delivered flow with that area. A discrepancy between
> the "required flow" and the "round-trip check" larger than about 0.1% would indicate a
> rounding error in the orifice count calculation.

- **Momentum ratio near 1.0** at the bottom of the report indicates balanced impingement
  forces in a doublet element pattern. Ratios far from 1.0 in an unlike-doublet injector
  produce asymmetric impingement, uneven mixing, and a tilted combustion zone. Real injectors
  are designed to hit a momentum ratio close to 1.0 for each doublet pair.
