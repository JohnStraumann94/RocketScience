## Background

A rocket chamber runs at 3000–3600 K. Every structural metal melts below that: copper at
1358 K, stainless steel around 1700 K, the best nickel superalloys around 1700 K. Something
must remove heat from the wall continuously, or the chamber fails in seconds.

Regenerative cooling routes the fuel through channels milled or electroformed into the
chamber and nozzle walls before injecting it. The heat is absorbed and then burnt in the
chamber — it is not lost. Thermodynamically the cost is almost zero. The engineering
challenge is keeping the wall below its strength limit and the coolant below its degradation
temperature at heat fluxes of 20–80 MW/m² at the throat.

The balance is governed by two equations: `Q = h × A × (T_gas − T_wall)` gives the total
heat rate into the coolant, and `ΔT_coolant = Q / (m_dot_coolant × cp)` gives the coolant's
temperature rise. The design passes when the outlet temperature stays below the limit:
600 K for RP-1 (coking threshold), the local boiling point for cryogens, and much higher for
hydrogen (which has cp of 14300 J/(kg·K) — seven times kerosene — and no coking problem).

The comparison table at the bottom shows four coolants subjected to the same heat load. The
hydrogen row demonstrates why hydrogen engines can be run at extreme conditions: the coolant
barely warms up even at high heat flux, because its specific heat is so large and the warmed
hydrogen is injected and burnt anyway. The RL10 expander cycle is the extreme case — the heat
absorbed by the fuel in the cooling jacket is the only energy available to drive the turbopump.

## Experiments to try

1. Raise `h` from 8000 to 25000 W/(m²·K) (the throat region of a high-pressure engine). The
   heat rate and heat flux both increase sharply. Check whether the coolant is still adequate
   by watching the outlet temperature against the limit.

2. Increase `twall` close to `tgas`. The sample returns a validation error at `twall ≥ tgas`
   because heat flows down a temperature gradient only. Try a wall temperature of 800 K, then
   1200 K — the heat rate is proportional to `(tgas − twall)` and falls as the wall heats up.

3. Set `cp` to 14300 J/(kg·K) (LH₂) and observe how the temperature rise falls by nearly
   a factor of seven compared with RP-1. Then check the utilisation fraction — hydrogen
   can absorb much more heat than it receives at these conditions.

4. Set `mdotcool` to a value that makes `tOut` exceed `tlimit`. The report raises the
   inadequacy warning, computes the required additional flow, and lists five remedies. This
   is the diagnostic the design review would use when a thermal analysis shows a shortfall.

5. Reduce `area` from 1.2 to 0.3 m² (cooling only the throat region). The total heat rate
   falls proportionally but the average heat flux stays the same. A shorter cooled section
   reduces total heat load but concentrates it at the most critical point.

## Watch out for

- **`h` (heat transfer coefficient) is a simplified input**, not a computed result. In a real
  design, h is estimated using the Bartz correlation or similar convective heat transfer
  correlations that account for chamber diameter, gas properties, Mach number, and film
  cooling fractions. The values 5000–20000 W/(m²·K) are representative; the throat can
  exceed 50000 W/(m²·K) in a high-pressure engine.

- **`cp` is the coolant's specific heat at operating conditions.** For RP-1, cp is fairly
  constant near 2100 J/(kg·K). For hydrogen, cp varies strongly with temperature and
  pressure — 14300 J/(kg·K) is a representative value for gaseous hydrogen near 300 K and
  typical cooling jacket pressures. The actual variation along the jacket matters in a
  detailed thermal model.

> If `tOut` exceeds `tlimit` for RP-1, the warning "COOLING IS INADEQUATE" appears. With
> kerosene the mechanism is coking: carbon deposits on the channel wall, insulating it, which
> raises the wall temperature, which accelerates coking further. That runaway ends in a burn-
> through. The required additional coolant flow is printed as a direct design action.

- **Film cooling**, ablatives, and radiation cooling are listed as remedies when regenerative
  cooling is insufficient, but they are not computed here. They are qualitative options whose
  sizing would require separate analyses outside the scope of this sample.
