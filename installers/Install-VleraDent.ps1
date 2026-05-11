param(
    [string]$AppUrl = "https://hospitalmanagmentsystem.onrender.com/Auth/Login"
)

$ErrorActionPreference = "Stop"

function Get-BrowserPath {
    $candidates = @(
        "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe",
        "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
        "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
        "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe"
    )

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return $candidate
        }
    }

    throw "Microsoft Edge ose Google Chrome nuk u gjet. Instalo Edge/Chrome dhe provo prape."
}

$appName = "Vlera Dent"
$browserPath = Get-BrowserPath
$sourceDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$installDir = Join-Path $env:LOCALAPPDATA "VleraDent"
$profileDir = Join-Path $installDir "BrowserProfile"
$desktopPath = [Environment]::GetFolderPath("Desktop")
$startMenuPath = Join-Path ([Environment]::GetFolderPath("Programs")) "Vlera Dent"
$shortcutArgs = "--user-data-dir=""$profileDir"" --app=""$AppUrl"""

New-Item -ItemType Directory -Force -Path $installDir | Out-Null
New-Item -ItemType Directory -Force -Path $profileDir | Out-Null
New-Item -ItemType Directory -Force -Path $startMenuPath | Out-Null

$sourceIcon = Join-Path $sourceDir "vlera-dent.ico"
$iconPath = Join-Path $installDir "vlera-dent.ico"
if (Test-Path -LiteralPath $sourceIcon) {
    Copy-Item -LiteralPath $sourceIcon -Destination $iconPath -Force
} else {
    $iconPath = $browserPath
}

$shell = New-Object -ComObject WScript.Shell

foreach ($target in @(
    (Join-Path $desktopPath "$appName.lnk"),
    (Join-Path $startMenuPath "$appName.lnk")
)) {
    $shortcut = $shell.CreateShortcut($target)
    $shortcut.TargetPath = $browserPath
    $shortcut.Arguments = $shortcutArgs
    $shortcut.WorkingDirectory = Split-Path -Parent $browserPath
    $shortcut.IconLocation = $iconPath
    $shortcut.Description = "Vlera Dent dental clinic app"
    $shortcut.Save()
}

$uninstallScript = Join-Path $installDir "Uninstall-VleraDent.ps1"
@"
Remove-Item -LiteralPath "$desktopPath\$appName.lnk" -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath "$startMenuPath" -Recurse -Force -ErrorAction SilentlyContinue
Get-Process msedge,chrome -ErrorAction SilentlyContinue | Where-Object { `$_.CommandLine -like '*VleraDent*' } | Stop-Process -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath "$installDir" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Vlera Dent u hoq nga ky kompjuter."
"@ | Set-Content -LiteralPath $uninstallScript -Encoding UTF8

Write-Host ""
Write-Host "Vlera Dent u instalua me sukses." -ForegroundColor Green
Write-Host "Ikona u krijua ne Desktop dhe Start Menu."
Write-Host "URL: $AppUrl"
Write-Host "Po hapet aplikacioni..."
Start-Process -FilePath $browserPath -ArgumentList $shortcutArgs
Write-Host ""
Read-Host "Shtyp Enter per ta mbyllur"
