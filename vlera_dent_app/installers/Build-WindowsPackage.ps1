param(
    [string]$BackendUrl = "https://vlera-dent-frontend.drin80614.workers.dev/Auth/Login",
    [string]$Flutter = "..\tools\flutter\bin\flutter.bat"
)

$ErrorActionPreference = "Stop"

$appRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $appRoot
$flutterPath = Join-Path $repoRoot $Flutter

Push-Location $appRoot
try {
    & $flutterPath pub get
    & $flutterPath build windows --release --dart-define "BACKEND_URL=$BackendUrl"

    $releaseDir = Join-Path $appRoot "build\windows\x64\runner\Release"
    $packageDir = Join-Path $appRoot "build\installer"
    $zipPath = Join-Path $packageDir "VleraDent-Windows.zip"

    New-Item -ItemType Directory -Force $packageDir | Out-Null
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    Compress-Archive -Path (Join-Path $releaseDir "*") -DestinationPath $zipPath
    Write-Host "Windows package created: $zipPath"
}
finally {
    Pop-Location
}
