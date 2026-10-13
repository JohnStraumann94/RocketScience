## Background

The free-flight portion of a ballistic missile trajectory is not a parabola. It is a piece of
a Keplerian ellipse with the centre of the Earth at one focus, and it only appears parabolic
when the range is short enough that the planet's curvature and the variation of gravitational
acceleration with altitude can both be ignored.

The trajectory is completely determined by three burnout conditions: altitude, speed, and
flight-path angle phi (measured from the local horizontal). The Q parameter — the ratio of
the burnout kinetic energy to the energy needed to stay in a circular orbit at that radius —
links the burnout state to the orbit:

    Q = v_bo² r_bo / mu

Q = 1 with a horizontal burnout (phi = 0) is a circular orbit. Q = 2 is escape speed. Every
real ballistic missile has 0 < Q < 1, meaning the "orbit" has its perigee underground.

The free-flight range equation gives the ground range directly from Q and phi without solving
the full orbit problem. The optimal loft angle — the angle that maximises range for a given
speed — equals (π − Ψ)/4, where Ψ is the free-flight range angle. For short-range missiles
this is close to 45°; for intercontinental missiles it is well below 45°.

## Experiments to try

1. Run with the defaults (100 km burnout altitude, 6.5 km/s, 25°). Read the range, apogee
   altitude and flight time. Then press **Visualize Trajectory** to see the arc against a
   round Earth — the difference from the familiar parabola is visible once the range exceeds
   a few hundred kilometres.

2. Change `phi` to the optimal angle printed in the OPTIMALITY section. The range will
   increase and you will see the message noting this is the minimum-energy trajectory.

3. Reduce `phi` to 5° (a very flat, depressed trajectory). The range will be shorter than the
   optimal, the apogee will be much lower, and the flight time will be reduced. This is the
   "low road" — stealthy because it stays at low altitude but costly in propellant.

4. Increase `phi` to 60° (a lofted, high-arcing trajectory). The range is again shorter than
   optimal, the apogee is much higher, and the flight time is longer. This is the "high road"
   — easier to track and intercept because it is slow near apogee.

5. Raise `speed` to 7.8 km/s with `phi` = 0 (horizontal burnout). Q is close to 1 — this
   is almost a circular orbit. The range angle will approach 360° and the flight time will
   approach the full orbital period.

6. Lower `altitude` to 10 km and `speed` to 3.0 km/s. This is a short-range artillery
   trajectory. The arc should look nearly parabolic in the visualiser because the range is
   small compared with Earth's radius.

## Reading the output

The OPTIMALITY section compares your burnout angle with the minimum-energy angle for the same
achieved range. If you are away from the optimum, the output notes that two different angles
(a high road and a low road) achieve the same range — this is the classic "equal-range"
property of ballistic trajectories.

## Watch out for

- **Burnout speed must give Q < 2.** Above escape speed (Q ≥ 2) the vehicle does not
  return and there is no free-flight range to compute. The sample will reject such inputs.

- **Altitude is above the surface; the burnout radius is computed internally.** Entering
  the radius from the Earth's centre instead of the altitude will place burnout at an
  altitude thousands of kilometres above the intended point.

- **The free-flight time does not include the powered ascent.** Only the unpowered arc from
  burnout to impact is modelled. The actual time from launch to detonation is longer.

> The arc in the visualiser is drawn against a round Earth at the correct scale. Short-range
> trajectories (a few hundred kilometres) will look parabolic because the arc genuinely is
> almost parabolic at that scale. Intercontinental trajectories (several thousand kilometres)
> will show obvious curvature relative to the horizon — the physical signature of the
> Keplerian ellipse.
