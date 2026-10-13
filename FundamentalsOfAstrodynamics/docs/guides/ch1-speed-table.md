## Background

For a circular orbit the vis-viva equation simplifies to v = sqrt(mu/r), and Kepler's third
law gives T = 2 pi sqrt(a³/mu). Both relationships are worth building intuition for as a
table rather than a formula.

Two features stand out immediately. First, speed falls only as the square root of radius: a
satellite 100 times further out still moves 10% as fast, not 1%. Second, the period grows as
the 3/2 power of the radius, which is much steeper — so while the speed drops modestly, the
time to complete one orbit grows dramatically. Geostationary altitude (35,786 km) is defined
by the radius at which the period equals one sidereal day: 23 hours, 56 minutes and 4 seconds,
not the 24-hour solar day.

The table makes it easy to see that there is no sharp boundary between "orbital" and "deep
space" altitudes. LEO satellites at 400 km move at about 7.66 km/s and take 92 minutes per
orbit. MEO GPS satellites at 20,200 km move at 3.87 km/s and take 12 hours. GEO satellites
at 35,786 km move at 3.07 km/s and take one sidereal day. The progression is smooth all the
way out.

## Experiments to try

1. Run with the defaults (200 to 40,000 km, 12 steps). Locate geostationary altitude
   (35,786 km) between two of the rows. Confirm that the period there is about 23.93 hours.

2. Set `min` to 200, `max` to 200, and `steps` to 1. You get a single row for low Earth
   orbit: about 7.784 km/s, 88.5 minutes. This is the rough lower bound for a stable orbit
   (below about 160 km drag de-orbits in hours).

3. Set `min` to 42164 and `max` to 42164. A single row for geostationary orbit: 3.075 km/s,
   approximately 23.934 hours. Note that this is a *sidereal* day. A solar day (24 h) would
   give a slightly different orbital radius.

4. Extend `max` to 384400 (the Moon's mean orbital radius). The Moon should appear at about
   1.018 km/s and a period near 27.3 days — consistent with observation.

5. Set `steps` to 60 for a dense sweep and scan for where the period crosses 2 hours, 12
   hours, and 24 hours. Those are the rough boundaries of LEO, MEO, and GEO.

6. Change `min` to 160 to see the absolute minimum altitude for a sustained circular orbit.
   The speed is just under 7.8 km/s and the period is about 87.7 minutes.

## Watch out for

- **The table uses altitude above the surface, not radius from the centre.** The Earth's
  radius (6378.137 km) is added internally. Do not add it yourself when typing `min` and
  `max`.

- **`steps` is clamped to the range 2 to 60.** Typing 1 gives you two rows (the minimum);
  typing 200 gives you sixty. If the table looks shorter than expected, check this value.

- **The period shown is the Keplerian period — time for one full revolution relative to
  inertial space.** It is not the time between successive passes over a given point on the
  ground, which also depends on Earth's rotation.

> The note at the bottom of the output reminds you that GEO is defined by the *sidereal* day
> (86164 s), not the solar day (86400 s). The difference — 3 minutes 56 seconds per orbit —
> is why a solar-day orbit would not be geostationary: the satellite would drift 0.986 degrees
> eastward per day.
