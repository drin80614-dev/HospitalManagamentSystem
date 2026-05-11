@echo off
setlocal
title Vlera Dent Installer
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-VleraDent.ps1"
endlocal
