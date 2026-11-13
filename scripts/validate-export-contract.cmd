@echo off
setlocal
cd /d "%~dp0.."
echo ======================================================
echo Ristretto Alpha 20 - contratto export .NET -> VS Code
 echo ======================================================
where dotnet >nul 2>nul || (echo ERRORE: .NET 10 SDK necessario & exit /b 2)
where node >nul 2>nul || (echo ERRORE: Node.js 22 necessario & exit /b 2)
set "RISTRETTO_VSCODE_EXPORT_OUTPUT=%CD%\.local-cache\vscode-contract\export-from-dotnet.json"
if not exist ".local-cache\vscode-contract" mkdir ".local-cache\vscode-contract" || exit /b 1
if exist "%RISTRETTO_VSCODE_EXPORT_OUTPUT%" del /q "%RISTRETTO_VSCODE_EXPORT_OUTPUT%" || exit /b 1
dotnet test tests\TextExpander.Tests\TextExpander.Tests.csproj -c Release --filter "FullyQualifiedName~VscodeExportContractTests" || exit /b 1
if not exist "%RISTRETTO_VSCODE_EXPORT_OUTPUT%" (echo ERRORE: export .NET non prodotto & exit /b 1)
node tools\ristretto-vscode-extension\cli\verify-dotnet-export.js "%RISTRETTO_VSCODE_EXPORT_OUTPUT%" || exit /b 1
del /q "%RISTRETTO_VSCODE_EXPORT_OUTPUT%"
echo CONTRATTO INCROCIATO SUPERATO
