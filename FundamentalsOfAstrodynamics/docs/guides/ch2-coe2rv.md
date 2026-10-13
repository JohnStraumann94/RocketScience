## Background

Going from orbital elements to a state vector is a two-stage process. First, write the
position and velocity in the perifocal (PQW) frame — a coordinate system attached to the
orbit itself, with the P axis pointing at periapsis, the Q axis 90° ahead in the orbital
plane, and the W axis along the angular momentum. In this frame the algebra is almost trivial:

    r_PQW = ( p / (1 + e cos nu) ) * [cos nu, sin nu, 0]
    v_PQW = sqrt(mu/p)             * [−sin nu, e + cos nu, 0]

Second, rotate into the inertial equatorial frame (ECI) using the classical 3-1-3 Euler
sequence: rotate by −RAAN around the Z axis, then by −i around the X axis, then by −argp
around the Z axis again. Each rotation undoes one of the three orienting elements in exactly
the order they were defined.

The defaults are the inverse of the Vallado Example 2-5 state vector. Running both this
sample and the rv2coe sample with each other's inputs produces a round-trip that is useful for
verifying that the two conversions are consistent.

## Experiments to try

1. Run with the defaults. Verify the angular momentum vector printed at the bottom. Its
   magnitude should be sqrt(mu * p) = sqrt(398600.4 * a * (1 − e²)).

2. Set `ecc` to 0. You now have a circular orbit. The true anomaly (`nu`) still determines
   where on the circle you are, but the argument of periapsis (`argp`) loses its meaning —
   there is no periapsis to measure from. The position and velocity are still computed
   correctly; only the interpretation of argp changes.

3. Set `inc` to 0 and `raan` to 0. You have an equatorial orbit. The resulting velocity
   should be entirely in the equatorial plane, with `vz` ≈ 0.

4. Set `nu` to 0. You are at periapsis. The flight-path angle printed should be exactly 0°
   and the speed should be the maximum on this orbit.

5. Set `nu` to 180. You are at apoapsis. The flight-path angle should again be 0° and the
   speed should be the minimum. With a = 36,127 km and e = 0.832853 the apoapsis radius is
   about 66,300 km — close to the Moon's sphere of influence.

6. Set `ecc` to 1.2 (a hyperbola). The conversion still works; the semi-major axis becomes
   the transverse axis of the hyperbola. Try `nu` approaching the asymptotic angle
   (arccos(−1/e) ≈ 146°) to see the radius diverge.

## Watch out for

- **Angles in the input boxes are in degrees.** The code converts them to radians internally.
  Entering radian values (e.g. 1.53 for 87.7°) will give the wrong answer without any error
  message.

- **The specific energy shown depends on the speed at the given true anomaly.** It must equal
  −mu / (2a) regardless of nu. If it does not, the input is inconsistent.

- **`sma` for a hyperbolic orbit should still be entered as a positive number here.** The
  sign convention for hyperbolic semi-major axis varies between textbooks; this sample uses
  the positive-definite convention for the input and handles the sign internally.

> Setting `ecc` to exactly 1.0 is the parabolic case. The semi-latus rectum p = a(1 − e²)
> collapses to zero, which makes the conversion singular. The sample will catch this and
> refuse the input rather than returning a nonsensical state.
