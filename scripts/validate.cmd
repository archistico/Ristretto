@echo off
setlocal
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "AVALONIA_TELEMETRY_OPTOUT=1"
cd /d "%~dp0.."
echo ======================================================
echo Ristretto - gate CORE: audit C#, build e test
for /f "usebackq delims=" %%A in ("VERSION.txt") do echo %%A
echo ======================================================
call "%~dp0validate-gate.cmd" core
exit /b %ERRORLEVEL%
