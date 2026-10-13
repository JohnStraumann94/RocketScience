; =====================================================================================
;  propulsion.iss  -  installer definition for "Rocket Propulsion Elements"
;  =====================================================================================
;  This file deliberately contains no logic. It supplies the handful of values that
;  differ between the two books and then includes common.iss, which holds every actual
;  installer setting. See common.iss for what each symbol means and what the installer
;  does.
;
;  Build with:   installer\make-installers.ps1
;  or directly:  "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\propulsion.iss
; =====================================================================================

; A permanent identity for this product. Never change this value: Windows uses it to
; recognise an upgrade of an existing install rather than a brand new program.
#define MyAppId        "4B09DC44-AAFC-4BB5-BFA9-5475BE27630D"

#define MyAppName      "Rocket Propulsion Elements"
#define MyAppShortName "RocketPropulsionElements"
#define MyAppExeName   "Propulsion.App.exe"
#define MyAppVersion   "1.0.0"

; The published, self-contained output produced by publish.ps1. Path is relative to
; this script's own folder.
#define MySourceDir    "..\dist\RocketPropulsionElements"

#include "common.iss"
