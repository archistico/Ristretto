@echo off
setlocal
cd /d "%~dp0.."
where py >nul 2>nul
if not errorlevel 1 (
    py -3 scripts\audit_source.py || exit /b 1
    py -3 -m unittest discover -s scripts -p test_audit_source.py -v || exit /b 1
    py -3 scripts\verify_static.py || exit /b 1
    exit /b 0
)
where python >nul 2>nul || (echo ERRORE: Python 3 non disponibile & exit /b 2)
python scripts\audit_source.py || exit /b 1
python -m unittest discover -s scripts -p test_audit_source.py -v || exit /b 1
python scripts\verify_static.py || exit /b 1
exit /b 0
