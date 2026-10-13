## Background

A planetary flyby looks, from the planet's frame of reference, like a simple hyperbolic orbit.
The spacecraft arrives from far away with speed v_infinity, follows a hyperbolic arc around
the planet, and departs with the same v_infinity — magnitude unchanged, direction rotated by
the turning angle delta:

    sin(delta / 2) = 1 / e

where e is the eccentricity of the flyby hyperbola, determined by the periapsis distance and
the hyperbolic excess speed:

    e = 1 + r_p v_infinity² / mu

The speed is unchanged, but the direction is not. The planet is itself moving around the Sun
at several km/s. In the heliocentric frame, rotating the velocity vector adds vectorially to
the planet's orbital velocity, and the spacecraft can gain up to 2 v_infinity sin(delta/2) of
heliocentric speed — without using any propellant. The energy comes from the planet's orbital
momentum. Voyager 2 gained roughly 10 km/s from a Jupiter flyby; Jupiter's orbital energy
decreased by an amount far too small to measure.

Whether the spacecraft gains or loses speed in the heliocentric frame depends on which side
of the planet it passes. Flying past the trailing hemisphere (relative to the planet's orbital
motion) adds speed; flying past the leading hemisphere removes it. Mercury missions (MESSENGER,
BepiColombo) use repeated leading-hemisphere flybys of Venus and Mercury itself to shed energy
and fall into a low Mercury orbit.

## Experiments to try

1. Run with the defaults (v_inf = 5.6 km/s, periapsis = 100,000 km, Jupiter mu). Read the
   turning angle and maximum heliocentric gain. Then check the sensitivity table — it shows
   how the gain varies if you halve or double the periapsis distance.

2. Reduce `periapsis` to 71492 (Jupiter's surface radius — the minimum physically possible).
   The eccentricity drops toward 1 and the turning angle increases sharply. This is the
   maximum possible turning angle for this v_infinity.

3. Increase `periapsis` to 500000. The trajectory is a gentler curve, the turning angle
   falls, and the gain decreases. A more distant flyby is always less effective.

4. Change `mu` to 398600.4 (Earth) with `periapsis` = 6578 (just above the surface). An
   Earth gravity assist at low altitude — the type used by many deep-space missions. Note how
   much smaller the gain is compared with Jupiter.

5. Increase `vinf` to 15 km/s (a fast flyby). Higher v_infinity means a flatter hyperbola
   (larger e), less turning, but more absolute speed to redirect. Watch whether the gain
   increases or decreases.

## Reading the output

The sensitivity table at the bottom of the output runs the same calculation for periapsis
values of 0.25×, 0.5×, 1×, 2×, 4× and 8× the value you entered. This makes it easy to see
the trade-off between flyby altitude (constrained by the planet's atmosphere or radiation
belts) and trajectory gain.

## Watch out for

- **`periapsis` is the distance from the planet's centre, not the altitude above the
  surface.** For Jupiter's default value of 100,000 km, the altitude above the cloud tops is
  about 28,500 km. Entering the altitude above the surface would understate the periapsis
  significantly.

- **The heliocentric gain printed is a maximum.** It assumes the planet's orbital velocity is
  perpendicular to the incoming v_infinity vector, which is the optimal geometry. Real flyby
  geometries are usually less favourable.

- **Changing `mu` alone without also checking that `periapsis` is above the surface is a
  common mistake.** Jupiter's radius is 71,492 km; entering `periapsis` = 6578 for Jupiter
  would put the flyby deep inside the planet.

> A gravity assist does not create energy from nothing. It transfers momentum from the planet
> to the spacecraft. The planet slows down by a tiny, immeasurable amount. What makes the
> manoeuvre remarkable is not conservation of energy — which is not violated — but the
> leverage ratio: a small change in planetary momentum (measured in planetary-mass × km/s)
> produces a large change in spacecraft speed (measured in spacecraft-mass × km/s), because
> the planet is so much more massive than the spacecraft.
