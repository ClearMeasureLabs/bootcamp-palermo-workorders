# Build + filtered tests + PR + issue comment in one call, for any lean item once the edits are made.
param([Parameter(Mandatory)][int]$Issue, [Parameter(Mandatory)][string]$Type, [Parameter(Mandatory)][string]$Branch)
$ErrorActionPreference = 'Stop'
& "$PSScriptRoot/Invoke-AgentBuildTest.ps1" -Filter "FullyQualifiedName~${Type}Tests"; if ($LASTEXITCODE) { exit 1 }
$title = gh issue view $Issue --repo ClearMeasureLabs/bootcamp-palermo-workorders --json title -q .title
& "$PSScriptRoot/Publish-AgentPr.ps1" -Issue $Issue -Branch $Branch -Title $title
