# Testing Notes — Rocket Propulsion Elements

Thank you for taking a look at this. It should take about twenty minutes to get a feel for,
and rather longer if you get interested.

---

## Installing it

Run **`RocketPropulsionElements-Setup-1.0.0.exe`** and follow the prompts.

It installs for you alone, so there is **no administrator password and no User Account
Control prompt**. It adds a Start menu entry, and a desktop shortcut only if you tick the box.
Nothing else on your machine is touched: no runtimes are installed, no services, no drivers,
and nothing outside its own folder.

To remove it: **Settings > Apps > Installed apps > Rocket Propulsion Elements >
Uninstall**, or use the Uninstall shortcut in its Start menu folder. This has been tested and
leaves nothing behind.

**Windows will warn you.** The installer is not code-signed, so SmartScreen will show
*"Windows protected your PC"*. Click **More info**, then **Run anyway**. This warning appears
for any program from a publisher who has not bought a code-signing certificate; it is not a
statement that anything is wrong with the file. If you would rather not proceed, say so and
you will be sent the plain zip instead.

Requirements: 64-bit Windows 10 version 2004 (build 19041) or newer, and about 250 MB of disk
space. It also runs on ARM64 Windows machines. No internet connection is needed at any point.

**Prefer not to install anything?** A `.zip` of the same application is also available. Unzip
it anywhere and double-click `Propulsion.App.exe`; it runs exactly the same way, with no Start
menu entry and no uninstaller. Delete the folder to remove it.

**On a Mac?** There is no Mac build yet. The Python examples included here do run on macOS
today, with no installation - see the Python section below.

---

## What this is

An interactive companion to Sutton and Biblarz's *Rocket Propulsion Elements*. Every
significant piece of engineering mathematics in the book has been implemented as real, tested
code, and the application lets you run it, change the numbers, and see what happens.

It is not a design tool and it will not size a real engine for you. It is the book's
equations, made executable, with the working shown.

---

## Where to start

1. Press **Help** in the top right and skim it. It explains the whole window in about two
   minutes.
2. Click any topic in the table of contents on the left, then press **Run**.
3. Press **Instructions** on a topic that interests you. It gives background, a list of
   specific experiments to try, and the traps to watch out for.
4. Find a topic badged **ANIMATED** in orange — **Vertical Ascent** or
   **Gravity Turn Ascent** — and press the orange **Visualize Trajectory** button.
5. Scroll down to **THE CODE THAT RUNS** and flip the **C# / Python** toggle.

---

## Things it would be genuinely useful to have checked

Please do not feel obliged to work through all of these. Anything you notice is helpful.

**Does it run at all**
- Does it start on your machine, and how long does it take to appear?
- Does the window fit your screen? Please say what resolution and display scaling you use
  if anything looks cramped, clipped, or falls off the edge.

**The engineering**
- If you know this material: are the numbers right? Wrong answers matter far more than
  anything cosmetic here.
- Do the units stated next to each input match what the sample actually expects? Pressures in
  particular appear as Pa in some places and MPa or bar in others — please flag anything
  inconsistent.

**The written material**
- Is the **Help** document clear to someone who has not read the textbook?
- Are the per-topic **Instructions** actually followable? Specifically: do the suggested
  experiments produce roughly the result they claim they will?

**Breaking it**
- Type nonsense into an input box — letters, a negative pressure, zero, an enormous number —
  and press Run. You should get a clear explanation, never a crash. Please report anything
  that crashes, hangs, or produces a blank output.

**The animations**
- Do they play smoothly? Do Pause, Restart, the speed selector and the scrub slider all work?

**Python**
- If you have Python 3.10 or newer: open the `python\examples` folder next to the .exe and
  run any script, for example `python ch3-cstar-cf.py`. It should print the same numbers the
  application does. No packages need installing — it uses only the standard library.

---

## Known and deliberate

Please do not report these; they are intentional.

- **There are two opposing mass-ratio conventions in the library.** Sutton's *MR* is
  `final / initial` and is less than one; the rocket-equation samples use `initial / final`
  and it is greater than one. Both appear in the book, so both are implemented, and the
  Instructions warn you wherever it matters.
- **The thermochemistry is representative, not a real equilibrium solve.** Chamber temperature,
  gamma and molecular mass come from a small catalogue of published values rather than from a
  CEA-style minimisation. Isp therefore varies with area ratio but not with off-nominal
  mixture ratio.
- **The ascent plots are not to equal scale.** Hundreds of kilometres downrange against tens of
  kilometres of altitude would squash the interesting part into a sliver.
- **The app never contacts the internet.** All calculations run on your machine, as the footer
  says. Feel free to test it with the network disconnected - nothing will be missing. The only
  exception is the Donate button below, and only if you press it.
- **The download is 57 MB but it installs to about 230 MB.** It carries its own private copy of
  the .NET and Windows App SDK runtimes, so no separate runtime download is ever required and
  nothing it installs can affect other software on your machine.
- **The Donate button opens a web page.** It opens `https://johnstraumann.com/Donate` in your
  normal browser. The program itself never handles a payment, and nothing is locked, limited or
  nagged about if you ignore it. Worth a click to confirm the page loads for you.

---

## Reporting something

Please include:

1. **The build stamp** from the bottom-right corner of the window, e.g. `v1.0.0 build 2026-09-07`.
2. **Which topic** you had selected.
3. **The input values** you had typed in.
4. **What you expected, and what you got.** The **Copy output** button puts the whole report on
   your clipboard, which is the most useful thing you can paste.
5. A screenshot if it is a layout or drawing problem.
6. Your Windows version, screen resolution and display scaling if it looks wrong on screen.
