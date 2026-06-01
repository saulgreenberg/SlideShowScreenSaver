<#
.SYNOPSIS
    Signs SlideShowScreenSaver.scr using the SimplySign/Certum code signing certificate.
.DESCRIPTION
    Run after a Release build. Automatically launches SimplySign Desktop if it is not
    already running, then waits for you to log in before proceeding.
.PARAMETER FilePath
    Path to the .scr (or .exe) to sign. Defaults to the Release build output.
.PARAMETER Thumbprint
    SHA-1 thumbprint of the SimplySign certificate.
.PARAMETER SignTool
    Path to signtool.exe. Defaults to the Windows 10 SDK location.
.EXAMPLE
    .\Sign.ps1
.EXAMPLE
    .\Sign.ps1 -FilePath "C:\MyPath\SlideShowScreenSaver.scr"
#>
param(
    [string]$FilePath     = "",
    [string]$Thumbprint   = "B6FF9831D50B47E1500DD47A0612E01E371CABC4",
    [string]$SignTool     = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe",
    [string]$TimestampUrl = "http://timestamp.certum.pl"
)

$ErrorActionPreference = "Stop"

# --- Ensure SimplySign Desktop is running ------------------------------------
$ssRunning = Get-Process -Name "SimplySignDesktop" -ErrorAction SilentlyContinue
if (-not $ssRunning) {
    Write-Host "SimplySign Desktop is not running. Searching for executable..." -ForegroundColor Yellow

    $candidates = @(
        "C:\Program Files\Certum\SimplySign Desktop\SimplySignDesktop.exe",
        "C:\Program Files (x86)\Certum\SimplySign Desktop\SimplySignDesktop.exe",
        "$env:LOCALAPPDATA\Programs\SimplySign Desktop\SimplySignDesktop.exe"
    )
    $ssExe = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1

    if (-not $ssExe) {
        Write-Host "  Scanning Program Files..." -ForegroundColor Yellow
        $ssExe = Get-ChildItem "C:\Program Files", "C:\Program Files (x86)" `
            -Recurse -Filter "SimplySignDesktop.exe" -ErrorAction SilentlyContinue |
            Select-Object -First 1 -ExpandProperty FullName
    }

    if (-not $ssExe) {
        Write-Error "SimplySign Desktop not found. Install it or start it manually, then re-run."
    }

    Write-Host "  Starting: $ssExe" -ForegroundColor Yellow
    Start-Process $ssExe

    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show(
        "SimplySign Desktop is launching.`n`nLog in with your credentials (you will need your phone for the TOTP code), then click OK to continue signing.",
        "SimplySign - Log In Required",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Information
    ) | Out-Null
} else {
    Write-Host "SimplySign Desktop is running." -ForegroundColor Green
}

# --- Resolve the file to sign ------------------------------------------------
if (-not $FilePath) {
    $releaseDir = "$PSScriptRoot\SlideShowScreenSaver\bin\Release\net10.0-windows\win-x64"
    $scrPath    = Join-Path $releaseDir "SlideShowScreenSaver.scr"
    $exePath    = Join-Path $releaseDir "SlideShowScreenSaver.exe"

    if (Test-Path $scrPath) {
        $FilePath = $scrPath
    } elseif (Test-Path $exePath) {
        $FilePath = $exePath
    } else {
        Write-Error "Could not find SlideShowScreenSaver.scr or .exe in:`n  $releaseDir"
    }
}

if (-not (Test-Path $FilePath)) {
    Write-Error "File not found: $FilePath"
}

# --- Locate signtool.exe -----------------------------------------------------
if (-not (Test-Path $SignTool)) {
    $kitBin   = "C:\Program Files (x86)\Windows Kits\10\bin"
    $SignTool = Get-ChildItem "$kitBin\*\x64\signtool.exe" -ErrorAction SilentlyContinue |
        Sort-Object { [version]($_.Directory.Parent.Name) } -Descending |
        Select-Object -First 1 -ExpandProperty FullName

    if (-not $SignTool) {
        Write-Error "signtool.exe not found. Install the Windows SDK."
    }
}

Write-Host "File     : $FilePath"
Write-Host "signtool : $SignTool"
Write-Host "Cert     : $Thumbprint"
Write-Host ""

# --- Sign --------------------------------------------------------------------
Write-Host "Signing..."
& $SignTool sign /sha1 $Thumbprint /fd SHA256 /tr $TimestampUrl /td sha256 /v $FilePath

if ($LASTEXITCODE -ne 0) {
    Write-Error "signtool sign failed (exit $LASTEXITCODE). Make sure SimplySign Desktop is running and that you are logged in."
}

# --- Verify ------------------------------------------------------------------
Write-Host ""
Write-Host "Verifying..."
& $SignTool verify /pa /v $FilePath

if ($LASTEXITCODE -ne 0) {
    Write-Error "Signature verification failed (exit $LASTEXITCODE)."
}

Write-Host ""
Write-Host "Done. $FilePath is signed and verified." -ForegroundColor Green
