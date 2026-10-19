# v40g: local JetBrains InspectCode (Qodana's .NET engine) on changed .cs files; reports only findings on added/changed lines.
# Round 26: Qodana REDs found only in CI (c40 x2 RedundantTypeArgumentsOfMethod, c34 '??' on non-nullable, c25/c26) cost a
# 7-min CI cycle + Wait + resubmit each. ~45 s per run; first run installs the tool (~1.5 min).
# Prints FIX: lines + INSPECT=FIX, INSPECT=OK, or INSPECT=SKIPPED <reason>. Never fails the caller on tool problems.
$ErrorActionPreference = 'Continue'
Set-Location (git rev-parse --show-toplevel)
$env:PATH = "$HOME/.dotnet/tools$([IO.Path]::PathSeparator)$env:PATH"
$files = @(git status --porcelain -uall -- src | ForEach-Object { $_.Substring(3) } | Where-Object { $_ -like '*.cs' -and $_ -notmatch '[/\\]Generated[/\\]' -and (Test-Path $_) })
if (-not $files) { 'INSPECT=SKIPPED no changed .cs'; return }
if (-not (Get-Command jb -ErrorAction SilentlyContinue)) {
  dotnet tool install -g JetBrains.ReSharper.GlobalTools 2>&1 | Out-Null
  if (-not (Get-Command jb -ErrorAction SilentlyContinue)) { 'INSPECT=SKIPPED tool install failed'; return } }
# changed line numbers per file (untracked/new = every line)
$added = @{}; $usingAdded = @{}
foreach ($f in $files) {
  $set = [Collections.Generic.HashSet[int]]::new()
  if (git ls-files -- $f) {
    foreach ($h in git diff -U0 HEAD -- $f | Select-String '^@@ .*\+(\d+)(?:,(\d+))? @@') { $s = [int]$h.Matches[0].Groups[1].Value; $n = if ($h.Matches[0].Groups[2].Success) { [int]$h.Matches[0].Groups[2].Value } else { 1 }; for ($i = $s; $i -lt $s + $n; $i++) { [void]$set.Add($i) } }
  } else { 1..(@(Get-Content $f).Count + 1) | ForEach-Object { [void]$set.Add($_) } }
  $added[($f -replace '\\', '/')] = $set
  # vFinal: an added `using` can make untouched fully-qualified names redundant (round 33 c89: CI RED on lines 677/681)
  if ((git ls-files -- $f) -and (git diff -U0 HEAD -- $f | Select-String '^\+using ')) { $usingAdded[($f -replace '\\', '/')] = $true } }
$out = Join-Path ([IO.Path]::GetTempPath()) 'agent-inspect.sarif'; Remove-Item $out -ErrorAction SilentlyContinue
$inc = ($files | ForEach-Object { '**/' + (Split-Path $_ -Leaf) }) -join ';'
jb inspectcode src/ChurchBulletin.sln "--include=$inc" --format=Sarif "--output=$out" --no-build --severity=SUGGESTION 2>&1 | Out-Null
if (-not (Test-Path $out)) { 'INSPECT=SKIPPED inspectcode produced no report'; return }
$sarif = Get-Content $out -Raw | ConvertFrom-Json
$fix = foreach ($r in $sarif.runs[0].results) {
  $loc = $r.locations[0].physicalLocation; $uri = ($loc.artifactLocation.uri -replace '\\', '/'); $line = [int]$loc.region.startLine
  $key = $added.Keys | Where-Object { $_ -eq "src/$uri" -or $_ -eq $uri -or $_.EndsWith("/$uri") } | Select-Object -First 1
  if ($key -and ($added[$key].Contains($line) -or ($usingAdded[$key] -and $r.ruleId -eq 'RedundantNameQualifier'))) { "FIX: ${key}:$line $($r.ruleId): $($r.message.text)" } }
$fix = @($fix | Select-Object -Unique)
if ($fix) { $fix | Select-Object -First 15; "INSPECT=FIX ($($fix.Count) Qodana-style findings on your lines; CI fails on them)" } else { 'INSPECT=OK' }
