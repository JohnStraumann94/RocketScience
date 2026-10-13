## Background

An interplanetary transfer applies the same patched-conic logic as the lunar case, but at
solar-system scale. There are now three conics in sequence: a departure hyperbola relative to
Earth, a heliocentric transfer ellipse from one planetary orbit to another, and an arrival
hyperbola at the target planet.

The planetary spheres of influence are so small compared with an astronomical unit (Earth's is
about 0.6% of 1 AU, Mars's about 0.4%) that the patch points can be treated as coincident
with the planets themselves. This is the same Hohmann geometry as Chapter 3, with the Sun
replacing the Earth and planetary orbits replacing spacecraft orbits.

Two numbers dominate mission planning. C3 = v_infinity² is the "characteristic energy" used
by launch vehicle providers in their performance curves — it captures how much energy beyond
escape is available for the heliocentric trajectory. The synodic period tells you how often
the departure geometry repeats: for Earth-Mars it is about 780 days (~26 months), which is
why Mars launches happen in clusters.

The required departure phase angle shows where the target planet must be relative to Earth at
launch. For an Earth-to-Mars transfer the target must lead by roughly 44°.

## Experiments to try

1. Run with the defaults (Earth to Mars). Read C3 and compare it with what a real Mars
   mission requires (about 8–15 km²/s² for recent missions). The Hohmann C3 is the theoretical
   minimum; real launches need more to compensate for plane changes and finite burn losses.

2. Change `r2` to 0.7233 (Venus). Venus is interior, so the transfer time is shorter and the
   geometry reverses — the target must *trail* Earth at launch. Check the sign of the
   departure phase angle in the output.

3. Change `r2` to 5.2026 (Jupiter). The transfer time grows to about 2.7 years. The synodic
   period is about 399 days (~13 months), giving more frequent windows than Mars. C3 is much
   larger — Jupiter is far and you must escape Earth with significant excess speed.

4. Raise `park1` to 400. The parking orbit is slightly higher; the injection delta-v changes
   only marginally because the departure hyperbola is nearly the same shape at 200 vs 400 km
   parking altitude.

5. Change `mu2`, `radius2`, and `park2` to Venus values (mu2 = 324858.6, radius2 = 6051.8,
   park2 = 300). The capture delta-v at Venus will be much larger than at Mars because Venus
   has a deeper gravity well.

## Watch out for

- **`r1` and `r2` are in astronomical units (AU), not kilometres.** The conversion to km is
  done internally. Earth = 1.000, Mars ≈ 1.524, Venus ≈ 0.723, Jupiter ≈ 5.203.

- **`mu2`, `radius2`, and `park2` describe the target planet.** The departure planet is always
  Earth (hardcoded). To change the departure planet you would need to run outside this sample.

- **The model assumes coplanar circular planetary orbits.** Real planetary orbits are elliptic
  and inclined. The actual delta-v budget for any mission is higher because of the plane
  changes and timing corrections this model ignores.

> The departure phase angle and synodic period together define the launch window. The phase
> angle tells you the required geometry; the synodic period tells you how long until that
> geometry repeats. If you miss the Mars window you wait roughly 26 months. There is no way
> to shortcut this without a much larger C3 — and even then the geometry must be close enough
> for the launcher's performance curve to reach the required C3.
