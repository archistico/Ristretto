@echo off
setlocal
call "%~dp0validate-gate.cmd" source
exit /b %ERRORLEVEL%
