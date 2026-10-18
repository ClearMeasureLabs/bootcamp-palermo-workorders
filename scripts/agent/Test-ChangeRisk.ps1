# v14g: local CI-trap check on the workspace src diff (catches what caused past CI REDs, before pushing).
#  CRAP_RISK: changed method with estimated cyclomatic complexity > 5 (CI fails above 6; extract a helper).
#  QODANA_RISK: added `new[] {`/`new List<..> {` (use [a, b]); new options { get; set; } without the ReSharper line.
#  NUMBER_RISK: string literal > 7 chars assigned to a work order Number in tests.
$ErrorActionPreference = 'Stop'
git add -A -N src 2>$null | Out-Null
$base = if (git rev-parse -q --verify origin/analysis/token-reduction 2>$null) { 'origin/analysis/token-reduction' } else { 'origin/master' }
$changed = @{}; $file = $null
foreach ($l in git diff -U0 $base -- src) {
  if ($l -match '^\+\+\+ b/(.+)$') { $file = $Matches[1]; $changed[$file] = @(); continue }
  if ($l -match '^@@ .*\+(\d+)(?:,(\d+))? @@') { $s = [int]$Matches[1]; $c = if ($Matches[2]) { [int]$Matches[2] } else { 1 }; if ($c) { $changed[$file] += $s..($s + $c - 1) }; continue }
  if ($l -match '^\+(?!\+\+)(.*)$' -and $file) { $a = $Matches[1]
    if ($a -match 'new\s*(\w+)?\[\]\s*\{|new List<[^>]+>\s*\{') { "QODANA_RISK $file : use a collection expression [a, b]: $($a.Trim())" }
    if ($file -match 'Tests' -and $a -match 'Number\s*=\s*"([^"]{8,})"') { "NUMBER_RISK $file : work order Number '$($Matches[1])' > 7 chars" } }
}
foreach ($f in $changed.Keys) {
  if ($f -notmatch '\.cs$' -or $f -match 'Tests' -or -not (Test-Path $f)) { continue }
  $src = @(Get-Content $f); $set = [Collections.Generic.HashSet[int]]::new([int[]]$changed[$f])
  if ($f -match 'Options\.cs$') { for ($i = 0; $i -lt $src.Count; $i++) {
    if ($set.Contains($i + 1) -and $src[$i] -match 'public .*\{ get; set; \}' -and ($i -eq 0 -or $src[$i - 1] -notmatch 'ReSharper disable')) {
      "QODANA_RISK ${f}:$($i + 1) : add // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global above it" } } }
  for ($i = 0; $i -lt $src.Count; $i++) {
    if ($src[$i] -notmatch '^\s*(public|private|protected|internal)\b[^=;]*\(' -or $src[$i] -match '\b(class|record|struct|interface)\b') { continue }
    $name = if ($src[$i] -match '(\w+)\s*(<[^>]*>)?\s*\(') { $Matches[1] } else { '?' }
    $depth = 0; $started = $false; $cc = 1; $hit = $false; $j = $i
    for (; $j -lt $src.Count; $j++) { $t = $src[$j] -replace '"(\\.|[^"\\])*"', '""' -replace '//.*$', ''
      if ($set.Contains($j + 1)) { $hit = $true }
      $cc += ([regex]::Matches($t, '\bif\b|\bcase\b|\bfor\b|\bforeach\b|\bwhile\b|\bcatch\b|&&|\|\||\?\?|\s\?\s|=>(?!\s*$)')).Count
      if ($j -gt $i -and $t -match '=>') { }  # switch arms counted above
      $depth += ([regex]::Matches($t, '\{')).Count - ([regex]::Matches($t, '\}')).Count
      if ($t -match '\{') { $started = $true }
      if (($started -and $depth -le 0) -or (-not $started -and $t -match ';\s*$')) { break } }
    if ($src[$i] -match '=>') { $cc -= 1 }  # the expression-body arrow itself
    if ($hit -and $cc -gt 5) { "CRAP_RISK ${f}:$($i + 1) $name est. CC=$cc (CI fails > 6): move new branches into a private helper" }
    $i = [Math]::Max($i, $j)
  }
}
