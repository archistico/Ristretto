@echo off
setlocal
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "AVALONIA_TELEMETRY_OPTOUT=1"
cd /d "%~dp0.."
set "MODE=%~1"
if "%MODE%"=="" set "MODE=core"
where dotnet >nul 2>nul || (echo ERRORE: installa .NET SDK 10. Python non e' richiesto. & exit /b 2)
dotnet run -c Release --project "tools\Ristretto.DevTools\Ristretto.DevTools.csproj" -- gate "%MODE%"
exit /b %ERRORLEVEL%
