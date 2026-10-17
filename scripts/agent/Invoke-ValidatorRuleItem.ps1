# Canned recipe for "add a FluentValidation rule + unit tests". It chains the rule onto RuleFor(x => x.<Property>), writes or extends the
# validator's test file with one passing and one failing case, then builds, tests and publishes in one call.
# Example: -Validator UserLoggedInEventValidator -Property UserName -Rule 'MaximumLength(100)' -New 'new UserLoggedInEvent({0})'
#          -Valid '"user"' -Invalid 'new string(''a'', 101)'
param([Parameter(Mandatory)][int]$Issue, [Parameter(Mandatory)][string]$Validator, [Parameter(Mandatory)][string]$Property,
      [Parameter(Mandatory)][string]$Rule, [Parameter(Mandatory)][string]$New, [Parameter(Mandatory)][string]$Valid,
      [Parameter(Mandatory)][string]$Invalid, [Parameter(Mandatory)][string]$Branch, [switch]$NoPublish)
$ErrorActionPreference = 'Stop'
$src = Get-ChildItem -Recurse src -Filter "$Validator.cs" | Where-Object FullName -notmatch '[\\/](bin|obj|UnitTests)[\\/]' | Select-Object -First 1
if (-not $src) { throw "validator $Validator not found" }
$code = Get-Content $src.FullName -Raw
$ruleName = ($Rule -replace '\(.*$', '')
if ($code -notmatch [regex]::Escape(".$Rule")) {
  $rx = "(RuleFor\(\s*\w+\s*=>\s*\w+\.$Property\s*\)[^;]*);"
  $m = [regex]::Match($code, $rx); if (-not $m.Success) { throw "RuleFor(x => x.$Property) not found" }
  $code = $code.Remove($m.Index, $m.Length).Insert($m.Index, "$($m.Groups[1].Value).$Rule;")
  Set-Content $src.FullName $code -NoNewline
}
$usings = (Get-Content $src.FullName | Where-Object { $_ -match '^using ClearMeasure' }) -join "`n"
$ns = ([regex]::Match($code, 'namespace\s+([\w\.]+)')).Groups[1].Value
$testNs = 'ClearMeasure.Bootcamp.UnitTests.' + ($ns -replace '^ClearMeasure\.Bootcamp\.', '')
$testPath = "src/UnitTests/" + (($ns -replace '^ClearMeasure\.Bootcamp\.', '') -replace '\.', '/') + "/${Validator}Tests.cs"
$validNew = $New -f $Valid; $invalidNew = $New -f $Invalid
$tests = @"
    [Test]
    public void ShouldPass_${Property}_${ruleName}_WhenValid()
    {
        new $Validator().TestValidate($validNew).ShouldNotHaveValidationErrorFor(x => x.$Property);
    }

    [Test]
    public void ShouldFail_${Property}_${ruleName}_WhenInvalid()
    {
        new $Validator().TestValidate($invalidNew).ShouldHaveValidationErrorFor(x => x.$Property);
    }
"@
if (-not (Test-Path $testPath)) {
  New-Item -ItemType Directory -Force (Split-Path $testPath) | Out-Null
  Set-Content $testPath "$usings`nusing $ns;`nusing FluentValidation.TestHelper;`n`nnamespace $testNs;`n`n[TestFixture]`npublic class ${Validator}Tests`n{`n$tests`n}`n" -NoNewline
} elseif ((Get-Content $testPath -Raw) -notmatch "ShouldFail_${Property}_${ruleName}_") {
  $t = Get-Content $testPath -Raw; $i = $t.LastIndexOf('}')
  Set-Content $testPath ($t.Substring(0, $i).TrimEnd() + "`n`n$tests`n}`n") -NoNewline
}
"EDITED=$($src.Name),$testPath"
if ($NoPublish) { & "$PSScriptRoot/Invoke-AgentBuildTest.ps1" -Filter "FullyQualifiedName~${Validator}Tests"; exit $LASTEXITCODE }
& "$PSScriptRoot/Complete-AgentItem.ps1" -Issue $Issue -Type $Validator -Branch $Branch
