@echo off
setlocal
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "AVALONIA_TELEMETRY_OPTOUT=1"
cd /d "%~dp0.."
echo ======================================================
echo Ristretto - test nativi EDIT posseduto
echo Eseguire solo in sessione Windows desktop interattiva.
echo ======================================================
where powershell >nul 2>nul || (echo ERRORE: PowerShell non disponibile & exit /b 3)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0verify-release.ps1" || exit /b 3
where dotnet >nul 2>nul || (echo ERRORE: .NET 10 SDK non disponibile & exit /b 2)
dotnet run -c Release --project tools\Ristretto.WindowsOutputLab\Ristretto.WindowsOutputLab.csproj -- --replacement-test
if errorlevel 1 exit /b 1
echo Test nativo automatico in processo figlio cooperante (senza input umano)...
dotnet run -c Release --no-build --project tools\Ristretto.WindowsOutputLab\Ristretto.WindowsOutputLab.csproj -- --cooperating-smoke
if errorlevel 1 exit /b 1
echo Test Alpha 18: richiesta esplicita verso un editor EDIT posseduto dal processo figlio...
dotnet run -c Release --no-build --project tools\Ristretto.WindowsOutputLab\Ristretto.WindowsOutputLab.csproj -- --remote-owned-smoke
exit /b %ERRORLEVEL%
