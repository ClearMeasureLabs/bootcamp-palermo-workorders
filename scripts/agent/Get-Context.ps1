# v15g: one-call context finder, capped. Runs from the repo root regardless of cwd. Lists files matching -Find
# (production first, then tests). Outlines (line-numbered types/members + matching lines) only for the first -Max files of
# each kind; the rest are listed by name. Total output is capped (~3.5 KB) so a broad regex can't flood the context.
param([string]$Find = '', [int]$Issue = 0, [int]$Max = 3, [int]$Lines = 18, [int]$Cap = 5500)
$ErrorActionPreference = 'Stop'
Set-Location (git rev-parse --show-toplevel)
# v21g: hard limit of 2 calls per workspace (round 7 y05 made 13 calls despite the skill rule).
$cnt = Join-Path ([IO.Path]::GetTempPath()) 'agent-getcontext-count'
$used = if (Test-Path $cnt) { [int](Get-Content $cnt) } else { 0 }
if ($used -ge 2) { 'LIMIT: Get-Context already used twice. Read line ranges of files listed above (or copy the RECIPE), then edit.'; exit 0 }
Set-Content $cnt ($used + 1)
# v17g: -Issue N prints the issue's Outcome/Acceptance criteria (boilerplate stripped) and, if -Find is not given, builds it
# from the PascalCase identifiers named in backticks. One call replaces `gh issue view` + the first Get-Context call.
# v18g: issue text is printed only on the first call (-Issue without -Find); round 4 reprinted it on every -Find retry.
# Also prints EXISTING= src files whose names contain 2+ issue keywords (round 4 u05/u07: the feature already existed and
# NO_TYPES sent agents guessing type names for 6-12 calls). On NO_TYPES those files become the -Find pattern.
if ($Issue -and -not $Find) {
  $body = gh api "repos/ClearMeasureLabs/bootcamp-palermo-workorders/issues/$Issue" --jq .body
  $keep = ($body -split "`n" | Where-Object { $_ -notmatch 'Token-reduction experiment|The PR targets master|^## Done when' }) -join "`n"
  "ISSUE #$Issue"; $keep.Trim()
  # v21g: recipes = complete exemplar of the change kind (round 7 y04/y05: 17 reads / 13 calls hunting the query+validator+test pattern)
  foreach ($r in Get-Content (Join-Path $PSScriptRoot 'recipes.txt') | Where-Object { $_ -and $_ -notmatch '^#' }) {
    $parts = $r -split "`t"; if ($keep -notmatch "(?i)$($parts[1])") { continue }
    $Max = 1; $Cap = 2500  # recipe already shows the pattern; keep the outline small
    "RECIPE $($parts[0]) (complete; copy this shape, don't re-read these):"
    foreach ($spec in $parts[2] -split '\s+') { $fp, $rg = $spec -split ':'; if (-not (Test-Path $fp)) { continue }
      $all = @(Get-Content $fp); $a = 1; $b = $all.Count; if ($rg) { $a, $b = [int[]]($rg -split '-'); $b = [Math]::Min($b, $all.Count) }
      "== $fp $a-$b of $($all.Count)"; for ($i = $a; $i -le $b; $i++) { $l = $all[$i - 1]; if ($l -notmatch '^\s*(using |$)') { "{0,3}:{1}" -f $i, $l.TrimEnd() } } }
  }
  $ids = @([regex]::Matches($keep, '\b[A-Z][a-z]+[A-Z]\w*\b') | ForEach-Object { $_.Value }) +
         @([regex]::Matches($keep, '`([a-z]+(-[a-z]+)+)`') | ForEach-Object { $_.Groups[1].Value }) | Where-Object { $_ -notmatch '^(WorkOrder|Employee|DbUp|SQLite|SqlServer)$' } | Sort-Object -Unique
  $stop = 'with|that|this|from|when|test|tests|unit|integration|have|then|each|only|work|order|orders|true|false|returns|null|empty|value|text|page|change|criteria|outcome|acceptance|works|given|server|sqlite|existing|optional|parameter|description|case|insensitively|unchanged|current|behavior|omitted|called|call|which|default|added'
  $kw = @([regex]::Matches($keep.ToLower(), '[a-z]{4,}') | ForEach-Object { $_.Value.TrimEnd('s') } | Where-Object { $_ -notmatch "^($stop)$" } | Sort-Object -Unique)
  $cand = @(rg --files src -g '*.cs' -g '*.razor' 2>$null | Where-Object { $_ -notmatch '[/\\](bin|obj)[/\\]' } | ForEach-Object {
    $n = [IO.Path]::GetFileNameWithoutExtension($_).ToLower(); $hit = @($kw | Where-Object { $n.Contains($_) }).Count
    if ($hit -ge 2) { [pscustomobject]@{ f = $_; h = $hit } } } | Sort-Object @{e={$_.h};Descending=$true}, @{e={$_.f -match 'Tests'}} | Select-Object -First 6 | ForEach-Object { $_.f })
  $pc = @($cand | Where-Object { $_ -notmatch 'Tests' }); $cand = $pc
  if ($cand) { "EXISTING (may already do part of this; check before adding new types): " + ($cand -join ' ') }
  # v20g: 'the Counter page' -> the Razor page file name
  $ids = @([regex]::Matches($keep, '\b([A-Z]\w+) page\b') | ForEach-Object { $_.Groups[1].Value } | Where-Object { Test-Path "src/UI.Shared/Pages/$_.razor" }) + @($ids)
  if (@($ids).Count -eq 0 -and $cand) { $ids = @($ids) + @($cand | Where-Object { $_ -notmatch 'Tests' } | Select-Object -First 3) | ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_) } }
  $ids = @([regex]::Matches($keep, '`([A-Z]\w+)\.[A-Z]\w*') | ForEach-Object { 'class ' + $_.Groups[1].Value + '\b' }) + @($ids)  # Type.NewMember -> the type
  $Find = (@($ids) | Select-Object -Unique -First 6) -join '|'
  if (-not $Find) { 'NO_TYPES: one more call with -Find ExactTypeName'; exit 0 }
  "FIND=$Find"
}
if (-not $Find) { 'need -Find or -Issue'; exit 1 }  # v16g: was $PSScriptRoot/../.. which is "/" when run from the temp copy (round 2: every run broke)
$files = @(rg -l --glob '*.cs' --glob '*.razor' --glob '*.sql' --glob '!**/Generated/**' -e $Find src 2>$null | Sort-Object { $_.Length })
# v20g: a brand-new type matches nothing; fall back to the EXISTING look-alikes so the agent sees the pattern to copy
if (-not $files -and $cand) { $Find = ($cand | ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_) }) -join '|'; "FIND=$Find (look-alikes)"
  $files = @(rg -l --glob '*.cs' --glob '*.razor' --glob '!**/Generated/**' -e $Find src 2>$null | Sort-Object { $_.Length }) }
# v20g: rank by relevance, not path length (round 6 x04: ServerApplication.cs outranked EmployeeTools.cs). 3 = file name is a
# search term, 2 = declares it (type or MCP tool name), 1 = mentions it.
$terms = @($Find -split '\|' | ForEach-Object { ($_ -replace '^class ', '' -replace '\\b', '').Trim() } | Where-Object { $_ })
$rank = { param($f) $b = [IO.Path]::GetFileNameWithoutExtension($f) -replace '\.razor$', ''
  if ($terms | Where-Object { $b -eq $_ -or $b -eq "${_}Tests" }) { return 3 }
  $txt = Get-Content $f -Raw
  if ($terms | Where-Object { $txt -match "(class|record|interface|enum)\s+$([regex]::Escape($_))\b|Name = `"$([regex]::Escape($_))`"" }) { return 2 }; 1 }
$scored = @($files | ForEach-Object { [pscustomobject]@{ f = $_; r = (& $rank $_) } } | Sort-Object @{e={$_.r}; Descending=$true}, @{e={$_.f.Length}})
$strong = @($scored | Where-Object { $_.r -ge 2 })
$files = @($scored | ForEach-Object { $_.f })
$weak = @(); if ($strong) { $weak = @($scored | Where-Object { $_.r -lt 2 } | ForEach-Object { $_.f }); $files = @($strong | ForEach-Object { $_.f }) }
$prod = @($files | Where-Object { $_ -notmatch 'Tests[/\\]' }); $test = @($files | Where-Object { $_ -match 'Tests[/\\]' -and $_ -notmatch 'AcceptanceTests' })
# v19g: the sibling test of each top production file (<Name>Tests.cs) goes first among tests, so the agent needn't hunt for it.
$sib = @($prod | Select-Object -First $Max | ForEach-Object { $b = [IO.Path]::GetFileNameWithoutExtension($_) -replace '\.razor$', ''
  $t1 = rg --files src -g "${b}Tests.cs" -g "!**/AcceptanceTests/**" 2>$null | Select-Object -First 1
  if (-not $t1) { $t1 = rg -l -w $b src/UnitTests src/IntegrationTests 2>$null | Sort-Object Length | Select-Object -First 1 }  # v20g: e.g. EmployeeTools -> McpEmployeeToolTests
  $t1 })
$test = @(@($sib | Where-Object { $_ -and $_ -notmatch 'AcceptanceTests' }) + @($test) | Select-Object -Unique)
$decl = '^\s*(@page|@code|@inject|@implements|@inherits|(public|private|protected|internal)\b.*(class|record|interface|enum|struct)\b|(public|private|protected|internal)\b[^=;]*\(|(public|internal|protected)\b[^(]*\{\s*get|CREATE |ALTER )'
$sb = [Text.StringBuilder]::new()
[void]$sb.AppendLine("FILES prod=$($prod.Count) test=$($test.Count)$(if ($files.Count -gt 2 * $Max) { ' (use exact type names to narrow)' })")
# v19g: files of <= 35 lines are printed in full ("FULL", no need to read them); round 5: reads before the first edit were 51%
# of input and most were whole small files (queries, handlers, sibling tests) right after their outline.
foreach ($f in @($prod | Select-Object -First $Max) + @($test | Select-Object -First $Max)) {
  $all = @(Get-Content $f); $n = 0
  $lim = if ($f -eq $prod[0]) { 90 } elseif ($f -eq $test[0]) { 60 } else { 35 }  # v20g: likely edit target / sibling test in full
  if ($all.Count -le $lim) {
    [void]$sb.AppendLine("== $f FULL ($($all.Count) lines)")
    foreach ($l in $all) { $n++; if ($l -notmatch '^\s*(using |$)') { [void]$sb.AppendLine(("{0,3}:{1}" -f $n, $l.TrimEnd())) } }
    continue
  }
  $out = foreach ($l in $all) { $n++
    if ($l -match $decl -or $l -match $Find) { $t = $l.Trim(); "{0,4}: {1}" -f $n, $t.Substring(0, [Math]::Min(100, $t.Length)) } }
  [void]$sb.AppendLine("== $f ($n lines)"); $out | Select-Object -First $Lines | ForEach-Object { [void]$sb.AppendLine($_) }
}
$rest = @($prod | Select-Object -Skip $Max) + @($test | Select-Object -Skip $Max) | Select-Object -First 12
$rest = @(@($rest) + @($weak | Where-Object { $_ -notmatch 'AcceptanceTests' } | Select-Object -First 8))
if ($rest) { [void]$sb.AppendLine("ALSO (mention only): " + ($rest -join ' ')) }
$s = $sb.ToString(); if ($s.Length -gt $Cap) { $s = $s.Substring(0, $Cap) + "`n...(capped; narrow -Find)" }; $s
