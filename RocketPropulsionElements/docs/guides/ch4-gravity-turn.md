## Background

The gravity turn is one of the more elegant results in flight mechanics. A launch vehicle
cannot fly straight up (orbit requires horizontal speed) and cannot steer aerodynamically
at high speed (the structural loads would be fatal). The resolution is to let gravity itself
do the steering.

The vehicle lifts off vertically, flies straight up for a few seconds, then tips slightly —
one to five degrees — away from vertical. From that moment it holds zero angle of attack,
pointing exactly along its own velocity vector. Because the velocity now has a small
downward component, gravity acts partly perpendicular to the flight path and steadily rotates
it. No further control input is needed. The turn continues, smoothly and by itself, until the
vehicle is flying nearly horizontally at burnout.

The single pitch-over angle `apitch` is therefore the most sensitive input in the sample.
Too small and the turn is too gentle; the vehicle arrives at burnout still climbing steeply
with far too little horizontal speed for orbit. Too large and the vehicle pitches over
aggressively, builds excessive dynamic pressure in the lower atmosphere, and may curve back
toward the ground. There is a narrow range that works — finding it by varying `apitch` by
hand is exactly how early ascent programmes were designed.

The centrifugal relief term `v²/(R+h)` in the flight path angle equation is physically
remarkable: when `v = √(g(R+h))`, the term exactly cancels gravity's rotation of the flight
path, gamma stops changing, and the vehicle is in orbit. The equation of motion contains the
definition of orbital velocity as a special case.

**Press "Visualize Trajectory"** to open an animated side view of the flight path. Orange
segments show the powered burn; blue shows the ballistic coast. The axes use different scales,
as every published ascent profile does.

## Experiments to try

1. Change `apitch` from 3.5° to 1°. Run, then visualise. The flight path rotates very slowly
   and the burnout flight path angle is large (still climbing steeply). The horizontal
   component of burnout velocity is far below orbital speed. Then try 8°; the vehicle turns
   over quickly and the trajectory curves dramatically downrange.

2. Reduce `tpitch` from 12 s to 2 s. The pitch-over starts almost immediately at low speed.
   At high speed it would be dangerous; at low speed it just starts the turn earlier and
   gives slightly more time to build horizontal speed.

3. Set `coast` to 0 s. The trajectory ends at burnout. Now set it to 400 s and visualise —
   watch the vehicle arc upward to apogee and then begin the descent, with the blue coast
   segment drawn out. The apogee altitude printed in the report shows how high the ballistic
   arc peaks.

4. Increase `thrust` from 900 to 1300 kN (higher T/W). The burn time shortens, the vehicle
   accelerates more quickly, and max-Q occurs earlier and at a higher dynamic pressure. The
   shortfall against orbital speed typically increases because the vehicle has less time to
   turn over.

5. Look at "Saved by turning over" in the loss accounting. This is the reduction in gravity
   loss compared with a purely vertical flight of the same duration. It is the quantitative
   payoff of the gravity turn — the velocity the vehicle would have lost to gravity if it had
   not pitched over.

## Watch out for

- **`apitch` is in degrees and is the single most sensitive input.** Small changes (half a
  degree) produce large changes in burnout flight path angle and horizontal speed. Start
  with the default and adjust by 0.5° steps.

> **The trajectory axes use different scales.** The horizontal axis (downrange distance in
> km) and the vertical axis (altitude in km) have different spans. This is not a distortion
> — it is how every ascent profile in the literature is plotted, because a 200 km downrange
> against 80 km altitude drawn to equal scale would be an unreadable sliver. The axis labels
> always give the true values.

- **This model flies zero angle of attack at all times**, starting from the moment of pitch-
  over. Any offset between the vehicle axis and the velocity vector (angle of attack) produces
  aerodynamic loads that this simulation does not model. In a real vehicle, angle of attack
  is kept small by active attitude control during the pitch-over phase.

- **The integration uses Runge-Kutta 4**, not the explicit Euler method of the vertical
  ascent sample. RK4 is significantly more accurate per time step. Reducing `dt` below 0.1 s
  rarely changes the result by more than a fraction of a percent.
