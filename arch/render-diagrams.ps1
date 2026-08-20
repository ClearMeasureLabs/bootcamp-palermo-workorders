<#
Render PlantUML diagrams in this repository.

This script renders all .puml files under the arch/ folder (excluding templates/) into PNG and SVG.
It prefers Docker (uses plantuml/plantuml image, pinned) and falls back to a local plantuml.jar if present.
The script attempts to mount the repository into the container so local includes (templates/plantuml-theme.puml) resolve.
#>
param(
    [string[]] $Formats = @("png","svg")
)

# Directory containing this script (arch/)
$scriptDir = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
# repo root is parent of arch/
$repoRoot = (Resolve-Path (Join-Path $scriptDir '..')).Path

Set-Location $scriptDir

$files = Get-ChildItem -Path $scriptDir -Recurse -Include *.puml | Where-Object { $_.FullName -notmatch "[\\/]templates[\/]" }
if (-not $files) {
    Write-Host "No .puml files found under arch/"
    exit 0
}

$hasDocker = (Get-Command docker -ErrorAction SilentlyContinue) -ne $null

# check for plantuml.jar in repo root or arch/.tools
$jarAtRepo = Join-Path $repoRoot '.tools\plantuml.jar'
$jarAtArch = Join-Path $scriptDir '.tools\plantuml.jar'
$hasJar = Test-Path $jarAtRepo -PathType Leaf -or Test-Path $jarAtArch -PathType Leaf
$jarPath = if (Test-Path $jarAtRepo) { $jarAtRepo } elseif (Test-Path $jarAtArch) { $jarAtArch } else { $null }

$plantumlImage = 'plantuml/plantuml:1.2026.2'

foreach ($f in $files) {
    $inPath = $f.FullName
    foreach ($fmt in $Formats) {
        $outPath = [System.IO.Path]::ChangeExtension($inPath, ".$fmt")
        Write-Host "Rendering $inPath -> $outPath"

        if ($hasDocker) {
            # Compute relative path from repo root so we can mount the repo and let PlantUML resolve includes
            $absIn = (Resolve-Path $inPath).Path
            $relative = $absIn.Substring($repoRoot.Length+1).Replace('\', '/')

            Write-Host "Attempting Docker mount render (image: $plantumlImage) for /workspace/$relative"
            $dockerArgs = @('run','--rm','-v',"$repoRoot`:/workspace",'-w','/workspace',$plantumlImage,"-t$fmt","/workspace/$relative")
            $proc = Start-Process -FilePath docker -ArgumentList $dockerArgs -NoNewWindow -Wait -PassThru -ErrorAction SilentlyContinue
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Docker mount-based rendering failed for $inPath ($fmt). Falling back to pipe mode." -ForegroundColor Yellow
                # fallback to piping file contents into PlantUML inside the container
                Get-Content -Raw -Path $inPath | docker run --rm -i $plantumlImage -t$fmt -pipe > $outPath
                if ($LASTEXITCODE -ne 0) {
                    Write-Host "Docker pipe-based rendering also failed for $inPath ($fmt)" -ForegroundColor Red
                    exit 1
                }
            }
        }
        elseif ($hasJar) {
            Write-Host "Using local plantuml.jar at $jarPath"
            & java -jar $jarPath -t$fmt -charset UTF-8 $inPath
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Local jar rendering failed for $inPath ($fmt)" -ForegroundColor Red
                exit 1
            }
        }
        else {
            Write-Host "No renderer found (docker or .tools/plantuml.jar)." -ForegroundColor Yellow
            Write-Host "Install Docker or place plantuml.jar at $repoRoot\\.tools\\plantuml.jar" -ForegroundColor Yellow
            exit 1
        }
    }
}

Write-Host "Rendering complete." -ForegroundColor Green
