# v14g: one-call context finder. Lists files matching -Find (production first, then tests) and prints a compact,
# line-numbered OUTLINE of each (types, members, [Test] names, razor @code/@page) instead of full contents.
# Then read only the line ranges you need. Usage: pwsh -NoProfile scripts/agent/Get-Context.ps1 -Find 'Regex' [-Max 6]
param([Parameter(Mandatory)][string]$Find, [int]$Max = 6, [int]$Lines = 30)
$ErrorActionPreference = 'Stop'
$files = @(rg -l --glob '*.cs' --glob '*.razor' --glob '*.sql' -e $Find src 2>$null | Sort-Object)
$prod = @($files | Where-Object { $_ -notmatch 'Tests[/\\]' }); $test = @($files | Where-Object { $_ -match 'Tests[/\\]' })
"FILES prod=$($prod.Count) test=$($test.Count)" + $(if ($files.Count -gt 2 * $Max) { ' (narrow -Find for fewer)' })
$decl = '^\s*(@page|@code|@inject|@implements|@inherits|namespace |(public|private|protected|internal)\b.*(class|record|interface|enum|struct)\b|(public|private|protected|internal)\b[^=;]*\(|(public|internal|protected)\b[^(]*\{\s*get|CREATE |ALTER )'
foreach ($f in @($prod | Select-Object -First $Max) + @($test | Select-Object -First $Max)) {
  $n = 0; $out = foreach ($l in Get-Content $f) { $n++
    if ($l -match $decl -or $l -match $Find) { "{0,4}: {1}" -f $n, $l.Trim().Substring(0, [Math]::Min(110, $l.Trim().Length)) } }
  "== $f ($n lines)"; $out | Select-Object -First $Lines
}
