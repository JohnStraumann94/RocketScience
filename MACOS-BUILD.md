# Building and distributing the macOS version

**Short answer to the question that prompted this document: no, you do not need the Mac App
Store.** You can ship a `.dmg` from your own website exactly the way you ship the Windows
installer. But you do need two things you do not have yet: **a Mac** and **a $99/year Apple
Developer account**.

---

## 1. What was actually tested

This is not guesswork. Both applications were compiled for macOS from this Windows machine to
find out precisely where the process stops.

```
dotnet build Astro.App.csproj      (TargetFramework = net10.0-maccatalyst)
Build succeeded.  0 Warning(s)  0 Error(s)
```

**The C# compiles for macOS cleanly, with zero warnings.** That is a more useful result than it
looks: it proves the application uses no Windows-only APIs. The only hand-written
Windows-specific file in either project is `Platforms/Windows/App.xaml.cs`, which is the stock
MAUI template file and has a Mac counterpart generated automatically.

The build produced `Astro.App.dll` and `Astro.Core.dll` compiled for Mac Catalyst, plus the 8
Mac native runtime libraries (`.dylib`) and 22 static libraries the app would link against.

**What it did not produce was a `.app` bundle.** The build stopped after the managed
compilation and left behind its plan for the remaining steps:

```
codesign-bundle.items   ->  <_CodesignBundle Include="Fundamentals of Astrodynamics - Interactive Code Samples.app">
                            <CodesignSigningKey></CodesignSigningKey>      <- empty
postprocessing.items    ->  the native link / bundle / asset-compile steps
```

Those final steps need Apple's own tools - `clang` to link the Mach-O binary, `actool` to
compile the asset catalogue, and `codesign` to sign the bundle. All three ship only with Xcode,
and Xcode runs only on macOS. There is no cross-compiler and no legal way to run those tools on
Windows.

**Conclusion: the port is already done. What is missing is a Mac to finish the build on, not
work on the code.**

---

## 2. Do you need the Mac App Store?

No. There are two independent routes, and the App Store is the one most small projects avoid.

| | Direct download (`.dmg` from your site) | Mac App Store |
|---|---|---|
| Apple review of your app | None | Yes, and it can be rejected |
| Time to publish an update | Minutes | Days, per review |
| Apple's cut of donations | None | Not applicable - but IAP would be 15-30% |
| Annual cost | $99 | $99 |
| Sandbox restrictions | None | Strict; your app must be sandboxed |
| What you must do | Sign + notarize | Sign + submit + pass review |
| Matches your Windows story | Yes | No |

**Recommendation: direct download.** You already hand people a Windows installer from your own
site and you already run a donation page on your own domain. A `.dmg` link beside them is
consistent, free of Apple's review queue, and updateable the moment you decide to update it.

The App Store would only be worth it if you wanted discoverability - people browsing and finding
the app who had never heard of you. For a companion to two specific textbooks, that is not where
your audience comes from.

---

## 3. What the $99 actually buys, and what happens without it

The Apple Developer Program membership is $99/year and is what lets you obtain a **Developer ID
Application certificate**. With it you can:

1. **Code-sign** the app, so macOS knows who published it.
2. **Notarize** it - upload the signed app to Apple, who scan it automatically for malware and
   return a ticket you attach ("staple") to the `.dmg`.

Notarization is automated. It is not a review, nobody at Apple looks at your app, and it usually
completes in a few minutes.

**Without signing and notarization, macOS does not merely warn - it refuses.** Gatekeeper on
current macOS tells the user the app "is damaged and can't be opened" or "cannot be opened
because Apple cannot check it for malicious software", and the workaround requires the user to
go into System Settings and explicitly override it, or right-click and choose Open. Most people
will not do that, and asking them to is precisely the wrong signal to send about software you
want them to trust.

This is stricter than Windows. Your unsigned Windows installer produces a SmartScreen warning
with a "More info -> Run anyway" path that people do take. The Mac equivalent is a wall.

**So for macOS the $99 is not optional in practice.** Budget it as part of shipping a Mac
version at all.

---

## 4. How to get a Mac to build on

You do not need to buy a Mac, though it is the simplest option.

| Option | Cost | Notes |
|---|---|---|
| **Mac mini (M-series)** | ~$599 one-off | Cheapest real Mac. Also becomes your test machine, which you need anyway - see section 7. |
| **GitHub Actions macOS runner** | Free for public repos; ~$0.08/min for private | No hardware to buy. Builds run in the cloud on every commit. Certificates go in encrypted secrets. Best option if the code is on GitHub. |
| **MacStadium / AWS EC2 Mac** | ~$60-120/month | Rented dedicated Mac. Overkill unless you are building constantly. |
| **Borrow one** | Free | Fine for a one-off build, awkward for updates, and you would have to install your signing certificate on someone else's machine - don't. |

**Recommendation: a Mac mini if you plan to maintain this, GitHub Actions if you just want the
build automated and the code is going into a repository anyway.** The deciding factor is section
7 - you will want to actually look at the app running on macOS before sending it to anyone, and
for that a real Mac is hard to substitute.

---

## 5. The exact build steps, on a Mac

### One-time setup

```bash
# 1. Install Xcode from the App Store, then accept its licence and install the CLI tools
sudo xcodebuild -license accept
xcode-select --install

# 2. Install the .NET SDK (same version this project uses)
#    https://dotnet.microsoft.com/download/dotnet/10.0

# 3. Install the MAUI workload
dotnet workload install maui

# 4. Enrol at https://developer.apple.com/programs/ ($99/year), then in Xcode:
#    Settings -> Accounts -> Manage Certificates -> + -> Developer ID Application
```

### Enable the Mac target

In both `Astro.App.csproj` and `Propulsion.App.csproj` the Mac target framework is already
present and commented out, precisely so this is a one-line change. Replace:

```xml
<TargetFrameworks>net10.0-windows10.0.19041.0</TargetFrameworks>
```

with:

```xml
<TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">net10.0-windows10.0.19041.0</TargetFrameworks>
<TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('osx'))">net10.0-maccatalyst</TargetFrameworks>
```

The condition matters: it keeps the Windows build working untouched on your current machine
while adding the Mac build on a Mac. Do not simply list both unconditionally, or every Windows
build will start failing on the missing Xcode.

### Build a signed, universal app

```bash
dotnet publish src/Astro.App/Astro.App.csproj \
  -f net10.0-maccatalyst \
  -c Release \
  -p:RuntimeIdentifiers="maccatalyst-x64;maccatalyst-arm64" \
  -p:CreatePackage=false \
  -p:EnableCodeSigning=true \
  -p:CodesignKey="Developer ID Application: John Straumann (YOURTEAMID)" \
  -p:UseHardenedRuntime=true
```

Notes on the flags, because several are easy to get wrong:

- **`RuntimeIdentifiers` (plural)** with both architectures produces a universal binary that runs
  natively on both Apple Silicon and older Intel Macs. Using the singular `RuntimeIdentifier`
  gives you one architecture only.
- **`UseHardenedRuntime=true` is required for notarization.** Apple will reject the submission
  without it, and the error message is not especially clear about why.
- **`CreatePackage=false`** produces a plain `.app` bundle. You want that, because you are going
  to wrap it in a `.dmg` yourself. Setting it to `true` produces a `.pkg` installer instead,
  which is the other legitimate choice but is a heavier experience for a self-contained app.

Output lands in `bin/Release/net10.0-maccatalyst/maccatalyst-x64/` as
`Fundamentals of Astrodynamics - Interactive Code Samples.app`.

### Wrap it in a .dmg

```bash
brew install create-dmg

create-dmg \
  --volname "Fundamentals of Astrodynamics" \
  --window-size 600 400 \
  --icon-size 100 \
  --app-drop-link 450 200 \
  "FundamentalsOfAstrodynamics-1.0.0.dmg" \
  "bin/Release/net10.0-maccatalyst/maccatalyst-x64/Fundamentals of Astrodynamics - Interactive Code Samples.app"
```

`--app-drop-link` is what produces the familiar "drag the icon onto the Applications folder"
window. That drag *is* the install on macOS; there is no installer program, which is why the Mac
side needs no equivalent of Inno Setup.

### Notarize and staple

```bash
# Store credentials once (use an app-specific password from appleid.apple.com, not your real one)
xcrun notarytool store-credentials "notary" \
  --apple-id "you@example.com" --team-id "YOURTEAMID" --password "abcd-efgh-ijkl-mnop"

# Submit and wait (usually a few minutes)
xcrun notarytool submit FundamentalsOfAstrodynamics-1.0.0.dmg --keychain-profile "notary" --wait

# Attach the ticket so the app validates even when the user is offline
xcrun stapler staple FundamentalsOfAstrodynamics-1.0.0.dmg

# Confirm it will actually pass Gatekeeper before you publish it
spctl -a -t open --context context:primary-signature -v FundamentalsOfAstrodynamics-1.0.0.dmg
```

That last check is worth running every time. It is the difference between finding out now and
finding out from a tester.

---

## 6. Cost and effort summary

| Item | One-off | Recurring |
|---|---|---|
| Apple Developer Program | - | $99/year |
| Mac mini (if buying) | ~$599 | - |
| Enabling the Mac target in the projects | ~5 minutes | - |
| First build, signing and notarization setup | Half a day, mostly Apple account admin | - |
| Each subsequent release | ~15 minutes | - |
| Testing the UI on macOS | Half a day, see below | Some per release |

---

## 7. The part that is genuinely unknown

Everything above is mechanical. This is not.

The layout of both applications was designed, tuned and verified on Windows. Mac Catalyst will
render the same XAML with macOS controls, and some things reliably need attention:

- **The window sizing logic.** The apps size themselves against the display, with a fix already
  in place for small laptop screens. That code uses cross-platform MAUI APIs and should work,
  but macOS reports display metrics differently and it has never been run there.
- **Fonts and text metrics.** Column widths in the sample list and the tables in the
  documentation viewer were tuned against Windows font rendering. Mac text is typically wider.
- **The trajectory animation.** It is drawn with `GraphicsView`, which is fully cross-platform,
  but frame timing has only ever been measured on Windows.
- **The Donate button.** It calls `Launcher.Default.OpenAsync`, which is cross-platform and
  should open Safari without changes.
- **Menu bar conventions.** macOS puts an application menu at the top of the screen and expects
  Cmd-Q to quit. The apps currently have no menu bar at all.

None of these are hard problems, but they are real work, and none of them can be found without a
Mac in front of you. Budget for a round of visual fixes rather than assuming the first build is
shippable.

---

## 8. Recommendation

**Ship Windows now, and treat macOS as a decision rather than a task.**

The Windows installers are built, tested and ready. Nothing about the Mac version needs to hold
them up.

Before spending the $99 and the hardware, find out how many of your testers are actually on
Macs. If the answer is "one or two", the honest move is to tell them the Mac version is coming
and get feedback from the Windows users first. If a meaningful share of your audience is on
macOS - and for an academic and hobbyist audience it often is - then the path above is
well-trodden and the code is already proven to compile.

One genuinely useful intermediate option: **the Python examples already run on macOS today.**
Every calculation in both applications is duplicated as a standalone Python script using only
the standard library. A Mac user can run those right now with no build, no signing and no
Developer account. That is not the same experience as the app, but it does mean no Mac user is
completely locked out while you decide.
