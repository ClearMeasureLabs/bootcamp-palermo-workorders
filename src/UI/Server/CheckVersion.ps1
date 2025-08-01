param(
    [string]$server,
    [string]$version
)

Write-Host "Provided server url: $server"
Write-Host "Provided version: $version"

$uri = "$server/version"

Write-Host "Checking version at $uri"

# Delay to ensure the new container app has been deployed
Start-Sleep -Seconds 60
Invoke-WebRequest $uri -UseBasicParsing | Foreach {
    
    $_.Content.Contains($version) | Foreach {
        if(-Not($_)) {
            Throw "Incorrect version. Expected $version as parameter but received $_ from $url"
        }
        else {
            Write-Host "Correct version: $version"
        }
    }
}