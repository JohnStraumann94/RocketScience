<#
.SYNOPSIS
    Builds a single-file Windows installer (.exe) for each of the two book applications.

.DESCRIPTION
    This is the second half of the distribution pipeline. The first half is publish.ps1,
    which produces a self-contained folder per application under .\dist\. This script
    takes those folders and wraps each one in an Inno Setup installer, so a tester
    receives one file, double-clicks it, and gets a normal Windows program with Start
    menu shortcuts and a working uninstaller.

    WHY AN INSTALLER RATHER THAN THE ZIP
    ------------------------------------
    The zip already works, but it asks the recipient to do three things correctly:
    unblock the download, extract it somewhere sensible, and find the right .exe among
    roughly 680 files. Every one of those is a place to lose a tester. The installer
    removes all three, and gives a clean uninstall in return.

    WHAT IT DOES NOT DO
    -------------------
    It does not code-sign the result. Without a purchased certificate, Windows
    SmartScreen will still warn on first run. That is a known, documented limitation
    described in each book's TESTERS.md; it is a matter of buying a certificate, not of
    changing this script.

.PARAMETER DistRoot
    Folder containing the published application folders. Defaults to .\dist next to
    this script's parent folder.

.PARAMETER Only
    Build just one installer. Accepts 'astro' or 'propulsion'. Omit to build both.

.PARAMETER SkipPublish
    By default this script verifies that the published folders exist and are current.
    It never publishes for you; this switch simply suppresses the staleness warning.

.EXAMPLE
    .\installer\make-installers.ps1
    Builds both installers into .\dist\

.EXAMPLE
    .\installer\make-installers.ps1 -Only astro
    Builds only the Fundamentals of Astrodynamics installer.
#>

[CmdletBinding()]
param(
    [string]   $DistRoot,
    [ValidateSet('astro', 'propulsion')]
    [string]   $Only,
    [switch]   $SkipPublish
)

# Stop on the first unhandled error rather than continuing with a half-built installer.
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Split-Path -Parent $scriptDir
if (-not $DistRoot) { $DistRoot = Join-Path $repoRoot 'dist' }

# -------------------------------------------------------------------------------------
# Locate the Inno Setup command line compiler.
# -------------------------------------------------------------------------------------
# ISCC.exe is the non-interactive compiler that ships with Inno Setup. Inno is a 32-bit
# application, so on a 64-bit machine it installs under "Program Files (x86)" by
# default, but a per-user install lands elsewhere entirely. Check the usual places and
# then fall back to PATH before giving up with an actionable message.
$isccCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    $iscc = (Get-Command ISCC.exe -ErrorAction SilentlyContinue).Source
}
if (-not $iscc) {
    throw "Inno Setup 6 was not found. Install it with:  winget install --id JRSoftware.InnoSetup"
}
Write-Host "Using Inno Setup compiler: $iscc" -ForegroundColor DarkGray

# -------------------------------------------------------------------------------------
# The two products. Each entry pairs an .iss script with the published folder it packs
# and the executable that folder must contain.
# -------------------------------------------------------------------------------------
$products = @(
    [pscustomobject]@{
        Key     = 'astro'
        Name    = 'Fundamentals of Astrodynamics'
        Script  = Join-Path $scriptDir 'astro.iss'
        Payload = Join-Path $DistRoot  'FundamentalsOfAstrodynamics'
        Exe     = 'Astro.App.exe'
    },
    [pscustomobject]@{
        Key     = 'propulsion'
        Name    = 'Rocket Propulsion Elements'
        Script  = Join-Path $scriptDir 'propulsion.iss'
        Payload = Join-Path $DistRoot  'RocketPropulsionElements'
        Exe     = 'Propulsion.App.exe'
    }
)

if ($Only) { $products = $products | Where-Object Key -eq $Only }

# -------------------------------------------------------------------------------------
# Pre-flight checks.
# -------------------------------------------------------------------------------------
# Inno will happily build an installer around a missing or half-published folder and
# the failure would only surface on the tester's machine, so validate the payload here
# where the error is cheap. Checking for the executable specifically (rather than just
# the folder) catches the common case of a publish that failed partway through.
foreach ($p in $products) {
    if (-not (Test-Path $p.Payload)) {
        throw "Published folder not found: $($p.Payload)`nRun publish.ps1 first."
    }
    $exePath = Join-Path $p.Payload $p.Exe
    if (-not (Test-Path $exePath)) {
        throw "$($p.Exe) is missing from $($p.Payload) - the publish looks incomplete. Re-run publish.ps1."
    }
    # The installer creates Start menu shortcuts to these folders, so a missing one
    # would produce a shortcut that opens nothing.
    foreach ($required in 'docs', 'python') {
        if (-not (Test-Path (Join-Path $p.Payload $required))) {
            throw "'$required' folder is missing from $($p.Payload). Re-run publish.ps1, which copies it."
        }
    }
    if (-not (Test-Path (Join-Path $p.Payload 'appicon.ico'))) {
        throw "appicon.ico is missing from $($p.Payload); the installer needs it for its own icon."
    }

    # Warn if the published payload predates the newest source file. This is advisory
    # only: it is legitimate to package an older build deliberately, but shipping a
    # stale build by accident is a genuinely expensive mistake to discover later.
    if (-not $SkipPublish) {
        $srcRoot = Join-Path $repoRoot ($(if ($p.Key -eq 'astro') { 'FundamentalsOfAstrodynamics' } else { 'RocketPropulsionElements' }) + '\src')
        if (Test-Path $srcRoot) {
            $newestSource = Get-ChildItem $srcRoot -Recurse -File -Include *.cs, *.xaml, *.md, *.py -ErrorAction SilentlyContinue |
                            Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } |
                            Sort-Object LastWriteTime -Descending | Select-Object -First 1
            if ($newestSource -and $newestSource.LastWriteTime -gt (Get-Item $exePath).LastWriteTime) {
                Write-Warning "$($p.Name): source file '$($newestSource.Name)' is newer than the published exe. Consider re-running publish.ps1."
            }
        }
    }
}

# -------------------------------------------------------------------------------------
# Compile.
# -------------------------------------------------------------------------------------
# Solid LZMA2 compression of a ~230 MB payload is CPU-bound and takes a few minutes per
# product, which is why the progress messages are explicit about it.
$results = @()
foreach ($p in $products) {
    $payloadMb = [math]::Round((Get-ChildItem $p.Payload -Recurse -File | Measure-Object Length -Sum).Sum / 1MB, 1)
    Write-Host ""
    Write-Host "Building installer for $($p.Name)" -ForegroundColor Cyan
    Write-Host "    payload  : $payloadMb MB"
    Write-Host "    compressing (this takes a few minutes)"

    $started = Get-Date

    # /Q suppresses the per-file progress spam but still reports warnings and errors.
    & $iscc /Q $p.Script
    if ($LASTEXITCODE -ne 0) {
        throw "ISCC failed for $($p.Name) with exit code $LASTEXITCODE"
    }

    # Find what was actually produced rather than assuming the filename, so a version
    # bump in the .iss does not silently break the summary below.
    $setup = Get-ChildItem $DistRoot -Filter '*-Setup-*.exe' |
             Where-Object { $_.LastWriteTime -ge $started } |
             Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $setup) {
        throw "ISCC reported success for $($p.Name) but no installer appeared in $DistRoot"
    }

    $setupMb = [math]::Round($setup.Length / 1MB, 1)
    Write-Host ("    done     : {0}  ({1} MB, {2:n0}% of payload, {3:n0}s)" -f `
                $setup.Name, $setupMb, (100 * $setupMb / $payloadMb), ((Get-Date) - $started).TotalSeconds) -ForegroundColor Green

    $results += [pscustomobject]@{
        Product    = $p.Name
        Installer  = $setup.Name
        SizeMB     = $setupMb
        PayloadMB  = $payloadMb
    }
}

Write-Host ""
Write-Host "All installers built into $DistRoot" -ForegroundColor Green
$results | Format-Table -AutoSize
Write-Host "Note: these installers are NOT code-signed, so Windows SmartScreen will warn" -ForegroundColor Yellow
Write-Host "      on first run. See TESTERS.md for the wording testers should expect." -ForegroundColor Yellow
