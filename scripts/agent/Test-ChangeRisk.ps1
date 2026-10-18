# v15g: local CI-trap check on the workspace src diff (catches what caused past CI REDs, before pushing).
#  CRAP_RISK: a changed method whose estimated cyclomatic complexity grew vs the base version AND is > 5 (CI fails > 6).
#             Methods that were already complex but did not get more complex are not flagged (round 1 r01 false positive).
#  QODANA_RISK: added `new[] {`/`new List<..> {` (use [a, b]); new options { get; set; } without the ReSharper line.
#  NUMBER_RISK: string literal > 7 chars assigned to a work order Number in tests.
$ErrorActionPreference = 'Stop'
Set-Location (git rev-parse --show-toplevel)  # v16g: was $PSScriptRoot/../.. which is "/" when run from the temp copy (round 2: every run broke)
function Get-MethodCc([string[]]$src) {
  for ($i = 0; $i -lt $src.Count; $i++) {
    if ($src[$i] -notmatch '^\s*(public|private|protected|internal)\b[^=;]*\(' -or $src[$i] -match '\b(class|record|struct|interface)\b') { continue }
    $name = if ($src[$i] -match '(\w+)\s*(<[^>]*>)?\s*\(') { $Matches[1] } else { '?' }
    $depth = 0; $started = $false; $cc = 1; $j = $i
    for (; $j -lt $src.Count; $j++) { $t = $src[$j] -replace '"(\\.|[^"\\])*"', '""' -replace '//.*$', ''
      $cc += ([regex]::Matches($t, '\bif\b|\bcase\b|\bfor\b|\bforeach\b|\bwhile\b|\bcatch\b|&&|\|\||\?\?|\s\?\s|=>(?!\s*$)')).Count
      # v25g: lambdas (e => e.X, () => ..., (a, b) => ...) are separate methods for CRAP; only switch arms count (round 11 t07 EF map false CRAP_RISK)
      $cc -= ([regex]::Matches($t, '(\b[a-z_]\w*|\))\s*=>(?!\s*$)')).Count
      $depth += ([regex]::Matches($t, '\{')).Count - ([regex]::Matches($t, '\}')).Count
      if ($t -match '\{') { $started = $true }
      if (($started -and $depth -le 0) -or (-not $started -and $t -match ';\s*$')) { break } }
    if ($src[$i] -match '=>') { $cc -= 1 }
    [pscustomobject]@{ Name = $name; Start = $i; End = $j; Cc = $cc }
    $i = [Math]::Max($i, $j)
  }
}
function Test-ImplicitUsings([string]$f) { $d = Split-Path $f
  while ($d) { $p = @(Get-ChildItem $d -Filter *.csproj -ErrorAction SilentlyContinue); if ($p) { return (Get-Content $p[0].FullName -Raw) -match 'ImplicitUsings>enable' }; $d = Split-Path $d }
  $false }
git add -A -N src 2>$null | Out-Null
$base = if (git rev-parse -q --verify origin/analysis/token-reduction 2>$null) { 'origin/analysis/token-reduction' } else { 'origin/master' }
$changed = @{}; $file = $null
foreach ($l in git diff -U0 $base -- src) {
  if ($l -match '^\+\+\+ b/(.+)$') { $file = $Matches[1]; $changed[$file] = @(); continue }
  if ($l -match '^@@ .*\+(\d+)(?:,(\d+))? @@') { $s = [int]$Matches[1]; $c = if ($Matches[2]) { [int]$Matches[2] } else { 1 }; if ($c) { $changed[$file] += $s..($s + $c - 1) }; continue }
  if ($l -match '^\+(?!\+\+)(.*)$' -and $file) { $a = $Matches[1]
    if ($file -match 'Tests' -and $a -match '\.(Click|Change|Input|Submit)\(') { "QODANA_RISK $file : use the async bUnit overload (ClickAsync/ChangeAsync/...) and await it: $($a.Trim())" }  # v18g: round 4 u03
    if ($a -match 'new\s*(\w+)?\[\]\s*\{|new List<[^>]+>\s*\{') { "QODANA_RISK $file : use a collection expression [a, b]: $($a.Trim())" }
    # v27g: round 12 CI REDs: s02 RedundantNameQualifier (new Microsoft.AspNetCore.Components.Web.MouseEventArgs()), s05 RedundantUsingDirective (using System.Linq; with ImplicitUsings)
    if ($a -notmatch '^\s*(using|namespace)\b' -and $a -match '\b(System|Microsoft)\.[A-Z]\w*\.[\w.]*[A-Z]\w*\s*[(<{]') { "QODANA_RISK $file : add a using and use the short type name (RedundantNameQualifier): $($a.Trim())" }
    if ($a -match '^\s*using (System|System\.(Linq|IO|Collections\.Generic|Net\.Http|Threading|Threading\.Tasks));' -and (Test-ImplicitUsings $file)) { "QODANA_RISK $file : remove '$($a.Trim())' (implicit usings already cover it)" }
    # v28g: whole-word Number, not PhoneNumber (round 13 r07 false NUMBER_RISK)
    if ($file -match 'Tests' -and $a -match '(?<!\w)Number\s*=\s*"([^"]{8,})"') { "NUMBER_RISK $file : work order Number '$($Matches[1])' > 7 chars" } }
}
foreach ($f in $changed.Keys) {
  if ($f -notmatch '\.cs$' -or $f -match 'Tests' -or -not (Test-Path $f)) { continue }
  $src = @(Get-Content $f); $set = [Collections.Generic.HashSet[int]]::new([int[]]$changed[$f])
  if ($f -match 'Options\.cs$') { for ($i = 0; $i -lt $src.Count; $i++) {
    if ($set.Contains($i + 1) -and $src[$i] -match 'public .*\{ get; set; \}' -and ($i -eq 0 -or $src[$i - 1] -notmatch 'ReSharper disable')) {
      "QODANA_RISK ${f}:$($i + 1) : add // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global above it" } } }
  # v23g: new settable entity property only set in initializers -> Qodana PropertyCanBeMadeInitOnly.Global (round 9 w07 CI RED)
  if ($f -match 'Core[/\\]Model[/\\]' -and -not ($src -match 'ReSharper disable PropertyCanBeMadeInitOnly')) { for ($i = 0; $i -lt $src.Count; $i++) {
    if ($set.Contains($i + 1) -and $src[$i] -match 'public .*\{ get; set; \}' -and ($i -eq 0 -or $src[$i - 1] -notmatch 'ReSharper disable')) {
      "QODANA_RISK ${f}:$($i + 1) : add // ReSharper disable once PropertyCanBeMadeInitOnly.Global above it (EF needs the setter)" } } }
  $baseCc = @{}; $baseSrc = @(git show "${base}:$f" 2>$null)
  foreach ($m in Get-MethodCc $baseSrc) { if (-not $baseCc.ContainsKey($m.Name) -or $m.Cc -gt $baseCc[$m.Name]) { $baseCc[$m.Name] = $m.Cc } }
  foreach ($m in Get-MethodCc $src) {
    $hit = $false; for ($k = $m.Start; $k -le $m.End; $k++) { if ($set.Contains($k + 1)) { $hit = $true; break } }
    $was = if ($baseCc.ContainsKey($m.Name)) { $baseCc[$m.Name] } else { 0 }
    if ($hit -and $m.Cc -gt 5 -and $m.Cc -gt $was) { "CRAP_RISK ${f}:$($m.Start + 1) $($m.Name) est. CC $was -> $($m.Cc) (CI fails > 6): move the new branches into a private helper" }
  }
}
