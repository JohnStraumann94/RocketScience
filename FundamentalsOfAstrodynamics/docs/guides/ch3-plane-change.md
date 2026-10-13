## Background

Changing an orbit's plane without changing its size or shape requires the velocity vector to
be rotated through the plane-change angle. Because the before and after velocities form an
isosceles triangle, the required delta-v is:

    dv = 2 v sin(di / 2)

Speed appears as a direct multiplier. In low Earth orbit, where v is about 7.7 km/s, even a
10° plane change costs over 1.3 km/s — more than a Hohmann transfer to MEO. A 90° plane
change in LEO would cost more than it took to reach orbit in the first place.

Two strategies reduce this cost. First, do the plane change where the orbit is already moving
slowly: at the apoapsis of a highly elliptical orbit. Second — and almost always better —
fold the plane change into a burn that is being made for another reason, using the combined-
manoeuvre formula:

    dv = sqrt( vi² + vf² − 2 vi vf cos(di) )

This is the law of cosines. You change speed *and* direction in one burn, and the geometry
works in your favour. The geostationary apogee kick motor is a textbook example: it
circularises a GTO transfer orbit and simultaneously removes the 28.5° inclination from the
Cape Canaveral launch, all in one burn at GEO altitude where the vehicle is moving slowly.

The defaults model exactly that GTO-to-GEO apogee kick.

## Experiments to try

1. Run with the defaults. The combined burn should cost noticeably less than the separate
   burns (speed change first, then plane change). The saving is typically several hundred
   metres per second — larger than the entire Hohmann first burn.

2. Change `di` to 0. The combined burn reduces to a pure speed change and the saving over
   "separate" drops to zero. The comparison becomes trivial; re-read the formula to confirm
   why.

3. Set `di` to 90. The plane change now costs a great deal, and the combined manoeuvre saves
   a large fraction of it. At 90° the vectors are perpendicular and the law-of-cosines
   formula gives dv = sqrt(vi² + vf²).

4. Set `vleo` to 7.905 (the circular speed at one Earth radius). The comparison section will
   show how expensive a plane change is at the absolute minimum altitude. Compare it with the
   apogee speed (`vi`) to see the factor of roughly 5 difference in cost for the same
   geometric rotation.

5. Set `vi` equal to `vf` (a pure plane change with no speed change). The saving over
   "separate" is maximised in this case, and the combined formula simplifies to
   2 v sin(di/2), which is the simple plane-change formula — no saving at all because there
   is nothing to fold the plane change into.

## Watch out for

- **`vi` and `vf` are speeds, not velocities.** They are scalar magnitudes with no direction.
  The angle between the corresponding velocity vectors is given by `di`, the plane-change
  angle.

- **The combined manoeuvre is only possible when a speed change and a plane change coincide.**
  If you need a plane change at a point on the orbit where no other burn is planned, you
  cannot fold it in — it costs the full 2 v sin(di/2).

- **The savings are largest when vi and vf are similar in magnitude and di is large.** When
  one speed is much larger than the other the triangle is very unequal and the combined
  formula offers less advantage.

> The comparison section in the output shows the same plane change performed at LEO speed
> (about 7.7 km/s) versus at GTO apogee speed (about 1.6 km/s). The ratio is roughly 5 to 1.
> This is why every geostationary satellite climbs to GEO before doing its inclination work,
> and why it would be disastrously expensive to do that work in the parking orbit instead.
