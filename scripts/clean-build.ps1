# Ristretto - pulizia delle cartelle di compilazione .NET (bin e obj)
# Posizionare il file nella cartella scripts del progetto.
# Esempi: .\scripts\clean-build.cmd -Preview
#          .\scripts\clean-build.cmd
#          .\scripts\clean-build.cmd -Force
param(
    [Alias('DryRun')]
    [switch]$Preview,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

try {
    $root = (Resolve-Path -LiteralPath (Join-Path -Path $PSScriptRoot -ChildPath '..')).Path
    $solution = Join-Path -Path $root -ChildPath 'Ristretto.sln'
    $buildProps = Join-Path -Path $root -ChildPath 'Directory.Build.props'

    if (-not (Test-Path -LiteralPath $solution -PathType Leaf) -or
        -not (Test-Path -LiteralPath $buildProps -PathType Leaf)) {
        throw "Cartella Ristretto non riconosciuta: $root. Posizionare lo script in Ristretto\scripts."
    }

    # Individua tutti i progetti .NET, inclusi quelli sotto tools/ e tests/.
    # Non attraversa intenzionalmente repository Git, dipendenze JS o output preesistenti.
    $excludedComponents = @('.git', '.vs', 'node_modules', 'bin', 'obj', 'packages')
    $projects = @(Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.csproj' -Force |
        Where-Object {
            $relative = $_.FullName.Substring($root.Length).TrimStart([char[]]@('\', '/'))
            $segments = $relative -split '[\\/]'
            -not (@($segments | Where-Object { $excludedComponents -contains $_ }).Count -gt 0)
        })

    $paths = @(
        foreach ($project in $projects) {
            foreach ($folderName in @('bin', 'obj')) {
                $candidate = Join-Path -Path $project.DirectoryName -ChildPath $folderName
                if (Test-Path -LiteralPath $candidate -PathType Container) {
                    $item = Get-Item -LiteralPath $candidate -Force
                    if (($item.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
                        Write-Warning "Ignorato collegamento/junction: $candidate"
                        continue
                    }
                    # Evita la rimozione ricorsiva se ci sono junction/link anche all'interno.
                    $nestedLink = Get-ChildItem -LiteralPath $candidate -Recurse -Force -ErrorAction Stop |
                        Where-Object { ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0 } |
                        Select-Object -First 1
                    if ($null -ne $nestedLink) {
                        Write-Warning "Ignorata cartella contenente link/junction: $candidate"
                        continue
                    }
                    $item.FullName
                }
            }
        }
    ) | Sort-Object -Unique

    Write-Host "Ristretto: $root"
    Write-Host "Progetti .NET trovati: $($projects.Count)"
    if ($paths.Count -eq 0) {
        Write-Host 'Nessuna cartella bin/obj da eliminare.'
        exit 0
    }

    Write-Host "Cartelle bin/obj trovate: $($paths.Count)"
    foreach ($path in $paths) {
        Write-Host ('  ' + $path.Substring($root.Length).TrimStart([char[]]@('\', '/')))
    }

    if ($Preview) {
        Write-Host 'ANTEPRIMA: nessun file eliminato.'
        exit 0
    }

    if (-not $Force) {
        $answer = Read-Host 'Scrivi ELIMINA per confermare (qualsiasi altra risposta annulla)'
        if ($answer -cne 'ELIMINA') {
            Write-Host 'Operazione annullata.'
            exit 0
        }
    }

    $deleted = 0
    $failed = 0
    foreach ($path in $paths) {
        try {
            Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction Stop
            Write-Host "ELIMINATA: $path"
            $deleted++
        }
        catch {
            Write-Warning "Non eliminata: $path - $($_.Exception.Message)"
            $failed++
        }
    }
    Write-Host "Completato. Eliminate: $deleted; errori: $failed."
    if ($failed -gt 0) { exit 1 }
    exit 0
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}
