@echo off
setlocal
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "AVALONIA_TELEMETRY_OPTOUT=1"
cd /d "%~dp0.."
echo ======================================================
echo Ristretto R5 - prova esplicita Win32 Edit esterno
echo Legge temporaneamente contenuto di prova in RAM.
echo Non registra tasti o testo e non modifica applicazioni.
echo ======================================================
where dotnet >nul 2>nul || (echo ERRORE: serve .NET SDK 10. & exit /b 2)
dotnet run -c Release --project tools\Ristretto.WindowsOutputLab\Ristretto.WindowsOutputLab.csproj -- --observe-classic-edit-text
exit /b %ERRORLEVEL%
