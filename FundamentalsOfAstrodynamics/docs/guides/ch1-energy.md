## Background

The vis-viva equation is one of the two conservation laws of the two-body problem. At any
point on an orbit the speed is determined entirely by the current radius *r* and the
semi-major axis *a*:

    v = sqrt( mu * (2/r - 1/a) )

The specific mechanical energy eps = v²/2 − mu/r is constant along an orbit, which is exactly
why the equation works: the two terms trade off as the vehicle climbs or descends, but their
difference never changes. The sign of eps classifies the trajectory outright — negative for a
bound ellipse or circle, zero for a parabolic escape, positive for a hyperbola.

The circular speed at any radius is the speed that makes centripetal acceleration equal to
gravity: v_c = sqrt(mu/r). The escape speed is exactly sqrt(2) times that, a ratio that holds
at every altitude. Running this sample with `sma` set equal to the computed radius (i.e.
altitude + 6378.137) puts you on a circular orbit and the vis-viva speed will match the
circular speed printed below it.

## Experiments to try

1. Set `altitude` to 400 and `sma` to 6778.137. The vis-viva speed should equal the circular
   speed exactly — you are describing a circular orbit — and the energy is negative, confirming
   it is bound. The period will be about 92.5 minutes.

2. Keep `altitude` at 400 but increase `sma` to 200000. You are now at the periapsis of a
   very elongated ellipse. The speed at 400 km altitude will be far above the circular speed
   there; the orbit is still bound, but the apoapsis is deep in cislunar space.

3. Set `altitude` to 35786 and `sma` to 42164.17. This is a geostationary orbit — you should
   see a speed close to 3.075 km/s and a period of about 23.93 hours (one sidereal day).

4. Keep `altitude` at 35786, then reduce `sma` to 6778. Now you are at the apoapsis of a
   transfer ellipse whose periapsis is at low Earth orbit. The speed at geostationary altitude
   is much lower than the circular speed there — about 1.6 km/s instead of 3.1 km/s. This is
   why the second Hohmann burn at GEO costs about 1.5 km/s.

5. To observe a hyperbolic orbit, set `altitude` to 400 and `sma` to −50000 (negative). The
   energy will be positive and the output will report v_infinity, the speed left over after
   escaping Earth.

## Watch out for

- **`altitude` is above the surface; `sma` is from the centre of the Earth.** A 400 km orbit
  has `altitude` = 400 and `sma` ≈ 6778. Typing the same value for both describes an orbit
  whose periapsis is underground.

- **The circular speed is the speed for *a circular orbit at this radius*, not the speed of
  your orbit at this point.** They match only when `sma` equals the current radius.

- **Negative `sma` is physically meaningful.** For a hyperbolic orbit the semi-major axis is
  defined negative by convention, and the vis-viva equation handles it correctly because the
  1/a term then adds to rather than subtracts from 2/r. Do not interpret a negative `sma` as
  an error.

> The parabolic boundary — zero energy — is numerically sharp. Setting `sma` to a very large
> positive number approaches it from the bound side but never reaches it exactly. If you want
> to see the parabolic classification, set `sma` to something like 1e12 and the energy will
> print as a very small negative number indistinguishable from zero for practical purposes.
