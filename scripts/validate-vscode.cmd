@echo off
setlocal
cd /d "%~dp0.."
echo ======================================================
echo Ristretto Alpha 20 - adapter VS Code (test Node)
echo ======================================================
where node >nul 2>nul || (echo ERRORE: Node.js 22 o successivo richiesto & exit /b 2)
where npm >nul 2>nul || (echo ERRORE: npm non disponibile & exit /b 2)
pushd tools\ristretto-vscode-extension
call npm run check
set "RESULT=%ERRORLEVEL%"
popd
exit /b %RESULT%
