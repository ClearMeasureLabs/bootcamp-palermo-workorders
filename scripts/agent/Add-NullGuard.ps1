# Canned edit for the "guard null parameter + unit test" work type. Idempotent.
param([Parameter(Mandatory)][string]$Type, [string]$Method = 'Execute', [string]$Param = 'context', [string]$ArgType = 'StateCommandContext',
      [string]$Ctor)
$ErrorActionPreference = 'Stop'
$src = Get-ChildItem -Recurse src -Filter "$Type.cs" | Where-Object FullName -notmatch '[\\/](bin|obj)[\\/]' | Select-Object -First 1
$test = Get-ChildItem -Recurse src/UnitTests -Filter "$Type*Tests.cs" | Select-Object -First 1
if (-not $src -or -not $test) { throw "SRC or TEST not found for $Type" }
$code = Get-Content $src.FullName -Raw
$guard = "ArgumentNullException.ThrowIfNull($Param);"
if ($code -notmatch [regex]::Escape($guard)) {
  $rx = "(\b$Method\s*\([^)]*\b$Param\)\s*\r?\n(\s*)\{\r?\n)"
  $m = [regex]::Match($code, $rx); if (-not $m.Success) { throw "$Method($Param) not found in $($src.Name)" }
  $indent = $m.Groups[2].Value + '    '
  $code = $code.Remove($m.Index + $m.Length, 0).Insert($m.Index + $m.Length, "$indent$guard`n`n")
  Set-Content $src.FullName $code -NoNewline
}
$t = Get-Content $test.FullName -Raw
$name = "${Method}ShouldThrowWhen$([char]::ToUpper($Param[0]) + $Param.Substring(1))IsNull"
if ($t -notmatch $name) {
  if (-not $Ctor) { $Ctor = "new $Type(new WorkOrder { Creator = new Employee() }, new Employee())" }
  $body = @"

    [Test]
    public void $name()
    {
        var command = $Ctor;
        var ex = Assert.Throws<ArgumentNullException>(() => command.$Method(null!));
        Assert.That(ex!.ParamName, Is.EqualTo("$Param"));
    }
}
"@
  $i = $t.LastIndexOf('}'); $t = $t.Substring(0, $i).TrimEnd() + "`n" + $body.TrimStart("`r","`n").Insert(0, "`n") + "`n"
  Set-Content $test.FullName $t -NoNewline
}
"EDITED=$($src.Name),$($test.Name)"
