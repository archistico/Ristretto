@echo off
setlocal
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "AVALONIA_TELEMETRY_OPTOUT=1"
cd /d "%~dp0.."
echo ======================================================
echo Ristretto - prova MANUALE Inserisci in EDIT sintetico
echo Non modificare appunti o documenti da parte del laboratorio.
echo Richiede Windows desktop, Ristretto avviato e uno snippet di prova.
echo NON viene eseguito da CI o validate-native.cmd.
echo ======================================================
where powershell >nul 2>nul || (echo ERRORE: PowerShell non disponibile & exit /b 3)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0verify-release.ps1" || exit /b 3
where dotnet >nul 2>nul || (echo ERRORE: .NET 10 SDK non disponibile & exit /b 2)
dotnet run -c Release --project tools\Ristretto.WindowsOutputLab\Ristretto.WindowsOutputLab.csproj -- --picker-insert-demo
exit /b %ERRORLEVEL%
