# General lean context: issue body + every src/ path named in it, printed compactly, in ONE call.
# Small files are printed whole (comments/blank lines stripped). Large files print the header, member signatures,
# and the full body of any member named in -Focus. Test files also get one complete example test as the house pattern.
param([Parameter(Mandatory)][int]$Issue, [string[]]$Focus = @(), [int]$WholeFileMaxLines = 80,
      [string]$Repo = 'ClearMeasureLabs/bootcamp-palermo-workorders')
$ErrorActionPreference = 'Stop'
$j = gh issue view $Issue --repo $Repo --json title,body | ConvertFrom-Json
"TITLE=$($j.title)"
$body = ($j.body -split "`n" | Where-Object { $_ -notmatch 'token-reduction experiment|closed without merging' }) -join "`n"
"--- ISSUE"; $body.Trim()
$paths = [regex]::Matches($j.body, 'src/[\w\./-]+\.(cs|razor)') | ForEach-Object Value | Select-Object -Unique
# Bare type names in backticks (e.g. `WorkOrderMap`) resolve to files too.
$names = [regex]::Matches($j.body, '`([A-Z]\w+)(\.\w+)?(\(.*?\))?`') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
foreach ($n in $names) {
  $f = Get-ChildItem -Recurse src -Filter "$n.cs" -ErrorAction SilentlyContinue | Where-Object FullName -notmatch '[\\/](bin|obj)[\\/]' | Select-Object -First 1
  if ($f) { $paths += $f.FullName.Substring((Get-Location).Path.Length + 1).Replace('\', '/') }
}
# Add the matching unit-test file for every named production file.
foreach ($p in @($paths)) {
  if ($p -match 'Tests\.cs$') { continue }
  $t = Get-ChildItem -Recurse src/UnitTests -Filter "$([IO.Path]::GetFileNameWithoutExtension($p))Tests.cs" -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($t) { $paths += $t.FullName.Substring((Get-Location).Path.Length + 1).Replace('\', '/') }
}
foreach ($p in ($paths | Select-Object -Unique)) {
  if (-not (Test-Path $p)) { "--- FILE $p (NEW: does not exist yet)"; continue }
  $lines = Get-Content $p | Where-Object { $_.Trim() -and $_.Trim() -notmatch '^(//|///)' }
  if ($lines.Count -le $WholeFileMaxLines) { "--- FILE $p"; $lines; continue }
  "--- FILE $p ($($lines.Count) lines; outline)"
  $lines | Where-Object { $_ -match '^(using|namespace|\[|public|internal|private|protected|\s{4}(public|private|protected|internal|\[Test))' -and $_ -notmatch '^\s{8}' }
  $text = $lines -join "`n"
  foreach ($m in $Focus) {
    $mm = [regex]::Match($text, "(?ms)^(\s*)[^\n]*\b$m\b[^\n]*\n\1\{.*?\n\1\}")
    if ($mm.Success) { "--- FOCUS $m"; $mm.Value }
  }
  if ($p -match 'Tests\.cs$') {
    $all = [regex]::Matches($text, '(?ms)^(\s*)\[Test\]\s*\n\s*public (async Task|void) [^\n]*\n\1\{.*?\n\1\}')
    if ($all.Count) { '--- LAST TEST (house pattern; insert new tests right after it, before the class closing brace)'; $all[$all.Count - 1].Value }
  }
}
'--- NEXT: make the edits with your editor tool (do not re-read files), then run Complete-AgentItem.ps1.'
