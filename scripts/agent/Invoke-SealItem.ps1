# Canned recipe for the "seal a non-inherited class" visibility type: verify, seal, build, test, publish in one call.
param([Parameter(Mandatory)][int]$Issue, [Parameter(Mandatory)][string]$Type, [Parameter(Mandatory)][string]$Branch)
$ErrorActionPreference = 'Stop'
$hits = git grep -n -E ":\s*$Type\b|For<$Type>|Mock<$Type>" -- src
if ($hits) { "SEAL=REFUSED (inherited or mocked):"; $hits | Select-Object -First 5; exit 1 }
$src = Get-ChildItem -Recurse src -Filter "$Type.cs" | Where-Object FullName -notmatch '[\\/](bin|obj)[\\/]' | Select-Object -First 1
$code = Get-Content $src.FullName -Raw
if ($code -notmatch "public sealed class $Type\b") {
  $new = [regex]::Replace($code, "public (partial )?class $Type\b", "public sealed `$1class $Type", 1)
  if ($new -eq $code) { throw "public class $Type not found" }
  Set-Content $src.FullName $new -NoNewline
}
"SEALED=$($src.Name)"
& "$PSScriptRoot/Complete-AgentItem.ps1" -Issue $Issue -Type $Type -Branch $Branch
