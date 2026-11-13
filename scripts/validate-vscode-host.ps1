param(
    [Parameter(Mandatory = $true)][string]$CodeExecutable,
    [string]$OutputDirectory = '',
    [ValidateRange(30, 600)][int]$TimeoutSeconds = 120,
    [switch]$PrepareOnly
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$code = (Resolve-Path -LiteralPath $CodeExecutable).Path
if ([IO.Path]::GetExtension($code) -ne '.exe') { throw 'Specify the installed VS Code executable (Code.exe), not code.cmd.' }
if (!$OutputDirectory) {
    $OutputDirectory = Join-Path $root ('.local-cache\vscode-host-' + [Guid]::NewGuid().ToString('N'))
}
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw 'OutputDirectory must be a new directory; previous evidence is never overwritten.' }
$workspace = Join-Path $output 'workspace'
$portable = Join-Path $output 'portable'
$settingsDirectory = Join-Path $portable 'user-data\User'
New-Item -ItemType Directory -Path $workspace, $settingsDirectory, (Join-Path $portable 'extensions') -Force | Out-Null
$utf8 = New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllText((Join-Path $settingsDirectory 'settings.json'), @'
{
  "telemetry.telemetryLevel": "off",
  "update.mode": "none",
  "extensions.autoCheckUpdates": false,
  "extensions.autoUpdate": false,
  "workbench.startupEditor": "none",
  "window.restoreWindows": "none",
  "files.hotExit": "off"
}
'@, $utf8)
$report = Join-Path $output 'host-results.json'
$oldReport = $env:RISTRETTO_VSCODE_HOST_REPORT
$oldWorkspace = $env:RISTRETTO_VSCODE_HOST_WORKSPACE
$oldElectron = $env:ELECTRON_RUN_AS_NODE
$oldPortable = $env:VSCODE_PORTABLE
$process = $null
$launchStatus = 'LAUNCH_ERROR'
$hostExitCode = $null
try {
    $env:RISTRETTO_VSCODE_HOST_REPORT = $report
    $env:RISTRETTO_VSCODE_HOST_WORKSPACE = $workspace
    $env:ELECTRON_RUN_AS_NODE = $null
    # Portable installations ignore --user-data-dir/--extensions-dir. Override
    # their portable root too, so no existing profile is reused.
    $env:VSCODE_PORTABLE = $portable
    $arguments = @(
        ('"' + $workspace + '"'),
        ('--user-data-dir="' + (Join-Path $portable 'user-data') + '"'),
        ('--extensions-dir="' + (Join-Path $portable 'extensions') + '"'),
        ('--extensionDevelopmentPath="' + (Join-Path $root 'tools\ristretto-vscode-extension') + '"'),
        ('--extensionTestsPath="' + (Join-Path $root 'tools\ristretto-vscode-extension\test\host\index.js') + '"'),
        '--disable-extensions', '--disable-workspace-trust', '--disable-telemetry',
        '--disable-updates',
        '--skip-welcome', '--skip-release-notes', '--new-window'
    )
    if ($PrepareOnly) {
        [IO.File]::WriteAllText((Join-Path $output 'launch-arguments.json'),
            (ConvertTo-Json -InputObject $arguments) + [Environment]::NewLine, $utf8)
        $launchStatus = 'PREPARED_NOT_RUN'
        Write-Host "Profile and arguments prepared without starting VS Code: $output"
        return
    }
    $process = Start-Process -FilePath $code -ArgumentList $arguments -PassThru -WindowStyle Hidden `
        -RedirectStandardOutput (Join-Path $output 'stdout.log') -RedirectStandardError (Join-Path $output 'stderr.log')
    # Keep an open handle before the short-lived process exits, so Windows
    # PowerShell 5.1 can still retrieve its exit code after waiting.
    $null = $process.Handle
    if (!$process.WaitForExit($TimeoutSeconds * 1000)) {
        # Terminate exactly the instance we started. Never select user editors
        # by executable name, and never retry an interrupted host run.
        $launchStatus = 'TIMEOUT_NOT_VERIFIED'
        $process.Kill()
        throw 'VS Code host test timeout; see launch-result.json.'
    }
    $hostExitCode = $process.ExitCode
    if (!(Test-Path -LiteralPath $report)) {
        $launchStatus = 'HOST_NOT_RUN'
        throw "Host report missing. Inspect logs in $output"
    }
    $launchStatus = 'HOST_TESTS_FAILED'
    $result = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
    Write-Host "VS Code $($result.version): $($result.passed) passed, $($result.failed) failed. Report: $report"
    if ($process.ExitCode -ne 0 -or $result.schema -ne 'ristretto/vscode-host-tests/1' -or
        $result.failed -ne 0 -or $result.passed -ne 14 -or @($result.results).Count -ne 14 -or
        @($result.results | Where-Object { $_.status -ne 'PASS' }).Count -ne 0) {
        throw 'VS Code host tests failed; see host-results.json and logs.'
    }
    $launchStatus = 'PASS'
} finally {
    $env:RISTRETTO_VSCODE_HOST_REPORT = $oldReport
    $env:RISTRETTO_VSCODE_HOST_WORKSPACE = $oldWorkspace
    $env:ELECTRON_RUN_AS_NODE = $oldElectron
    $env:VSCODE_PORTABLE = $oldPortable
    if ($process) {
        try { if (!$process.HasExited) { $process.Kill() } }
        finally { $process.Dispose() }
    }
    $summary = [ordered]@{
        schema = 'ristretto/vscode-host-launch/1'
        timestampUtc = [DateTime]::UtcNow.ToString('o')
        status = $launchStatus
        exitCode = $hostExitCode
        hostReportProduced = (Test-Path -LiteralPath $report)
        isolatedPortableProfile = $true
        containsUserText = $false
    }
    [IO.File]::WriteAllText((Join-Path $output 'launch-result.json'), ($summary | ConvertTo-Json) + [Environment]::NewLine, $utf8)
}
