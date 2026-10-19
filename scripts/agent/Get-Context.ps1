# v15g: one-call context finder, capped. Runs from the repo root regardless of cwd. Lists files matching -Find
# (production first, then tests). Outlines (line-numbered types/members + matching lines) only for the first -Max files of
# each kind; the rest are listed by name. Total output is capped (~3.5 KB) so a broad regex can't flood the context.
param([string]$Find = '', [string]$File = '', [int]$Issue = 0, [int]$Max = 3, [int]$Lines = 18, [int]$Cap = 5500)
$ErrorActionPreference = 'Stop'
Set-Location (git rev-parse --show-toplevel)
# v21g: hard limit of 2 calls per workspace (round 7 y05 made 13 calls despite the skill rule).
$cnt = Join-Path ([IO.Path]::GetTempPath()) 'agent-getcontext-count'
# v32g fix: parallel Get-Context calls race on this file and can leave "2`n2" -> [int](Object[]) threw and the agent replied
# SCRIPT_FAIL (round 19 s02: -File and -Find issued in the same turn). Take the largest number; write atomically.
# v33g: a new -Issue starts a fresh 3-call budget (multi-item sessions)
$curf = Join-Path ([IO.Path]::GetTempPath()) 'agent-getcontext-issue'
if ($Issue -and -not $File -and -not $PSBoundParameters.ContainsKey('Find')) { $prev = if (Test-Path $curf) { "$(Get-Content $curf -ErrorAction SilentlyContinue)".Trim() } else { '' }
  if ($prev -and $prev -ne "$Issue") { Remove-Item $cnt -ErrorAction SilentlyContinue }; Set-Content $curf $Issue }
$used = 0; if (Test-Path $cnt) { $used = [int](@(Get-Content $cnt -ErrorAction SilentlyContinue | ForEach-Object { if ("$_" -match '(\d+)') { [int]$Matches[1] } }) + 0 | Measure-Object -Maximum).Maximum }
# v30g: limit 3 (issue + 2). v29g's 6 became a target (round 16: 49 calls, 6.1/run, 4 runs hit LIMIT then shell-read).
if ($File -and $Find) { 'REJECTED: use -File or -Find, not both (this call was not counted)'; exit 0 }
# v36g: after the 3 calls, small ranged reads instead of nothing (round 22 c01: 43 failed probe edits).
# v37g: ranged reads batch: -File 'a:1-40,b:80-140' (max 5 ranges x 80 lines per call, 4 such calls, 800 lines total).
# Round 23 c10 made 16 single-range calls (and rewrote the counter file); c09 fell back to 23 grep/read_file calls; turns, not
# bytes, drove its 1.9M tokens.
$parts = @($File -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$ranged = $parts -and -not @($parts | Where-Object { $_ -notmatch ':\d+-\d+$' })
$lnf = "$cnt.lines"; $lnUsed = if (Test-Path $lnf) { [int]("0$(Get-Content $lnf)".Trim()) } else { 0 }  # v38g: v37 stored lines*10 -> budget hit after ~80 lines (round 24 c17/c18)
# v39g: -Find allowed in the 4 extra calls (round 25 c25/c26: 17 refused calls, then 8 shell reads)
if ($used -ge 7 -or $lnUsed -ge 800 -or ($used -ge 3 -and -not ($ranged -or $PSBoundParameters.ContainsKey('Find')))) { 'NEXT: read budget used up (normal, not an error; never reply SCRIPT_FAIL for this). Edit now with what you have, then Submit; Submit names anything missing. Do not call Get-Context again. Never shell/grep/read tools, never touch /tmp/agent-*, never probe with edits.'; exit 0 }
$tmpc = "$cnt.$PID"; Set-Content $tmpc ($used + 1); Move-Item -Force $tmpc $cnt
# v30g: an explicit -Find is a capped grep: matching lines with 2 lines of context, max 25 hits / ~1500 chars (v29g returned
# 3-5 KB outlines per -Find and often missed what the agent was hunting for).
if ($PSBoundParameters.ContainsKey('Find')) {
  $out = [Text.StringBuilder]::new(); $hits = 0
  $raw = @(rg -n -C2 --max-count 5 --glob '*.cs' --glob '*.razor' --glob '*.sql' --glob '*.css' --glob '*.yml' --glob '*.ps1' --glob '!**/Generated/**' -e $Find src .github *.ps1 2>$null)  # v36g: workflow/build-script items
  foreach ($l in $raw) {
    if ($l -match '^[^:]+:\d+:') { $hits++ }
    if ($hits -gt 25 -or $out.Length -gt 1500) { [void]$out.AppendLine('...(capped: narrow the regex)'); break }
    $t = $l.TrimEnd(); [void]$out.AppendLine($t.Substring(0, [Math]::Min(160, $t.Length))) }
  if (-not $raw) { "NO_MATCH for '$Find'" } else { "FIND $Find ($hits hits; 'path:line:' = match, 'path-line-' = context)"; $out.ToString() }
  exit 0
}
# v29g: -File path[:a-b] = capped read. Small files print whole; big files print an outline first, then ask for a range.
if ($File) {
  $max = if ($used -ge 3) { 79 } else { 119 }; $lines = 0
  foreach ($part in $parts | Select-Object -First 5) {
  $fp, $rg = $part -split ':'; if (-not (Test-Path $fp)) { "NO_FILE $fp (use -Find to locate it)"; continue }
  $all = @(Get-Content $fp); $a = 1; $b = $all.Count
  if ($rg) { $a, $b = [int[]]($rg -split '-'); $b = [Math]::Min([Math]::Min($b, $all.Count), $a + $max) }
  elseif ($all.Count -gt 120) {
    "== $fp OUTLINE ($($all.Count) lines; read ranges with -File '${fp}:a-b,other:c-d', max 120 lines each)"
    $n = 0; $all | ForEach-Object { $n++; if ($_ -match '^\s*(@page|@code|\[Test|(public|private|protected|internal)\b)') { "{0,4}: {1}" -f $n, $_.Trim().Substring(0, [Math]::Min(100, $_.Trim().Length)) } } | Select-Object -First 60
    continue }
  "== $fp $a-$b of $($all.Count)"
  $us = @($all | Where-Object { $_ -match '^\s*using [\w.]+;' } | ForEach-Object { ($_ -replace '^\s*using\s+|;\s*$', '') }); if ($us -and $a -gt 1) { "  usings: " + ($us -join '; ') }
  for ($i = $a; $i -le $b; $i++) { "{0,3}:{1}" -f $i, $all[$i - 1].TrimEnd() }
  $lines += $b - $a + 1 }
  if ($used -ge 3) { Set-Content $lnf ($lnUsed + $lines) }
  exit 0
}
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
  # v29g: optional 4th column = recipes that supersede this one when both match (keeps output to the most specific recipe)
  $hits = @(Get-Content (Join-Path $PSScriptRoot 'recipes.txt') | Where-Object { $_ -and $_ -notmatch '^#' } | Where-Object { $keep -match "(?i)$(($_ -split "`t")[1])" })
  $names = @($hits | ForEach-Object { ($_ -split "`t")[0] })
  foreach ($r in $hits) {
    $parts = $r -split "`t"; if ($parts.Count -gt 3 -and @($parts[3] -split ',' | Where-Object { $names -contains $_ })) { continue }
    $Max = 1; $Cap = 2500  # recipe already shows the pattern; keep the outline small
    "RECIPE $($parts[0]) (complete; copy this shape, don't re-read these):"
    foreach ($spec in $parts[2] -split '\s+') { $fp, $rg = $spec -split ':'; if (-not (Test-Path $fp)) { continue }
      $all = @(Get-Content $fp); $a = 1; $b = $all.Count; if ($rg) { $a, $b = [int[]]($rg -split '-'); $b = [Math]::Min($b, $all.Count) }
      "== $fp $a-$b of $($all.Count)"
      $us = @($all | Where-Object { $_ -match '^\s*using [\w.]+;' } | ForEach-Object { ($_ -replace '^\s*using\s+|;\s*$', '') }); if ($us) { "  usings: " + ($us -join '; ') }  # v28g: round 13 r07 read 2 recipe files in full
      for ($i = $a; $i -le $b; $i++) { $l = $all[$i - 1]; if ($l -notmatch '^\s*(using |$)') { "{0,3}:{1}" -f $i, $l.TrimEnd() } } }
  }
  # v27g: db changes -> next DbUp script number (round 12 s07 globbed scripts/Update to find it)
  if ($keep -match '(?i)\b(DbUp|column)\b') { $last = Get-ChildItem src/Database/scripts/Update -Filter '*.sql' | Sort-Object Name | Select-Object -Last 1
    if ($last -and $last.Name -match '^(\d+)_') { "NEXT_SCRIPT=src/Database/scripts/Update/{0:D3}_<Name>.sql (last: $($last.Name))" -f ([int]$Matches[1] + 1) } }
  # v33g: gRPC contract changes: edit only the .proto; Submit regenerates the checked-in C# (round 19 s08 hand-edited it, 1.71M tokens)
  if ($keep -match '(?i)\.proto\b|\bgRPC\b') { "PROTO: edit src/UI/Server/Protos/*.proto only; Submit-Change regenerates src/UI/Server/Generated/Protos (never open or edit those). Generated property = PascalCase of the field (instructions -> Instructions)." }
  # v34g: null handling on a non-nullable member fails Qodana (round 20 t04 CI RED in all 3 arms)
  if ($keep -match '(?i)\bnull\b|empty string') { "NULLABLE: a member declared without '?' counts as never null: '?? x' or null checks on it fail Qodana; if null must be handled, make it nullable ('string? Name')." }
  # v27g: backticked names that occur nowhere in src are new; say so, so the agent doesn't spend its -Find call proving it
  # (round 12: 6 of 8 second calls were -Find on the not-yet-existing name, 2.5-3.8 KB each).
  $newNames = @([regex]::Matches($keep, '`(?:[A-Z]\w+\.)?([A-Za-z]\w{3,})(?:\([^`]*\))?`') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique |
    Where-Object { -not (rg -l -w -F -e $_ src -g '*.cs' -g '*.razor' -g '*.sql' 2>$null | Select-Object -First 1) })
  if ($newNames) { "NEW (not in src yet; don't search for them): " + ($newNames -join ', ') }
  $ids = @([regex]::Matches($keep, '\b[A-Z][a-z]+[A-Z]\w*\b') | ForEach-Object { $_.Value }) +
         @([regex]::Matches($keep, '`([a-z]+(-[a-z]+)+)`') | ForEach-Object { $_.Groups[1].Value }) | Where-Object { $_ -notmatch '^(WorkOrder|Employee|DbUp|SQLite|SqlServer)$' } | Sort-Object -Unique
  $stop = 'with|that|this|from|when|test|tests|unit|integration|have|then|each|only|work|order|orders|true|false|returns|null|empty|value|text|page|change|criteria|outcome|acceptance|works|given|server|sqlite|existing|optional|parameter|description|case|insensitively|unchanged|current|behavior|omitted|called|call|which|default|added|store|number|nothing|uses|yet|new|name|names|gain|gains|field|fields|response|english|example'
  $kw = @([regex]::Matches($keep.ToLower(), '[a-z]{4,}') | ForEach-Object { $_.Value.TrimEnd('s') } | Where-Object { $_ -notmatch "^($stop)$" } | Sort-Object -Unique)
  $cand = @(rg --files src -g '*.cs' -g '*.razor' 2>$null | Where-Object { $_ -notmatch '[/\\](bin|obj)[/\\]' } | ForEach-Object {
    $n = [IO.Path]::GetFileNameWithoutExtension($_).ToLower(); $hit = @($kw | Where-Object { $n.Contains($_) }).Count
    if ($hit -ge 2) { [pscustomobject]@{ f = $_; h = $hit } } } | Sort-Object @{e={$_.h};Descending=$true}, @{e={$_.f -match 'Tests'}} | Select-Object -First 6 | ForEach-Object { $_.f })
  $pc = @($cand | Where-Object { $_ -notmatch 'Tests' }); $cand = $pc
  if ($cand) { "EXISTING (may already do part of this; check before adding new types): " + ($cand -join ' ') }
  # v20g: 'the Counter page' -> the Razor page file name
  $ids = @([regex]::Matches($keep, '\b([A-Z]\w+) page\b') | ForEach-Object { $_.Groups[1].Value } | Where-Object { Test-Path "src/UI.Shared/Pages/$_.razor" }) + @($ids)
  if (@($ids).Count -eq 0 -and $cand) { $ids = @($ids) + @($cand | Where-Object { $_ -notmatch 'Tests' } | Select-Object -First 3) | ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_) } }
  # v23g: a backticked bare type (`Employee`) -> that class (round 9 w07 got junk look-alikes instead)
  $ids = @([regex]::Matches($keep, '`([A-Z][a-z]\w*)`') | ForEach-Object { 'class ' + $_.Groups[1].Value + '\b' }) + @($ids)
  $ids = @([regex]::Matches($keep, '`([A-Z]\w+)\.[A-Z]\w*') | ForEach-Object { 'class ' + $_.Groups[1].Value + '\b' }) + @($ids)  # Type.NewMember -> the type
  $Find = (@($ids) | Select-Object -Unique -First 6) -join '|'
  # v36g: non-code items (workflow, build script, docs): print the named files whole (round 22 c04 spent 2 calls fetching them)
  $nc = @([regex]::Matches($body, '[\w./-]+\.(ya?ml|ps1|md)\b') | ForEach-Object Value | Select-Object -Unique | Where-Object { $_ -notmatch '^src/' -and (Test-Path $_) } | Select-Object -First 3)
  foreach ($p in $nc) { $all = @(Get-Content $p); "== $p FULL ($($all.Count) lines)"; for ($i = 1; $i -le [Math]::Min(150, $all.Count); $i++) { "{0,3}:{1}" -f $i, $all[$i - 1].TrimEnd() } }
  if ($nc -and -not $Find) { 'NON_CODE: edit these, then Submit (it skips the .NET build/tests when src/ is unchanged)'; exit 0 }
  if (-not $Find) { 'NO_TYPES: one more call with -Find ExactTypeName'; exit 0 }
  "FIND=$Find"
}
if (-not $Find) { 'need -Find or -Issue'; exit 1 }
if ($PSBoundParameters.ContainsKey('Find') -and $Cap -eq 5500) { $Cap = 3000 }  # v22g: follow-up -Find calls were ~6 KB each in round 8 (7 of 8 runs made one)  # v16g: was $PSScriptRoot/../.. which is "/" when run from the temp copy (round 2: every run broke)
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
  if (-not $t1) { $t1 = rg --files src -g "*$($b -replace 's$', '')*Tests.cs" -g "!**/AcceptanceTests/**" 2>$null | Sort-Object Length | Select-Object -First 1 }  # v27g: EmployeeTools -> McpEmployeeToolTests (round 12 s03 got TestHost.cs)
  if (-not $t1) { $mir = (Split-Path $_) -replace '^src[/\\]', 'src/UnitTests/'  # v27g: no test yet -> a sibling test in the mirror folder to copy (round 12 s06: 3 greps)
    if (Test-Path $mir) { $t1 = Get-ChildItem $mir -Filter '*Tests.cs' | Sort-Object Length | Select-Object -First 1 | ForEach-Object { Resolve-Path -Relative $_.FullName } | ForEach-Object { $_ -replace '^\./', '' } } }
  if (-not $t1) { $t1 = rg -l -w $b src/UnitTests src/IntegrationTests 2>$null | Sort-Object Length | Select-Object -First 1 }  # v20g: e.g. EmployeeTools -> McpEmployeeToolTests
  $t1 })
$test = @(@($sib | Where-Object { $_ -and $_ -notmatch 'AcceptanceTests' }) + @($test) | Select-Object -Unique)
$decl = '^\s*(@page|@code|@inject|@implements|@inherits|(public|private|protected|internal)\b.*(class|record|interface|enum|struct)\b|(public|private|protected|internal)\b[^=;]*\(|(public|internal|protected)\b[^(]*\{\s*get|CREATE |ALTER )'
$sb = [Text.StringBuilder]::new(); $tail = $null
[void]$sb.AppendLine("FILES prod=$($prod.Count) test=$($test.Count)$(if ($files.Count -gt 2 * $Max) { ' (use exact type names to narrow)' })")
# v19g: files of <= 35 lines are printed in full ("FULL", no need to read them); round 5: reads before the first edit were 51%
# of input and most were whole small files (queries, handlers, sibling tests) right after their outline.
foreach ($f in @($prod | Select-Object -First $Max) + @($test | Select-Object -First $Max)) {
  $all = @(Get-Content $f); $n = 0
  $lim = if ($f -eq $prod[0]) { 90 } elseif ($f -eq $test[0]) { 60 } else { 35 }  # v20g: likely edit target / sibling test in full
  if ($all.Count -le $lim) {
    [void]$sb.AppendLine("== $f FULL ($($all.Count) lines)")
    # v24g: keep usings (one compact line); round 10 v02 rewrote files from FULL output and lost them -> BUILD=FAIL + grep hunt
    $us = @($all | Where-Object { $_ -match '^\s*using [\w.]+;' } | ForEach-Object { ($_ -replace '^\s*using\s+|;\s*$', '') })
    if ($us) { [void]$sb.AppendLine("  usings: " + ($us -join '; ')) }
    foreach ($l in $all) { $n++; if ($l -notmatch '^\s*(using |$)') { [void]$sb.AppendLine(("{0,3}:{1}" -f $n, $l.TrimEnd())) } }
    continue
  }
  $out = foreach ($l in $all) { $n++
    if ($l -match $decl -or $l -match $Find) { $t = $l.Trim(); "{0,4}: {1}" -f $n, $t.Substring(0, [Math]::Min(100, $t.Length)) } }
  [void]$sb.AppendLine("== $f ($n lines)"); $out | Select-Object -First $Lines | ForEach-Object { [void]$sb.AppendLine($_) }
  # v27g: for the sibling test, print its last test in full (shape to copy + where to append); round 12: 4 of 8 runs re-read it
  if ($f -eq $test[0]) { $k = $all.Count - 1; while ($k -gt 0 -and $all[$k] -notmatch '^\s*\[(Test|Fact|TestCase)\b') { $k-- }
    $from = [Math]::Max($k, $all.Count - 30)
    $tail = [Text.StringBuilder]::new(); [void]$tail.AppendLine("== $f last test (append new tests after it; not capped):")
    $us = @($all | Where-Object { $_ -match '^\s*using [\w.]+;' } | ForEach-Object { ($_ -replace '^\s*using\s+|;\s*$', '') })
    if ($us) { [void]$tail.AppendLine("  usings: " + ($us -join '; ')) }
    for ($i = $from; $i -lt $all.Count; $i++) { if ($all[$i].Trim()) { [void]$tail.AppendLine(("{0,3}:{1}" -f ($i + 1), $all[$i].TrimEnd())) } } }
}
$rest = @($prod | Select-Object -Skip $Max) + @($test | Select-Object -Skip $Max) | Select-Object -First 12
$rest = @(@($rest) + @($weak | Where-Object { $_ -notmatch 'AcceptanceTests' } | Select-Object -First 8))
if ($rest) { [void]$sb.AppendLine("ALSO (mention only): " + ($rest -join ' ')) }
$s = $sb.ToString(); if ($s.Length -gt $Cap) { $s = $s.Substring(0, $Cap) + "`n...(capped; narrow -Find)" }; $s
if ($tail) { $tail.ToString() }
if ($Issue) {
  # v30g: TEST SETUP = test-project helper types the sibling test uses (round 16: p03/p06/p05/p08 spent 3-5 calls hunting a
  # capturing logger, DbContext setup, bUnit rendering). The sibling test above is the same-kind example to copy.
  if ($test) { $st = Get-Content $test[0] -Raw
    $own = [IO.Path]::GetFileNameWithoutExtension($test[0])
    $helpers = @([regex]::Matches($st, '\b([A-Z]\w{3,})\b') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique | Where-Object { $_ -ne $own } | ForEach-Object {
      $d = rg -l -e "(class|record|interface) $_\b" --glob '*.cs' src/UnitTests src/IntegrationTests src/AcceptanceTests 2>$null | Select-Object -First 1
      if ($d) { "$_ ($d)" } } | Select-Object -First 8)
    "TEST SETUP (example: $($test[0])): " + $(if ($helpers) { $helpers -join ', ' } else { 'no test-project helpers; copy the example' }) }
  # v32g: ACCESS for internal edit targets (round 18: r08 BUILD=FAIL testing an internal class its tests couldn't see; r01 tested
  # around an internal mapper through a constructor and hit TEST=FAIL). Says whether UnitTests can see internals, or the exact fix.
  $acc = [ordered]@{}
  foreach ($f in @($prod | Where-Object { $_ -match '\.cs$' } | Select-Object -First $Max)) {
    $int = @(Select-String -Path $f -Pattern '^\s*internal\b.*\b(class|record|interface|struct|enum)\s+(\w+)' | ForEach-Object { $_.Matches[0].Groups[2].Value })
    if (-not $int) { continue }
    $d = Split-Path $f; $proj = $null; while ($d -and -not $proj) { $proj = Get-ChildItem $d -Filter *.csproj -ErrorAction SilentlyContinue | Select-Object -First 1; if (-not $proj) { $d = Split-Path $d } }
    if ($proj) { $acc[$proj.FullName] = @(@($acc[$proj.FullName]) + $int | Where-Object { $_ }) } }
  foreach ($k in $acc.Keys) { $pd = Split-Path $k; $pp = (Resolve-Path -Relative $k) -replace '^\./', ''
    $vis = (Get-Content $k -Raw) -match 'InternalsVisibleTo Include="ClearMeasure\.Bootcamp\.UnitTests"' -or [bool](rg -l -F 'InternalsVisibleTo("ClearMeasure.Bootcamp.UnitTests")' --glob '*.cs' $pd 2>$null)
    if ($vis) { "ACCESS: $($acc[$k] -join ', ') internal in $pp; visible to UnitTests: test it directly" }
    else { "ACCESS: $($acc[$k] -join ', ') internal in $pp; NOT visible to UnitTests: add <InternalsVisibleTo Include=`"ClearMeasure.Bootcamp.UnitTests`" /> inside an <ItemGroup> in $pp (not an assembly attribute)" } }
  # v30g: existing tests that already mention the issue's existing symbols / backticked names (round 16 p05: an acceptance test
  # contradicted the change and was only found after a CI RED)
  $syms = @(@($terms) + @([regex]::Matches($keep, '`([\w-]{4,})(?:\([^`]*\))?`') | ForEach-Object { $_.Groups[1].Value }) | Where-Object { $_ -cmatch '^[A-Z][a-z0-9]+[A-Z]\w+$' -and $_ -notin $newNames } | Sort-Object -Unique | Select-Object -First 8)
  if ($syms) { $refs = @(rg -n -w -F @($syms | ForEach-Object { '-e'; $_ }) --glob '*.cs' src/UnitTests src/IntegrationTests src/AcceptanceTests 2>$null |
      Where-Object { $test.Count -eq 0 -or $_ -notlike "$($test[0])*" } | Select-Object -First 8 | ForEach-Object { $t = $_.Trim(); $t.Substring(0, [Math]::Min(140, $t.Length)) })
    if ($refs) { "TESTS ALREADY MENTIONING $($syms -join '/'):"; $refs } }
}
