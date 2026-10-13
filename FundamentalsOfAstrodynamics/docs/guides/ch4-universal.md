## Background

The classical Kepler solvers need a different equation for each conic type — elliptic,
parabolic, hyperbolic — and all of them degrade or become singular near eccentricity 1.
Universal variable propagation replaces this with a single equation covering every conic
continuously, using the Sundman change of independent variable.

The key is a universal anomaly chi defined by ds = dt * sqrt(mu) / r. A universal form of
Kepler's equation in chi converges for all conic types. Once chi is found, the new state
vector follows from the Lagrange f and g coefficients:

    r(t) = f * r0 + g * v0
    v(t) = f_dot * r0 + g_dot * v0

Energy and angular momentum are exactly conserved (to machine precision) by this formulation,
because the f and g functions are derived from the exact two-body solution. The output shows
both conservation checks, and both should be below 1e-9 in relative error.

The default state is a standard reference orbit from Vallado. The propagation time is 3600
seconds (one hour). You can propagate forward or backward; a negative `dt` reverses the
clock.

## Experiments to try

1. Run with the defaults and read the conservation checks. Energy drift and angular-momentum
   drift should be at machine precision — roughly 1e-10 or smaller. The reversibility check
   (propagate forward then back) should give a position error below 1e-9 km.

2. Set `dt` to the orbital period of the default state (compute it from the printed orbit
   type and semi-major axis). After one full period, the position error in the reversibility
   check should still be negligible — the propagator is symplectic in this sense.

3. Set `dt` to a negative value, for example −3600. You propagate one hour backward. The
   "propagated by" section shows a state that is one hour earlier on the same orbit. Applying
   a second propagation of +3600 should restore the original state.

4. Make the orbit hyperbolic: set `vx` to −5.0 and `vy` to 9.0. The orbit type printed at
   the top should switch to HYPERBOLIC. The propagator does not need any code change — the
   same universal-variable iteration handles it.

5. Set `dt` to 86400 (one day) for an elliptic orbit. The vehicle has completed roughly
   several revolutions (depending on the period). Compare the propagated state with what you
   expect from the period, and confirm that energy is still conserved.

## Watch out for

- **The propagator works in absolute inertial coordinates, not relative to a planet's
  surface.** The position components can be large (thousands of km) and that is normal.

- **Propagating over many periods accumulates no numerical error** in this two-body model —
  there is no integration and no step-size error. The universal-variable solution is the exact
  analytic answer for the two-body problem.

- **The orbit type is determined from the initial state, not from `dt`.** Setting `dt` to a
  value that would take the vehicle past escape does not change a bound elliptic orbit into a
  hyperbola; the orbit type is fixed by energy at the start.

> The conservation checks in the output are the internal self-test for the propagator. Seeing
> non-zero drift (say, 1e-4) would indicate a serious numerical problem. In practice, both
> checks should consistently be below 1e-9 — the floor set by double-precision arithmetic —
> because the Lagrange f and g formulas are exact within floating-point limits.
