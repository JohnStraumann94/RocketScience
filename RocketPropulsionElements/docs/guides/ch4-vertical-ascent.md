## Background

The rocket equation gives ideal delta-v — the velocity change in field-free, atmosphereless
space. Real launch vehicles lose a substantial portion of that to three mechanisms, and
understanding each one requires running the full equation of motion through the atmosphere.

**Gravity loss** is the unavoidable cost of fighting gravity while the vehicle is still
climbing. For a purely vertical flight it is exactly `g₀ × tb` — the standard gravity
constant times the burn time. A 160-second burn loses 1570 m/s to gravity alone, before the
engine has done any useful work. Real vehicles reduce this by pitching over as quickly as
aerodynamic loads allow, because the loss scales as `g₀ × sin(γ) × t` — flying more
horizontally reduces the gravity component acting against the flight path.

**Drag loss** is the integral of `D/m` over the flight. It is typically 100–200 m/s for a
well-shaped vehicle — smaller than gravity loss for most launchers — because by the time the
vehicle is fast, the atmosphere is already thin. Max-Q is the peak dynamic pressure point:
`q = ½ρv²` peaks when the rapidly rising v² first overcomes the exponentially falling
density. Real vehicles throttle down around max-Q to reduce structural loads.

This sample integrates the one-dimensional equation of motion with explicit Euler steps. It
is crude compared with production trajectory software but reproduces all the physics terms
and shows every step.

**Press "Visualize Trajectory"** to open an animated plot of altitude against time. The
horizontal axis is mission time, not distance — this is a purely vertical flight, so
downrange distance is zero throughout.

## Experiments to try

1. Reduce `thrust` until the lift-off T/W is just above 1.0. The gravity loss grows
   substantially because the vehicle spends longer in the lower atmosphere at low speed.
   Confirm that "gravity loss as g × tb" equals the gravity loss column, since this is a
   vertical flight.

2. Set `cd` to 0 (no drag). The drag loss row goes to zero and the efficiency rises. Compare
   the drag-free burnout velocity against the full simulation to isolate the drag penalty.
   For the defaults, drag loss is small but non-negligible.

3. Increase `aref` from 7 to 20 m² (a blunt or wide vehicle). The drag loss grows markedly.
   Combine with a low drag coefficient to explore the sensitivity of each term.

4. Raise `isp` from 310 to 450 s. The burn time shortens (because the engine uses propellant
   faster at the same thrust) and the gravity loss falls. Higher Isp is better in two ways:
   directly through the rocket equation, and indirectly by shortening the burn.

5. Vary `dt` from 0.2 to 0.01 s and compare the burnout velocity and gravity loss. The
   explicit Euler integrator accumulates truncation error with larger steps; reducing `dt`
   improves accuracy at the cost of longer computation time.

## Watch out for

- **`pexit` is in bar, not pascals.** The default of 0.5 bar is a lightly-expanded nozzle;
  0.05 bar is a vacuum-optimised nozzle. The sea-level thrust is computed as
  `vacuum thrust − p_ambient × A_exit`, so a large exit area and a high exit pressure
  both reduce the sea-level figure, and T/W at lift-off is checked against this sea-level
  thrust.

> If the sample throws a validation error "Sea-level thrust-to-weight is X, which is not
> greater than 1", the vehicle cannot lift off. You must raise the vacuum thrust, reduce the
> initial mass, or reduce the nozzle exit area (which reduces the back-pressure penalty at
> sea level).

- **The gravity loss table at the bottom shows the loss at fixed flight path angle.** These
  are analytical estimates (`g₀ × sin(γ) × tb`), not integrated trajectories. They show
  the motivation for pitching over, but the actual gravity loss depends on how the flight
  path angle evolves through the burn.

- **The visualisation shows altitude vs. time, not altitude vs. downrange.** For a purely
  vertical flight, downrange distance is zero at every instant. See ch4-gravity-turn for
  the two-dimensional trajectory through actual space.
