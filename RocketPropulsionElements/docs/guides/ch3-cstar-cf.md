## Background

The split of rocket performance into c* and C_F is one of the most practically useful ideas
in the subject. It separates what the chamber does from what the nozzle does, and it makes
each independently measurable.

Characteristic velocity c* measures the combustion chamber alone. Its formula contains no
exit area, no ambient pressure, and no expansion ratio — only the propellant chemistry
(through γ, T1, and M). In a hot-fire test you can measure c* directly: fire the chamber with
a plain choked throat, record the throat area, chamber pressure, and mass flow rate, and
compute `c* = p1 × At / m_dot`. A c* efficiency of 95–99% is normal; anything lower points
at the injector pattern or the chamber residence time (L*), not the nozzle.

Thrust coefficient C_F measures the nozzle alone. It is dimensionless and captures the nozzle
geometry, the ambient pressure, and the expansion ratio. Its value at optimum expansion rises
from about 0.6 (no divergent section) toward a theoretical maximum C_F,max set only by gamma.
In vacuum that maximum is the upper bound; at sea level the ambient pressure term subtracts
from it.

The product `c = c* × C_F` must hold exactly, and the sample verifies it along two
independent calculation paths: once from the identity and once from `F / m_dot`. The
difference is pure floating-point rounding — these are the same computation done differently.
That cross-check is the identity the test suite asserts on.

## Reading the output

The report is in four blocks. The first gives c* and the mass flow rate, including the
check `c* = p1 × At / m_dot`. The second gives C_F and its components: exit pressure (which
is set by the area ratio, not by the designer), ambient pressure, and the resulting thrust
coefficient. The third block verifies the identity `c = c* × C_F` numerically. The fourth
gives thrust from `F = C_F × p1 × At`, which requires no knowledge of mass flow or exhaust
velocity at all.

## Experiments to try

1. Set `p3` to 0 (vacuum). C_F rises because the negative ambient pressure term vanishes.
   The gap between the sea-level and vacuum values of C_F is proportional to `p3 × A2 / (p1 × At)`.

2. Increase `eps` from 16 to 60. The exit pressure p2 drops substantially. At sea level
   (p3 = 1.01325 bar) watch the expansion state move from under-expanded to over-expanded.
   C_F may actually decrease if the nozzle over-expands far enough, because the pressure
   thrust term goes negative.

3. Change `t1` from 3500 to 2000 K. c* falls noticeably; C_F does not change at all. This
   is the separation at work: chamber temperature affects the chamber, not the nozzle.

4. Change `at` from 200 to 100 cm². Thrust halves exactly — `F = C_F × p1 × At` — while
   c*, C_F, Isp, and the mass flow rate per unit area all remain constant. The throat area
   is a pure scale factor.

5. Set `mm` to 10 kg/kmol (hydrogen exhaust) at the same T1 and gamma. c* rises because the
   specific gas constant R = R'/M is larger. C_F does not change.

## Watch out for

- **`at` is entered in cm², converted internally to m²** by multiplying by 1×10⁻⁴. Entering
  200 means 200 cm² = 0.02 m². If the thrust looks off by a factor of ten thousand, this is
  the cause.

- **`p3` is in bar, not pascals.** The standard atmosphere is 1.01325 bar, not 101325. The
  default is 1.01325 bar.

> **Exit pressure p2 is not a free parameter.** It is computed from the area ratio and gamma
> by inverting the area–Mach relation (numerically, since there is no closed form). You cannot
> independently choose the area ratio and the exit pressure; once you fix one, the other
> follows from gamma. Setting a high area ratio and expecting a high exit pressure is a
> physical impossibility.

- **The identity c = c* × C_F holds only for the same ambient pressure.** c* does not depend
  on p3, but C_F does. The c computed via c* × C_F is the effective exhaust velocity at the
  specified p3; at a different altitude, C_F and therefore c change while c* does not.
