@echo off
setlocal
set "DOTNET_CLI_TELEMETRY_OPTOUT=1"
set "AVALONIA_TELEMETRY_OPTOUT=1"
cd /d "%~dp0.."
echo Ristretto - verifica picker senza desktop interattivo
where dotnet >nul 2>nul || (echo ERRORE: SDK .NET 10 non disponibile & exit /b 2)
dotnet run -c Release --project tools\Ristretto.DevTools\Ristretto.DevTools.csproj -- audit
if errorlevel 1 exit /b 1
dotnet test tests\TextExpander.Tests\TextExpander.Tests.csproj -c Release --filter "FullyQualifiedName~PickerEnterGestureTests|FullyQualifiedName~PickerSessionControllerTests|FullyQualifiedName~WindowsPickerPasteTests|FullyQualifiedName~PickerClipboardReadbackTests|FullyQualifiedName~PickerViewModelTests|FullyQualifiedName~PickerOwnedEditReceiptTests|FullyQualifiedName~PickerNotepadProfileSeederTests"
exit /b %ERRORLEVEL%
