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
$base = 'HEAD'  # v43g: the agent's own changes only (agent VMs may lack origin/analysis/token-reduction; master differs from the skill tip)
$changed = @{}; $file = $null; $prevA = ''; $lenProps = @{}
foreach ($l in git diff -U0 $base -- src ':(exclude)src/**/Generated/**') {  # v33g: generated code is not ours (round 19 s08)
  if ($l -match '^\+\+\+ b/(.+)$') { $file = $Matches[1]; $changed[$file] = @(); $prevA = ''; continue }
  if ($l -match '^@@ .*\+(\d+)(?:,(\d+))? @@') { $s = [int]$Matches[1]; $c = if ($Matches[2]) { [int]$Matches[2] } else { 1 }; if ($c) { $changed[$file] += $s..($s + $c - 1) }; continue }
  if ($l -match '^\+(?!\+\+)(.*)$' -and $file) { $a = $Matches[1]
    if ($file -match 'Tests' -and $a -match '\.(Click|Change|Input|Submit)\(') { "QODANA_RISK $file : use the async bUnit overload (ClickAsync/ChangeAsync/...) and await it: $($a.Trim())" }  # v18g: round 4 u03
    if ($a -match 'new\s*(\w+)?\[\]\s*\{|new List<[^>]+>\s*\{') { "QODANA_RISK $file : use a collection expression [a, b]: $($a.Trim())" }
    # v27g: round 12 CI REDs: s02 RedundantNameQualifier (new Microsoft.AspNetCore.Components.Web.MouseEventArgs()), s05 RedundantUsingDirective (using System.Linq; with ImplicitUsings)
    if ($a -notmatch '^\s*(using|namespace)\b' -and $a -match '\b(System|Microsoft)\.[A-Z]\w*\.[\w.]*[A-Z]\w*\s*[(<{]') { "QODANA_RISK $file : add a using and use the short type name (RedundantNameQualifier): $($a.Trim())" }
    if ($a -match '^\s*using (System|System\.(Linq|IO|Collections\.Generic|Net\.Http|Threading|Threading\.Tasks));' -and (Test-ImplicitUsings $file)) { "QODANA_RISK $file : remove '$($a.Trim())' (implicit usings already cover it)" }
    # v30g: round 16 CI RED (RedundantUsingDirective): added using of a repo namespace with none of its types used in the file
    if ($a -match '^\s*using ([A-Z][\w.]+);' -and (Test-Path $file)) { $ns = $Matches[1]
      $decl = @(rg -l --glob '*.cs' -e "^namespace $([regex]::Escape($ns))\s*[;{]" src 2>$null)
      if ($decl) { $types = @($decl | ForEach-Object { [regex]::Matches((Get-Content $_ -Raw), '\b(?:class|record|interface|enum|struct)\s+(\w+)') | ForEach-Object { $_.Groups[1].Value } } | Sort-Object -Unique)
        $body = (Get-Content $file | Where-Object { $_ -notmatch '^\s*(using|namespace)\b' }) -join "`n"
        if ($types -and -not ($types | Where-Object { $body -match "\b$_\b" } | Select-Object -First 1)) { "QODANA_RISK $file : '$($a.Trim())' looks unused (no type from $ns appears); remove it (RedundantUsingDirective)" } } }
    # v31g: round 17 CI REDs (q02, q05, q07) from Qodana rules not checked before
    if ($prevA -match '^\s*var (\w+) = new [\w<>.,\s]+\(\);' -and $a -match "^\s*$($Matches[1])\.\w+\s*=[^=]") { "QODANA_RISK $file : use an object initializer (new T { Prop = ... }) instead of assigning after construction (UseObjectOrCollectionInitializer): $($a.Trim())" }
    if ($a -match '"[^"]*\\x[0-9A-Fa-f]') { "QODANA_RISK $file : use a fixed-length \uXXXX escape, not \x (VariableLengthStringHexEscapeSequence): $($a.Trim())" }
    if ($a -match 'Encoding\.UTF8\.GetBytes\(\s*"') { "QODANA_RISK $file : use a UTF-8 literal (`"...`"u8) (UseUtf8StringLiteral): $($a.Trim())" }
    if ($a -match '^\s*(Func|Action)(<[^=]+>)?\s+\w+\s*=\s*(async\s+)?(\([^)]*\)|\w+)\s*=>') { "QODANA_RISK $file : use a local function instead of a lambda in a Func/Action local (ConvertToLocalFunction): $($a.Trim())" }
    # v34g: round 20 t04 CI RED in all 3 arms: `Name ?? ""` / null check on a member declared non-nullable
    #   (NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract, ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract)
    foreach ($nm in [regex]::Matches($a, '\b([A-Z]\w*)\s*(?:\?\?(?!=)|[!=]=\s*null\b|is\s+(?:not\s+)?null\b)')) { $pn = $nm.Groups[1].Value
      $dre = "^\s*(public|internal|protected|private)\s+(required\s+|virtual\s+|override\s+)*[\w<>.,]+\??\s+$pn\s*(\{|=|;)"
      $dl = @(Get-Content $file | Where-Object { $_ -match $dre })  # same file first; else every declaration in src must agree
      if (-not $dl) { $dl = @(rg -N --glob '*.cs' --glob '!**/*Tests*/**' --glob '!**/Generated/**' -e $dre src 2>$null) }
      if ($dl -and -not ($dl | Where-Object { $_ -match "\?\s+$pn\b" }) -and ($dl | Where-Object { $_ -match "\b(string|[A-Z]\w*(<[^>]+>)?)\s+$pn\b" })) {
        "QODANA_RISK $file : '$pn' is declared non-nullable, so '??'/null checks on it fail Qodana (NullCoalescingConditionIsAlwaysNotNull...): drop the check, or make the declaration nullable ('string? $pn') if null must be handled: $($a.Trim())" } }
    # v34g: round 20 C t03 CI RED x2: private positional test records -> NotAccessedPositionalProperty.Local / PropertyCanBeMadeInitOnly.Local
    if ($file -match 'Tests' -and $a -match '^\s*(private|file)\s+(sealed\s+)?record\s+\w+\s*\(') { "QODANA_RISK $file : private positional records in tests trip NotAccessedPositionalProperty.Local; use an anonymous object (new { A = 1, B = (string?)null }) or a public record: $($a.Trim())" }
    if ($file -match '^src/DataAccess/' -and $a -match 'Property\(\s*\w+\s*=>\s*\w+\.(\w+)\s*\)[^;]*HasMaxLength\(([^)]+)\)') { $lenProps["$((Split-Path $file -Leaf) -replace '(Map|Mapping|Configuration)?\.cs$','').$($Matches[1])"] = "${file}: $($Matches[2])" }  # v42g; v43g: Entity.Prop
    # v28g: whole-word Number, not PhoneNumber (round 13 r07 false NUMBER_RISK)
    if ($file -match 'Tests' -and $a -match '(?<!\w)Number\s*=\s*"([^"]{8,})"') { "NUMBER_RISK $file : work order Number '$($Matches[1])' > 7 chars" }; $prevA = $a }
}
# v42g: SQLite ignores nvarchar(n), SQL Server CI truncates: a new max-length column needs a Bogus clamp (round 28 c51 CI RED
# 'String or binary data would be truncated ... PhoneNumber': AutoBogus made 22-char phone numbers)
$bogus = if (Test-Path src/UnitTests/BogusOverrides.cs) { Get-Content src/UnitTests/BogusOverrides.cs -Raw } else { '' }
# v43g: look only inside that entity's `case Entity x:` block (round 29 c59: WorkOrderAttachment.Description passed because the
# WorkOrder block sets order.Description)
foreach ($k in @($lenProps.Keys)) { $en, $p = $k -split '\.', 2
  $blk = if ($bogus -match "(?s)case\s+$en\b[^:]*:(.*?)(?=\bcase\s|\z)") { $Matches[1] } else { '' }
  if ($blk -notmatch "\.$p\s*=") { "LENGTH_RISK $($lenProps[$k]) : new max-length column '$p': clamp it in the 'case $en' block of src/UnitTests/BogusOverrides.cs (x.$p = x.$p.ClampLength(1, n), or null if optional; add the case if missing); SQL Server CI rejects longer random values, SQLite does not" } }
# v43g: an existing test file that shrinks a lot was probably overwritten (round 29 c60: WorkOrderMappingTests.cs +16 -403, green)
foreach ($ns in git diff --numstat $base -- src) { $add, $del, $tf = $ns -split "`t"
  if ($tf -notmatch 'Tests' -or $del -eq '-') { continue }
  $orig = @(git show "${base}:$tf" 2>$null).Count; $shrink = [int]$del - [int]$add
  if ($orig -and ($shrink -gt 100 -or $shrink -gt $orig / 2)) { "TEST_DELETE_RISK $tf : shrinks by $shrink lines (-$del +$add of $orig); existing tests were removed or the file was overwritten. Keep the existing tests and add yours (append after the last test)" } }
foreach ($f in $changed.Keys) {
  if ($f -notmatch '\.cs$' -or $f -match 'Tests' -or -not (Test-Path $f)) { continue }
  $src = @(Get-Content $f); $set = [Collections.Generic.HashSet[int]]::new([int[]]$changed[$f])
  # v33g: round 19 s04 CI RED PossibleMultipleEnumeration: an IEnumerable<T> parameter enumerated by 2+ LINQ calls
  foreach ($pm in [regex]::Matches(($src -join "`n"), 'IEnumerable<[\w<>, ?]+>\s+(\w+)\s*[,)]')) { $pn = $pm.Groups[1].Value
    $uses = @(for ($i = 0; $i -lt $src.Count; $i++) { if ($src[$i] -match "\b$pn\.(First|FirstOrDefault|Any|All|Where|Count|Select|Single|Last|ToList|ToArray|OrderBy)\w*\(") { $i + 1 } })
    if ($uses.Count -ge 2 -and ($uses | Where-Object { $set.Contains($_) })) { "QODANA_RISK ${f}:$($uses[0]) : '$pn' (IEnumerable) is enumerated $($uses.Count) times; materialize once (var list = $pn.ToList();) or take IReadOnlyList (PossibleMultipleEnumeration)" } }
  # v35g: round 21 u04 CI RED UseObjectOrCollectionInitializer: `x.Prop = ...` / `x.Dict["k"] = ...` right after `var x = new T...;`
  for ($i = 1; $i -lt $src.Count; $i++) {
    if (-not $set.Contains($i + 1) -or $src[$i] -notmatch '^\s*(\w+)\.\w+(\[[^\]]+\])?\s*=[^=]') { continue }; $vn = $Matches[1]
    $k = $i - 1; while ($k -gt 0 -and -not $src[$k].Trim()) { $k-- }
    if ($src[$k] -notmatch ';\s*$') { continue }
    $j = $k; while ($j -gt 0 -and ($k - $j) -lt 40 -and $src[$j] -notmatch "^\s*var\s+$vn\s*=\s*new\b" -and ($j -eq $k -or $src[$j] -notmatch ';\s*$')) { $j-- }
    if ($src[$j] -match "^\s*var\s+$vn\s*=\s*new\b" -and ($j -eq $k -or $src[$k] -match "^\s*\}\s*;")) { "QODANA_RISK ${f}:$($i + 1) : move into $vn's object initializer (Prop = v / Dict = { [`"k`"] = v }) instead of assigning after construction (UseObjectOrCollectionInitializer): $($src[$i].Trim())" } }
  if ($f -match 'Options\.cs$') { for ($i = 0; $i -lt $src.Count; $i++) {
    if ($set.Contains($i + 1) -and $src[$i] -match 'public .*\{ get; set; \}' -and ($i -eq 0 -or $src[$i - 1] -notmatch 'PropertyCanBeMadeInitOnly')) {
      "QODANA_RISK ${f}:$($i + 1) : put // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global, PropertyCanBeMadeInitOnly.Global -- required for IOptions<T> configuration binding on the line above (v31g: round 17 q02 CI RED PropertyCanBeMadeInitOnly.Global)" } } }
  # v23g: new settable entity property only set in initializers -> Qodana PropertyCanBeMadeInitOnly.Global (round 9 w07 CI RED)
  if ($f -match 'Core[/\\]Model[/\\]' -and -not ($src -match 'ReSharper disable PropertyCanBeMadeInitOnly')) { for ($i = 0; $i -lt $src.Count; $i++) {
    if ($set.Contains($i + 1) -and $src[$i] -match 'public .*\{ get; set; \}' -and ($i -eq 0 -or $src[$i - 1] -notmatch 'ReSharper disable')) {
      "QODANA_RISK ${f}:$($i + 1) : add // ReSharper disable once PropertyCanBeMadeInitOnly.Global above it (EF needs the setter)" } } }
  $baseCc = @{}; $baseSrc = @(git show "${base}:$f" 2>$null)
  foreach ($m in Get-MethodCc $baseSrc) { if (-not $baseCc.ContainsKey($m.Name) -or $m.Cc -gt $baseCc[$m.Name]) { $baseCc[$m.Name] = $m.Cc } }
  foreach ($m in Get-MethodCc $src) {
    $hit = $false; for ($k = $m.Start; $k -le $m.End; $k++) { if ($set.Contains($k + 1)) { $hit = $true; break } }
    $was = if ($baseCc.ContainsKey($m.Name)) { $baseCc[$m.Name] } else { 0 }
    if ($hit -and $m.Cc -gt 5 -and $m.Cc -gt $was) { "CRAP_RISK ${f}:$($m.Start + 1) $($m.Name) est. CC $was -> $($m.Cc) (CI fails > 6): keep the method's branch count: put the new condition in a private static helper and return early, e.g. 'if (ShouldBypass(x)) { return await next(ctx); }' with 'private static bool ShouldBypass(T x) => x.A && !x.B;' (no new if/else/?:/&&/|| in $($m.Name) beyond that one if)" }
  }
}
