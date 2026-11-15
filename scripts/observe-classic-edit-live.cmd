@echo off
setlocal
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "AVALONIA_TELEMETRY_OPTOUT=1"
cd /d "%~dp0.."
echo ======================================================
echo Ristretto R5 - osservazione eventi Edit Win32
ECHO Esperimento esplicito, SOLO su documento sintetico.
echo Legge temporaneamente testo in RAM, NON salva il contenuto.
echo NON intercetta tasti e NON modifica documenti.
echo ======================================================
where dotnet >nul 2>nul || (echo ERRORE: serve .NET SDK 10. & exit /b 2)
set "SECONDS=%~1"
if "%SECONDS%"=="" set "SECONDS=15"
dotnet run -c Release --project tools\Ristretto.WindowsOutputLab\Ristretto.WindowsOutputLab.csproj -- --observe-classic-edit-live "%SECONDS%"
exit /b %ERRORLEVEL%
