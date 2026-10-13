## Background

Total impulse, mass fraction, mass ratio, and thrust-to-weight are the four bookkeeping
quantities that engineers use to describe a propulsion system without reference to what it
is burning or how the nozzle is shaped. They connect the engine's performance to the vehicle's
trajectory, and understanding their relationships prevents a class of errors that arise from
treating them as independent.

Total impulse is the time-integral of thrust: for constant thrust, `It = F × tb`. It is the
quantity that most directly predicts what a stage can accomplish — two engines with the same
total impulse will change the same vehicle's velocity by the same amount, regardless of
whether one burned for ten seconds at high thrust or a hundred seconds at low thrust.

Mass ratio as defined in this library follows Sutton's convention: `MR = mf / m0`, the ratio
of final to initial mass. This is less than one and equals `1 - zeta` exactly, where zeta is
the propellant mass fraction. Many other texts define mass ratio as `m0 / mf`, which is
greater than one. Both conventions appear in this application; see the warning in the output
and the note in the help documentation.

Thrust-to-weight at ignition decides whether the vehicle can leave the pad. The report flags
this explicitly: a first stage needs T/W above 1.0, and in practice closer to 1.2–1.5 to
avoid heavy gravity losses during the slow initial climb. Upper stages and spacecraft can
operate below 1.0 because they are already in a trajectory, not fighting the pad.

## Experiments to try

1. Set `thrust` to 500 kN and `m0` to 100000 kg. The lift-off T/W drops to about 0.51. The
   report flags this as an upper stage or in-space burn. Now raise `thrust` to 1200 kN and
   watch T/W exceed 1.0 — the vehicle can lift off.

2. Reduce `mp` from 88000 to 70000 kg (a worse propellant fraction). The implied Isp drops
   because the same thrust for the same burn time requires the same impulse, but is now spread
   over less propellant. The consistency check `m_dot × tb` should still reproduce `mp`.

3. Keep everything at the defaults and observe the "Ideal delta-v" line at the bottom. This is
   the maximum velocity the stage can deliver in field-free space — the rocket equation applied
   directly. Now halve `mp` to 44000 kg and note how much delta-v is lost: less than half,
   because the relationship is logarithmic.

4. Set `mp` equal to `m0`. The sample returns a validation error: propellant mass must be
   strictly less than initial mass, because the vehicle needs structure and payload. This is
   the guard clause that prevents a physically meaningless mass ratio of zero.

5. Raise `burntime` from 160 to 320 s while holding `thrust` and `mp` constant. The implied
   Isp halves — the same total impulse over the same propellant mass, but now the engine must
   be delivering half the thrust. The consistency check at the top still closes.

## Watch out for

- **`thrust` is entered in kilonewtons, not newtons.** The sample converts it internally
  (`× 1000`) before calling the library. The output labels confirm the unit; if the thrust
  figures look a thousand times too small, check this first.

> **Mass ratio convention.** `Performance.MassRatio(m0, mf)` returns `mf/m0` — Sutton's
> definition, less than one. The report shows this and also shows its reciprocal `m0/mf`.
> The rocket equation uses `ln(m0/mf)`, which is the *reciprocal*. The output prints both so
> you can verify which is which.

- **The consistency check `m_dot × tb` reproduces `mp`.** This is not a coincidence — it is
  a requirement. If the check fails, the inputs are self-inconsistent: the engine cannot burn
  the stated propellant in the stated time at the stated thrust. The sample does not enforce
  this in real time, but the discrepancy will be obvious in the report.

- **The ideal delta-v ignores gravity, drag and back-pressure losses.** Real launchers lose
  800–2000 m/s to these effects. The number here is the theoretical ceiling from the rocket
  equation alone; see the Chapter 4 samples for the full loss accounting.
