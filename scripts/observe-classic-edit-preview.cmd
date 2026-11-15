@echo off
setlocal
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "AVALONIA_TELEMETRY_OPTOUT=1"
cd /d "%~dp0.."
echo ======================================================
echo Ristretto R5 - ANTEPRIMA trigger in Edit Win32
ECHO SOLO campo sintetico. Nessuna sostituzione e nessun log del testo.
echo Lettura TEMPORANEA in RAM del testo del controllo esterno.
echo ======================================================
where dotnet >nul 2>nul || (echo ERRORE: serve .NET SDK 10. & exit /b 2)
set "SECONDS=%~1"
if "%SECONDS%"=="" set "SECONDS=15"
dotnet run -c Release --project tools\Ristretto.WindowsOutputLab\Ristretto.WindowsOutputLab.csproj -- --observe-classic-edit-preview "%SECONDS%"
exit /b %ERRORLEVEL%
