## Background

In a solid motor the thrust-versus-time profile is entirely determined by grain geometry.
There are no valves to open or close, no throttle to adjust. The shape of the propellant
cross-section at the moment of casting is the only thing that controls how the thrust
evolves through the burn. Getting the grain geometry right is the central design art of
solid motor engineering.

The BATES grain (Ballistic Test and Evaluation System) is the canonical simple geometry: a
cylinder with a central circular perforation, inhibited on its outer surface (bonded to the
case) and left exposed on both end faces. As the web burns outward, two competing area
contributions evolve:

- The inner cylindrical surface grows as the core opens out (progressive contribution).
- The end-face annular area grows as the core opens, but the grain shortens as the ends
  burn back (partially regressive contribution).

For a length-to-diameter ratio near 2–3, these effects nearly cancel and the total burning
area stays approximately constant — a neutral burn profile. The simulation shows whether
the chosen L/D actually delivers neutrality or whether progressive or regressive behaviour
dominates.

The simulation marches the web forward in small time steps. At each step it computes the
burning area from the geometry, uses the equilibrium pressure formula to find the chamber
pressure at that burning area, reads the burn rate from Saint Robert's law, and advances the
web. This is the same self-consistent internal ballistics loop a real motor design code runs.

## Experiments to try

1. Increase `len` from 500 to 900 mm (higher L/D). The grain becomes longer relative to its
   diameter. The end faces contribute less total area, the inner cylinder dominates, and the
   burn becomes more progressive — thrust rises through the burn. The profile classification
   block confirms this.

2. Reduce `len` to 200 mm (low L/D). The end faces now dominate early, the grain shortens
   quickly, and the burn becomes regressive. Compare the thrust at 10%, 50%, and 90% of burn.

3. Change `id` from 70 to 120 mm (a larger perforation). The web thickness `(od − id) / 2`
   shrinks and the burn is shorter. The initial burning area is larger (the core is already
   wider), so the initial thrust is higher. The profile classification may shift.

4. Increase `cf` from 1.55 to 1.80 (a higher-expansion nozzle). Thrust scales as
   `F = C_F × p × At`; raising C_F raises thrust at every instant. Total impulse rises too.
   Chamber pressure and burn rate are unchanged — C_F only scales the thrust output.

5. Halve `dt` from 0.02 to 0.01 s. The mass balance error in the performance summary should
   roughly halve — it is the accumulated truncation error of the time march. If it does not
   halve, the step size was already small enough that other numerical effects dominate.

## Watch out for

- **All geometry inputs are in mm**, converted to metres internally. The outer diameter `od`
  is the case inner diameter. The grain outer surface is the case bore — it is inhibited (no
  burning) and the full thickness is the web available to burn.

- **`n` must be below 1** — the same constraint as in the equilibrium pressure sample. If n
  ≥ 1 the sample throws a validation error before the simulation runs, because no stable
  equilibrium exists at any burning area.

> The volumetric loading fraction reported at the top is the ratio of propellant volume to
> the available case bore volume (`π/4 × od² × length`). High loading (above 0.85) is
> desirable for mass efficiency but leaves little room for inhibitor and case liner. For
> BATES grains the loading is limited by the web thickness — a very thin web gives high
> loading but also means the burn time is short.

- **The burn simulation stops when `web ≥ maximum_web`**, where `maximum_web = (od − id) / 2`.
  This is the geometry burnout point — the perforation has opened to the case inner wall.
  In a real motor, the case liner and inhibitor provide a small additional burn time after
  this point, which is not modelled here. The "mass balance error" line shows how closely
  the simulated burnt mass tracks the actual grain mass.

- **`cf` is entered directly** — it is not computed from a nozzle geometry in this sample.
  Use ch3-cstar-cf or ch3-full-engine to find C_F from a specific expansion ratio and then
  enter it here.
