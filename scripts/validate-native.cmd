@echo off
setlocal
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "AVALONIA_TELEMETRY_OPTOUT=1"
cd /d "%~dp0.."
echo ======================================================
echo Ristretto - test nativi EDIT posseduto e registrazioni hotkey
echo Eseguire solo in sessione Windows desktop interattiva.
echo ======================================================
where dotnet >nul 2>nul || (echo ERRORE: .NET 10 SDK non disponibile & exit /b 2)
dotnet run -c Release --project tools\Ristretto.DevTools\Ristretto.DevTools.csproj -- audit
if errorlevel 1 exit /b 1
dotnet run -c Release --project tools\Ristretto.WindowsOutputLab\Ristretto.WindowsOutputLab.csproj -- --replacement-test
if errorlevel 1 exit /b 1
echo Test nativo automatico in processo figlio cooperante (senza input umano)...
dotnet run -c Release --no-build --project tools\Ristretto.WindowsOutputLab\Ristretto.WindowsOutputLab.csproj -- --cooperating-smoke
if errorlevel 1 exit /b 1
echo Test Alpha 18: richiesta esplicita verso un editor EDIT posseduto dal processo figlio...
dotnet run -c Release --no-build --project tools\Ristretto.WindowsOutputLab\Ristretto.WindowsOutputLab.csproj -- --remote-owned-smoke
if errorlevel 1 exit /b 1
echo Registrazioni temporanee Ctrl+Alt+Shift+F6..F11, senza input o appunti...
dotnet run -c Release --no-build --project tools\Ristretto.WindowsOutputLab\Ristretto.WindowsOutputLab.csproj -- --snippet-hotkey-test
exit /b %ERRORLEVEL%
