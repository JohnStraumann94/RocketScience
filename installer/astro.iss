; =====================================================================================
;  astro.iss  -  installer definition for "Fundamentals of Astrodynamics"
;  =====================================================================================
;  This file deliberately contains no logic. It supplies the handful of values that
;  differ between the two books and then includes common.iss, which holds every actual
;  installer setting. See common.iss for what each symbol means and what the installer
;  does.
;
;  Build with:   installer\make-installers.ps1
;  or directly:  "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\astro.iss
; =====================================================================================

; A permanent identity for this product. Never change this value: Windows uses it to
; recognise an upgrade of an existing install rather than a brand new program.
#define MyAppId        "DDDC243E-F669-454D-88A1-43874BD31137"

#define MyAppName      "Fundamentals of Astrodynamics"
#define MyAppShortName "FundamentalsOfAstrodynamics"
#define MyAppExeName   "Astro.App.exe"
#define MyAppVersion   "1.0.0"

; The published, self-contained output produced by publish.ps1. Path is relative to
; this script's own folder.
#define MySourceDir    "..\dist\FundamentalsOfAstrodynamics"

#include "common.iss"
