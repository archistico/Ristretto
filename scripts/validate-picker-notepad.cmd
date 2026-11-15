@echo off
setlocal
cd /d "%~dp0.."
echo ======================================================
echo Ristretto - prova Notepad con documento sintetico isolato
echo Non legge tasti e non modifica documenti personali.
echo ======================================================
where dotnet >nul 2>nul || (echo ERRORE: SDK .NET 10 richiesto. & exit /b 2)
dotnet run -c Release --project "tools\Ristretto.DevTools\Ristretto.DevTools.csproj" -- notepad %*
exit /b %ERRORLEVEL%
