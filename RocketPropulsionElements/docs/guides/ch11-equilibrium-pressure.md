## Background

A solid motor has no valves, no pumps, and no throttle. Once ignited it burns to completion.
Chamber pressure is not set by an operator; it is determined by a self-balancing equilibrium
between gas generation at the burning surface and gas discharge through the choked throat.

Gas generation follows Saint Robert's law: burn rate `r = a × p^n`. The exponent n is the
single most important property of a solid propellant, for a reason that has nothing to do
with combustion chemistry and everything to do with stability.

Gas discharge through a choked throat is proportional to p¹. For stability, the discharge
rate must grow faster with pressure than the generation rate. This means the discharge
exponent (1) must exceed the generation exponent (n). If n < 1, a pressure perturbation
increases discharge faster than generation, and the system self-corrects. If n ≥ 1,
generation grows at least as fast as discharge, pressure runs away, and the motor ruptures.

The equilibrium pressure formula contains this stability criterion as an explicit exponent:
`p = [K × ρ × a × c*]^(1/(1−n))`. When n approaches 1, the exponent 1/(1−n) diverges and
the pressure becomes hypersensitive to any change in burning area. When n ≥ 1, the formula
has no physically meaningful positive solution.

The burning area sensitivity table is the most practically important output. A crack in the
grain exposes new surface, K jumps, and pressure jumps harder — amplified by the factor
1/(1−n). With n = 0.35 the exponent is 1.538, so a 10% area increase causes a 15.8%
pressure increase. With n = 0.80 the exponent is 5.0, so the same 10% area increase causes a
61% pressure increase. This is why grains are X-rayed before flight.

## Experiments to try

1. Set `n` to 0.95. The stability check still passes (n < 1), but the sensitivity exponent
   is 20. Look at the sensitivity table — a 10% burning area increase causes a pressure jump
   of over 500%. Then set `n` to 1.05. The report immediately stops and states the motor is
   unstable, with no equilibrium to compute.

2. Reduce `at` from 60 cm² to 30 cm². The klemmung K = Ab/At doubles and the equilibrium
   pressure increases by `2^(1/(1−n))`. For n = 0.35, this is about 2^1.54 ≈ 2.9, so
   pressure nearly triples. Confirm this against the klemmung sweep table.

3. Increase `ab` from 2.5 to 5.0 m² (crack the grain conceptually). The burning area doubles.
   The sensitivity table for +100% shows how the pressure rises, amplified by the exponent.
   Compare this amplification against n = 0.20 and n = 0.50.

4. Change `a` (burn rate coefficient) over a decade — from 1e-5 to 1e-4 SI. The equilibrium
   pressure changes strongly; this coefficient captures the propellant's intrinsic burn speed
   at any given pressure. A faster-burning propellant at the same geometry gives a higher
   equilibrium pressure.

5. Read the temperature sensitivity block at the bottom. A motor soaked cold burns more
   slowly at lower pressure; a motor soaked hot burns faster and at higher pressure. The
   total impulse barely changes, but the peak pressure and burn time both shift — which drives
   the case design and the guidance system's burn time model.

## Watch out for

- **`a` is in SI units with pressure in Pa and burn rate in m/s.** A typical APCP value is
  5×10⁻⁵ in these units, which gives burn rates of a few mm/s at operating pressure. This
  coefficient is not dimensionless; its numerical value changes completely if you use bar
  or psi. The unit label says "SI" as a reminder.

- **`at` is entered in cm²**, converted to m² by multiplying by 1×10⁻⁴. The default of
  60 cm² = 0.0060 m² is a modest thruster. The klemmung K = Ab/At is printed in the report.

> **n must be strictly below 1.** The sample checks this before doing any other computation.
> If n ≥ 1, the report returns a clear statement that the motor is physically impossible and
> stops. There is no workaround — the propellant formulation itself must be changed. No amount
> of case strengthening fixes an unstable burn exponent.

- **The equilibrium pressure formula requires c* as an input.** In practice, c* is measured
  from a hot-fire test or computed from propellant thermochemistry. The default of 1550 m/s
  is representative of APCP. A lower c* (poorer combustion quality) shifts the equilibrium
  to a lower pressure.
