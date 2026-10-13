<#
.SYNOPSIS
    Builds both sample applications into self-contained folders that testers can run
    without installing anything, and optionally zips them for distribution.

.DESCRIPTION
    Both applications are unpackaged WinUI 3 desktop apps (WindowsPackageType=None).
    By default such an app needs BOTH the .NET runtime AND the Windows App SDK runtime
    already present on the machine that runs it. Testers will not have either, and asking
    them to install two runtimes before they can look at a rocket equation is a good way
    to never receive any feedback.

    This script therefore publishes with:

        -p:SelfContained=true             .NET runtime is copied into the output folder
        -p:WindowsAppSDKSelfContained=true  Windows App SDK is copied in as well

    The result is a folder of roughly 230 MB per application containing an .exe that runs
    on any Windows 10 build 17763 or newer x64 machine with nothing installed at all.

    The Python translations are copied in beside the executable as well. They are already
    embedded inside the app for display purposes, but a tester who wants to actually RUN
    the Python needs real files on disk.

.PARAMETER OutputRoot
    Where to write the published folders. Defaults to .\dist next to this script.

.PARAMETER Zip
    Also produce a .zip of each published folder, ready to hand out.

.PARAMETER Configuration
    Build configuration. Defaults to Release; there is no good reason to send testers a
    Debug build.

.EXAMPLE
    .\publish.ps1
    Publishes both apps into .\dist\

.EXAMPLE
    .\publish.ps1 -Zip
    Publishes both apps and produces .\dist\FundamentalsOfAstrodynamics.zip and
    .\dist\RocketPropulsionElements.zip
#>

[CmdletBinding()]
param(
    [string]$OutputRoot = (Join-Path $PSScriptRoot 'dist'),
    [switch]$Zip,
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

# The single Windows target framework both apps declare. Kept in one place so that a future
# change to the TFM only has to be made once.
$framework = 'net10.0-windows10.0.19041.0'

# Each entry describes one shippable application. Name is used for the output folder and the
# zip file; Project is the csproj to publish; BookRoot is the folder holding docs\ and python\.
$applications = @(
    @{
        Name     = 'FundamentalsOfAstrodynamics'
        Project  = 'FundamentalsOfAstrodynamics\src\Astro.App\Astro.App.csproj'
        BookRoot = 'FundamentalsOfAstrodynamics'
        Exe      = 'Astro.App.exe'
    },
    @{
        Name     = 'RocketPropulsionElements'
        Project  = 'RocketPropulsionElements\src\Propulsion.App\Propulsion.App.csproj'
        BookRoot = 'RocketPropulsionElements'
        Exe      = 'Propulsion.App.exe'
    }
)

Write-Host ''
Write-Host "Publishing $($applications.Count) applications ($Configuration, self-contained x64)" -ForegroundColor Cyan
Write-Host "Output root: $OutputRoot"
Write-Host ''

foreach ($app in $applications) {

    $project = Join-Path $PSScriptRoot $app.Project
    $target  = Join-Path $OutputRoot $app.Name

    Write-Host "==> $($app.Name)" -ForegroundColor Cyan

    if (-not (Test-Path $project)) {
        throw "Project not found: $project"
    }

    # A stale output folder would leave orphaned files from a previous run mixed in with the
    # new ones, which is exactly the sort of thing that produces an unreproducible bug report.
    if (Test-Path $target) {
        Write-Host '    cleaning previous output'
        Remove-Item $target -Recurse -Force
    }

    Write-Host '    publishing (this takes a few minutes)'

    # RuntimeIdentifierOverride rather than RuntimeIdentifier: the MAUI targets set the RID
    # themselves, and the override is what wins against them.
    & dotnet publish $project `
        -c $Configuration `
        -f $framework `
        -p:RuntimeIdentifierOverride=win-x64 `
        -p:SelfContained=true `
        -p:WindowsAppSDKSelfContained=true `
        -o $target `
        --nologo `
        -v quiet

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $($app.Name) with exit code $LASTEXITCODE"
    }

    $exePath = Join-Path $target $app.Exe

    if (-not (Test-Path $exePath)) {
        throw "Publish completed but $($app.Exe) is missing from $target"
    }

    # Copy the Python translations so testers can run them, and the guides so they are
    # readable outside the app. Both are already embedded in the executable; these copies
    # exist purely so they can be opened and executed directly.
    foreach ($extra in @('python', 'docs')) {

        $source = Join-Path $PSScriptRoot (Join-Path $app.BookRoot $extra)

        if (Test-Path $source) {
            Copy-Item $source (Join-Path $target $extra) -Recurse -Force
            Write-Host "    copied $extra\"
        }
    }

    # TESTERS.md is the first thing a tester should read, so it goes in the root of the folder.
    $testers = Join-Path $PSScriptRoot (Join-Path $app.BookRoot 'TESTERS.md')

    if (Test-Path $testers) {
        Copy-Item $testers $target -Force
        Write-Host '    copied TESTERS.md'
    }

    # donate.url, if present, overrides the donation address compiled into the application.
    # Copying it here means the donation page can be corrected - or switched to a different
    # payment processor entirely - on a build that has already been published, without a rebuild.
    # See DONATIONS-SETUP.md. Its absence is completely normal and is the expected case: the app
    # then uses the address built into it, which is a live donation page.
    $donate = Join-Path $PSScriptRoot (Join-Path $app.BookRoot 'donate.url')

    if (Test-Path $donate) {
        $overrideUrl = (Get-Content $donate | Where-Object { $_.Trim() -and -not $_.Trim().StartsWith('#') } | Select-Object -First 1)
        Copy-Item $donate $target -Force
        Write-Host "    copied donate.url - Donate button overridden to $overrideUrl" -ForegroundColor DarkYellow
    }
    else {
        Write-Host '    no donate.url - Donate button uses the address built into the app'
    }

    $sizeMb = [math]::Round(((Get-ChildItem $target -Recurse -File | Measure-Object Length -Sum).Sum / 1MB), 1)
    Write-Host "    done: $target  ($sizeMb MB)" -ForegroundColor Green

    if ($Zip) {
        $zipPath = Join-Path $OutputRoot "$($app.Name).zip"

        if (Test-Path $zipPath) {
            Remove-Item $zipPath -Force
        }

        Write-Host '    compressing'
        Compress-Archive -Path (Join-Path $target '*') -DestinationPath $zipPath

        $zipMb = [math]::Round(((Get-Item $zipPath).Length / 1MB), 1)
        Write-Host "    zipped: $zipPath  ($zipMb MB)" -ForegroundColor Green
    }

    Write-Host ''
}

Write-Host 'All applications published.' -ForegroundColor Green
Write-Host ''
Write-Host 'To hand out: send the folder (or the .zip) and tell the tester to run the .exe.'
Write-Host 'Nothing needs to be installed on their machine.'
Write-Host ''
