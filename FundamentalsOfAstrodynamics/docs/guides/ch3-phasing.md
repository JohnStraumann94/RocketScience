## Background

Suppose a target is 30° ahead of you in the same circular orbit. The natural impulse is to
thrust forward — but that is exactly wrong. Thrusting prograde raises your orbit, increases
your period, and you fall further behind. The correct response is to brake.

Slowing down drops you into a smaller, faster-period phasing orbit. After one (or more)
complete revolutions in that lower orbit you arrive back at the departure point having gained
the required angle on the target. A second burn, equal and opposite, restores the original
orbit — and you are now in the right place to rendezvous.

This is the fundamental logic of every orbital rendezvous ever flown, from the Gemini
programme's first experiments to routine ISS cargo deliveries. The mathematics is a Hohmann
transfer between two versions of the same orbit: the phasing orbit has a period chosen so
that the angle deficit closes over exactly the chosen number of revolutions.

The output confirms this with the sign of the first burn: negative (retrograde) when the
target is ahead, positive (prograde) when the target is behind.

## Experiments to try

1. Run with the defaults (400 km orbit, 30° ahead, 1 revolution). The first burn should be
   retrograde — you slow down to catch a target ahead. The total delta-v is modest, a few
   tens of metres per second.

2. Set `phase` to −30 (the target is 30° behind you). The first burn is now prograde — you
   speed up. The delta-v magnitude should be nearly identical to the 30° case, just opposite
   in sign.

3. Increase `revs` to 3 with `phase` still at 30. The phasing orbit changes only slightly
   between each revolution, so the delta-v per manoeuvre falls — spreading the catch-up over
   more orbits is cheaper in fuel. The total time roughly triples.

4. Set `phase` to 180 — the target is on the opposite side of the orbit. With `revs` = 1
   you must make half a revolution of extra (or less) travel. The phasing orbit differs
   significantly from the original and the delta-v is much larger. Increase `revs` to reduce
   the cost.

5. Set `radius` to 384400 (the Moon's orbit) with `phase` = 1 and `revs` = 1. The burn is
   tiny — orbital mechanics at lunar distance are very gentle — but the total manoeuvre time
   is about two lunar periods, nearly two months.

## Watch out for

- **`revs` must be at least 1.** The sample clamps values below 1 to 1. Entering 0 or a
  negative number does not make physical sense and is silently corrected.

- **`phase` is in degrees, not radians.** A value of 3.14 is interpreted as 3.14 degrees,
  not as pi radians.

- **The first burn and second burn are equal in magnitude but opposite in sign.** This is
  exact in the two-body model; real rendezvous operations require trim burns to compensate for
  navigation errors, drag differential and finite burn duration.

> The paradox — slowing down to catch something ahead — surprises almost everyone the first
> time. It is a direct consequence of Kepler's third law: lower orbits have shorter periods.
> The orbit is not a road; speed and position are coupled through the gravitational potential
> in a way that has no everyday analogy.
