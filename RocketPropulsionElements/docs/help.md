# What this application is

This is an interactive companion to **George P. Sutton, *Rocket Propulsion Elements***. The
engineering mathematics of the book has been implemented as real, tested C# code, and this
application lets you run it, change the numbers, and see what happens.

It is not a design tool and it will not size a real engine for you. It is the book's
equations, made executable, with the working shown.

> Nothing here needs an internet connection, a login, or a cloud account. Everything runs
> locally on your machine.

---

## The window at a glance

```
+----------------------------------------------------------------------+
|  Rocket Propulsion Elements                               [ Help ]    |   header
+---------------------+------------------------------------------------+
|  TABLE OF CONTENTS  |   WHAT THIS DEMONSTRATES                       |
|                     |   ...the theory behind the selected sample...  |
|  Chapter 2          |                                                |
|    Thrust and Isp   |   INPUTS                                       |
|    Total Impulse    |   [ box ] [ box ] [ box ]                      |
|  Chapter 3          |   [Run] [Reset] [Visualize] [Copy] [Instructions]
|    Ideal Exhaust V  |                                                |
|    c* and C_F       |   OUTPUT                                       |
|    Nozzle Sizing    |   ...the computed engineering report...        |
|  ...                |                                                |
|                     |   THE CODE THAT RUNS            [ C# ][Python] |
|                     |   ...the source that produced it...            |
+---------------------+------------------------------------------------+
|  25 runnable samples            All calculations run on this computer |   footer
+----------------------------------------------------------------------+
```

**Left: the table of contents.** Every sample, grouped by Sutton chapter. Click one to load
it. This is the only navigation in the app - there are no menus, tabs or dialogs to get lost
in.

**Right: the detail pane.** Everything about the selected sample, top to bottom, in the order
you need it: what it is, what you can change, the answer, and the code.

---

## How to use it

1. **Pick a topic on the left.** The detail pane fills in immediately. Every sample loads with
   realistic default values already in place, taken from a worked example in the book or from
   a real engine where one exists.
2. **Read "What this demonstrates".** This is the theory: what is being calculated, which
   equation it comes from, and why an engineer cares. It is worth reading before you touch the
   numbers.
3. **Press Run.** You do not have to change anything first. The defaults produce a meaningful
   result on their own, so you can always see what "correct" looks like before you start
   experimenting.
4. **Change an input and run it again.** This is the entire point of the application. Reading
   that thrust coefficient rises with area ratio only until the nozzle over-expands is one
   thing; watching the number turn over as you sweep the ratio is another.
5. **Press Instructions** for a guide written specifically for the sample you are looking at:
   what each input does, what is worth trying, and what to watch out for.

---

## The buttons

| Button | What it does |
| --- | --- |
| **Run** | Executes the selected sample with the current input values and prints the report. |
| **Reset inputs** | Restores every box to its default. Nothing you type can permanently break a sample. |
| **Visualize Trajectory** | Opens an animated illustration of the flight path. Only appears on the two ascent samples - the ones badged **ANIMATED** in the table of contents. |
| **Copy output** | Puts the output report on the clipboard. |
| **Instructions** | Opens a step-by-step guide for the selected sample in its own window. |
| **Help** | Opens this document. |

Every one of these is a **real, separate window**. You can drag them to a second monitor,
resize them, and keep several open at once - an ascent animation next to the instructions that
describe it, for instance.

---

## Editing the inputs

Every input is a plain decimal number.

- Use a **full stop** for the decimal point, regardless of your regional settings.
- **Scientific notation works**: `1.5e6` is a perfectly good way to type a chamber pressure in
  pascals.
- **The units printed beside each box are not decoration.** This library works in **SI base
  units** almost everywhere: pressures in **pascals**, not bar or psi; areas in **square
  metres**, not square centimetres; masses in **kilograms**; temperatures in **kelvin**. A
  chamber pressure of "70" means seventy pascals, which is a hard vacuum, not 70 bar.
  Getting this wrong is by far the most common mistake, and no program can catch it for you.
- **Specific impulse is in seconds**, the conventional engineering unit, and is converted
  internally to an effective exhaust velocity by multiplying by standard gravity.

### When the output turns red

That is not a crash. It means the sample checked your inputs, found them physically impossible
or thermodynamically inconsistent, and said so. The message names the offending value.

This is a deliberate design decision. Asking for a nozzle with an exit area smaller than its
throat, a burn-rate exponent above 1 (which is unstable and would run away), or a mass ratio
below 1, *should* produce a clear refusal rather than a plausible-looking wrong number. Read
the message; it is usually telling you something real about the physics.

---

## The trajectory animation

Two samples describe an actual flight path. You can spot them without clicking anything:
their table-of-contents row carries an orange **ANIMATED** badge, and the footer tells you
how many there are. Select one and an orange **Visualize Trajectory** button appears next to
*Run*. Pressing it opens a window that plays the ascent back.

- **Orange path** - the engine is burning.
- **Blue path** - the vehicle is coasting, unpowered.
- **Dashed grey** - where it is going next. Toggle with *Hide path ahead*.
- **Ringed markers** - mission events: pitch-over, max-Q, burnout. They stay hidden until the
  vehicle reaches them, so the plot tells the story in order instead of spoiling the ending.
- **Telemetry panel** - live altitude, downrange, speed, flight path angle, mass, acceleration
  and dynamic pressure.
- **Footer** - the modelling assumptions. Always read this. It tells you what the animation is
  and is not claiming.

The transport bar gives you pause, restart, a path-ahead toggle, a speed selector from 0.25x
to 4x, and a slider you can drag to any moment in the flight.

> **The two axes use different scales** on the gravity-turn plot, exactly as every published
> ascent profile does. Two hundred kilometres downrange against three hundred kilometres of
> altitude would be readable at equal scale; two thousand against eighty would not. The axis
> labels always tell you the real numbers.

> **The playback clock is not the mission clock.** Playback always lasts about fourteen
> seconds of real time. The true mission time is the `T+` field in the telemetry panel, and
> the compression factor actually being applied is printed in brackets beside the transport
> bar.

---

## C# and Python

Every sample exists in **both languages**, and the toggle above the source listing switches
between them.

They are not the same code translated mechanically. Each is idiomatic for its language: the C#
uses the strongly-typed records and static classes of `Propulsion.Core`, while the Python uses
dataclasses and plain arithmetic with no third-party packages at all. What they share is the
mathematics and the answers.

**The C# is the code this application actually executes** when you press Run. It is not an
illustration - it is the real thing, calling the real library.

**The Python is a standalone script.** It does not need this application, or .NET, or anything
but a Python interpreter:

```
cd python
python examples/ch4-rocket-equation.py
```

Every script prints the same report you see in the OUTPUT pane. They import a small
`propulsion_core` package that sits beside them and mirrors the C# library, function for
function, so you can read either version and find the same names.

> Why both? Because the engineering is the point and the language is not. If you already think
> in Python, having to decode C# generics to follow a derivation is friction that teaches you
> nothing about nozzles.

---

## A warning about two conventions in one library

The library follows Sutton's notation, and Sutton uses **mass ratio** in the opposite sense to
most other texts. This is preserved deliberately rather than quietly "fixed", because a reader
holding the book open needs the code to match what is on the page.

| Function | Returns | Convention |
| --- | --- | --- |
| `Performance.MassRatio(m0, mf)` | final over initial, so **less than 1** | Sutton |
| `RocketEquation.RequiredMassRatio(...)` | initial over final, so **greater than 1** | Tsiolkovsky |

Both are correct. They are simply reciprocals of each other. If a result is out by exactly the
square of what you expected, this is very often why.

---

## How the project is laid out

```
RocketPropulsionElements/
  RocketPropulsionElements.slnx        Visual Studio 2026 solution
  src/
    Propulsion.Core/                   The physics. No UI, no dependencies.
    Propulsion.App/                    This application (.NET MAUI).
  tests/
    Propulsion.Core.Tests/             95 tests pinning the library to published values.
  python/
    propulsion_core/                   The Python port of the physics library.
    examples/                          One runnable script per sample.
  docs/
    help.md                            This document.
    guides/                            One instruction guide per sample.
```

The separation matters. `Propulsion.Core` contains no user interface code and no input or
output of any kind, which is exactly what makes it testable. **Every number this application
displays comes out of a function the test suite exercises.** The application itself contains no
physics whatsoever - it gathers numbers from boxes, calls the library, and formats what comes
back.

---

## How far to trust the numbers

The library is pinned to published values and to closed-form identities, not merely to itself:

- The identity `c = c* x C_F` must hold for every operating point, which independently checks
  the characteristic velocity and the thrust coefficient against each other.
- `cp - cv = R` and `Tt/T1 = 2/(gamma+1)` are asserted directly.
- Mass balance is checked across the solid-motor burn march, and generation must equal
  discharge at equilibrium chamber pressure.
- The electric propulsion mass breakdown must sum to the initial mass.

**All 95 tests pass.** Every sample has also been executed end to end through the user
interface, and both trajectory animations have been opened and checked.

### What is deliberately not modelled

These are simplifications, stated here so nobody mistakes them for bugs:

- **`Staging.IdealPayloadFraction` always improves with more stages**, because the idealised
  model charges nothing for an interstage, a separation system or an extra engine. The
  *Optimum Number of Stages* sample explains this at length and shows the marginal-gain column
  that real designers actually use.
- **Propellant chamber temperatures, molecular masses and gammas are representative** values
  good to a few percent. They are not CEA-derived. Treat the resulting Isp figures as
  indicative, not as design data.
- **One-dimensional, isentropic, frozen-composition nozzle flow.** No boundary layer, no
  shifting equilibrium, no shock structure.
- **The ascent samples are point masses in a plane**: zero lift, zero angle of attack, an
  exponential atmosphere, a constant drag coefficient, no staging and no Earth rotation.

Each sample's own Instructions window states the assumptions that matter for that particular
calculation.

---

## Does this need the internet? No.

**Everything runs on your computer.** All 25 samples are closed-form or numerical mathematics
that the CPU in front of you finishes in microseconds to milliseconds. There is no server, no
account to create, no data sent anywhere and no telemetry. You can run this on a machine that
has never been connected to a network and nothing will be missing.

The only thing in either application that touches the internet is the **Donate** button, which
opens a web page in your browser - and only if you press it.

### Then what is `AzurePlaceholderService`?

A list of ideas, not a missing part. It records features that do not exist yet and where they
would attach if they were ever built:

- **Cosmos DB** - save and reload named engine designs *across devices*. Saving them on this one
  machine only needs a file.
- **Azure Functions and Redis** - a NASA CEA equilibrium solve for real thermochemistry. That is
  a library problem rather than a hosting problem; it could equally run in-process.
- **Durable Functions** - fan out design sweeps. Genuinely parallel work, but a modern desktop
  has plenty of cores for any sweep this app would display.
- **Event Hubs and Data Explorer** - hot-fire test-stand telemetry. This one really is cloud
  shaped, and it needs a physical test stand producing the data - which is rather the point: it
  is not a gap in this program.
- **Entra ID** - sign-in, which is only needed *because* of per-user cloud storage.
- **Application Insights** - telemetry. Deliberately not implemented: a teaching tool that
  quietly reports what its user is doing is a poor trade for the information gained.
- **Key Vault** - relevant only once one of the above introduces a credential. The application
  currently has no secrets of any kind.

In short, none of it is required, and the program is complete without it.

---

## Where to go next

- **New to the subject?** Start at Chapter 2, *Thrust, Exhaust Velocity and Specific Impulse*.
  Everything else in the book is built on the two-term thrust equation it introduces.
- **Want to see something move?** Chapter 4, *Gravity Turn Ascent Trajectory*, then press
  **Visualize Trajectory**. Watching gravity itself steer the vehicle over, with no steering
  command at all after a single three-degree kick, is the clearest possible demonstration of
  why launch vehicles fly the way they do.
- **Here for engine design?** Chapter 3, *Complete Engine Operating Point*, then
  *Altitude Sweep* - and find the altitude where your nozzle stops being over-expanded.
- **Interested in why rockets have stages?** Chapter 4, *Payload Fraction and the Single-Stage
  Limit*, then *Multi-Stage Vehicle Analysis*.
- **Building on this code?** Read `README.md` in the solution folder. It covers the library
  conventions, how to add a sample, and how to add a trajectory visualisation.

---

## The Donate button

The **Donate** button in the top-right corner opens a donation page in your normal web browser.

**This program never handles the payment itself.** It does not ask for card details, does not see
an amount, and does not talk to a bank. All it does is hand a web address to Windows, which opens
your browser - the same thing that happens when you click a link in an email. The payment, if you
choose to make one, happens entirely on the payment provider's own secure page, where you can see
the padlock and the address for yourself.

Donations are entirely voluntary. Nothing in either application is locked, time-limited, reduced
or nagged about if you never press the button, and the program does not remember whether you did.

The page it opens is **https://johnstraumann.com/Donate**. If you would rather not let a program
open your browser, you are welcome to type that address in yourself instead.
