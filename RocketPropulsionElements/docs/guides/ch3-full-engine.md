## Background

The previous two samples presented c* and C_F separately. This one combines them into a
single call — `EngineAnalysis.Analyse` — and returns every Chapter 3 quantity in one
consistent record. The value is not just convenience: because all quantities are computed
from the same inputs in a single pass, they are guaranteed to be mutually consistent. There
is no risk of a mass flow computed at one pressure ratio disagreeing with an Isp computed at
another.

The one genuinely difficult step is inverting the area ratio to obtain exit pressure. The
area–Mach relation has no closed form for Mach number given area ratio, so the library
brackets and bisects on pressure ratio until the computed area ratio matches the requested
value to within floating-point tolerance. It is robust because the function is strictly
monotone on the supersonic branch.

The INTERPRETATION block at the bottom of the report is the engineering payoff. It identifies
the expansion state and, critically, raises a separation warning when `p2 < 0.4 × p3`. Flow
separation in an over-expanded nozzle is not a minor inefficiency — it produces unsteady,
asymmetric side loads that have destroyed flight hardware. The sample will tell you when your
combination of area ratio and ambient pressure puts you there.

## Experiments to try

1. Set `p3` to 0 (vacuum) and run. The nozzle is under-expanded — gas still has pressure
   when it leaves, and a larger nozzle could recover it. Note the optimum area ratio
   reported at the bottom (shown as "infinite" for vacuum, because more expansion always
   helps in vacuum).

2. Keep the defaults (p3 = 1.01325 bar, eps = 22) and raise `eps` to 100. The exit pressure
   drops well below ambient. The report switches to "OVER-EXPANDED" and eventually raises
   the flow separation warning. Reduce `p1` simultaneously to make separation occur sooner.

3. Find the optimum: set `eps` to the "optimum area ratio at this altitude" value printed in
   the report, then re-run. The expansion state should read "OPTIMUM" and the pressure thrust
   term should be nearly zero.

4. Lower `t1` from 3600 to 2500 K (representing a poorer propellant). c* and Isp both fall;
   C_F and exit pressure do not change. This confirms that C_F is purely a nozzle quantity
   and is unaffected by what is burning in the chamber.

5. Double `at` from 300 to 600 cm². Thrust doubles, mass flow doubles, Isp is unchanged.
   The throat area is a scale factor for the whole engine; the nozzle quality metrics
   (C_F, c*, Isp) are intensive properties that do not scale with size.

## Watch out for

- **`p1` and `p3` are in bar; `at` is in cm².** These are the same units as the c*/C_F
  sample. The internal conversions are `BarToPa` and `× 1×10⁻⁴`. Getting them wrong gives
  answers that are wrong by four orders of magnitude, and the output's physical
  plausibility check is the only protection.

> **The flow separation warning is serious.** When p₂ falls below approximately 0.4 × p₃,
> the output reads "FLOW SEPARATION WARNING". Real nozzles subjected to sustained separation
> develop asymmetric side loads that can rupture the nozzle wall or rip the engine from its
> mount. If this warning appears for your sea-level operating conditions, reduce the area
> ratio or raise the chamber pressure.

- **The exit Mach number is not a direct input or output of `EngineAnalysis.Analyse`.** It
  is implicit in the exit pressure: once you know gamma, p1, and p2, the Mach number follows
  from the isentropic relation. See the ch5-thermochemistry sample for the explicit Mach
  table.

- **The ideal payload fraction assumes isentropic, frozen, one-dimensional flow.** A real
  engine delivers roughly 92–96% of this theoretical Isp. See ch3-loss-budget for where the
  remaining few percent go.
