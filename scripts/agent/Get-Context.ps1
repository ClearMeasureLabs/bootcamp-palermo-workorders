# v15g: one-call context finder, capped. Runs from the repo root regardless of cwd. Lists files matching -Find
# (production first, then tests). Outlines (line-numbered types/members + matching lines) only for the first -Max files of
# each kind; the rest are listed by name. Total output is capped (~3.5 KB) so a broad regex can't flood the context.
param([string]$Find = '', [int]$Issue = 0, [int]$Max = 3, [int]$Lines = 18, [int]$Cap = 2500)
$ErrorActionPreference = 'Stop'
Set-Location (git rev-parse --show-toplevel)
# v17g: -Issue N prints the issue's Outcome/Acceptance criteria (boilerplate stripped) and, if -Find is not given, builds it
# from the PascalCase identifiers named in backticks. One call replaces `gh issue view` + the first Get-Context call.
if ($Issue) {
  $body = gh api "repos/ClearMeasureLabs/bootcamp-palermo-workorders/issues/$Issue" --jq .body
  $keep = ($body -split "`n" | Where-Object { $_ -notmatch 'Token-reduction experiment|The PR targets master|^## Done when' }) -join "`n"
  "ISSUE #$Issue"; $keep.Trim()
  if (-not $Find) {
    $ids = @([regex]::Matches($keep, '\b[A-Z][a-z]+[A-Z]\w*\b') | ForEach-Object { $_.Value }) +
           @([regex]::Matches($keep, '`([a-z]+(-[a-z]+)+)`') | ForEach-Object { $_.Groups[1].Value }) | Sort-Object -Unique
    $Find = ($ids | Select-Object -First 6) -join '|'
  }
  if (-not $Find) { 'NO_TYPES: run again with -Find ExactTypeName'; exit 0 }
  "FIND=$Find"
}
if (-not $Find) { 'need -Find or -Issue'; exit 1 }  # v16g: was $PSScriptRoot/../.. which is "/" when run from the temp copy (round 2: every run broke)
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
