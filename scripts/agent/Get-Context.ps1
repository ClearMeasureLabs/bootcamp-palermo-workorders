# v15g: one-call context finder, capped. Runs from the repo root regardless of cwd. Lists files matching -Find
# (production first, then tests). Outlines (line-numbered types/members + matching lines) only for the first -Max files of
# each kind; the rest are listed by name. Total output is capped (~3.5 KB) so a broad regex can't flood the context.
param([string]$Find = '', [int]$Issue = 0, [int]$Max = 3, [int]$Lines = 18, [int]$Cap = 3500)
$ErrorActionPreference = 'Stop'
Set-Location (git rev-parse --show-toplevel)
# v17g: -Issue N prints the issue's Outcome/Acceptance criteria (boilerplate stripped) and, if -Find is not given, builds it
# from the PascalCase identifiers named in backticks. One call replaces `gh issue view` + the first Get-Context call.
# v18g: issue text is printed only on the first call (-Issue without -Find); round 4 reprinted it on every -Find retry.
# Also prints EXISTING= src files whose names contain 2+ issue keywords (round 4 u05/u07: the feature already existed and
# NO_TYPES sent agents guessing type names for 6-12 calls). On NO_TYPES those files become the -Find pattern.
if ($Issue -and -not $Find) {
  $body = gh api "repos/ClearMeasureLabs/bootcamp-palermo-workorders/issues/$Issue" --jq .body
  $keep = ($body -split "`n" | Where-Object { $_ -notmatch 'Token-reduction experiment|The PR targets master|^## Done when' }) -join "`n"
  "ISSUE #$Issue"; $keep.Trim()
  $ids = @([regex]::Matches($keep, '\b[A-Z][a-z]+[A-Z]\w*\b') | ForEach-Object { $_.Value }) +
         @([regex]::Matches($keep, '`([a-z]+(-[a-z]+)+)`') | ForEach-Object { $_.Groups[1].Value }) | Where-Object { $_ -notmatch '^(WorkOrder|Employee|DbUp|SQLite|SqlServer)$' } | Sort-Object -Unique
  $stop = 'with|that|this|from|when|test|tests|unit|integration|have|then|each|only|work|order|orders|true|false|returns|null|empty|value|text|page|change|criteria|outcome|acceptance|works|given|server|sqlite|existing|optional|parameter|description|case|insensitively|unchanged|current|behavior|omitted|called|call|which|default|added'
  $kw = @([regex]::Matches($keep.ToLower(), '[a-z]{4,}') | ForEach-Object { $_.Value.TrimEnd('s') } | Where-Object { $_ -notmatch "^($stop)$" } | Sort-Object -Unique)
  $cand = @(rg --files src -g '*.cs' -g '*.razor' 2>$null | Where-Object { $_ -notmatch '[/\\](bin|obj)[/\\]' } | ForEach-Object {
    $n = [IO.Path]::GetFileNameWithoutExtension($_).ToLower(); $hit = @($kw | Where-Object { $n.Contains($_) }).Count
    if ($hit -ge 2) { [pscustomobject]@{ f = $_; h = $hit } } } | Sort-Object @{e={$_.h};Descending=$true}, @{e={$_.f -match 'Tests'}} | Select-Object -First 6 | ForEach-Object { $_.f })
  $pc = @($cand | Where-Object { $_ -notmatch 'Tests' }); $cand = $pc
  if ($cand) { "EXISTING (may already do part of this; check before adding new types): " + ($cand -join ' ') }
  if (@($ids).Count -lt 2 -and $cand) { $ids = @($ids) + @($cand | Where-Object { $_ -notmatch 'Tests' } | Select-Object -First 3) | ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_) } }
  $ids = @([regex]::Matches($keep, '`([A-Z]\w+)\.[A-Z]\w*') | ForEach-Object { 'class ' + $_.Groups[1].Value + '\b' }) + @($ids)  # Type.NewMember -> the type
  $Find = (@($ids) | Select-Object -Unique -First 6) -join '|'
  if (-not $Find) { 'NO_TYPES: one more call with -Find ExactTypeName'; exit 0 }
  "FIND=$Find"
}
if (-not $Find) { 'need -Find or -Issue'; exit 1 }  # v16g: was $PSScriptRoot/../.. which is "/" when run from the temp copy (round 2: every run broke)
$files = @(rg -l --glob '*.cs' --glob '*.razor' --glob '*.sql' -e $Find src 2>$null | Sort-Object { $_.Length })
$prod = @($files | Where-Object { $_ -notmatch 'Tests[/\\]' }); $test = @($files | Where-Object { $_ -match 'Tests[/\\]' -and $_ -notmatch 'AcceptanceTests' })
# v19g: the sibling test of each top production file (<Name>Tests.cs) goes first among tests, so the agent needn't hunt for it.
$sib = @($prod | Select-Object -First $Max | ForEach-Object { $b = [IO.Path]::GetFileNameWithoutExtension($_) -replace '\.razor$', ''
  rg --files src -g "${b}Tests.cs" 2>$null | Select-Object -First 1 })
$test = @(@($sib | Where-Object { $_ }) + @($test) | Select-Object -Unique)
$decl = '^\s*(@page|@code|@inject|@implements|@inherits|(public|private|protected|internal)\b.*(class|record|interface|enum|struct)\b|(public|private|protected|internal)\b[^=;]*\(|(public|internal|protected)\b[^(]*\{\s*get|CREATE |ALTER )'
$sb = [Text.StringBuilder]::new()
[void]$sb.AppendLine("FILES prod=$($prod.Count) test=$($test.Count)$(if ($files.Count -gt 2 * $Max) { ' (use exact type names to narrow)' })")
# v19g: files of <= 35 lines are printed in full ("FULL", no need to read them); round 5: reads before the first edit were 51%
# of input and most were whole small files (queries, handlers, sibling tests) right after their outline.
foreach ($f in @($prod | Select-Object -First $Max) + @($test | Select-Object -First $Max)) {
  $all = @(Get-Content $f); $n = 0
  if ($all.Count -le 35) {
    [void]$sb.AppendLine("== $f FULL ($($all.Count) lines)")
    foreach ($l in $all) { $n++; if ($l -notmatch '^\s*(using |$)') { [void]$sb.AppendLine(("{0,3}:{1}" -f $n, $l.TrimEnd())) } }
    continue
  }
  $out = foreach ($l in $all) { $n++
    if ($l -match $decl -or $l -match $Find) { $t = $l.Trim(); "{0,4}: {1}" -f $n, $t.Substring(0, [Math]::Min(100, $t.Length)) } }
  [void]$sb.AppendLine("== $f ($n lines)"); $out | Select-Object -First $Lines | ForEach-Object { [void]$sb.AppendLine($_) }
}
$rest = @($prod | Select-Object -Skip $Max) + @($test | Select-Object -Skip $Max) | Select-Object -First 12
if ($rest) { [void]$sb.AppendLine("ALSO: " + ($rest -join ' ')) }
$s = $sb.ToString(); if ($s.Length -gt $Cap) { $s = $s.Substring(0, $Cap) + "`n...(capped; narrow -Find)" }; $s
