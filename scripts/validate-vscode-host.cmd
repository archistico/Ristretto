@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0validate-vscode-host.ps1" %*
exit /b %ERRORLEVEL%
