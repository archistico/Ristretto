@echo off
setlocal
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
cd /d "%~dp0.."
if "%~1"=="" (
    echo Uso: scripts\seed-picker-notepad.cmd TOKEN_SESSIONE
    echo Prima eseguire: scripts\validate-picker-notepad.cmd prepare --open
    exit /b 2
)
if not "%~2"=="" (echo ERRORE: previsto un solo token di sessione. & exit /b 2)
where dotnet >nul 2>nul || (echo ERRORE: SDK .NET 10 necessario. & exit /b 2)
dotnet run -c Release --project "tools\Ristretto.WindowsOutputLab\Ristretto.WindowsOutputLab.csproj" -- --seed-picker-profile "%~1"
exit /b %ERRORLEVEL%
