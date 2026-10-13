# Rocket Propulsion Elements — Interactive Code Samples

A Visual Studio 2026 solution that implements the engineering mathematics of
**George P. Sutton, *Rocket Propulsion Elements*** as working, tested C# code, and
presents it through a .NET MAUI desktop application with a left-hand table of contents.

Pick a topic from the TOC, edit the inputs, press **Run**, and read the computed
engineering report. Every sample also shows the theory behind it and the actual C# that
produces the answer. Where a sample produces an actual flight path, a
**Visualize Trajectory** button opens an animated illustration of it.

Every sample ships with written instructions of its own, and every sample exists in **both C# and
Python** — a toggle above the source listing switches between them.

---

## Solution layout

```
RocketPropulsionElements/
├── RocketPropulsionElements.slnx        Visual Studio 2026 solution
├── src/
│   ├── Propulsion.Core/                 Physics library (net10.0, no UI dependencies)
│   └── Propulsion.App/                  .NET MAUI application (net10.0-windows)
│                                         + animated trajectory window (Views/TrajectoryPage)
├── docs/
│   ├── help.md                          The general Help document
│   └── guides/<sample-id>.md            Per-sample Instructions, one file per sample
├── python/
│   ├── propulsion_core/                 Pure-stdlib Python port of Propulsion.Core
│   └── examples/<sample-id>.py          One runnable Python script per sample
└── tests/
    └── Propulsion.Core.Tests/           xUnit suite - 95 tests
```

The split matters. `Propulsion.Core` is a plain class library with **no** MAUI, no UI and
no I/O. That is what makes it testable, and every number the app displays comes from a
method the test suite exercises. `Propulsion.App` contains no physics whatsoever — it
gathers inputs, calls the library and formats the result.

---

## Chapter coverage

| Sutton chapter | Core file(s) | Topics implemented |
|---|---|---|
| 2 — Definitions | `Performance.cs` | Two-term thrust equation, effective exhaust velocity, specific impulse, total impulse, mass fractions, mass ratio, thrust-to-weight |
| 3 — Nozzle theory | `NozzleTheory.cs`, `NozzleGeometry.cs`, `NozzleLosses.cs`, `EngineAnalysis.cs` | Ideal exhaust velocity, Vandenkerckhove function, c\*, C_F, area–Mach relations, throat conditions, optimum expansion, nozzle sizing, conical and bell geometry, loss factors, expansion state and flow separation |
| 4 — Flight performance | `RocketEquation.cs`, `Staging.cs`, `FlightPerformance.cs`, `GravityTurn.cs` | Rocket equation both directions, payload fraction, single-stage delta-v ceiling, multi-stage bookkeeping, optimum stage count, gravity/drag losses, numerically integrated vertical ascent, max-Q, **full 2-D gravity-turn ascent (RK4, exponential atmosphere, inverse-square gravity, pitch-over kick, optional ballistic coast)** |
| 5 — Thermochemistry | `Thermochemistry.cs` | Specific heats, speed of sound, isentropic stagnation relations, adiabatic flame temperature |
| 6 — Liquid engine fundamentals | `LiquidEngine.cs` | Feed pressure budget, pressure-fed vs pump-fed trade |
| 7 — Liquid propellants | `Propellants.cs` | Five standard combinations, bulk density, ideal Isp, density impulse, mixture ratio flow split, tank volumes |
| 8 — Thrust chambers | `LiquidEngine.cs` | Injector orifice equation, orifice sizing and count, injector stiffness |
| 9 — Cooling | `LiquidEngine.cs` | Convective heat load, coolant temperature rise, coking and boiling limits |
| 10 — Turbopumps | `LiquidEngine.cs` | Pump power, head, specific speed, impeller selection, cavitation |
| 11–12 — Solid motors | `SolidMotor.cs` | Saint Robert's law, Klemmung, equilibrium chamber pressure, the `n < 1` stability criterion, BATES grain geometry, thrust-time integration, temperature sensitivity |
| 17, 19 — Electric propulsion | `ElectricPropulsion.cs` | Jet power, power balance, thrust from power, optimum exhaust velocity, coupled mission mass breakdown |

---

## The 25 samples in the app

**Chapter 2 — Definitions and Fundamentals**
1. Thrust, Exhaust Velocity and Specific Impulse
2. Total Impulse and Mass Fractions

**Chapter 3 — Nozzle Theory and Thermodynamic Relations**
3. Ideal Exhaust Velocity
4. Characteristic Velocity c\* and Thrust Coefficient C_F
5. Complete Engine Operating Point
6. Altitude Sweep — One Nozzle, Every Altitude
7. Nozzle Sizing and Geometry
8. Nozzle Loss Budget

**Chapter 4 — Flight Performance**
9. The Rocket Equation
10. Payload Fraction and the Single-Stage Limit
11. Multi-Stage Vehicle Analysis
12. Optimum Number of Stages
13. Vertical Ascent with Gravity and Drag Losses  *(animated)*
14. **Gravity Turn Ascent Trajectory**  *(animated)* — the full 2-D ascent, flagship sample

**Chapters 5, 7 and 12 — Thermochemistry and Propellants**
15. Thermochemistry and Isentropic Relations
16. Propellant Comparison
17. Mixture Ratio, Flow Split and Tank Sizing

**Chapters 6, 8, 9 and 10 — Liquid Propellant Engines**
18. Injector Orifice Sizing
19. Feed System Pressure Budget
20. Turbopump Power and Specific Speed
21. Regenerative Cooling Heat Balance

**Chapters 11 and 12 — Solid Propellant Motors**
22. Solid Motor Equilibrium Pressure and Stability
23. BATES Grain Thrust-Time Curve

**Chapters 17 and 19 — Electric Propulsion**
24. Electric Thruster Power Balance
25. Electric Mission Optimisation

---

## Visualize Trajectory — the animation window

Some samples do not just produce numbers, they produce a **flight path**. Those samples
show an extra orange **“Visualize Trajectory”** button next to *Run*. Pressing it opens a
second window that plays the flight back as an animation.

You do not have to hunt for them: any sample that can be animated carries an orange
**ANIMATED** badge on its row in the table of contents, and the footer states how many there
are. Both the badge and the button bind to the same `CodeSample.CanVisualize` property, so
they cannot disagree.

There is no video file and no video codec anywhere in this solution. The animation is a
game loop: an `IDispatcherTimer` ticks every 16 ms (~60 fps), advances a playback clock,
writes that clock into the drawable and calls `Invalidate()` on a `GraphicsView`. The
picture is redrawn from the physics on every single frame.

### What you see

| Element | Meaning |
|---|---|
| **Orange path** | The engine is burning over that segment. |
| **Blue path** | The vehicle is coasting — unpowered, ballistic. |
| **Dashed grey path** | Where the vehicle is *going* to go. Toggle with *Hide path ahead*. |
| **Rotated glyph** | The vehicle, pointed along its true velocity vector. The flickering plume only appears while thrusting. |
| **Ringed markers** | Mission events (pitch-over, max-Q, burnout, apoapsis, impulsive burns). A marker stays hidden until the vehicle actually reaches it, so the plot narrates the flight rather than spoiling it. |
| **Telemetry panel** | Live values interpolated from the trajectory at the current instant. |
| **Footer** | The modelling assumptions for that particular trajectory — always read this. |

The transport bar gives you Pause/Resume, Restart, a path-ahead toggle, a 0.25x–4x speed
selector, and a scrub slider you can drag to any moment in the flight.

### Time compression

The playback always lasts roughly 11–16 seconds of wall clock, no matter how long the real
mission is — a 3-minute ascent and a 259-day interplanetary cruise both take about the same
time to watch. The clock at the right of the transport bar shows the **true mission time**
and, in brackets, the compression factor actually being applied (for example `2307000x
real`). Do not read the wall clock as flight time; read the `T+` field.

### The architecture, in one paragraph

`Models/Trajectory.cs` is the only contract between physics and picture. A sample builds a
`Trajectory` — a list of `TrajectoryPoint`s (time, x, y, heading, powered flag, and an
arbitrary array of telemetry channel values), plus `TrajectoryChannel` descriptors,
`TrajectoryMarker`s, `TrajectoryBody`s and a few display hints. `Views/TrajectoryDrawable.cs`
renders that and nothing else: it has no idea whether it is drawing a launch vehicle or a
Hohmann transfer. **All unit conversion happens inside the sample**, so the renderer is
completely unit-agnostic and the axis labels are supplied as plain strings.

Two display hints matter a lot:

- **`EqualAspect`** — `true` for orbits and ballistic arcs, where an ellipse must keep its
  true shape or the physics is misrepresented; `false` for launch profiles, where hundreds
  of kilometres downrange against tens of kilometres of altitude would otherwise squash the
  interesting part into an invisible sliver. Every published ascent profile does this too,
  and the footer says so explicitly.
- **`TrajectoryBody` with a fully transparent fill** (`#00000000`) renders as a **ring**.
  That is how reference orbits are drawn without inventing another type.

### Adding a visualisation to a sample

Give the `CodeSample` a `BuildTrajectory` delegate. That is the entire wiring — the button
appears automatically, because `CodeSample.CanVisualize` is simply `BuildTrajectory is not
null` and the button’s `IsVisible` is bound through the view model.

```csharp
BuildTrajectory = inputs =>
{
    var result = SomePhysics.Simulate(inputs.GetDouble("thrust"), ...);

    return new Trajectory
    {
        Title      = "What This Shows",
        Subtitle   = "the inputs, echoed back",
        XAxisLabel = "Downrange distance (km)",
        YAxisLabel = "Altitude (km)",
        Points     = points,        // already in display units
        Channels   = channels,      // telemetry descriptors
        Markers    = markers,       // mission events
        Bodies     = bodies,        // planets, reference rings
        EqualAspect     = false,
        PlaybackSeconds = 14.0,
        Notes      = "State the model's limitations here."
    };
}
```

One deliberate rendering convenience is documented in the code: an impulsive burn takes
zero time and is therefore literally invisible, so a handful of points either side of each
burn are flagged `Powered` purely so the plume flashes at the right place. The footer text
on those samples tells you the burns are impulsive.

---

## Building and running

Requires the .NET 10 SDK and the MAUI Windows workload (both installed with
Visual Studio 2026).

```powershell
# Restore and build everything
dotnet build RocketPropulsionElements.slnx

# Run the tests
dotnet test tests\Propulsion.Core.Tests\Propulsion.Core.Tests.csproj

# Build and launch the app
dotnet build src\Propulsion.App\Propulsion.App.csproj -f net10.0-windows10.0.19041.0
.\src\Propulsion.App\bin\Debug\net10.0-windows10.0.19041.0\win-x64\Propulsion.App.exe
```

Or simply open `RocketPropulsionElements.slnx` in Visual Studio 2026, set
`Propulsion.App` as the startup project and press F5.

### Target frameworks

`Propulsion.App` targets **Windows only** (`net10.0-windows10.0.19041.0`). The Android,
iOS and MacCatalyst target frameworks from the MAUI template are **commented out rather
than deleted** in `Propulsion.App.csproj`, so they can be restored in a single edit.
Building for those platforms needs a JDK or Xcode, neither of which this desktop sample
browser requires.

---

## Conventions used throughout the library

These are worth knowing before reading the code, because a few of them differ from other
textbooks:

- **SI units everywhere internally.** Pascals, kelvin, metres, kilograms, seconds,
  newtons. The UI converts to bar, kN, cm² and mm for readability, but the library never
  sees anything but SI.
- **Angles are in degrees on the public API.** `Constants.DegToRad` and `RadToDeg` do the
  conversion internally.
- **Mass ratio is `mf / m0`** — final over initial, so it is *less than one* and equals
  `1 - zeta`. This is Sutton's convention. Many other sources use the reciprocal. The
  rocket equation methods take the two masses separately, so the convention never bites.
- **`Guard`** validates every public entry point and throws `ArgumentException` /
  `ArgumentOutOfRangeException` with an explanatory message. The UI catches these and
  shows the message rather than crashing.
- **Records for results.** `EngineOperatingPoint`, `VehicleResult`, `AscentResult` and
  friends are immutable `readonly record struct`s with a formatted `ToString()`.

---

## Help, Instructions, and the C# / Python toggle

Three things in the window exist purely to explain the application to somebody who has never
opened the textbook.

### The Help button

Top right of the header. It opens a window rendering `docs/help.md` — what the application is,
a labelled map of the window, how to read the output, what the units are, and what the app
deliberately does *not* do. Edit that one Markdown file and the Help window changes; there is no
copy of the text in any `.cs` file.

### The Instructions button

Sits next to *Copy output*, and is **per sample**. It opens a document assembled at runtime by
`src/Propulsion.App/Models/GuideComposer.cs` from two sources:

| Section | Where it comes from |
|---|---|
| "What this sample computes" | Generated from the sample's own summary and theory text. |
| "How to run it" | Generated step list. Gains a *Visualize Trajectory* step automatically when the sample has one. |
| Input reference table | Generated by walking the sample's `Parameters` — name, unit, default. |
| The written guide | `docs/guides/<sample-id>.md`, inserted verbatim: background, experiments to try, and things to watch out for. |
| "If something goes wrong" | Shared troubleshooting text. |

The split is deliberate. The generated half is derived from the `CodeSample` object itself, so it
can never drift out of date when a parameter is renamed or a default changes. The hand-written
half carries the insight a generator cannot invent. **Every sample gets a complete document even
if nobody has written its guide yet**, so the button is never dead.

### The C# / Python toggle

Above the source listing. C# is what actually executes when you press *Run* — the Python is a
faithful, independently runnable port of the same mathematics, and it is displayed rather than
executed. The toggle is deliberately sticky: pick Python once and every sample you click stays in
Python.

The Python is not a transliteration written for the app. It is a real package:

```powershell
cd python\examples
python ch3-cstar-cf.py         # prints the same report as the app
```

`python/propulsion_core/` mirrors `src/Propulsion.Core/` file for file (`NozzleTheory.cs` →
`nozzle_theory.py`, and so on) using only the standard library — no NumPy, no SciPy — so it runs
anywhere Python 3.10 does. Each `python/examples/<id>.py` puts the sample's inputs in a clearly
marked block of module-level constants at the top, so changing a number and re-running is a
two-second edit.

> The Python inherits the library's one genuine trap verbatim: `mass_ratio(m0, mf)` returns
> `mf / m0` (Sutton's MR, less than one) while `required_mass_ratio` returns `m0 / mf`. Two
> opposite conventions in the same library, preserved because both appear in the book.

### How the content is wired in

All of it is plain files on disk, embedded at build time by three globs in
`Propulsion.App.csproj`:

```xml
<EmbeddedResource Include="..\..\docs\help.md"          LogicalName="doc.help" />
<EmbeddedResource Include="..\..\docs\guides\*.md"      LogicalName="guide.%(Filename)" />
<EmbeddedResource Include="..\..\python\examples\*.py"  LogicalName="py.%(Filename)" />
```

`Services/ContentLibrary.cs` reads them all into a dictionary at startup and looks them up by
**sample id**. That is the whole binding mechanism. There is no registry, no attribute and no
extra property on `CodeSample`:

> To add a guide or a Python listing to a sample, drop a file named exactly
> `<sample-id>.md` or `<sample-id>.py` into the right folder and rebuild. The button and the
> toggle appear on their own.

Writing the documentation as real files rather than C# string literals means nothing has to be
escaped, the Python is genuinely runnable standalone, and the guides are readable directly on
GitHub.

`Views/DocumentRenderer.cs` renders a deliberate subset of Markdown into MAUI views: headings,
bullets with hanging indents, numbered steps, fenced code, tables, `> ` callouts, `---` rules,
and inline `**bold**`, `*italic*` and `` `code` ``. Source lines hard-wrapped at 95 columns are
folded back into whole paragraphs first, exactly as Markdown requires, so a bold span may safely
straddle a line break. Anything outside that subset renders literally — keep to the subset.

---

## Adding a new sample

The UI is entirely data-driven — there is no per-sample XAML, view or view-model. To add
a topic:

1. Implement the physics in `Propulsion.Core` and write tests for it.
2. Open the appropriate partial file under `src/Propulsion.App/Models/`
   (`SampleCatalog.Nozzles.cs`, `SampleCatalog.Flight.cs`, and so on).
3. Add a private factory method returning a `CodeSample`:

```csharp
private static CodeSample MyNewSample() => new()
{
    Id         = "chNN-my-sample",
    Chapter    = "Chapter NN",
    Title      = "Human readable title",
    Summary    = "One line for the TOC.",
    Theory     = """ ...several paragraphs explaining the physics... """,
    CodeSnippet= """ ...the C# that mirrors the Run body... """,
    Parameters =
    [
        new SampleParameter("key", "Label", defaultValue: 1.0, units: "m/s", help: "...")
    ],
    Run = inputs =>
    {
        double x = inputs["key"];
        // Call Propulsion.Core and format a report.
        return report;
    }
};
```

4. Reference it from `SampleCatalog.Build()` in `SampleCatalog.cs`.

That is all. The TOC, the parameter editor, the Run button, the error handling and the
output pane pick it up automatically.

---

## Azure integration — optional, and not required for anything

**Nothing in this application needs Azure, or any network at all.** Every sample is closed-form
or numerical mathematics that runs on the local CPU in microseconds to milliseconds. No Azure
resources are provisioned, `IsCloudConfigured` returns `false`, and every stub returns `null`.
`AzurePlaceholderService` is a record of ideas for features that do not exist yet - a roadmap,
not a missing dependency.

The footer says so plainly: *All calculations run on this computer - no internet connection
needed.* An earlier wording advertised that "Azure integrations are stubbed", which read like a
degraded mode and misrepresented a program that is complete offline.

| Stub | Intended Azure service | Purpose | Does it actually need a cloud? |
|---|---|---|---|
| `SaveScenarioAsync` | Azure Cosmos DB | Persist named engine and vehicle designs | **No** — a local file does this. Azure only matters for syncing across devices. |
| `SolveEquilibriumAsync` | Azure Functions + Redis cache | Wrap NASA CEA for real equilibrium thermochemistry instead of the representative gas properties used here | **No** — a library problem, not a hosting problem; it could equally run in-process. |
| `SubmitVehicleOptimisationAsync` | Durable Functions / Azure Batch | Large parametric sweeps — staging optimisation, low-thrust trajectory shaping | **No** — `Parallel.For` on a desktop handles any sweep this app would display. |
| *(planned)* | Event Hubs → Azure Data Explorer | Ingest and query test-stand telemetry | **Yes** — genuinely cloud shaped, but it needs a physical hot-fire test stand producing the data, which is rather the point. |
| *(planned)* | Microsoft Entra ID | Sign-in for saved designs | Only needed *because* of per-user cloud storage. Circular. |
| *(planned)* | Application Insights | Usage and error telemetry | Deliberately not implemented. A teaching tool that quietly reports what its user is doing is a poor trade for the information gained. |
| *(planned)* | Azure Key Vault | Connection strings and API keys |

---

## Verification

- **`dotnet test` -> 95 passed, 0 failed.** The tests assert against published values,
  closed-form identities (`c = c* × C_F`, `cp − cv = R`, `Tt/T1 = 2/(γ+1)`) and
  conservation checks (mass balance in the solid motor march, generation = discharge at
  equilibrium, sum of the electric mass breakdown).
- **All 25 UI samples smoke-tested** by driving the running application through Windows
  UI Automation: select each TOC entry, invoke **Run**, and scan the output pane for the
  view-model's failure marker. Result: 0 failures.
- **Both animated samples verified end to end** by the same automation: the
  **Visualize Trajectory** button is located and invoked, the child window is confirmed to
  open with the expected title, the playback clock is sampled twice to prove the animation
  is actually advancing, and a screenshot of the rendered frame is captured and inspected.
  Result: 2 windows opened, 0 failures.

### Known model limitations

These are deliberate simplifications, documented here so nobody mistakes them for bugs:

- `Staging.IdealPayloadFraction` is **monotonically increasing** in stage count, because
  the idealised model charges nothing for an interstage, a separation system or an extra
  engine. `OptimumStageCount` therefore always returns `maximumStages`. The
  *Optimum Number of Stages* sample explains this at length and shows the marginal-gain
  column that real designers actually use.
- `Propellants` chamber temperatures, molecular masses and gammas are **representative**
  values good to a few percent — they are not CEA-derived. Treat the resulting Isp figures
  as indicative, not as design data.
- `FlightPerformance.VerticalAscent` uses an explicit Euler step and an exponential
  atmosphere. It reproduces the physics faithfully and is fully transparent, but it is not
  a substitute for a real trajectory tool.
- `Thermochemistry.AdiabaticFlameTemperature` ignores dissociation and so over-predicts
  above roughly 2500 K.

---

## Companion solution

`../FundamentalsOfAstrodynamics/` does the same thing for Bate, Mueller & White,
*Fundamentals of Astrodynamics* — 18 samples, 52 tests, same architecture.

---

## Sending this to testers

The app is an **unpackaged WinUI 3 desktop app** (`WindowsPackageType=None`). A plain
`dotnet publish` therefore produces something that will only start on a machine that already has
**two** things installed - the .NET 10 desktop runtime *and* the Windows App SDK runtime. Testers
should not have to install either, so the build is published **fully self-contained**.

### One command

From the `Books` folder:

```powershell
.\publish.ps1 -Zip
```

That produces, under `dist\`, one folder and one `.zip` per book. The zip is about **89 MB** and
the unpacked folder about **232 MB**; the difference is the bundled runtimes, which is exactly the
cost of the tester needing to install nothing.

### Then wrap it in an installer

```powershell
.\installer\make-installers.ps1
```

That produces `dist\RocketPropulsionElements-Setup-1.0.0.exe` - a single **57 MB** file, which is
smaller than the zip because Inno Setup's solid LZMA2 compression is markedly better than the zip
format at squeezing hundreds of near-identical satellite assemblies.

The zip already worked, so the installer earns its place by removing three chances to lose a
tester: unblocking the download, extracting it somewhere sensible, and finding the right `.exe`
among roughly 690 files. In exchange it gives a Start menu entry and a real uninstaller.

Deliberate choices, documented in `installer\common.iss`:

- **Per-user install, so no UAC prompt.** Testers are already being asked to run software from an
  unknown publisher; making them type an administrator password raises the stakes of that decision
  for no benefit, since nothing here needs machine-wide access. A user who wants an all-users
  install can still choose it, and Inno elevates at that point.
- **The desktop shortcut is offered but not ticked.** Unrequested desktop icons are a small
  discourtesy. The Start menu entry is always created.
- **Uninstall removes the folder outright**, so the promise in `TESTERS.md` that it leaves nothing
  behind is literally true. This is verified, not assumed.
- **Both books share `installer\common.iss`.** Two near-identical scripts would drift apart; each
  book's `.iss` supplies only its name, GUID and icon.

Both installers are verified end to end - silent install, payload, shortcut targets, uninstall
registration, launching the installed copy and confirming its window and sample list render, then
silent uninstall and a check that the folder, shortcuts and registry entry are all gone.

They are **not code-signed**, so SmartScreen warns on first run. That needs a purchased
certificate, not a change to the build.

### macOS

There is no Mac build yet, and it is not blocked by the code. Both applications compile cleanly
for Mac Catalyst - this was tested from Windows and produced zero errors and zero warnings - but
the final link, bundle and codesign steps require Xcode, which runs only on macOS. See
[`../MACOS-BUILD.md`](../MACOS-BUILD.md) for the full picture, including why the Mac App Store is
*not* required.

`publish.ps1` also copies three things next to the executable that a normal publish would leave
behind, and without which half the features silently do nothing:

- `python\` - the standalone Python package and the 43 example scripts, so the code shown by the
  **Python** toggle can actually be run rather than just read.
- `docs\` - the Markdown behind the Help and Instructions windows.
- `TESTERS.md` - the note the tester should read first.

### The MSBuild incantation, and why it is what it is

```powershell
dotnet publish <App>.csproj -c Release -f net10.0-windows10.0.19041.0 `
  -p:RuntimeIdentifierOverride=win-x64 `
  -p:SelfContained=true `
  -p:WindowsAppSDKSelfContained=true
```

Two of those flags are not obvious:

- **`RuntimeIdentifierOverride`, not `RuntimeIdentifier`.** The MAUI targets set the RID
  themselves during evaluation, so a plain `RuntimeIdentifier` is overwritten before it is used.
  `RuntimeIdentifierOverride` is applied afterwards and is the one that wins.
- **`WindowsAppSDKSelfContained` is separate from `SelfContained`.** The first bundles the .NET
  runtime; the second bundles the Windows App SDK. Setting only `SelfContained=true` produces a
  build that still refuses to start without the Windows App SDK installed - which looks exactly
  like a crash on launch and is a miserable thing to debug remotely.

### `TESTERS.md`

Each book folder has a `TESTERS.md` written for someone who has never seen the project. It covers
how to start the app, why **SmartScreen** shows a warning (the executable is unsigned) and how to
get past it, where to begin, a checklist of what to exercise, a *"known and deliberate - do not
report these"* list so nobody spends an afternoon reporting the documented model simplifications,
and what to include in a report.

### The build stamp

The bottom-right corner of the main window shows something like `v1.0  build 2026-09-07`. It is
read at runtime from the assembly's informational version and the file's last-write time, so it
cannot be forgotten or left stale. Testers are asked to quote it first in any report, which makes
"works on my machine" answerable instead of a stalemate.

### Input validation

Every parameter box is parsed with `double.TryParse` under `InvariantCulture`, and a failure raises
a `FormatException` **naming the field and echoing what was typed**. Values that parse but are not
finite are rejected too - since .NET Core 3.0 `double.TryParse("1e400")` *succeeds* and yields
infinity, which would otherwise pass any guard that only checks for a positive value and fill the
report with `Infinity` and `NaN` instead of raising an error. All of it is caught in the view
model, so bad input produces a readable message in the output pane and never closes the app. This
was checked by driving both applications through UI Automation with letters, a negative value,
zero, `1e400` and an empty box: 10 cases, 10 clear messages, 0 crashes.

---

## The Donate button

The header carries a **Donate** button. It contains **no payment code at all** - it hands a URL to
the operating system, which opens the default browser. This is the same design Paint.NET uses, and
it is deliberate: software that never touches cardholder data falls entirely outside PCI DSS
scope, users trust a processor-hosted page far more than a dialog inside a desktop app, and
changing payment provider later never requires touching application code.

The address lives in `Services/DonationOptions.cs` and is **`https://johnstraumann.com/Donate`**.

That is a page on a domain we control rather than a payment processor's own URL, and the
indirection is deliberate: the processor behind the page can be swapped, or several offered side
by side, without this address ever changing. No copy of the program already in someone's hands can
be left pointing at something dead.

The address can be overridden two ways:

- **`donate.url`** - a plain text file beside the `.exe` whose first non-comment line is the
  address. `publish.ps1` copies it into the published folder if it is present in the book root.
  This works on a copy that has already been published and sent out, which avoids rebuilding and
  redistributing 230 MB just to correct a URL.
- **`DefaultUrl`** - the compiled-in constant, for the version you intend to keep.

`DonationOptions.IsConfigured` still recognises the original placeholder, so if the constant is
ever reset the button degrades into an explanatory window rather than opening a dead link.

Only `http` and `https` are accepted from either source. This is a real check, not decoration:
`file:///C:/Windows/System32/calc.exe` and `javascript:alert(1)` are both rejected and fall back
to the default, so neither a typo nor a tampered `donate.url` can turn the button into a way of
launching an arbitrary local program.

Background on the payment side - choosing a processor, the tax position, and what belongs on the
donation page - is in **`../DONATIONS-SETUP.md`**.

Verified two ways. The resolution logic was exercised directly against the real source file for
every input state: absent, empty, comment-only, comment-then-URL, leading blank lines and
surrounding whitespace, a non-URL, a `file:` URL and a `javascript:` URL - correct in all nine
cases. End to end, clicking **Donate** in both applications was confirmed to open a browser at
`https://johnstraumann.com/Donate` (page title `Donate - John Straumann`, HTTP 200) with no
placeholder window shown.
