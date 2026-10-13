; =====================================================================================
;  common.iss  -  shared Inno Setup logic for both book applications
;  =====================================================================================
;
;  WHY THIS FILE EXISTS
;  --------------------
;  There are two applications to package (Fundamentals of Astrodynamics and Rocket
;  Propulsion Elements) and they need byte-for-byte identical installer behaviour.
;  Maintaining two near-identical scripts guarantees they drift apart, so all of the
;  real logic lives here exactly once and each book supplies only its own name, icon
;  and GUID via #define before including this file.
;
;  HOW TO USE IT
;  -------------
;  A per-book script (astro.iss / propulsion.iss) must #define these symbols and then
;  #include this file as its LAST line:
;
;    MyAppId          A GUID, unique and PERMANENT per application. Inno uses it as the
;                     identity of the installed product. Changing it later makes Windows
;                     treat a new build as a completely separate program, so an upgrade
;                     would install alongside the old copy instead of replacing it.
;    MyAppName        Human readable name shown in the wizard, Start menu and
;                     "Installed apps".
;    MyAppShortName   Filename-safe name, used for the output .exe and the install folder.
;    MyAppExeName     The executable to launch, e.g. Astro.App.exe
;    MyAppVersion     Version string shown in "Installed apps".
;    MySourceDir      Folder holding the published application, relative to this script.
;
;  WHAT THE INSTALLER DOES
;  -----------------------
;  Copies a self-contained .NET publish folder (roughly 230 MB, ~680 files) to disk,
;  creates Start menu and optional desktop shortcuts, and registers a proper uninstaller.
;  It installs no runtimes, no services, no drivers, writes nothing to the registry
;  beyond the standard uninstall entry, and requires no network access.
; =====================================================================================


[Setup]
; ---- Product identity ---------------------------------------------------------------
; AppId is doubled-brace escaped ({{...}}) because a single { is Inno's constant marker.
AppId={{{#MyAppId}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher=John Straumann
AppPublisherURL=https://johnstraumann.com
AppSupportURL=https://johnstraumann.com
AppUpdatesURL=https://johnstraumann.com

; ---- Privileges ---------------------------------------------------------------------
; "lowest" means the installer runs as the ordinary logged-on user: NO User Account
; Control prompt, and no administrator password. That matters here because the people
; testing this are being asked to install software from an unknown publisher; requiring
; them to elevate raises the stakes of that decision for no benefit. Nothing in this
; application needs machine-wide access.
;
; PrivilegesRequiredOverridesAllowed=dialog still offers a choice on first run, so a
; user who genuinely wants a single copy shared by every account on the PC can pick
; "Install for all users" and Inno will elevate at that point.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; ---- Install location ---------------------------------------------------------------
; {autopf} follows whichever privilege level was chosen above:
;   per-user  -> C:\Users\<name>\AppData\Local\Programs\<AppShortName>
;   all users -> C:\Program Files\<AppShortName>
DefaultDirName={autopf}\{#MyAppShortName}
DefaultGroupName={#MyAppName}
; The program group page is one more decision a tester does not care about.
DisableProgramGroupPage=yes
; Remember the folder if this is an upgrade over an existing install.
UsePreviousAppDir=yes

; ---- Platform requirements ----------------------------------------------------------
; The application is published self-contained for x64 and targets Windows 10 2004
; (build 19041) to match the app's target framework, so refuse anything older rather
; than installing something that cannot start. "x64compatible" also covers ARM64
; machines, which run x64 binaries under emulation.
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041

; ---- Output -------------------------------------------------------------------------
OutputDir=..\dist
OutputBaseFilename={#MyAppShortName}-Setup-{#MyAppVersion}
SetupIconFile={#MySourceDir}\appicon.ico

; ---- Compression --------------------------------------------------------------------
; The payload is a self-contained .NET runtime plus MAUI and the Windows App SDK, which
; compresses extremely well but slowly. lzma2/ultra64 with solid compression takes the
; ~230 MB folder down to roughly a third of that. SolidCompression treats every file as
; one continuous stream, which matters when hundreds of small satellite assemblies
; share near-identical content.
Compression=lzma2/ultra64
SolidCompression=yes
; Compress in a separate process so the 64 MB dictionary does not have to fit inside
; the compiler's own address space.
LZMAUseSeparateProcess=yes

; ---- Wizard appearance --------------------------------------------------------------
WizardStyle=modern
; Show the disk-space cost up front. This is a large install for what looks like a
; small teaching app, and a surprise is worse than a number.
DirExistsWarning=no
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName} {#MyAppVersion}

; ---- Upgrade behaviour --------------------------------------------------------------
; If the app is running when an upgrade is installed, use the Windows Restart Manager
; to ask for it to be closed rather than failing on a locked file. Do not silently
; relaunch it afterwards; the post-install checkbox covers that intentionally.
CloseApplications=yes
RestartApplications=no


[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"


[Tasks]
; Desktop shortcut is offered but NOT ticked by default. Unrequested desktop icons are
; a minor discourtesy, and the Start menu entry is always created regardless.
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked


[Files]
; One recursive rule copies the entire published output: the executable, the .NET
; runtime, MAUI, the Windows App SDK, the localisation satellite folders, and the
; docs\ and python\ folders that publish.ps1 places alongside them.
;
;   ignoreversion       - overwrite by content rather than by file version. Correct here
;                         because these are private copies of assemblies, not shared
;                         system files, and version comparison would skip files whose
;                         contents changed without a version bump.
;   recursesubdirs      - include everything below the source folder.
;   createallsubdirs    - reproduce empty directories too, so the layout matches exactly.
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs


[Icons]
; Start menu: the application itself.
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"

; Start menu: the two folders a tester is most likely to want to open by hand. These
; are folder shortcuts, which always open in File Explorer, rather than shortcuts to
; a .md file, which would fail on a machine with no Markdown file association.
Name: "{group}\Python Examples"; Filename: "{app}\python"; Comment: "The same calculations written in Python"
Name: "{group}\Documentation"; Filename: "{app}\docs"; Comment: "Help and per-topic instructions"

; Start menu: standard uninstall entry, so removal is discoverable without going to
; Settings.
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"

; Optional desktop shortcut, created only if the user ticked the task above.
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon


[Run]
; Offer to start the application straight after installation.
;   nowait          - do not block the installer on the app's lifetime.
;   postinstall     - render as a checkbox on the final wizard page.
;   skipifsilent    - a silent/automated install should not pop a window open.
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent


[UninstallDelete]
; The application itself writes nothing into its install folder, but a user following
; the donation documentation may have dropped a donate.url next to the executable, and
; Windows can leave empty directories behind. Removing the folder outright guarantees
; an uninstall leaves no trace, which is the promise made in TESTERS.md.
Type: filesandordirs; Name: "{app}"
