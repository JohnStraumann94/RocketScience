## Background

Lambert's problem is the targeting engine of spaceflight. Given two position vectors and a
time of flight, find the velocity at departure that makes the transfer. The result is not a
unique orbit — it is the orbit, because two points and a transfer time uniquely determine a
conic section (up to the choice of direction of travel).

Lambert's theorem (1761) is the mathematical foundation: the transfer time depends only on
the semi-major axis of the transfer orbit, the chord between the two positions, and the sum
|r1| + |r2|. Remarkably, the shape of the arc — how curved it is — drops out. This allows a
one-dimensional search over the semi-major axis to find the orbit that takes exactly the
requested time.

This implementation uses the universal-variable formulation with bisection on the Battin
variable psi. Bisection is guaranteed to converge, which matters because the time-of-flight
function has a poorly-behaved derivative near the parabolic boundary where classical Newton's
method can diverge.

Lambert's problem is the inner loop of every interplanetary porkchop plot. A grid of
departure dates versus arrival dates corresponds to thousands of Lambert solves, and the
contours of constant total delta-v are the launch windows.

## Experiments to try

1. Run with the defaults and check the verification at the bottom. The miss distance after
   propagating the departure velocity forward for the time of flight should be below 1e-4 km
   — effectively zero.

2. Set `tof` to 7200 (two hours). The same two points must be connected in twice the time;
   the orbit will be different — larger semi-major axis, different departure speed. Check
   whether it is still elliptic or whether doubling the time requires a hyperbola.

3. Set `longway` to 1. The solver sweeps the reflex angle (more than 180°) instead of the
   short way around. The departure and arrival velocities will be different. For some
   combinations of positions and times one direction requires far less delta-v than the other.

4. Move `r2x` to −5000 so that r2 is on the same side as r1. The geometry becomes extreme
   and the solver will either converge to a valid (possibly highly eccentric) orbit or report
   that no solution exists within the time requested.

5. Set `tof` very short — try 600 seconds. The solver must find an orbit that covers the same
   geometric distance in a small fraction of the usual time, which requires very high speed.
   The departure speed printed should be much larger than the default, and the transfer orbit
   may be hyperbolic.

6. Zero out `r1z` and `r2z`. Both positions are in the equatorial plane; the transfer orbit
   should also be equatorial (inclination ≈ 0° in the printed transfer orbit elements).

## Watch out for

- **`longway` accepts only 0 (short) or 1 (long).** Any other value is interpreted as 1.
  The short way sweeps the angle between r1 and r2 as measured by the cross product sign;
  the long way sweeps the supplement.

- **The two position vectors cannot be parallel.** If r1 and r2 are collinear — either the
  same direction (zero transfer angle) or exactly opposite (180° transfer) — the plane of the
  orbit is undefined and the solver has no solution. The sample will detect this and report
  the degenerate geometry.

- **Very short times of flight may require hyperbolic transfer orbits.** The solver handles
  this correctly, but the required departure speed will be very high, and the result may be
  physically impractical for a real mission.

> The bisection iterations printed in the output are typically 30 to 50. This is many more
> than the Newton iterations in the Kepler solver, but each bisection step is very cheap, and
> the guarantee of convergence matters more than speed for this application.
