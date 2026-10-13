## Background

Propellant flows from a tank to the chamber because pressure pushes it. The pressure at the
source must exceed chamber pressure by the sum of every loss in the path: injector pressure
drop, line and valve friction, flow acceleration, and any pressure drop across a regenerative
cooling jacket. Every one of those losses is real — it must be supplied by the tanks or the
turbopumps, which means real structural mass or real turbine power.

The choice between a pressure-fed and a pump-fed system turns on chamber pressure and
propellant load. In a pressure-fed system the tanks themselves hold the full source pressure,
so the tank walls must be pressure vessels. Wall thickness scales as `t = p × r / σ` (the
thin-wall hoop stress relation), and mass scales with wall thickness. At 20 bar the tank mass
penalty is modest and the system's simplicity — no turbomachinery, reliable restart, no start
transient — is worth far more. At 150 bar the tank walls become grotesquely heavy and a
turbopump is mandatory.

The cross-over is roughly at 20–30 bar. Below that threshold: pressure-fed. Above it:
pump-fed, which keeps tank pressure near 3 bar (just enough to prevent pump cavitation) and
lets the turbopump supply the rest. The SSME was pump-fed at 207 bar chamber pressure;
the Apollo lunar module descent engine was pressure-fed at about 15 bar.

The sample computes both options simultaneously for the same propellant volume, making the
mass saving from pump-feeding explicit. That saving must exceed the mass of the turbopump,
its turbine, its gas generator or preburner, and its development cost before pumping becomes
worthwhile.

## Experiments to try

1. Set `p1` to 20 bar (pressure-fed territory). Read the verdict. Now change it to 150 bar
   — the verdict changes, tank wall thickness becomes impractical, and the mass saving from
   pump-feeding is large. The transition in the verdict text is around 30 bar.

2. Change `sigma` (allowable stress) from 350 MPa (aerospace aluminium) to 150 MPa (mild
   steel). The tank wall thickness increases proportionally and the tank mass grows. Now
   change `rhomat` to 7800 kg/m³ (steel density). The tank mass grows further. This shows
   why aluminium and titanium replaced steel for cryogenic tanks.

3. Raise `dpinj` from 4 bar to 10 bar. The required source pressure rises and the pump-fed
   pressure rise required increases equally. A high injector drop improves stability (see
   ch8-injector) but costs more pump power. The budget shows both sides of this trade.

4. Set `volume` to 200 m³ (a large upper stage) and observe the tank mass for pressure-fed
   operation at 20 bar. Compare it against a realistic turbopump mass — the total system
   mass may still favour pressure-fed for the simplicity and reliability gains.

5. Change `tankr` from 1.2 to 2.0 m (a larger diameter vehicle). Wall thickness depends on
   both pressure and radius: `t = p × r / σ`. A bigger tank at the same pressure requires
   thicker walls — the two factors multiply. Longer tanks at the same diameter are more
   efficient (lower surface area for the same volume).

## Watch out for

- **All pressures are in bar; `sigma` is in MPa; `rhomat` is in kg/m³.** The sample converts
  bar to Pa and MPa to Pa internally. Entering 350 for `sigma` means 350 MPa — typical
  aerospace aluminium with a 1.25 safety factor already applied.

- **The tank geometry is simplified**: a cylindrical section with two hemispherical end caps.
  The cylindrical length is computed from the stated volume and radius. If the computed
  cylindrical length is negative (the hemispheres alone already exceed the volume), the
  result is dominated by the end-cap surface area, which is still physically reasonable but
  unusual.

> The pump-fed option always shows tank pressure of 3 bar regardless of inputs. This is the
> hardwired net positive suction head allowance in the code. Real NPSH requirements vary
> with propellant vapour pressure, pump inlet geometry, and vehicle acceleration; 3 bar is a
> conservative representative value, not a universal constant.

- **The tank mass is structural mass only.** It does not include insulation (critical for
  cryogens), valves, feedlines, pressurant tanks, or the pressurisation system itself. For
  a pressure-fed cryogenic stage, insulation mass can be significant.
