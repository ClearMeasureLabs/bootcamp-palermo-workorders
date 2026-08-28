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

$customCaPath = "/opt/copilot-runtime/mkcert-ca/rootCA.pem"
$useTruststore = $false
$truststoreFile = "truststore.jks"

if (Test-Path $customCaPath -PathType Leaf) {
    Write-Host "Custom CA certificate found at $customCaPath"
    Write-Host "Generating custom truststore inside container..."
    $truststoreFullPath = Join-Path $repoRoot $truststoreFile
    if (Test-Path $truststoreFullPath) {
        Remove-Item $truststoreFullPath -Force
    }
    
    $keytoolArgs = @('run', '--rm', '--entrypoint', 'keytool', '-v', '/opt/copilot-runtime/mkcert-ca:/ca', '-v', "${repoRoot}:/workspace", $plantumlImage, '-import', '-trustcacerts', '-keystore', "/workspace/$truststoreFile", '-storepass', 'changeit', '-noprompt', '-alias', 'copilot', '-file', '/ca/rootCA.pem')
    $proc = Start-Process -FilePath docker -ArgumentList $keytoolArgs -NoNewWindow -Wait -PassThru -ErrorAction SilentlyContinue
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Custom truststore generated successfully."
        $useTruststore = $true
    } else {
        Write-Host "Failed to generate custom truststore." -ForegroundColor Yellow
    }
}

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
            if ($useTruststore) {
                $dockerArgs = @('run','--rm','--entrypoint','java','-v',"${repoRoot}:/workspace",'-w','/workspace',$plantumlImage,"-Djavax.net.ssl.trustStore=/workspace/$truststoreFile",'-Djavax.net.ssl.trustStorePassword=changeit','-jar','/opt/plantuml.jar',"-t$fmt","/workspace/$relative")
            } else {
                $dockerArgs = @('run','--rm','-v',"${repoRoot}:/workspace",'-w','/workspace',$plantumlImage,"-t$fmt","/workspace/$relative")
            }
            $proc = Start-Process -FilePath docker -ArgumentList $dockerArgs -NoNewWindow -Wait -PassThru -ErrorAction SilentlyContinue
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Docker mount-based rendering failed for $inPath ($fmt). Falling back to pipe mode." -ForegroundColor Yellow
                # fallback to piping file contents into PlantUML inside the container
                if ($useTruststore) {
                    Get-Content -Raw -Path $inPath | docker run --rm -i --entrypoint java -v "${repoRoot}:/workspace" $plantumlImage "-Djavax.net.ssl.trustStore=/workspace/$truststoreFile" -Djavax.net.ssl.trustStorePassword=changeit -jar /opt/plantuml.jar -t$fmt -pipe > $outPath
                } else {
                    Get-Content -Raw -Path $inPath | docker run --rm -i $plantumlImage -t$fmt -pipe > $outPath
                }
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

if ($useTruststore) {
    $truststoreFullPath = Join-Path $repoRoot $truststoreFile
    if (Test-Path $truststoreFullPath) {
        Remove-Item $truststoreFullPath -Force
    }
}

Write-Host "Rendering complete." -ForegroundColor Green
