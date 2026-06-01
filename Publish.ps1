<#
.SYNOPSIS
    Builds a Release, renames exe->scr, signs, and packages as a zip.
.DESCRIPTION
    1. dotnet publish -c Release
    2. Renames SlideShowScreenSaver.exe -> SlideShowScreenSaver.scr
    3. Signs the .scr via Sign.ps1 (SimplySign Desktop must be running and authenticated)
    4. Creates SlideShowScreenSaver-v<version>.zip at the solution root

    Run from a PowerShell prompt at the solution root.
#>

$ErrorActionPreference = "Stop"

$solutionRoot = $PSScriptRoot
$project      = "$solutionRoot\SlideShowScreenSaver\SlideShowScreenSaver.csproj"
$releaseDir   = "$solutionRoot\SlideShowScreenSaver\bin\Release\net10.0-windows\win-x64"

# Read version from .csproj
[xml]$csproj = Get-Content $project
$version = ($csproj.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ } | Select-Object -First 1)
if (-not $version) { Write-Error "Could not read <Version> from $project" }

Write-Host ""
Write-Host "=== SlideShowScreenSaver v$version ===" -ForegroundColor Cyan

# ── Step 1: Build ──────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "[1/4] Building Release..." -ForegroundColor Yellow
dotnet publish $project -c Release -o "$releaseDir"
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish failed (exit $LASTEXITCODE)." }

# ── Step 2: Rename .exe -> .scr ────────────────────────────────────────────────
Write-Host ""
Write-Host "[2/4] Renaming .exe -> .scr..." -ForegroundColor Yellow

$exePath = "$releaseDir\SlideShowScreenSaver.exe"
$scrPath = "$releaseDir\SlideShowScreenSaver.scr"

if (-not (Test-Path $exePath)) { Write-Error "Expected build output not found: $exePath" }
if (Test-Path $scrPath) { Remove-Item $scrPath -Force }
Rename-Item $exePath $scrPath
Write-Host "  -> $scrPath"

# ── Step 3: Sign ──────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "[3/4] Signing (SimplySign Desktop must be running)..." -ForegroundColor Yellow
& "$solutionRoot\Sign.ps1" -FilePath $scrPath

# ── Step 4: Package zip ───────────────────────────────────────────────────────
Write-Host ""
Write-Host "[4/4] Packaging zip..." -ForegroundColor Yellow

$zipName = "SlideShowScreenSaver-v$version.zip"
$zipPath = "$solutionRoot\$zipName"

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$releaseDir\*" -DestinationPath $zipPath
Write-Host "  -> $zipPath" -ForegroundColor Green

Write-Host ""
Write-Host "=== Done: $zipName ===" -ForegroundColor Green
