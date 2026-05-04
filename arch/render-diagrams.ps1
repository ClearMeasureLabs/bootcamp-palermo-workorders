<#
Render PlantUML diagrams in this repository.

This script attempts to render all .puml files under the arch/ folder (excluding templates/) into PNG and SVG.

It prefers Docker (uses plantuml/plantuml image) and falls back to a local .tools/plantuml.jar if present.
#>
param(
    [string[]] $Formats = @("png","svg")
)

$root = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
Set-Location $root

$files = Get-ChildItem -Path . -Recurse -Include *.puml | Where-Object { $_.FullName -notmatch "[\\/]templates[\\/]" }
if (-not $files) {
    Write-Host "No .puml files found under arch/"
    exit 0
}

$hasDocker = (Get-Command docker -ErrorAction SilentlyContinue) -ne $null
$hasJar = (Test-Path "..\..\.tools\plantuml.jar" -PathType Leaf) -or (Test-Path ".\.tools\plantuml.jar" -PathType Leaf)

foreach ($f in $files) {
    $inPath = $f.FullName
    foreach ($fmt in $Formats) {
        $outPath = [System.IO.Path]::ChangeExtension($inPath, ".$fmt")
        Write-Host "Rendering $inPath -> $outPath"
        if ($hasDocker) {
            # Use Docker and pipe file contents into plantuml to avoid volume mount issues on some platforms
            $plantumlImage = "plantuml/plantuml:1.2026.2"
            $repoRoot = Split-Path -Path $root -Parent
            docker run --rm -v "$repoRoot":/workspace -w /workspace/arch $plantumlImage -t$fmt "$($f.Name)"
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Docker-based rendering failed for $inPath ($fmt)" -ForegroundColor Red
                exit 1
            }
        }
        elseif ($hasJar) {
            # Try local plantuml.jar; requires java >= 11
            $jarPath = if (Test-Path ".\.tools\plantuml.jar") { ".\.tools\plantuml.jar" } else { "..\..\.tools\plantuml.jar" }
            & java -jar $jarPath -t$fmt -charset UTF-8 $inPath
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Local jar rendering failed for $inPath ($fmt)" -ForegroundColor Red
                exit 1
            }
        }
        else {
            Write-Host "No renderer found (docker or .tools/plantuml.jar). Install Docker or download plantuml.jar into ./.tools/plantuml.jar" -ForegroundColor Yellow
            exit 1
        }
    }
}

Write-Host "Rendering complete." -ForegroundColor Green
