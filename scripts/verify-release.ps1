# Fail-fast anti-old-source gate for Windows release validation.
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$versionFile = Join-Path $root 'VERSION.txt'
$testsPath = Join-Path $root 'tests\TextExpander.Tests\WindowsOutputContractTests.cs'
$projectFile = Join-Path $root 'src\TextExpander.Desktop\TextExpander.Desktop.csproj'
$canonicalProps = Join-Path $root 'Directory.Build.props'

function Stop-Validation([string]$why) {
    Write-Host "ERRORE VALIDAZIONE RISTRETTO: $why" -ForegroundColor Red
    Write-Host "Controlla il messaggio precedente: non e' necessariamente un problema di estrazione o di cartelle mescolate." -ForegroundColor Yellow
    exit 3
}
if (!(Test-Path -LiteralPath $versionFile)) { Stop-Validation 'VERSION.txt non trovato: possibile vecchia versione.' }
if (!(Test-Path -LiteralPath $canonicalProps)) { Stop-Validation 'Directory.Build.props mancante.' }
[xml]$canonicalXml = Get-Content -LiteralPath $canonicalProps -Raw
$expectedVersion = [string]$canonicalXml.Project.PropertyGroup.Version
# Il suffisso di prerelease e' formato da una o piu' componenti numeriche:
# alpha.20.19, alpha.20.19.1, alpha.20.19.2 e versioni successive.
# Un vecchio controllo assumeva esattamente due componenti e bloccava Alpha 20.19.1
# PRIMA di dotnet restore/build, pur essendo valida in Directory.Build.props.
if ($expectedVersion -notmatch '^[0-9]+\.[0-9]+\.[0-9]+-alpha(?:\.[0-9]+)+$') {
    Stop-Validation "Formato versione sorgente non riconosciuto in Directory.Build.props: '$expectedVersion'. Verifica VERSION.txt e la regola del gate."
}
if (([System.IO.File]::ReadAllText($versionFile)).Trim() -ne "Ristretto $expectedVersion") { Stop-Validation 'VERSION.txt non corrisponde a Directory.Build.props.' }
if (!(Test-Path -LiteralPath $testsPath)) { Stop-Validation 'File test Windows non trovato.' }
$tests = [System.IO.File]::ReadAllText($testsPath)
if ($tests.Contains('[InlineData("\uD83D")]') -or $tests.Contains('[InlineData("\uDC4D")]')) {
    Stop-Validation 'I vecchi InlineData surrogate sono ancora presenti nel sorgente.'
}
foreach ($marker in @('void UnpairedHighSurrogateIsRejected()', 'void UnpairedLowSurrogateIsRejected()', 'void UnicodeReplacementCharacterRemainsValidText()')) {
    if (!$tests.Contains($marker)) { Stop-Validation "Manca il test surrogate: $marker" }
}
$coreCatalog = Join-Path $root 'src\TextExpander.Core\Matching\CompiledTriggerCatalog.cs'
$coreSession = Join-Path $root 'src\TextExpander.Core\Matching\CommittedInputSession.cs'
$sessionTests = Join-Path $root 'tests\TextExpander.Tests\CommittedInputSessionTests.cs'
foreach ($source in @($coreCatalog, $coreSession, $sessionTests)) {
    if (!(Test-Path -LiteralPath $source)) { Stop-Validation "F1a incompleta, manca: $source" }
}
# Regression gate for Alpha 20.1 CS0117: Application calls a Core validator.
# An 'internal' method is invisible outside Core even when using/namespace are correct.
$coreSessionText = [System.IO.File]::ReadAllText($coreSession)
if (!$coreSessionText.Contains('public static bool IsValidText(string text)')) {
    Stop-Validation 'API CommittedInputSession.IsValidText deve essere public: CS0117 cross-assembly.'
}
$activationSource = Join-Path $root 'src\TextExpander.Core\Matching\TriggerActivationSession.cs'
$activationTests = Join-Path $root 'tests\TextExpander.Tests\TriggerActivationSessionTests.cs'
$activationDoc = Join-Path $root 'docs\F1B_ACTIVATION_POLICY.md'
foreach ($source in @($activationSource, $activationTests, $activationDoc)) {
    if (!(Test-Path -LiteralPath $source)) { Stop-Validation "F1b incompleta, manca: $source" }
}
$csproj = [System.IO.File]::ReadAllText($projectFile)
foreach ($source in @(
    (Join-Path $root 'tools\Ristretto.WindowsOutputLab\Ristretto.WindowsOutputLab.csproj'),
    (Join-Path $root 'tools\Ristretto.WindowsOutputLab\Program.cs'),
    (Join-Path $root 'src\TextExpander.Infrastructure\Platform\Windows\WindowsExplicitTestTargetGate.cs'),
    (Join-Path $root 'tests\TextExpander.Tests\WindowsExplicitTestTargetGateTests.cs'),
    (Join-Path $root 'docs\F2A_WINDOWS_CONTROLLED_LAB.md')
)) {
    if (!(Test-Path -LiteralPath $source)) { Stop-Validation "F2a incompleta, manca: $source" }
}
if ($csproj.Contains('<Version>')) { Stop-Validation 'Versione desktop duplicata: deve ereditare Directory.Build.props.' }
foreach ($source in @(
    (Join-Path $root 'tools\Ristretto.WindowsOutputLab\OwnedEditTarget.cs'),
    (Join-Path $root 'tools\Ristretto.WindowsOutputLab\OwnedEditRecognitionSession.cs'),
    (Join-Path $root 'tools\Ristretto.WindowsOutputLab\NativeReplacementLab.cs'),
    (Join-Path $root 'docs\F2B_WINDOWS_OWNED_EDIT.md')
)) {
    if (!(Test-Path -LiteralPath $source)) { Stop-Validation "F2b Windows incompleta, manca: $source" }
}
foreach ($source in @(
    (Join-Path $root 'tools\Ristretto.UiReview\Ristretto.UiReview.csproj'),
    (Join-Path $root 'tools\Ristretto.UiReview\Program.cs'),
    (Join-Path $root 'docs\GUI.md')
)) {
    if (!(Test-Path -LiteralPath $source)) { Stop-Validation "Preview 9 incompleta, manca: $source" }
}
foreach ($source in @(
    (Join-Path $root 'src\TextExpander.Application\Features\Output\TriggerReplacementPlan.cs'),
    (Join-Path $root 'src\TextExpander.Application\Features\Output\TriggerReplacementExecutor.cs'),
    (Join-Path $root 'src\TextExpander.Infrastructure\Simulation\SimulatedReplacementTarget.cs'),
    (Join-Path $root 'tests\TextExpander.Tests\TriggerReplacementTests.cs'),
    (Join-Path $root 'tools\Ristretto.ReplacementLab\Ristretto.ReplacementLab.csproj'),
    (Join-Path $root 'tools\Ristretto.ReplacementLab\Program.cs'),
    (Join-Path $root 'docs\F2B_CONDITIONAL_REPLACEMENT.md')
)) {
    if (!(Test-Path -LiteralPath $source)) { Stop-Validation "F2b simulata incompleta, manca: $source" }
}
foreach ($source in @(
    (Join-Path $root 'tools\Ristretto.CoreBenchmark\Ristretto.CoreBenchmark.csproj'),
    (Join-Path $root 'tools\Ristretto.CoreBenchmark\Program.cs'),
    (Join-Path $root 'docs\CORE_BENCHMARK.md')
)) {
    if (!(Test-Path -LiteralPath $source)) { Stop-Validation "Benchmark Core incompleto, manca: $source" }
}
if (!([System.IO.File]::ReadAllText($coreCatalog)).Contains('GetAlternateLookup<ReadOnlySpan<char>>()')) {
    Stop-Validation 'Ottimizzazione allocazioni matcher assente.'
}
$activationContents = [System.IO.File]::ReadAllText($activationSource)
if (!$activationContents.Contains('HasEligiblePrefix(_suffix, exactLength)')) { Stop-Validation 'Hotfix F1b Immediate mancante.' }
$activationTestContents = [System.IO.File]::ReadAllText($activationTests)
foreach ($test in @('ImmediateAnywhereModeCanMatchInsideWordWhenExplicitlySelected', 'ImmediateCompletedTriggerIsNotMaskedByUnrelatedShorterPrefix', 'ImmediateEqualLengthPrefixOfLongerTriggerStillDefers')) {
    if (!$activationTestContents.Contains($test)) { Stop-Validation "Test regressione hotfix assente: $test" }
}
foreach ($source in @(
    (Join-Path $root 'src\TextExpander.Application\Features\Output\ExpansionAdmission.cs'),
    (Join-Path $root 'tests\TextExpander.Tests\ExpansionAdmissionTests.cs'),
    (Join-Path $root 'docs\F3_ADMISSION_GATE.md'),
    (Join-Path $root 'scripts\validate-native.cmd')
)) {
    if (!(Test-Path -LiteralPath $source)) { Stop-Validation "Alpha 8 incompleta, manca: $source" }
}
foreach ($source in @(
    (Join-Path $root 'src\TextExpander.Application\Features\Output\CooperatingLabProtocol.cs'),
    (Join-Path $root 'tools\Ristretto.WindowsOutputLab\CooperatingProcessLab.cs'),
    (Join-Path $root 'tests\TextExpander.Tests\CooperatingLabProtocolTests.cs'),
    (Join-Path $root 'docs\ALPHA9_COOPERATING_PROCESS.md')
)) {
    if (!(Test-Path -LiteralPath $source)) { Stop-Validation "Alpha 9 incompleta, manca: $source" }
}
$labProgram = [System.IO.File]::ReadAllText((Join-Path $root 'tools\Ristretto.WindowsOutputLab\Program.cs'))
foreach ($mode in @('--cooperating-demo', '--cooperating-child', '--cooperating-smoke', '--cooperating-child-smoke')) {
    if (!$labProgram.Contains($mode)) { Stop-Validation "Modalità laboratorio Alpha 9 assente: $mode" }
}
$nativeScript = [System.IO.File]::ReadAllText((Join-Path $root 'scripts\validate-native.cmd'))
if (!$nativeScript.Contains('--cooperating-smoke')) { Stop-Validation 'Smoke a due processi non collegato a validate-native.cmd' }
foreach ($source in @(
    (Join-Path $root 'src\TextExpander.Infrastructure\Platform\Windows\WindowsExternalEditorProbe.cs'),
    (Join-Path $root 'tools\Ristretto.WindowsOutputLab\ExternalEditorProbeCommand.cs'),
    (Join-Path $root 'tests\TextExpander.Tests\WindowsExternalEditorProbeTests.cs'),
    (Join-Path $root 'docs\ALPHA10_EXTERNAL_EDITOR_PROBE.md')
)) {
    if (!(Test-Path -LiteralPath $source)) { Stop-Validation "Alpha 10 incompleta, manca: $source" }
}
if (!$labProgram.Contains('--probe-external')) { Stop-Validation 'Modalità probe esterno Alpha 10 assente.' }
foreach ($source in @(
    (Join-Path $root 'src\TextExpander.Infrastructure\Platform\Windows\WindowsClassicEditSelectionProbe.cs'),
    (Join-Path $root 'tools\Ristretto.WindowsOutputLab\ExternalClassicEditSelectionCommand.cs'),
    (Join-Path $root 'tests\TextExpander.Tests\WindowsClassicEditSelectionProbeTests.cs'),
    (Join-Path $root 'docs\ALPHA11_CLASSIC_EDIT_SELECTION.md')
)) {
    if (!(Test-Path -LiteralPath $source)) { Stop-Validation "Alpha 11 incompleta, manca: $source" }
}
if (!$labProgram.Contains('--inspect-classic-edit')) { Stop-Validation 'Modalita selezione read-only Alpha 11 assente.' }
foreach ($source in @(
    (Join-Path $root 'src\TextExpander.Application\Features\Hotkeys\PickerHotkey.cs'),
    (Join-Path $root 'src\TextExpander.Infrastructure\Platform\Windows\WindowsPickerHotkeyService.cs'),
    (Join-Path $root 'tests\TextExpander.Tests\PickerHotkeyTests.cs'),
    (Join-Path $root 'docs\ALPHA12_PICKER_HOTKEY.md')
)) {
    if (!(Test-Path -LiteralPath $source)) { Stop-Validation "Alpha 12 hotkey incompleta, manca: $source" }
}
$desktopApp = [System.IO.File]::ReadAllText((Join-Path $root 'src\TextExpander.Desktop\App.axaml.cs'))
if (!$desktopApp.Contains('ConfigurePickerHotkeyAsync')) { Stop-Validation 'Hotkey non collegata al picker.' }
foreach ($source in @(
    (Join-Path $root 'src\TextExpander.Application\Features\Output\CommittedEditDeltaVerifier.cs'),
    (Join-Path $root 'tests\TextExpander.Tests\CommittedEditDeltaVerifierTests.cs'),
    (Join-Path $root 'docs\ALPHA13_COMMITTED_INPUT.md')
)) {
    if (!(Test-Path -LiteralPath $source)) { Stop-Validation "Alpha 13 incomplete, manca: $source" }
}
$ownedBridge = [System.IO.File]::ReadAllText((Join-Path $root 'tools\Ristretto.WindowsOutputLab\OwnedEditRecognitionSession.cs'))
if (!$ownedBridge.Contains('CommittedEditDeltaVerifier.Verify(')) {
    Stop-Validation 'Alpha 13 non collegata al bridge del laboratorio.'
}
foreach ($source in @(
    (Join-Path $root 'src\TextExpander.Application\Features\Output\CommittedEditMutationProvenance.cs'),
    (Join-Path $root 'tests\TextExpander.Tests\OwnedEditProvenanceTests.cs'),
    (Join-Path $root 'docs\ALPHA14_MUTATION_PROVENANCE.md')
)) {
    if (!(Test-Path -LiteralPath $source)) { Stop-Validation "Alpha 14 incompleta, manca: $source" }
}
if (!$ownedBridge.Contains('OwnedEditProvenancePolicy.Evaluate(')) {
    Stop-Validation 'Alpha 14 non collegata alla sessione del laboratorio.'
}
$ownedNative = [System.IO.File]::ReadAllText((Join-Path $root 'tools\Ristretto.WindowsOutputLab\OwnedEditTarget.cs'))
if (!$ownedNative.Contains('OwnedEditMutationEvidence') -or !$ownedNative.Contains('WmChar')) {
    Stop-Validation 'Alpha 14 manca la correlazione dei messaggi dell EDIT.'
}
$nativeLab = [System.IO.File]::ReadAllText((Join-Path $root 'tools\Ristretto.WindowsOutputLab\NativeReplacementLab.cs'))
if (!$nativeLab.Contains('target.WaitForMessage()') -or !$nativeLab.Contains('target.PumpPendingMessages(AfterOwnTurn)')) {
    Stop-Validation 'Alpha 16: ciclo per messaggio e attesa eventi mancanti.'
}
if (!$ownedNative.Contains('WaitMessage()') -or !$ownedNative.Contains('PostOwnedCharacterSequenceFixture')) {
    Stop-Validation 'Alpha 16: pump evento e fixture Unicode mancanti.'
}
foreach ($source in @(
    (Join-Path $root 'docs\ALPHA15_EDIT_CONTINUITY.md'),
    (Join-Path $root 'docs\ALPHA16_EVENT_PUMP.md')
)) {
    if (!(Test-Path -LiteralPath $source)) { Stop-Validation "Documentazione milestone mancante: $source" }
}
foreach ($source in @(
    (Join-Path $root 'src\TextExpander.Infrastructure\Platform\Windows\WindowsExternalAdapterCapability.cs'),
    (Join-Path $root 'tools\Ristretto.WindowsOutputLab\ExternalAdapterCapabilityCommand.cs'),
    (Join-Path $root 'tests\TextExpander.Tests\WindowsExternalAdapterCapabilityTests.cs'),
    (Join-Path $root 'docs\ALPHA17_EXTERNAL_ADAPTER_CAPABILITIES.md')
)) {
    if (!(Test-Path -LiteralPath $source)) { Stop-Validation "Alpha 17 incompleta, manca: $source" }
}
$alpha17Policy = [System.IO.File]::ReadAllText((Join-Path $root 'src\TextExpander.Infrastructure\Platform\Windows\WindowsExternalAdapterCapability.cs'))
if (!$alpha17Policy.Contains('ReplacementCapability.Unsupported') -or !$alpha17Policy.Contains('EveryWriteProof')) {
    Stop-Validation 'Alpha 17: contratto fail-closed non trovato.'
}
if (!$labProgram.Contains('--assess-external-adapter')) {
    Stop-Validation 'Alpha 17: diagnostica read-only non collegata al laboratorio.'
}
foreach ($source in @(
    (Join-Path $root 'src\TextExpander.Application\Features\Output\RemoteOwnedEditProtocol.cs'),
    (Join-Path $root 'tools\Ristretto.WindowsOutputLab\RemoteOwnedEditLab.cs'),
    (Join-Path $root 'tests\TextExpander.Tests\RemoteOwnedEditProtocolTests.cs'),
    (Join-Path $root 'docs\ALPHA18_REMOTE_OWNED_EDIT.md')
)) {
    if (!(Test-Path -LiteralPath $source)) { Stop-Validation "Alpha 18 incompleta, manca: $source" }
}
if (!$labProgram.Contains('--remote-owned-child') -or !$labProgram.Contains('--remote-owned-smoke')) {
    Stop-Validation 'Alpha 18: bridge di laboratorio non collegato.'
}
if (!$nativeScript.Contains('--remote-owned-smoke')) {
    Stop-Validation 'Alpha 18: smoke tra processi non incluso nella validazione nativa.'
}
$remoteProtocol = [System.IO.File]::ReadAllText((Join-Path $root 'src\TextExpander.Application\Features\Output\RemoteOwnedEditProtocol.cs'))
if (!$remoteProtocol.Contains('RemoteOwnedEditOneShotGate') -or !$remoteProtocol.Contains('Interlocked.Exchange')) {
    Stop-Validation 'Alpha 18: protezione anti-replay assente.'
}
foreach ($rel in @(
    'tools\ristretto-vscode-extension\package.json',
    'tools\ristretto-vscode-extension\src\extension.js',
    'tools\ristretto-vscode-extension\src\controller.js',
    'tools\ristretto-vscode-extension\src\planner.js',
    'tools\ristretto-vscode-extension\fixtures\synthetic-export.json',
    'tools\ristretto-vscode-extension\test\planner.test.js',
    'tools\ristretto-vscode-extension\test\controller.test.js',
    'docs\ALPHA19_VSCODE_ADAPTER.md',
    'scripts\validate-vscode.cmd'
)) {
    if (!(Test-Path (Join-Path $root $rel))) { Stop-Validation "Alpha 19 incompleta, manca: $rel" }
}
$extension = Get-Content -LiteralPath (Join-Path $root 'tools\ristretto-vscode-extension\package.json') -Raw | ConvertFrom-Json
if ($extension.contributes.configuration.properties.'ristrettoVscode.enabled'.default -ne $false) {
    Stop-Validation 'Alpha 19: adapter VS Code deve essere disabilitato per default.'
}
foreach ($rel in @(
    'tests\TextExpander.Tests\VscodeExportContractTests.cs',
    'tools\ristretto-vscode-extension\cli\verify-dotnet-export.js',
    'scripts\validate-export-contract.cmd',
    'scripts\validate-export-contract.sh',
    'docs\ALPHA20_VSCODE_CONTRACT.md'
)) {
    if (!(Test-Path (Join-Path $root $rel))) { Stop-Validation "Alpha 20 incompleta, manca: $rel" }
}
$controller20 = [System.IO.File]::ReadAllText((Join-Path $root 'tools\ristretto-vscode-extension\src\controller.js'))
if (!$controller20.Contains('captureTarget(vscode)') -or !$controller20.Contains('isCurrentTarget(vscode, target)')) {
    Stop-Validation 'Alpha 20: congelamento destinatario non trovato.'
}
# Include ALL explicitly-versioned projects, not only the Desktop application.
# Alpha 20.3 picker insert: explicit command only, target guard and test contract.
$pickerXaml = [System.IO.File]::ReadAllText((Join-Path $root 'src\TextExpander.Desktop\PickerWindow.axaml'))
$pickerCode = [System.IO.File]::ReadAllText((Join-Path $root 'src\TextExpander.Desktop\PickerWindow.axaml.cs'))
$appCode = [System.IO.File]::ReadAllText((Join-Path $root 'src\TextExpander.Desktop\App.axaml.cs'))
if (!$pickerXaml.Contains('Click="Insert_Click"') -or
    !$pickerCode.Contains('vm.InsertSelectedAsync(app.InsertFromPickerAsync)') -or
    !$pickerCode.Contains('vm.InsertFirstAsync(app.InsertFromPickerAsync)') -or
    !$pickerCode.Contains('vm.CopySelectedAsync(') -or
    !$pickerXaml.Contains('IsEnabled="{Binding CanCopy}"') -or
    !$appCode.Contains('CaptureExternalForeground()') -or
    !$appCode.Contains('request.ExecuteAsync(') -or
    !$appCode.Contains('CancelPickerInsertion()') -or
    !$pickerCode.Contains('CancelPickerInsertion()') -or
    !(Test-Path (Join-Path $root 'src\TextExpander.Infrastructure\Platform\Windows\WindowsPickerInsertRequest.cs')) -or
    !(Test-Path (Join-Path $root 'src\TextExpander.Infrastructure\Platform\Windows\WindowsPickerPasteService.cs')) -or
    !(Test-Path (Join-Path $root 'tests\TextExpander.Tests\WindowsPickerPasteTests.cs'))) {
    Stop-Validation 'Alpha 20.3: comando Inserisci o test del picker mancanti.'
}
foreach ($project in @(Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.csproj' -Recurse) +
                    @(Get-ChildItem -LiteralPath (Join-Path $root 'tests') -Filter '*.csproj' -Recurse) +
                    @(Get-ChildItem -LiteralPath (Join-Path $root 'tools') -Filter '*.csproj' -Recurse)) {
    [xml]$projectXml = Get-Content -LiteralPath $project.FullName -Raw
    $declaredVersion = $projectXml.Project.PropertyGroup.Version | Where-Object { $_ }
    if (@($declaredVersion).Count -gt 0) {
        Stop-Validation "Versione duplicata in $($project.FullName): usare Directory.Build.props."
    }
}

# Alpha 20.8: the desktop picker must preserve its original external target
# on a repeated hotkey, and invalidate stale async completions after reopen.
$pickerSessionSource = Join-Path $root 'src\TextExpander.Desktop\PickerSessionController.cs'
$pickerSessionTests = Join-Path $root 'tests\TextExpander.Tests\PickerSessionControllerTests.cs'
if (!(Test-Path -LiteralPath $pickerSessionSource) -or !(Test-Path -LiteralPath $pickerSessionTests)) {
    Stop-Validation 'Alpha 20.8: controller o regressioni picker mancanti.'
}
$pickerAppSource = [System.IO.File]::ReadAllText((Join-Path $root 'src\TextExpander.Desktop\App.axaml.cs'))
if (!$pickerAppSource.Contains('_pickerSession.Open(') -or !$pickerAppSource.Contains('_pickerSession.IsCurrent(')) {
    Stop-Validation 'Alpha 20.8: App non usa il controllo delle sessioni picker.'
}

Write-Host "SORGENTI RISTRETTO $expectedVersion VERIFICATI: $root" -ForegroundColor Green
exit 0
