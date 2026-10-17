# Prints only what an agent needs to edit: the target type's Execute method and the test file's usings plus class header.
param([Parameter(Mandatory)][string]$Type, [string]$Method = 'Execute')
$ErrorActionPreference = 'Stop'
$src = Get-ChildItem -Recurse -Path src -Filter "$Type.cs" | Where-Object FullName -notmatch '[\\/](bin|obj)[\\/]' | Select-Object -First 1
$test = Get-ChildItem -Recurse -Path src/UnitTests -Filter "$Type*Tests.cs" | Select-Object -First 1
"SRC=$($src.FullName.Replace((Get-Location).Path + [IO.Path]::DirectorySeparatorChar, ''))"
"TEST=$(if ($test) { $test.FullName.Replace((Get-Location).Path + [IO.Path]::DirectorySeparatorChar, '') } else { 'NONE' })"
$lines = Get-Content $src.FullName
$i = ($lines | Select-String -Pattern "\b$Method\s*\(" | Select-Object -First 1).LineNumber
if ($i) { "--- $Method @ line $i"; $lines[($i - 1)..([Math]::Min($i + 8, $lines.Count - 1))] } else { "--- $Method not found in $($src.Name)" }
if ($test) { '--- test header'; Get-Content $test.FullName -TotalCount 12 }
