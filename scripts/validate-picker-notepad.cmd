@echo off
setlocal
cd /d "%~dp0.."
echo ======================================================
echo Ristretto R2.05 - Notepad con file sintetico isolato
 echo NON esegue SendInput, non apre documenti personali.
echo ======================================================
where python >nul 2>nul || (echo ERRORE: Python 3.10+ richiesto & exit /b 2)
python "%~dp0qualify_notepad.py" %*
exit /b %ERRORLEVEL%
