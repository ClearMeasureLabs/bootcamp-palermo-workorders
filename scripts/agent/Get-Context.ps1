# v15g: one-call context finder, capped. Runs from the repo root regardless of cwd. Lists files matching -Find
# (production first, then tests). Outlines (line-numbered types/members + matching lines) only for the first -Max files of
# each kind; the rest are listed by name. Total output is capped (~3.5 KB) so a broad regex can't flood the context.
param([Parameter(Mandatory)][string]$Find, [int]$Max = 3, [int]$Lines = 18, [int]$Cap = 3500)
$ErrorActionPreference = 'Stop'
Set-Location (Resolve-Path "$PSScriptRoot/../..")
$files = @(rg -l --glob '*.cs' --glob '*.razor' --glob '*.sql' -e $Find src 2>$null | Sort-Object { $_.Length })
$prod = @($files | Where-Object { $_ -notmatch 'Tests[/\\]' }); $test = @($files | Where-Object { $_ -match 'Tests[/\\]' })
$decl = '^\s*(@page|@code|@inject|@implements|@inherits|(public|private|protected|internal)\b.*(class|record|interface|enum|struct)\b|(public|private|protected|internal)\b[^=;]*\(|(public|internal|protected)\b[^(]*\{\s*get|CREATE |ALTER )'
$sb = [Text.StringBuilder]::new()
[void]$sb.AppendLine("FILES prod=$($prod.Count) test=$($test.Count)$(if ($files.Count -gt 2 * $Max) { ' (use exact type names to narrow)' })")
foreach ($f in @($prod | Select-Object -First $Max) + @($test | Select-Object -First $Max)) {
  $n = 0; $out = foreach ($l in Get-Content $f) { $n++
    if ($l -match $decl -or $l -match $Find) { $t = $l.Trim(); "{0,4}: {1}" -f $n, $t.Substring(0, [Math]::Min(100, $t.Length)) } }
  [void]$sb.AppendLine("== $f ($n lines)"); $out | Select-Object -First $Lines | ForEach-Object { [void]$sb.AppendLine($_) }
}
$rest = @($prod | Select-Object -Skip $Max) + @($test | Select-Object -Skip $Max) | Select-Object -First 12
if ($rest) { [void]$sb.AppendLine("ALSO: " + ($rest -join ' ')) }
$s = $sb.ToString(); if ($s.Length -gt $Cap) { $s = $s.Substring(0, $Cap) + "`n...(capped; narrow -Find)" }; $s
