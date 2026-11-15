@echo off
setlocal
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "AVALONIA_TELEMETRY_OPTOUT=1"
cd /d "%~dp0.."
echo Ristretto - verifica picker senza desktop interattivo
where powershell >nul 2>nul || (echo ERRORE: PowerShell non disponibile & exit /b 3)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0verify-release.ps1" || exit /b 3
where dotnet >nul 2>nul || (echo ERRORE: SDK .NET 10 non disponibile & exit /b 2)
dotnet test tests\TextExpander.Tests\TextExpander.Tests.csproj -c Release --filter "FullyQualifiedName~PickerEnterGestureTests|FullyQualifiedName~PickerSessionControllerTests|FullyQualifiedName~WindowsPickerPasteTests|FullyQualifiedName~PickerClipboardReadbackTests|FullyQualifiedName~PickerViewModelTests|FullyQualifiedName~PickerOwnedEditReceiptTests|FullyQualifiedName~PickerNotepadProfileSeederTests"
exit /b %ERRORLEVEL%
