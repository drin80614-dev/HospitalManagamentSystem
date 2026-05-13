param(
    [string]$BackendUrl = "https://hospitalmanagamentsystem.onrender.com/Auth/Login",
    [string]$Flutter = "..\tools\flutter\bin\flutter.bat"
)

$ErrorActionPreference = "Stop"

$appRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $appRoot
$flutterPath = Join-Path $repoRoot $Flutter

Push-Location $appRoot
try {
    & $flutterPath pub get
    & $flutterPath build apk --release --dart-define "BACKEND_URL=$BackendUrl"

    $apkPath = Join-Path $appRoot "build\app\outputs\flutter-apk\app-release.apk"
    Write-Host "Android APK created: $apkPath"
}
finally {
    Pop-Location
}
