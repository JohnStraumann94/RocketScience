## Background

A turbopump must raise propellant from near-tank-pressure (roughly 3 bar) to above chamber
pressure. The hydraulic power required is `P = m_dot × Δp / (ρ × η)`. The density in the
denominator is the key insight: for a fixed mass flow and pressure rise, pump power is
inversely proportional to propellant density.

Liquid hydrogen has a density of 71 kg/m³ — about one eleventh of RP-1 at 810 kg/m³. A
hydrogen pump moving the same propellant mass to the same pressure therefore needs roughly
eleven times the shaft power of a kerosene pump. The density penalty comparison table at the
bottom of the report puts this side by side for five propellants — the contrast between LOX
and LH₂ makes the engineering challenge of hydrogen fuel pumps immediately obvious. The
SSME's high-pressure fuel turbopump extracted about 52 MW from a unit weighing approximately
350 kg, spinning at 35000 rpm.

Pump head `H = Δp / (ρg)` expresses the same pressure rise as an equivalent column of the
fluid. Head is the natural design variable because a given impeller at a given tip speed
produces a nearly constant head regardless of the fluid. A hydrogen pump therefore needs far
greater head than a LOX pump for the same pressure rise — in practice, multiple stages.

Specific speed is the dimensionless similarity parameter that determines the correct impeller
type: radial (centrifugal) for low flow and high head, mixed-flow in the middle, axial for
high flow and low head. A mismatch between specific speed and impeller type causes poor
efficiency regardless of how carefully the impeller is designed.

## Experiments to try

1. Change `rho` from 810 (RP-1) to 71 (LH₂). The required power jumps by a factor of
   approximately 11.4, the head becomes enormous, and the specific speed changes. The density
   penalty table shows this comparison for all five common propellants simultaneously.

2. Raise `dp` from 200 to 400 bar (a high-pressure staged-combustion engine). Power scales
   linearly with pressure rise. Compare the result for RP-1 with that for LH₂ at the same
   conditions to see the combined effect of density and pressure on turbopump power.

3. Change `rpm` from 25000 to 10000 rpm. The specific speed falls substantially. If it drops
   below 0.3, the report recommends a multi-stage design or a higher shaft speed. Then raise
   to 40000 rpm and watch the specific speed move into the radial or mixed-flow range.

4. Set `eta` to 0.90 (a very efficient pump) versus 0.65 (a marginal one). The power
   difference is the energy dissipated as heat in the pump, which appears in the "Power lost
   to inefficiency" line. For a large engine, an extra 5 percentage points of efficiency can
   be worth hundreds of kilowatts.

5. With RP-1 defaults, compute the torque at 25000 rpm. Then switch to LH₂ at the same
   conditions — the torque is much higher because the power is much higher even at the same
   shaft speed. High torque at high speed is what makes hydrogen turbopumps so mechanically
   demanding.

## Watch out for

- **`dp` is in bar, not MPa or pascals.** 200 bar = 20 MPa. `mdot` is in kg/s through this
  one pump — if the engine has both an oxidiser pump and a fuel pump, size them separately.

- **The specific speed formula uses SI units throughout: ω in rad/s, Q in m³/s, H in m, and
  g in m/s².** The result is the dimensionless form, not the older US customary form (which
  uses rpm, US gallons per minute, and feet). Published impeller type charts should use the
  same dimensionless convention for the ranges quoted in the output.

> The cavitation warning at the bottom is qualitative. The actual NPSH requirement depends on
> the propellant's vapour pressure at operating temperature, the pump inlet velocity, and the
> inducer's suction specific speed. Cryogenic propellants are especially problematic because
> they are already at their boiling point — any local pressure drop causes immediate
> vapourisation.

- **Efficiency `eta` is the pump hydraulic efficiency**, not the overall turbopump system
  efficiency. System efficiency also includes turbine efficiency, bearing losses, and seal
  leakage. A pump at 75% hydraulic efficiency in a system where the turbine is 70% efficient
  gives an overall system efficiency of about 52%.
