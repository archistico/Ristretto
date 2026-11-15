@echo off
setlocal EnableDelayedExpansion
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "AVALONIA_TELEMETRY_OPTOUT=1"
cd /d "%~dp0.."
set "RISTRETTO_GATE_MODE=%~1"
if "%RISTRETTO_GATE_MODE%"=="" set "RISTRETTO_GATE_MODE=core"
if defined RISTRETTO_PYTHON (
  "%RISTRETTO_PYTHON%" --version >nul 2>nul
  if errorlevel 1 (echo ERRORE: RISTRETTO_PYTHON non eseguibile & exit /b 2)
  "%RISTRETTO_PYTHON%" "%~dp0quality_gate.py" --mode "%RISTRETTO_GATE_MODE%"
  exit /b !ERRORLEVEL!
)
where py >nul 2>nul
if not errorlevel 1 (
  py -3 --version >nul 2>nul
  if not errorlevel 1 (
    py -3 "%~dp0quality_gate.py" --mode "%RISTRETTO_GATE_MODE%"
    exit /b !ERRORLEVEL!
  )
)
where python >nul 2>nul
if not errorlevel 1 (
  python --version >nul 2>nul
  if not errorlevel 1 (
    python "%~dp0quality_gate.py" --mode "%RISTRETTO_GATE_MODE%"
    exit /b !ERRORLEVEL!
  )
)
where python3 >nul 2>nul
if not errorlevel 1 (
  python3 --version >nul 2>nul
  if not errorlevel 1 (
    python3 "%~dp0quality_gate.py" --mode "%RISTRETTO_GATE_MODE%"
    exit /b !ERRORLEVEL!
  )
)
echo ERRORE: Python 3.10+ necessario per il gate. Nessuna verifica dichiarata passata.
exit /b 2
