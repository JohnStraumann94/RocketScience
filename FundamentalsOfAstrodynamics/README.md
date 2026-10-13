# Fundamentals of Astrodynamics — Interactive Code Samples

A Visual Studio 2026 solution that implements the mathematics of Bate, Mueller and White's
*Fundamentals of Astrodynamics* in C#, and puts it behind a .NET MAUI desktop application with a
chapter-by-chapter table of contents. Pick a topic on the left, adjust the inputs, press **Run**,
and the real library code executes and prints its results. Where a sample produces an actual
flight path, a **Visualize Trajectory** button opens an animated illustration of it.

Every sample ships with written instructions of its own, and every sample exists in **both C# and
Python** — a toggle above the source listing switches between them.

## Solution layout

| Project | Target | Purpose |
| --- | --- | --- |
| `src/Astro.Core` | `net10.0` | The physics. A dependency-free class library, one file per chapter topic, fully XML-documented. |
| `src/Astro.App` | `net10.0-windows10.0.19041.0` | The .NET MAUI shell: table of contents, input editors, live output, the source listing for each sample, and the animated trajectory window (`Models/Trajectory.cs`, `Views/TrajectoryDrawable.cs`, `Views/TrajectoryPage.xaml`). |
| `tests/Astro.Core.Tests` | `net10.0` | 52 xUnit tests that pin the library to published textbook values. |
| `docs/` | — | `help.md` (the general Help document) and `guides/<sample-id>.md` (the per-sample Instructions). Plain Markdown files, embedded into the app at build time. |
| `python/` | — | `astro_core/`, a pure-standard-library Python port of `Astro.Core`, and `examples/<sample-id>.py`, one runnable script per sample. Also embedded into the app. |

`Astro.Core` has no reference to the UI and no third-party dependencies, so it can be reused from a
console app, a web API, or an Azure Function without change.

## Chapter coverage

| Chapter | File in `Astro.Core` | What it provides |
| --- | --- | --- |
| 1 — Two-body orbital mechanics | `TwoBody.cs`, `Vector3.cs`, `Constants.cs` | Specific energy, vis-viva, circular and escape speed, period, mean motion, the orbit equation, flight-path angle. |
| 2 — Orbit determination | `OrbitConversions.cs`, `OrbitalElements.cs` | `rv2coe` and `coe2rv`, including every degenerate case (circular equatorial, circular inclined, elliptical equatorial). |
| 3 — Basic manoeuvres | `Maneuvers.cs` | Hohmann, bi-elliptic, simple and combined plane change, phasing/rendezvous. |
| 4 — Position and velocity vs. time | `KeplerSolver.cs`, `UniversalVariables.cs` | Kepler's equation (elliptic and hyperbolic), all anomaly conversions, Stumpff functions and universal-variable propagation. |
| 5 — Orbit determination from two positions | `Lambert.cs` | Universal-variable Lambert solver. |
| 6 — Ballistic missile trajectories | `BallisticMissile.cs` | Free-flight range equation, optimal flight-path angle, minimum burnout speed. |
| 7 — Lunar trajectories | `LunarTrajectory.cs` | Patched-conic geocentric leg, sphere of influence, lunar orbit insertion. |
| 8 — Interplanetary trajectories | `Interplanetary.cs` | Synodic period, C3, injection and capture budgets, flyby turning angle, gravity assist. |
| 9 — Perturbations | `Perturbations.cs` | J2 nodal regression and apsidal rotation, critical inclination, sun-synchronous design. |

## Building and running

```powershell
# From the solution folder
dotnet test tests\Astro.Core.Tests\Astro.Core.Tests.csproj
dotnet build src\Astro.App\Astro.App.csproj -f net10.0-windows10.0.19041.0
dotnet run  --project src\Astro.App\Astro.App.csproj -f net10.0-windows10.0.19041.0
```

Or simply open `FundamentalsOfAstrodynamics.slnx` in Visual Studio 2026 and press F5 with
`Astro.App` set as the startup project.

### Why Windows-only?

`Astro.App.csproj` currently declares only the Windows target framework. The Android, iOS and
Mac Catalyst lines are still present but commented out, so the solution builds on a clean machine
without needing a JDK, the Android SDK or Xcode. Uncomment them when you want those platforms —
nothing in the code is Windows-specific.

## Visualize Trajectory — the animation window

Four samples do not just produce numbers, they produce a **flight path**:

| Sample | What is animated |
|---|---|
| **Hohmann Transfer** (ch. 3) | The tangent transfer ellipse between two circular orbits, with both reference orbits drawn to scale as rings and both impulsive burns marked. |
| **Kepler's Equation Solver** (ch. 4) | One full revolution of an *e* = 0.8 orbit, sampled **uniformly in time**. The spacecraft visibly races through periapsis and crawls through apoapsis — Kepler's second law made visible, and the reason the equation has to be solved iteratively at all. |
| **Ballistic Missile Free-Flight Trajectory** (ch. 6) | The genuine Keplerian free-flight ellipse from burnout to re-entry, with the Earth drawn to scale at one focus, plus burnout and apogee markers. |
| **Interplanetary Hohmann Transfer** (ch. 8) | The heliocentric cruise from a 1.000 AU departure orbit to a 1.524 AU target orbit, both planetary orbits drawn to scale. |

Those samples show an extra orange **“Visualize Trajectory”** button next to *Run*. Pressing
it opens a second window that plays the flight back as an animation.

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

**Every orbital path here is sampled uniformly in TIME, never in true anomaly.** Sampling in
angle would produce the same static curve but completely destroy the physics, because the
whole point is that the spacecraft does not move at a constant angular rate. Each sample
steps the clock, solves Kepler's equation for that instant, and converts to a radius — the
same code path the numerical samples exercise.

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
`src/Astro.App/Models/GuideComposer.cs` from two sources:

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
python ch3-hohmann.py          # prints the same report as the app
```

`python/astro_core/` mirrors `src/Astro.Core/` file for file (`TwoBody.cs` → `two_body.py`, and so
on) using only the standard library — no NumPy, no SciPy — so it runs anywhere Python 3.10 does.
Each `python/examples/<id>.py` puts the sample's inputs in a clearly marked block of module-level
constants at the top, so changing a number and re-running is a two-second edit.

### How the content is wired in

All of it is plain files on disk, embedded at build time by three globs in `Astro.App.csproj`:

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

Every entry in the table of contents is one `CodeSample` object, and they all live in
`src/Astro.App/Models/SampleCatalog.cs`. To add one, write a factory method returning a
`CodeSample` (id, title, chapter, summary, theory text, the C# snippet to display, its input
parameters and a `Run` delegate) and add it to the appropriate `ChapterGroup` in `Build()`.

`SampleCatalog` is a **partial** class. The trajectory builders live in two sibling files —
`SampleCatalog.Trajectories.cs` (shared helpers, ballistic missile, Kepler orbit) and
`SampleCatalog.Transfers.cs` (the shared two-impulse builder, Hohmann, interplanetary) — so
the animation code stays out of the main 18-sample catalog.

No XAML changes are required. `MainPage.xaml` renders whatever the catalog contains, and the
*Visualize Trajectory* button appears by itself for any sample that sets `BuildTrajectory`.

## Azure — optional, and not required for anything

**Nothing in this application needs Azure, or any network at all.** Every sample is closed-form
or numerical mathematics that runs on the local CPU in microseconds to milliseconds. There is no
server, no account, no database and no telemetry. `AzurePlaceholderService` is a record of ideas
for features that do not exist yet - a roadmap, not a missing dependency.

The footer says so plainly: *All calculations run on this computer - no internet connection
needed.* An earlier wording advertised that "Azure integrations are stubbed", which read like a
degraded mode and misrepresented a program that is complete offline.

It is worth being honest about how little of the list below genuinely needs a cloud, since
"we'll put it in Azure" is easy to say and expensive to mean:

| Idea | Intended service | Does it actually need a cloud? |
|---|---|---|
| Saved scenarios | Cosmos DB | **No** — a local file does this. Azure only matters for syncing across devices. |
| Live TLE catalogue | Functions + Redis | Needs the **internet**, not Azure. CelesTrak is a plain HTTPS request; a hosted function only earns its keep if an API key must be hidden or a rate limit shared across users. |
| Porkchop plots | Durable Functions / Batch | **No** — embarrassingly parallel, but `Parallel.For` on a desktop handles any grid this app would draw. |
| Sign-in | Entra ID | Only needed *because* of per-user cloud storage. Circular. |
| Telemetry | Application Insights | Deliberately not implemented. A teaching tool that quietly reports what its user is doing is a poor trade for the information gained. |
| Secrets | Key Vault | Only once something above introduces a credential. There are currently no secrets of any kind. |

`AzurePlaceholderService` is registered as a singleton in `MauiProgram.cs`, so if any of these are
ever built it is a change to that one registration plus the bodies of the stub methods. Nothing
else in the application knows the cloud exists.

## Verification

The library is not just plausible-looking; it is pinned to published values:

- **Vallado Example 2-5** is the `rv2coe` regression fixture.
- **Lambert** is verified by round trip — propagate a known state, feed the two positions and the
  time of flight back in, and the recovered velocities must match to five decimal places.
- Hohmann 6,678 km → 42,164 km gives Δv = 3.8926 km/s over 5.275 h.
- Critical inclination comes out at 63.4349°; a sun-synchronous orbit at 800 km at 98.6°;
  the Moon's sphere of influence at ≈66,183 km.

All 52 tests pass, and all 18 UI samples have been executed end to end by driving the running
application through Windows UI Automation.

All **four animated samples** were verified by the same automation: the *Visualize Trajectory*
button is located and invoked, the child window is confirmed to open with the expected title,
the playback clock is sampled twice to prove the animation is genuinely advancing, and a
screenshot of the rendered frame is captured and inspected. Result: 4 windows opened,
0 failures.

One thing that looks like a drawing error but is not: in the **Kepler's Equation Solver**
visualisation the orbit passes through the Earth. With the default inputs *a* = 26,600 km and
*e* = 0.8, the periapsis radius is 5,320 km — about 1,058 km *below* the surface. That is a
property of the numbers you entered, not of the renderer, and the window's footer says so.

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

That produces `dist\FundamentalsOfAstrodynamics-Setup-1.0.0.exe` - a single **57 MB** file, which
is smaller than the zip because Inno Setup's solid LZMA2 compression is markedly better than the
zip format at squeezing hundreds of near-identical satellite assemblies.

The zip already worked, so the installer earns its place by removing three chances to lose a
tester: unblocking the download, extracting it somewhere sensible, and finding the right `.exe`
among roughly 680 files. In exchange it gives a Start menu entry and a real uninstaller.

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
