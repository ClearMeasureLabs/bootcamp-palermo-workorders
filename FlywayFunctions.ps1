. .\BuildFunctions.ps1

Function Setup-FlywayCLI {
    param (
        [Parameter(Mandatory = $true)]
        [string]$flywayCliDir,

        [Parameter(Mandatory = $false)]
        [string]$flywayDownloadUrl = "https://download.red-gate.com/maven/release/com/redgate/flyway/flyway-commandline/10.18.0/flyway-commandline-10.18.0-windows-x64.zip"
    )
	
	# Normalize the path to ensure compatibility with both Linux and Windows
	$flywayCliDir = $flywayCliDir -replace '/', '\'
	
	$base_dir = resolve-path .\

	#check that the CLI folder exists
    if (-Not (Test-Path -Path $flywayCliDir -PathType Container)) {
        try {
			Log-Message "Creating Directory '$flywayCliDir'" "INFO"
            New-Item -Path $flywayCliDir -ItemType Directory -Force | Out-Null
        } catch {
            Log-Message "Failed to create directory '$flywayCliDir'. Error: $_" "ERROR"
            return $false
        }
    } 
	else 
	{
		Log-Message "Found Directory '$flywayCliDir'" "INFO"
	}
    
    $flywayCmdPath = Join-Path -Path $flywayCliDir -ChildPath "flyway.cmd"

    # Check if flyway.cmd exists
    if (-Not (Test-Path -Path $flywayCmdPath -PathType Leaf)) {
        Log-Message "flyway.cmd not found in '$flywayCliDir'. Proceeding to download Flyway CLI." "INFO"

        $tempZipPath = Join-Path -Path $base_dir -ChildPath "flyway-cli.zip"
        try {

            # Download the Flyway CLI zip
            Log-Message "Downloading Flyway CLI from '$flywayDownloadUrl' to '$tempZipPath'." "INFO"
            Invoke-WebRequest -Uri $flywayDownloadUrl -OutFile $tempZipPath -UseBasicParsing
            Log-Message "Download completed." "SUCCESS"
        } catch {
            Log-Message "Failed to download Flyway CLI. Error: $_" "ERROR"
            return $false
        }

        try {

            # Create a temporary extraction directory
            $tempExtractDir = Join-Path -Path $base_dir -ChildPath "FlywayExtract"
            New-Item -Path $tempExtractDir -ItemType Directory -Force | Out-Null

            # Extract the zip to the temporary directory
            Log-Message "Extracting Flyway CLI to temporary directory '$tempExtractDir'." "INFO"
            Expand-Archive -Path $tempZipPath -DestinationPath $tempExtractDir -Force
            Log-Message "Extraction completed." "SUCCESS"

            # Move the contents from the extracted folder to flywayCliDir
            # Assuming the zip extracts to a single folder
            $extractedSubDir = Get-ChildItem -Path $tempExtractDir -Directory | Select-Object -First 1

            if ($extractedSubDir) {
                Get-ChildItem -Path $extractedSubDir.FullName -Recurse | ForEach-Object {
                    $destinationPath = $_.FullName.Replace($extractedSubDir.FullName, $flywayCliDir)
                    if ($_.PSIsContainer) {
                        New-Item -Path $destinationPath -ItemType Directory -Force | Out-Null
                    } else {
                        Move-Item -Path $_.FullName -Destination $destinationPath -Force
                    }
                }
            } else {
                Log-Message "No subdirectory found in the extracted Flyway CLI zip. Unexpected zip structure." "ERROR"
                return $false
            }
        } catch {
            Log-Message "Failed to extract Flyway CLI. Error: $_" "ERROR"
            return $false
        } finally {
            # Clean up temporary files
            if (Test-Path -Path $tempZipPath) {
                Remove-Item -Path $tempZipPath -Force
                #Log-Message "Removed temporary zip file '$tempZipPath'." "INFO"
            }
            if ($tempExtractDir -and (Test-Path -Path $tempExtractDir)) {
                Remove-Item -Path $tempExtractDir -Recurse -Force
                #Log-Message "Removed temporary extraction directory '$tempExtractDir'." "INFO"
            }
        }

        # Verify that flyway.cmd now exists
        if (Test-Path -Path $flywayCmdPath -PathType Leaf) {
            Log-Message "Flyway CLI setup completed successfully." "SUCCESS"
            return $true
        } else {
            Log-Message "flyway.cmd still not found after extraction. Setup failed." "ERROR"
            return $false
        }
    } else {
        Log-Message "flyway.cmd already exists in '$flywayCliDir'. No action needed." "INFO"
        return $true
    }
}



