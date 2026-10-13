## Background

A sun-synchronous orbit exploits the J2 nodal regression rather than fighting it. By choosing
a retrograde inclination (slightly above 90°), the node drifts eastward at exactly the rate
the Earth orbits the Sun: 360° per tropical year, or about 0.9856°/day. The orbital plane
then maintains a nearly fixed angle to the Sun–Earth line, so the satellite crosses the
equator at the same local solar time on every orbit, every day of the year.

For Earth-observation missions this consistency is crucial. Images taken weeks or months apart
have the same solar elevation angle and the same shadow geometry, which makes it possible to
detect land-use changes, monitor vegetation health, or track ice extent without the confounding
variation in illumination. Landsat, Sentinel, and essentially every operational weather
satellite in polar orbit uses a sun-synchronous ground track.

Inverting the nodal regression formula for inclination gives:

    cos(i) = −(2/3) (p²/Re²) (1/J2 n) * omega_Sun

where omega_Sun = 2π/year is the required drift rate. The result is always greater than 90°
(retrograde). At low altitudes the inclination is slightly above 96°; it increases with
altitude and reaches a physical limit at about 5,975 km, where cos(i) would need to be below
−1, which is impossible.

## Experiments to try

1. Run with the default (800 km altitude). The inclination should be about 98.6° and the
   resulting nodal drift should match 0.9856°/day to the precision shown.

2. Reduce `altitude` to 300 km. The inclination falls slightly toward 96.7°. The orbit is
   lower and faster (more orbits per day), so the node precesses faster per orbit, and a
   shallower retrograde angle is enough to hit the required daily rate.

3. Increase `altitude` to 1200 km. The inclination rises above 100°. The orbit is slower
   and a more steeply retrograde inclination is needed.

4. Set `altitude` to 5500 km and look at the altitude sweep. Somewhere between 5500 km and
   the `max` value in the sweep, the table will show "no sun-synchronous solution exists" —
   you have passed the altitude limit where J2 can supply the required precession.

5. Set `ecc` to 0.05 (a slightly elliptic sun-synchronous orbit). The semi-latus rectum p
   decreases for the same semi-major axis, so the precession rate changes and the required
   inclination adjusts. Compare with the circular case.

6. Adjust `min` and `max` to sweep 400–1800 km and read off the inclinations at 700 km
   (Landsat, about 98.2°) and 800 km (Sentinel-2, about 98.6°) to verify the model against
   known missions.

## Watch out for

- **The inclination is always retrograde (above 90°).** A sun-synchronous orbit cannot be
  prograde: the nodal regression formula requires cos(i) < 0. The sample will not return an
  inclination below 90°.

- **`ecc` should be close to zero for typical sun-synchronous orbits.** The inclination
  formula is exact for any eccentricity, but the sun-synchronous condition depends on the
  mean orbital elements, not the osculating elements. At significant eccentricity the
  actual drift will differ from the secular formula.

- **`altitude` is in kilometres above the surface.** The radius used in the formula is
  6378.137 + altitude. Entering a radius from the centre instead of an altitude will give
  a wrong inclination.

> Above about 5,975 km no sun-synchronous solution exists because J2 simply cannot precess a
> circular orbit fast enough. This is why sun-synchronous orbits are always low — they are
> LEO-to-MEO altitude operations, never GEO or beyond. GPS satellites at 20,200 km are so
> high that their nodal drift rate, even at 90° inclination, is far below the required 0.986°
> per day.
