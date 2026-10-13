# Rocket Science — Interactive Companions

Two Windows desktop apps that turn the equations of two classic astronautics textbooks into
runnable, heavily commented code. Pick a topic, edit the inputs, press **Run**, and see what
changes. Every sample is written in C# with a parallel Python version.

| App | Companion to | Samples | Source |
|---|---|---|---|
| Fundamentals of Astrodynamics | *Fundamentals of Astrodynamics* (Bate, Mueller & White) | 18 | [`FundamentalsOfAstrodynamics/`](FundamentalsOfAstrodynamics) |
| Rocket Propulsion Elements | *Rocket Propulsion Elements* (Sutton) | 25 | [`RocketPropulsionElements/`](RocketPropulsionElements) |

These are independent study aids. They contain no text from the books; you need the books
themselves to follow along.

## Just want to run the apps?

Go to the [**Releases**](../../releases/latest) page and download:

- `FundamentalsOfAstrodynamics-Setup-1.0.0.exe`
- `RocketPropulsionElements-Setup-1.0.0.exe`

Requirements: 64-bit Windows 10 (build 19041) or newer, about 250 MB of disk, no internet needed.
The installer is per-user (no administrator prompt). It is not code-signed, so Windows
SmartScreen shows "Windows protected your PC": choose **More info → Run anyway**.
Prefer no installer? Each release also has a `.zip` — unzip and run the `.exe` inside.
To uninstall use **Settings → Apps → Installed apps**.

## Building from source

Requires the .NET 10 SDK with the MAUI workload (Visual Studio 2026 or `dotnet workload install maui`).

```powershell
# open in Visual Studio 2026
FundamentalsOfAstrodynamics\FundamentalsOfAstrodynamics.slnx
RocketPropulsionElements\RocketPropulsionElements.slnx

# or from the command line
dotnet test FundamentalsOfAstrodynamics\FundamentalsOfAstrodynamics.slnx
dotnet test RocketPropulsionElements\RocketPropulsionElements.slnx
```

## Building the installers

1. `publish.ps1` — publishes both apps self-contained (win-x64) into `dist\`.
2. `installer\make-installers.ps1` — runs [Inno Setup 6](https://jrsoftware.org/isinfo.php)
   (`winget install JRSoftware.InnoSetup`) to produce `dist\*-Setup-1.0.0.exe` and the zips.

Then upload the four files in `dist\` as assets of a GitHub Release.

## Layout

```
FundamentalsOfAstrodynamics/   solution, Astro.Core library, Astro.App (MAUI), tests, docs, python
RocketPropulsionElements/      solution, Propulsion.Core library, Propulsion.App (MAUI), tests, docs, python
installer/                     Inno Setup scripts (common.iss + one per app) and make-installers.ps1
publish.ps1                    self-contained publish step
MACOS-BUILD.md                 notes on building for macOS
```

Each app folder has its own `README.md` (developer docs) and `TESTERS.md` (user guide).

Author: John Straumann
