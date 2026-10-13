## Background

Walter Hohmann showed in 1925 — before anything flew — that the cheapest two-impulse transfer
between two coplanar circular orbits is an ellipse tangent to both. Its periapsis sits on the
inner orbit, its apoapsis on the outer, so its semi-major axis is exactly (r1 + r2)/2. The
tangency is what makes both burns purely prograde or purely retrograde, with no wasted
component in any other direction.

Each burn is simply the difference between the vis-viva speed on the transfer ellipse and the
circular speed on the departure or target orbit. The coast is exactly half the transfer
ellipse's period:

    dv1 = v_transfer_at_r1 − v_circular_r1
    dv2 = v_circular_r2   − v_transfer_at_r2
    t   = pi sqrt( a_t³ / mu )

The defaults model the most-flown manoeuvre in history: from a 300 km parking orbit (r1 =
6678 km) to geostationary altitude (r2 = 42,164 km). The total delta-v is 3.8926 km/s over
about 5.27 hours.

The output also compares the result against the bi-elliptic break-even ratio (about 11.94).
For r2/r1 below that value, Hohmann is always optimal for two-impulse transfers.

## Experiments to try

1. Run with the defaults and note 3.8926 km/s and 5.27 hours. This is the ground truth for
   the GTO-to-GEO transfer.

2. Set `r2` to 384400 (the Moon's mean orbital distance). The transfer ellipse is now
   trans-lunar in size. The first burn is about 3.13 km/s and the second burn collapses to
   near zero — because the transfer ellipse almost reaches the Moon's orbital speed on its own.

3. Swap `r1` and `r2` to lower an orbit instead of raising it. The burns become retrograde
   (negative in the output) but the delta-v magnitudes are identical to the raising case.
   Hohmann is reversible.

4. Set `r2` to 7000 and `r1` to 6678 — a very small transfer. The total delta-v should be
   small (tens of m/s) and the transfer time only a few minutes. This is the regime of
   phasing manoeuvres.

5. Set `r2` so that r2/r1 exceeds about 11.94 (try r2 = 100000 with r1 = 6678). The output
   will note that the bi-elliptic might be cheaper. Then load the bi-elliptic sample and test
   the claim.

6. Set `r1` and `r2` equal. Both burns and the total are exactly zero — you are already
   there.

## Watch out for

- **Both radii are measured from the centre of the Earth.** A 300 km circular orbit has
  r = 6378.137 + 300 = 6678.137 km. Entering 300 instead of 6678 describes an orbit whose
  radius is smaller than Earth itself and the sample will reject it.

- **The transfer time is one-way.** It is the time from the first burn to the second — the
  half-period of the transfer ellipse. The return trip, if you need one, requires another
  Hohmann of the same duration.

- **The output notes "prograde" or "retrograde" for each burn.** Prograde means in the
  direction of motion (adding speed); retrograde means opposite. Both terms refer to the
  direction relative to the current velocity, not to any geographic east or west.

> The output warns you when r2/r1 exceeds the bi-elliptic break-even ratio of about 11.94.
> Past that threshold a three-burn bi-elliptic transfer can be cheaper in delta-v — but it
> is always far more expensive in time. The bi-elliptic sample in Chapter 3 lets you explore
> the trade-off directly.
