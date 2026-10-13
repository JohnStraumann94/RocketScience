## Background

Every bound orbit, unbound escape trajectory, and limit case between them is described by the
same polar equation:

    r = p / (1 + e cos(nu))

where p = a(1 − e²) is the semi-latus rectum and nu is the true anomaly measured from
periapsis. The shape is fixed entirely by the eccentricity e: zero gives a circle, values
between zero and one give ellipses, exactly one gives a parabola, and values greater than one
give hyperbolas. The size is set by a (or equivalently by p).

The flight-path angle gamma — the angle between the local horizontal and the velocity vector —
tells you which way the vehicle is heading relative to the orbit. It is zero at periapsis and
apoapsis (both are purely horizontal), and it is a signed quantity: positive means climbing,
negative means descending.

    tan(gamma) = e sin(nu) / (1 + e cos(nu))

This sample sweeps nu from 0 to 360 degrees at the number of steps you request, printing
radius, speed and gamma at each point. It is the conic as a table, which is useful for
building intuition before using any of the later samples.

## Experiments to try

1. Run with the defaults (a = 26,600 km, e = 0.74, 13 steps). This is a Molniya-like orbit.
   At nu = 0 (periapsis) the speed should be maximum and gamma should be zero. At nu = 180
   (apoapsis) the speed should be minimum and gamma should again be zero.

2. Set `ecc` to 0. All rows should show the same radius and the same speed — you have a
   circle, and gamma is zero everywhere.

3. Set `ecc` to 0.99 and increase `steps` to 40. The radius swings from a tiny periapsis to a
   huge apoapsis and back. Notice how quickly the radius column changes near nu = 0 and how
   slowly it changes near nu = 180 — this is the geometry behind Kepler's second law.

4. Set `ecc` to 1.1 (a hyperbola) and check where the denominator 1 + e cos(nu) would reach
   zero — that is the asymptote. Rows near the asymptotic angle will show very large radii and
   very high speeds. The output will still compute correctly; the orbit is simply unbound.

5. Set `sma` to 42164 and `ecc` to 0 for a geostationary orbit. Every row should show
   3.075 km/s and zero flight-path angle.

6. Set `ecc` to 0.74 and watch how gamma peaks somewhere between 0 and 90 degrees. The true
   anomaly of maximum flight-path angle is nu = 90 degrees for any eccentricity — check it.

## Watch out for

- **For a hyperbola, the true anomaly is bounded.** The denominator 1 + e cos(nu) must be
  positive, which restricts nu to less than arccos(−1/e). Rows with nu outside that range
  would have negative r, which is not physical. The code computes r directly and the value
  will be negative if you push into this region with a large `steps` count.

- **The flight-path angle is zero at both apsides, but for different reasons.** At periapsis
  the vehicle is at its fastest and ascending. At apoapsis it is at its slowest and the
  velocity is purely horizontal. Neither is a "flat" trajectory in any meaningful sense.

- **`steps` is clamped to 2 to 73.** 73 steps gives one row per five degrees; more than that
  does not fit neatly in the output column widths.

> The default orbit (e = 0.74, a = 26,600 km) puts the periapsis at about 6916 km — only
> 538 km above the surface. A real Molniya uses a similar shape but with a higher periapsis
> achieved by adjusting the argument of perigee. If you reduce `sma` here without changing
> `ecc`, the periapsis will go underground and the physics is still valid — the orbit simply
> intersects the Earth.
