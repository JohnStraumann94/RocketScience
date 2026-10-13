# What this application is

This is an interactive companion to **Bate, Mueller and White, *Fundamentals of
Astrodynamics***. Every significant piece of mathematics in the book has been implemented as
real, tested C# code, and this application lets you run it, change the numbers, and see what
happens.

It is not a simulator and it is not a mission planning tool. It is the book's equations, made
executable, with the working shown.

> Nothing here needs an internet connection, a login, or a cloud account. Everything runs
> locally on your machine.

---

## The window at a glance

```
+----------------------------------------------------------------------+
|  Fundamentals of Astrodynamics                            [ Help ]    |   header
+---------------------+------------------------------------------------+
|  TABLE OF CONTENTS  |   WHAT THIS DEMONSTRATES                       |
|                     |   ...the theory behind the selected sample...  |
|  Chapter 1          |                                                |
|    Energy, Momentum |   INPUTS                                       |
|    Speed Table      |   [ box ] [ box ] [ box ]                      |
|    Conic Geometry   |   [Run] [Reset] [Visualize] [Copy] [Instructions]
|  Chapter 2          |                                                |
|    rv2coe           |   OUTPUT                                       |
|    coe2rv           |   ...the computed report...                    |
|  ...                |                                                |
|                     |   THE CODE THAT RUNS            [ C# ][Python] |
|                     |   ...the source that produced it...            |
+---------------------+------------------------------------------------+
|  18 runnable samples            All calculations run on this computer |   footer
+----------------------------------------------------------------------+
```

**Left: the table of contents.** Every sample in the book, grouped by chapter. Click one to
load it. This is the only navigation in the app - there are no menus, tabs or dialogs to get
lost in.

**Right: the detail pane.** Everything about the selected sample, top to bottom, in the order
you need it: what it is, what you can change, the answer, and the code.

---

## How to use it

1. **Pick a topic on the left.** The detail pane fills in immediately. Every sample loads with
   sensible default values already in place, taken from a worked example in the book or from a
   real mission where one exists.
2. **Read "What this demonstrates".** This is the theory: what is being calculated, which
   equation it comes from, and why anybody cares. It is worth reading before you touch the
   numbers.
3. **Press Run.** You do not have to change anything first. The defaults produce a meaningful
   result on their own, so you can always see what "correct" looks like before you start
   experimenting.
4. **Change an input and run it again.** This is the entire point of the application. Reading
   that Hohmann transfer time scales as the three-halves power of the semi-major axis is one
   thing; watching the number move as you push the target radius outward is another.
5. **Press Instructions** for a guide written specifically for the sample you are looking at:
   what each input does, what is worth trying, and what to watch out for.

---

## The buttons

| Button | What it does |
| --- | --- |
| **Run** | Executes the selected sample with the current input values and prints the report. |
| **Reset inputs** | Restores every box to its default. Nothing you type can permanently break a sample. |
| **Visualize Trajectory** | Opens an animated illustration of the flight path. Only appears for samples whose subject genuinely is a trajectory - those are the ones badged **ANIMATED** in the table of contents. |
| **Copy output** | Puts the output report on the clipboard. |
| **Instructions** | Opens a step-by-step guide for the selected sample in its own window. |
| **Help** | Opens this document. |

Every one of these is a **real, separate window**. You can drag them to a second monitor,
resize them, and keep several open at once - a trajectory animation next to the instructions
that describe it, for instance.

---

## Editing the inputs

Every input is a plain decimal number.

- Use a **full stop** for the decimal point, regardless of your regional settings.
- **Scientific notation works**: `1.495979e8` is a perfectly good way to type an astronomical
  unit in kilometres.
- **The units printed beside each box are not decoration.** Almost every input is in
  kilometres, kilometres per second, degrees or seconds. Typing metres where kilometres are
  expected will not be rejected - it will silently give you an answer that is wrong by a
  factor of a thousand. This is the single most common mistake, and no program can catch it
  for you.
- **Radii are measured from the centre of the body, not from its surface.** A 200 km parking
  orbit around the Earth is a radius of 6578 km, not 200. Where a sample wants an altitude
  instead, the input is labelled "altitude" and says so.

### When the output turns red

That is not a crash. It means the sample checked your inputs, found them physically impossible
or numerically hopeless, and said so. The message names the offending value.

This is a deliberate design decision. Asking for a Kepler solve with an eccentricity greater
than 1, or a bi-elliptic transfer with an intermediate radius smaller than the orbit you are
leaving, *should* produce a clear refusal rather than a plausible-looking wrong number. Read
the message; it is usually telling you something real about the physics.

---

## The trajectory animation

Four samples describe an actual path through space. You can spot them without clicking
anything: their table-of-contents row carries an orange **ANIMATED** badge, and the footer
tells you how many there are. Select one and an orange **Visualize Trajectory** button
appears next to *Run*. Pressing it opens a window that plays the flight back.

- **Orange path** - the engine is burning.
- **Blue path** - the vehicle is coasting, unpowered.
- **Dashed grey** - where it is going next. Toggle with *Hide path ahead*.
- **Ringed markers** - mission events. They stay hidden until the vehicle reaches them, so the
  plot tells the story in order instead of spoiling the ending.
- **Telemetry panel** - live values at the current instant.
- **Footer** - the modelling assumptions. Always read this. It tells you what the animation is
  and is not claiming.

The transport bar gives you pause, restart, a path-ahead toggle, a speed selector from 0.25x
to 4x, and a slider you can drag to any moment in the flight.

> **The playback clock is not the mission clock.** Playback always lasts about eleven to
> sixteen seconds of real time, whether the flight takes twenty-four minutes or two hundred
> and fifty-nine days. The true mission time is the `T+` field in the telemetry panel, and the
> compression factor actually being applied is printed in brackets beside the transport bar.

---

## C# and Python

Every sample exists in **both languages**, and the toggle above the source listing switches
between them.

They are not the same code translated mechanically. Each is idiomatic for its language: the C#
uses the strongly-typed records and static classes of `Astro.Core`, while the Python uses
dataclasses and plain arithmetic with no third-party packages at all. What they share is the
mathematics and the answers.

**The C# is the code this application actually executes** when you press Run. It is not an
illustration - it is the real thing, calling the real library.

**The Python is a standalone script.** It does not need this application, or .NET, or anything
but a Python interpreter:

```
cd python
python examples/ch3-hohmann.py
```

Every script prints the same report you see in the OUTPUT pane. They import a small
`astro_core` package that sits beside them and mirrors the C# library, function for function,
so you can read either version and find the same names.

> Why both? Because the mathematics is the point and the language is not. If you already think
> in Python, having to decode C# generics to follow a derivation is friction that teaches you
> nothing about orbital mechanics.

---

## How the project is laid out

```
FundamentalsOfAstrodynamics/
  FundamentalsOfAstrodynamics.slnx     Visual Studio 2026 solution
  src/
    Astro.Core/                        The physics. No UI, no dependencies.
    Astro.App/                         This application (.NET MAUI).
  tests/
    Astro.Core.Tests/                  52 tests pinning the library to published values.
  python/
    astro_core/                        The Python port of the physics library.
    examples/                          One runnable script per sample.
  docs/
    help.md                            This document.
    guides/                            One instruction guide per sample.
```

The separation matters. `Astro.Core` contains no user interface code and no input or output of
any kind, which is exactly what makes it testable. **Every number this application displays
comes out of a function the test suite exercises.** The application itself contains no physics
whatsoever - it gathers numbers from boxes, calls the library, and formats what comes back.

---

## How far to trust the numbers

The library is pinned to published values, not merely to itself:

- Vallado's worked Example 2-5 is the regression fixture for the state-vector to
  orbital-elements conversion.
- The Lambert solver is verified by round trip: propagate a known state, feed the two
  positions and the time of flight back in, and the recovered velocity vectors must match to
  five decimal places.
- A Hohmann transfer from 6,678 km to 42,164 km gives a total delta-v of 3.8926 km/s over
  5.275 hours.
- The critical inclination comes out at 63.4349 degrees, a sun-synchronous orbit at 800 km
  comes out at 98.6 degrees, and the Moon's sphere of influence at about 66,183 km.

**All 52 tests pass.** Every sample has also been executed end to end through the user
interface, and every trajectory animation has been opened and checked.

### What is deliberately not modelled

These are simplifications, stated here so nobody mistakes them for bugs:

- **Two-body, point-mass gravity** everywhere except the Chapter 9 perturbation samples. No
  third bodies, no drag, no solar radiation pressure.
- **Impulsive burns.** Every manoeuvre is an instantaneous change of velocity. Real burns take
  minutes and cost extra delta-v; that loss is not modelled.
- **Coplanar transfers** unless a sample is explicitly about plane changes.
- **Patched conics** for lunar and interplanetary work, which is what the book uses. It is an
  excellent approximation and it is not exact.
- **Spherical bodies** except in the J2 samples, which is the entire subject of Chapter 9.

Each sample's own Instructions window states the assumptions that matter for that particular
calculation.

---

## Does this need the internet? No.

**Everything runs on your computer.** All 18 samples are closed-form or numerical mathematics
that the CPU in front of you finishes in microseconds to milliseconds. There is no server, no
account to create, no data sent anywhere and no telemetry. You can run this on a machine that
has never been connected to a network and nothing will be missing.

The only thing in either application that touches the internet is the **Donate** button, which
opens a web page in your browser - and only if you press it.

### Then what is `AzurePlaceholderService`?

A list of ideas, not a missing part. It records features that do not exist yet and where they
would attach if they were ever built:

- **Cosmos DB** - save and reload named mission scenarios *across devices*. Saving them on this
  one machine only needs a file.
- **Azure Functions and Redis** - fetch and cache TLE catalogue data. This needs the *internet*
  rather than Azure specifically; a hosted function only earns its keep if an API key has to be
  hidden or a rate limit shared between many users.
- **Durable Functions** - fan out porkchop-plot grid computations. Genuinely parallel work, but
  a modern desktop has plenty of cores for any grid this app would draw.
- **Entra ID** - sign-in, which is only needed *because* of per-user cloud storage.
- **Application Insights** - telemetry. Deliberately not implemented: a teaching tool that
  quietly reports what its user is doing is a poor trade for the information gained.
- **Key Vault** - relevant only once one of the above introduces a credential. The application
  currently has no secrets of any kind.

In short, none of it is required, and the program is complete without it.

---

## Where to go next

- **New to the subject?** Start at Chapter 1, *Orbital Energy and Angular Momentum*. Almost
  everything later in the book is a consequence of the two quantities it introduces.
- **Want to see something move?** Chapter 4, *Kepler's Equation Solver*, then press
  **Visualize Trajectory**. Watching a spacecraft crawl through apoapsis and race through
  periapsis explains Kepler's second law better than any paragraph can.
- **Here for mission design?** Chapter 3, *Hohmann Transfer*, then *Bi-elliptic Transfer* -
  and find the radius ratio where the bi-elliptic starts to win.
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
