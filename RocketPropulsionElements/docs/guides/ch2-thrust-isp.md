## Background

The two-term thrust equation is the foundation on which every other equation in the book
stands. It says that a nozzle produces force by two distinct mechanisms: momentum carried away
by the exhaust, and the pressure difference between the exit plane and the surrounding
atmosphere. Both are real forces acting on a real surface; neither is a approximation.

The effective exhaust velocity c is the single number that honestly summarises both terms. It
is the velocity a hypothetical perfectly-expanding nozzle would need to produce the same thrust
from the same mass flow — a level playing field for comparing engines at different altitudes.
Dividing c by g₀ = 9.80665 m/s² gives specific impulse in seconds. The constant g₀ is a
defined conversion factor, not the local gravitational field; Isp in seconds has the same
numerical value in any consistent unit system, which is why the convention persists.

The sign of the pressure thrust term is the most important single thing this sample
demonstrates. At sea level with a vacuum-optimised nozzle, p₂ can fall below p₃, making the
pressure term negative and actively subtracting from thrust. That is not an error — it is the
physics of an over-expanded nozzle.

## Experiments to try

1. Set `p3` to 0 (vacuum). The pressure thrust becomes `p2 * a2`, purely positive, and the
   gap between sea-level and vacuum thrust appears as the line "Thrust gained by leaving the
   atmosphere". Note the check `p3 * A2` confirms this exactly.

2. Set `p2` equal to `p3` (both to 101325 Pa). The pressure thrust term drops to zero, and
   `c` equals `v2` exactly. This is the definition of the perfectly-expanded nozzle — the
   condition where the exit pressure matches ambient.

3. Raise `p2` to 200000 Pa while keeping `p3` at 101325 Pa. The pressure thrust becomes
   positive (under-expanded). Now lower `p2` to 40000 Pa; it goes negative (over-expanded).
   Watch the percentage share of total thrust flip sign.

4. Increase `a2` from 1.5 to 3.0 m² while keeping all pressures constant. The pressure
   thrust doubles exactly, but the momentum thrust is unchanged. Thrust scales linearly with
   exit area only through the pressure term; the momentum term cares only about mass flow and
   velocity.

5. Set `v2` to 3500 m/s (a hydrogen engine level) and observe the implied Isp. Then set
   `mdot` back to 250 kg/s and note that doubling v2 doubles c but does not double thrust —
   the pressure term also changes, and with p2 at 70000 Pa and p3 at 101325 the nozzle is
   over-expanded, so the correction is negative.

## Watch out for

- **All pressures are in pascals.** 101325 Pa is sea level. 70000 Pa is below ambient — which
  means the nozzle in the default setup is over-expanded at sea level. That is intentional; it
  shows the sign flip.

- **`v2` and `c` are not the same.** Exit velocity v2 is the actual gas speed at the exit
  plane. Effective exhaust velocity c folds in the pressure term: `c = F / m_dot`. When the
  nozzle is perfectly expanded, c = v2. Otherwise they differ, sometimes significantly.

- **Isp = c / g₀, where g₀ = 9.80665 m/s² exactly.** It is not the local gravity and it
  does not change with altitude. Any code that substitutes the local g is wrong.

> The pressure thrust term can be negative in an over-expanded nozzle (p₂ < p₃). The output
> shows "NEGATIVE (over-expanded)" in that case. This is the correct behaviour of the physics,
> not a data entry error.

- **The "vacuum" comparison row holds the engine identical** — same v2, same p2, same a2 —
  and sets p3 to zero. The thrust gain is exactly `p3 * a2`, shown as a round-trip check at
  the bottom of the report. If your numbers do not satisfy that identity, one of your inputs
  is physically inconsistent.
