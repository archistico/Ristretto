@echo off
setlocal
REM Ristretto: elimina esclusivamente le cartelle bin/obj dei progetti .NET.
REM Uso: clean-build.cmd -Preview  oppure  clean-build.cmd [-Force]
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0clean-build.ps1" %*
exit /b %ERRORLEVEL%
