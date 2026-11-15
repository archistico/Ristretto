@echo off
setlocal
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "AVALONIA_TELEMETRY_OPTOUT=1"
cd /d "%~dp0.."
echo ======================================================
echo Ristretto R5 - osservazione temporanea di focus Windows
echo NON legge tasti o testo. NON modifica editor o clipboard.
echo ======================================================
where dotnet >nul 2>nul || (echo ERRORE: installare .NET SDK 10 per questa prova. & exit /b 2)
set "SECONDS=%~1"
if "%SECONDS%"=="" set "SECONDS=15"
dotnet run -c Release --project tools\Ristretto.WindowsOutputLab\Ristretto.WindowsOutputLab.csproj -- --observe-focus "%SECONDS%"
exit /b %ERRORLEVEL%
