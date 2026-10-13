## Background

Higher Isp is not automatically better for an electric mission, and understanding why
requires balancing two competing masses.

Propellant mass decreases as Isp rises, following the rocket equation: higher exhaust velocity
means less propellant for the same delta-v. This term pushes Isp upward.

Power plant mass increases as Isp rises. To deliver a given total impulse in a given *time*
at a higher exhaust velocity, you need more thrust-power, because `P = F × c / (2η)`. At
fixed mission time and fixed thrust requirement, higher c means more power, more power means
a heavier solar array or reactor. This term pushes Isp downward.

The sum of the two has a minimum at the optimum exhaust velocity:

    c_opt = √(2 × α × η × t_burn)

Three consequences are worth internalising:

1. c_opt scales as **√t_burn**. A mission with four times as long to manoeuvre should use
   twice the exhaust velocity. If the schedule is tight, use a *lower* Isp.

2. c_opt scales as **√α**. Better power technology (higher α) justifies higher Isp. Nuclear
   power sources have much higher α than solar arrays at large distances from the Sun.

3. c_opt is independent of delta-v and payload mass. It depends only on the propulsion system
   and the schedule.

At the true optimum, propellant mass and power plant mass are roughly equal. If one dominates
the mass breakdown, the design is on the wrong side of the optimum. The report prints both
fractions explicitly so you can see the balance at a glance.

The mass breakdown is solved by fixed-point iteration because it is self-referential: power
plant mass depends on thrust, which depends on total initial mass, which includes power plant
mass. The iteration converges quickly; the unit tests for this function use a slightly
looser tolerance than elsewhere in the library precisely because of this residual coupling.

## Experiments to try

1. Set `isp` to 0 (use the optimum). Note the computed optimum Isp. Then set `isp` to half
   the optimum and re-run. The power plant mass should be smaller but the propellant mass
   larger, and the total initial mass should be higher — the design is on the propellant-heavy
   side of the optimum.

2. Change `days` from 300 to 1200 (four times as long). The optimum Isp should roughly
   double, because c_opt scales as √t. Confirm this from the formula. The Isp sweep table
   at the bottom shows the full curve for each run.

3. Reduce `alpha` from 120 to 30 W/kg (old or inner-planet-degraded solar arrays). The
   optimum Isp falls because the power plant is heavier per watt. Compare this against a
   nuclear-electric value of 500 W/kg — the optimum Isp roughly doubles again.

4. Set `dv` to 12000 m/s (a deep-space mission) and `days` to 600. The required propellant
   mass at the optimum Isp is still modest compared with a chemical propulsion solution for
   the same mission. The delta-v table at the bottom of the electric power sample
   (ch19-electric-power) shows the comparison by dry mass.

5. Enter a specific `isp` that is clearly above and below the optimum in succession. At both
   points the initial mass is higher than at the optimum; the curves are flatter above the
   optimum than below it, because propellant mass falls slowly with rising Isp while power
   plant mass grows quickly.

## Watch out for

- **Enter `isp` = 0 to use the computed optimum.** Any positive value overrides the optimum
  and evaluates the mass breakdown at that specific Isp. Use this to explore the sensitivity
  of the initial mass to Isp choice — the answer is often that a wide range of Isp values
  near the optimum gives similar total masses, because the minimum is broad.

- **`days` is the available firing time, not the mission duration.** A spacecraft may spend
  years in transit with only intermittent engine firing. Enter only the time the engine is
  actually running; coasting arcs add no propellant but do allow a lower optimum Isp by
  lengthening the effective burn period.

> At the true optimum, propellant mass ≈ power plant mass. If the report shows one of these
> dominating by a factor of three or more, the Isp is significantly off-optimum in that
> direction. This is a useful quick check without doing a full optimisation.

- **The result depends on total initial mass, which includes the power plant, which depends
  on thrust, which depends on total initial mass.** The fixed-point iteration resolves this
  coupling, but it assumes a constant specific power α throughout the burn. Real missions
  accumulate propellant depletion and the power plant mass is constant, so the thrust-to-mass
  ratio improves through the mission. This model treats the whole burn as a steady state.
