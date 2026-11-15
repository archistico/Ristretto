@echo off
setlocal
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "AVALONIA_TELEMETRY_OPTOUT=1"
cd /d "%~dp0.."
echo ======================================================
echo Ristretto - gate CORE: sorgenti, build e test
for /f "usebackq delims=" %%A in ("VERSION.txt") do echo %%A
echo ======================================================
where powershell >nul 2>nul || (echo ERRORE: PowerShell richiesto per la verifica sorgenti & exit /b 3)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0verify-release.ps1" || exit /b 3
call "%~dp0validate-gate.cmd" core
exit /b %ERRORLEVEL%
