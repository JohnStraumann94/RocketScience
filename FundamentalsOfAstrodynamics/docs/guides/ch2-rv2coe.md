## Background

A radar track or GPS fix gives position and velocity directly — three numbers in kilometres
and three in kilometres per second. Catalogues, mission plans and human intuition all work in
the six classical orbital elements instead: semi-major axis, eccentricity, inclination, right
ascension of the ascending node (RAAN), argument of periapsis, and true anomaly. This
conversion is the bridge between the two representations.

The algorithm builds three vectors from the state:

- **h = r × v** — the angular momentum vector, perpendicular to the orbital plane, so its
  direction fixes the plane's orientation in space.
- **n = K × h** — the node vector, which lies in the equatorial plane and points at the
  ascending node.
- **e = [(v² − mu/r) r − (r·v) v] / mu** — the eccentricity vector, which always points
  from the focus towards periapsis.

Each element is then read off one of these vectors using dot products and arccosines, with a
quadrant test (usually checking the sign of the z-component) to resolve the 0-to-360
ambiguity that arccos cannot handle alone.

The defaults are the standard worked example from Vallado (*Fundamentals of Astrodynamics and
Applications*, Example 2-5). Running with these values lets you cross-check the output against
the published answer: a = 36,127.343 km, e = 0.832853, i = 87.870°, RAAN = 227.898°,
argp = 53.38°, nu = 92.335°.

## Experiments to try

1. Run with the defaults and verify the round-trip error at the bottom of the output. It
   should be below 1e-9 km in position and 1e-12 km/s in velocity — machine precision.

2. Change `vz` to 0. You now have a trajectory in the equatorial plane. The inclination
   should be exactly 0°, and RAAN becomes undefined (any longitude works). Check whether the
   output reports RAAN as 0 or flags the special case.

3. Zero out `ry` and `rz` as well as `vy` and `vz`, leaving only `rx` and `vx`. This is a
   radial trajectory — the angular momentum is zero and the conversion will reject it. The
   red output message will tell you what went wrong.

4. Enter a circular orbit explicitly: set `rx` to 7000, everything else to 0, and `vy` to
   sqrt(398600.4418 / 7000) ≈ 7.546 km/s. The output should show e ≈ 0, i = 0°, and no
   meaningful argp or nu (they are undefined for circular equatorial orbits).

5. Use the output from the coe2rv sample as input here. The round-trip should return the
   original six elements to full precision.

## Watch out for

- **RAAN, argp, and nu are undefined in special cases.** A circular orbit has no periapsis
  (argp is undefined), an equatorial orbit has no node (RAAN is undefined), and a circular
  equatorial orbit has neither. The code defines conventional values (usually zero) in these
  cases rather than refusing to run.

- **Angles are output in degrees.** The internal computation works in radians; the display
  converts to degrees. Do not re-enter the output in radians unless you also change the
  units of the input boxes, which are labelled in km and km/s.

- **The velocity components are in an inertial (ECI) frame, not relative to the ground.**
  A satellite moving east at 7.7 km/s has a significant component of that speed coming from
  Earth's rotation — but this sample does not account for Earth's rotation when displaying
  the state.

> The true anomaly printed is the value at the *instant* described by the state vector. It
> is not a constant property of the orbit; it changes with time. All five of the other
> elements are constants of the unperturbed two-body orbit.
