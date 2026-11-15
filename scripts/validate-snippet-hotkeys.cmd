@echo off
setlocal
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
cd /d "%~dp0.."
where dotnet >nul 2>nul || (echo ERRORE: serve .NET SDK 10. & exit /b 2)
if not exist ".local-cache\gates" mkdir ".local-cache\gates"
set "REPORT=%~1"
if "%REPORT%"=="" set "REPORT=.local-cache\gates\snippet-hotkeys-%RANDOM%-%RANDOM%.json"
echo Registrazioni temporanee Ctrl+Alt+Shift+F6..F11. Nessun input o appunti.
echo Report nuovo: %REPORT%
dotnet run -c Release --project tools\Ristretto.WindowsOutputLab\Ristretto.WindowsOutputLab.csproj -- --snippet-hotkey-test --report "%REPORT%"
exit /b %ERRORLEVEL%
