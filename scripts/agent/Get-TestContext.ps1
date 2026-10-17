# Context for the "unit tests for an untested class" type: the class source (no comments or blank lines), the test path to create, and the house test header.
param([Parameter(Mandatory)][string]$Type)
$ErrorActionPreference = 'Stop'
$src = Get-ChildItem -Recurse src -Filter "$Type.cs" | Where-Object FullName -notmatch '[\\/](bin|obj|UnitTests|IntegrationTests)[\\/]' | Select-Object -First 1
if (-not $src) { throw "SRC not found for $Type" }
$lines = Get-Content $src.FullName
$ns = (($lines | Select-String '^namespace\s+([\w\.]+)').Matches[0].Groups[1].Value)
$sub = $ns -replace '^ClearMeasure\.Bootcamp\.', '' -replace '\.', '/'
$testNs = "ClearMeasure.Bootcamp.UnitTests." + ($ns -replace '^ClearMeasure\.Bootcamp\.', '')
"TEST=src/UnitTests/$sub/${Type}Tests.cs  (exists: $(Test-Path "src/UnitTests/$sub/${Type}Tests.cs"))"
"--- SRC $($src.FullName.Substring((Get-Location).Path.Length + 1))"
$lines | Where-Object { $_.Trim() -and $_.Trim() -notmatch '^(//|///)' }
$body = $lines -join "`n"
Get-ChildItem $src.DirectoryName -Filter *.cs | Where-Object { $_.Name -ne $src.Name -and $body -match "\b$($_.BaseName)\b" } | ForEach-Object {
  "--- REF $($_.Name)"; Get-Content $_.FullName | Where-Object { $_.Trim() -and $_.Trim() -notmatch '^(//|///|namespace|using)' } | Select-Object -First 25 }
"--- HEADER (NUnit + Shouldly; [Test] methods named Should...; global usings cover NUnit)"
"using $ns;"; "using Shouldly;"; ""; "namespace $testNs;"; ""; "[TestFixture]"; "public class ${Type}Tests"; "{"; "}"
