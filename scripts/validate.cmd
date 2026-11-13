@echo off
setlocal
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "AVALONIA_TELEMETRY_OPTOUT=1"
cd /d "%~dp0.."
echo ======================================================
echo Ristretto v3.9-alpha.20.5 - sorgenti: %CD%
echo ======================================================
where powershell >nul 2>nul || (echo ERRORE: PowerShell richiesto per verificare la versione dei sorgenti & exit /b 3)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0verify-release.ps1" || exit /b 3
where py >nul 2>nul
if not errorlevel 1 (
    call "%~dp0validate-source.cmd" || exit /b 1
) else (
    where python >nul 2>nul
    if not errorlevel 1 (
        call "%~dp0validate-source.cmd" || exit /b 1
    ) else (
        echo AVVISO: Python 3 non disponibile - audit statico facoltativo non eseguito.
    )
)
where dotnet >nul 2>nul || (echo ERRORE: installare .NET 10 SDK & exit /b 2)
dotnet restore Ristretto.sln || exit /b 1
dotnet build Ristretto.sln -c Release --no-restore || exit /b 1
dotnet test Ristretto.sln -c Release --no-build --logger "console;verbosity=normal" || exit /b 1
dotnet restore tools\Ristretto.WindowsOutputLab\Ristretto.WindowsOutputLab.csproj || exit /b 1
dotnet build tools\Ristretto.WindowsOutputLab\Ristretto.WindowsOutputLab.csproj -c Release --no-restore || exit /b 1
echo Laboratorio Windows F2a/F2b compilato ma NON eseguito (modalita' di prova esplicite).
dotnet restore tools\Ristretto.CoreBenchmark\Ristretto.CoreBenchmark.csproj || exit /b 1
dotnet build tools\Ristretto.CoreBenchmark\Ristretto.CoreBenchmark.csproj -c Release --no-restore || exit /b 1
echo Benchmark Core compilato ma NON eseguito (richiede --output esplicito).
dotnet restore tools\Ristretto.ReplacementLab\Ristretto.ReplacementLab.csproj || exit /b 1
dotnet build tools\Ristretto.ReplacementLab\Ristretto.ReplacementLab.csproj -c Release --no-restore || exit /b 1
echo Laboratorio F2b simulato compilato ma NON eseguito (richiede --self-test esplicito).
dotnet restore tools\Ristretto.UiReview\Ristretto.UiReview.csproj || exit /b 1
dotnet build tools\Ristretto.UiReview\Ristretto.UiReview.csproj -c Release --no-restore || exit /b 1
echo Revisione UI compilata ma NON eseguita (richiede --output esplicito).

echo Verifica adapter VS Code Alpha 20 (facoltativa se Node non disponibile)...
where node >nul 2>nul
if errorlevel 1 (
    echo AVVISO: Node.js non disponibile - test VS Code NON ESEGUITI. Eseguire scripts\validate-vscode.cmd separatamente.
) else (
    where npm >nul 2>nul
    if errorlevel 1 (
        echo AVVISO: npm non disponibile - test VS Code NON ESEGUITI. Eseguire scripts\validate-vscode.cmd separatamente.
    ) else (
        call "%~dp0validate-vscode.cmd" || exit /b 1
        echo Verifica contratto incrociato con export JSON reale...
        call "%~dp0validate-export-contract.cmd" || exit /b 1
    )
)

echo VALIDAZIONE BUILD E TEST COMPLETATA. Eseguire validate-native.cmd separatamente su Windows interattivo.
exit /b 0
