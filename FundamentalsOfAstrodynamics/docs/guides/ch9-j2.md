## Background

The Earth is not a sphere. Centrifugal flattening during formation made it about 21 km wider
at the equator than at the poles, captured by the second zonal harmonic J2 = 1.08263 × 10⁻³.
That equatorial bulge exerts a gravitational torque on any inclined orbit, and the orbit
responds like a gyroscope — the angular momentum vector precesses rather than tilting.

J2 causes two secular drift rates that accumulate without bound over time:

    dRAAN/dt = −(3/2) n J2 (Re/p)² cos(i)   — nodal regression
    dargp/dt =  (3/4) n J2 (Re/p)² (5cos²i − 1)   — apsidal rotation

Both are proportional to the mean motion n and inversely proportional to p² = a²(1−e²)².
Lower, faster orbits precess faster.

Two special cases are exploited in practice. Setting cos(i) negative (i.e. i > 90°, a
retrograde orbit) makes the node drift eastward at a rate that can be tuned to match Earth's
orbital motion around the Sun — the sun-synchronous condition. Setting 5cos²(i) = 1 gives
i = 63.4349° (or 116.57°), at which the apsidal rotation stops completely — the critical
inclination, used by Molniya satellites to keep their high apogee permanently over the same
hemisphere.

## Experiments to try

1. Run with the defaults (Molniya orbit, i = 63.4349°). The apsidal rotation rate should be
   essentially zero — confirm it is below 0.001 deg/day. The nodal drift should be several
   degrees per day westward.

2. Change `inc` to 51.6 (the ISS inclination). The apsidal rotation becomes non-zero and the
   perigee drifts. After 365 days it will have moved significantly. This is why a station
   orbit cannot use the Molniya trick.

3. Set `inc` to 98.6. This is approximately the sun-synchronous inclination for a 700 km
   orbit. The nodal drift should be close to +0.9856 deg/day (the Earth's daily orbital
   motion around the Sun). The apsidal rotation is also non-zero and negative.

4. Change `inc` to 90 (polar orbit). The nodal drift goes to zero (cos 90° = 0) regardless
   of the orbit's other parameters. The node is stationary in inertial space.

5. Increase `days` to 3650 (ten years). The accumulated node and perigee drifts become
   large — many full revolutions for typical LEO satellites. This is why operational
   constellations must budget station-keeping fuel for J2 corrections.

6. Set `ecc` to 0 with `inc` = 63.4349. A circular orbit at the critical inclination. The
   apsidal rate is still zero (there is no perigee to drift), and the nodal rate still
   depends on the inclination and semi-major axis.

## Watch out for

- **The drift rates are secular — they accumulate linearly with time.** This is accurate for
  the first few revolutions. Over months or years higher-order terms and other perturbations
  (J3, drag, solar radiation pressure) cause the real drift to deviate from the linear
  extrapolation.

- **The formulas use the semi-latus rectum p = a(1−e²), not just a.** For circular orbits
  (e = 0), p = a. For highly eccentric orbits, p ≪ a, so the perturbations are much stronger
  than the semi-major axis alone would suggest.

- **The critical inclination is 63.4349° and also 180° − 63.4349° = 116.57°.** Both make
  5cos²(i) = 1. Molniya uses the prograde version (63.4°); a retrograde Molniya at 116.6° is
  theoretically equivalent but not commonly used.

> The Molniya orbit exists precisely because the critical inclination freezes the argument of
> perigee. Without that trick, the apogee would rotate around the orbit and could not be
> relied upon to stay over the northern hemisphere. The fact that 63.4° also provides twelve-
> hour coverage geometry is a fortunate coincidence, not a cause.
