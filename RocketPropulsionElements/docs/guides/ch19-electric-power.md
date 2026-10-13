## Background

Electric propulsion separates the propellant's two roles. In a chemical rocket, the
propellant provides both the mass to be thrown overboard and the energy to throw it. That
coupling caps exhaust velocity at what the chemistry can deliver — about 4500 m/s at best.
An electric thruster uses an external power source for the energy, supplying only mass from
the propellant. The chemical ceiling disappears and exhaust velocities of 15000–100000 m/s
become possible.

Energy conservation immediately replaces the chemistry constraint with a power constraint.
The kinetic power carried away in the jet is `P_jet = ½ × F × c`. The electrical power
consumed is that divided by the thruster efficiency: `P_elec = F × c / (2η)`. Rearranged:
`F = 2η P / c`. Thrust is inversely proportional to exhaust velocity at fixed power.
Doubling Isp halves the thrust for the same power. This is not a limitation of current
technology — it is a consequence of energy conservation and cannot be engineered away.

The numbers are sobering and worth sitting with. A Hall thruster at 5 kW produces roughly
300 mN — the weight of an apple. A Merlin 1D produces 845 000 N. Electric propulsion cannot
launch, land, or time-critical manoeuvre. What it can do is run for months. Dawn accumulated
more than 11 km/s of delta-v — more than any previous spacecraft — by firing almost
continuously for over five years, visiting two separate asteroid belt objects.

The power plant mass, characterised by specific power α in W/kg, is the hidden cost of high
Isp. Every watt of electrical power needed requires α⁻¹ kilograms of solar array or reactor.
This mass is why electric propulsion has an optimum Isp — examined in the next sample.

## Experiments to try

1. Fix all inputs and increase `isp` from 1800 to 3600 s (doubling Isp). The required power
   doubles (same thrust, twice the exhaust velocity). Power plant mass doubles. Thrust per
   kilowatt halves. The proportional relationships in the output are exact.

2. Look at the "Isp vs thrust at fixed power" table. Set your electrical power by adjusting
   `thrust` and `isp` so that `Required electrical power` is exactly 5.0 kW, then read the
   table. The thrust falls as 1/c — each row with twice the Isp shows half the thrust.

3. Reduce `eta` from 0.55 to 0.45 (a less efficient thruster). Required power rises for the
   same jet output. Efficiency directly drives the power plant mass and is one of the main
   figures of merit for electric thruster development.

4. Increase `hours` from 8000 to 40000 h (a long deep-space mission). The propellant
   consumed grows but is still tiny compared with chemical propulsion for the same delta-v
   (shown in the table at the bottom). Total impulse grows proportionally.

5. In the chemical comparison at the bottom, read the "power plant mass per newton" line.
   Compare it against the thrust-to-weight ratio of a chemical engine (60–150 N per kg).
   Electric propulsion is four to five orders of magnitude worse on this metric, which is
   why it cannot compete on thrust but competes decisively on total impulse per kilogram.

## Watch out for

- **`thrust` is in newtons, not kilonewtons.** The default of 0.25 N is a reasonable Hall
  thruster. Entering 250 would be 250 N — a very large electric thruster. The output
  confirms the unit by also printing in millinewtons.

- **`isp` for electric thrusters is typically 1000–5000 s**, not the 300–450 s range of
  chemical engines. A Hall thruster runs 1600–2000 s; a gridded ion engine 3000–4500 s. The
  higher the Isp, the more power is needed for the same thrust.

> The thrust-per-kilowatt figure is the key practical metric for comparing electric
> thrusters. Hall thrusters achieve roughly 60–70 mN/kW; gridded ion engines about
> 30–40 mN/kW, trading thrust density for higher Isp. The trade-off between these two
> thruster types is shown directly in the Isp-vs-thrust table.

- **`alpha` (specific power) degrades with solar distance** as the inverse square of distance
  from the Sun. At Mars distance (1.52 AU) a solar array delivers about 43% of its 1 AU
  power. Outer planet missions (Jupiter, 5.2 AU) receive about 4%. This is why electric
  missions beyond the asteroid belt generally require nuclear power sources.
