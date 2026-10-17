# One-shot lean loop for guard-clause items: edit, build, test, publish. Prints 3-4 lines.
param([Parameter(Mandatory)][int]$Issue, [Parameter(Mandatory)][string]$Type, [Parameter(Mandatory)][string]$Branch, [string]$AgentUrl = '')
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
& "$here/Add-NullGuard.ps1" -Type $Type
& "$here/Invoke-AgentBuildTest.ps1" -Filter "FullyQualifiedName~${Type}Tests"; if ($LASTEXITCODE) { exit 1 }
$title = gh issue view $Issue --repo ClearMeasureLabs/bootcamp-palermo-workorders --json title -q .title
& "$here/Publish-AgentPr.ps1" -Issue $Issue -Branch $Branch -Title $title -AgentUrl $AgentUrl
