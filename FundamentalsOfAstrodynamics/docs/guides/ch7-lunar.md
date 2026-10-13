## Background

The Earth-Moon-spacecraft system is a three-body problem with no closed-form solution. The
patched-conic approximation resolves this by dividing space into zones where a single body
dominates: outside the Moon's sphere of influence the spacecraft feels only Earth's gravity,
and inside it feels only the Moon's. The boundary — the sphere of influence — has a radius of:

    r_SOI = R_EM * (m_Moon / m_Earth)^(2/5) ≈ 66,200 km

which is about one-sixth of the Earth-Moon distance. At the patch point, the geocentric and
selenocentric solutions are stitched together by equating positions.

Phase 1 is a geocentric ellipse. The trans-lunar injection (TLI) burn places the vehicle on a
Hohmann-like transfer that reaches the patch point with the right speed. Phase 2 is a
selenocentric hyperbola: the spacecraft arrives at the patch point faster than the Moon's
escape speed and follows a hyperbolic arc down to periselene, where the lunar orbit insertion
(LOI) burn captures it.

The Oberth effect makes LOI cheaper the lower the periselene: the deeper you go before
burning, the faster you are moving and the less speed you need to shed. Apollo's 110 km
parking orbit was a balance between this Oberth advantage and the penalty of fighting the
Moon's gravity all the way down.

## Experiments to try

1. Run with the defaults (185 km parking orbit, v_inf = 0.8 km/s, 110 km lunar orbit). The
   TLI delta-v should be close to 3.1 km/s and the LOI close to 0.9 km/s — consistent with
   published Apollo mission data.

2. Raise the parking orbit altitude to 400 km. The TLI delta-v increases slightly because
   the parking orbit speed is lower and more energy is needed to reach the same transfer
   ellipse. The LOI is unaffected because `vinf` has not changed.

3. Increase `vinf` to 1.5 km/s. The spacecraft arrives at the Moon faster (a higher-energy
   transfer ellipse). LOI now costs more because more speed must be shed at periselene. There
   is a trade-off here: a faster transfer costs more in TLI and more in LOI.

4. Lower `lunarorbit` to 50 km. You are inserting into a lower lunar orbit. The Oberth effect
   makes LOI cheaper because the speed at periselene is higher and the fractional reduction in
   speed required is smaller. Compare the LOI cost with the 110 km default.

5. Raise `lunarorbit` to 1000 km. The LOI becomes more expensive — the spacecraft must slow
   from hyperbolic speed at a radius where the Moon's circular speed is lower.

## Watch out for

- **`vinf` is the selenocentric hyperbolic excess speed, not the geocentric approach speed.**
  It is the speed of the spacecraft relative to the Moon when it is still far away (at the
  patch point). It is an input here because the geocentric leg is computed separately and the
  two phases are only loosely coupled in the patched-conic model.

- **`parking` and `lunarorbit` are altitudes above the respective surfaces**, not radii from
  the centres. The Moon's radius (1737.4 km) is added internally for the selenocentric
  calculation.

- **The model assumes the Moon is in a circular orbit and the transfer is coplanar.** The
  actual inclination of the lunar orbit plane and the launch azimuth corrections are not
  modelled. Real Apollo TLI burns included a mid-course correction budget to compensate.

> The output compares the result with the Apollo historical data: TLI about 3.05–3.10 km/s
> and LOI about 0.9 km/s. The patched-conic model gives reasonable agreement. The residual
> difference comes from the actual Apollo trajectory using a higher-energy, shorter transfer
> (about 3 days rather than the minimum-energy several days) and from the real three-body
> dynamics that the patched-conic ignores.
