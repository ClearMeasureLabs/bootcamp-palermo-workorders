# Generalized canned recipe: change the max length of a WorkOrder string field that already has a
# `public const int <Field>MaxLength` constant (e.g. RoomNumber, Description, Instructions). It bumps the constant,
# fixes the EF map if it still has a literal, updates the UI error message, updates unit tests that pin the old
# number, and adds the next-numbered DbUp script (nullability copied from the last script that altered the column).
# Idempotent; preserves the BOM. For Title (no constant yet) use Set-TitleMaxLength.ps1.
param([Parameter(Mandatory)][string]$Field, [Parameter(Mandatory)][int]$Max, [string]$Column = $Field)
$ErrorActionPreference = 'Stop'
function Edit([string]$Path, [scriptblock]$Fn) {
  $full = (Resolve-Path $Path).Path; $bytes = [IO.File]::ReadAllBytes($full)
  $bom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
  $text = [IO.File]::ReadAllText($full); $new = & $Fn $text
  if ($new -ne $text) { [IO.File]::WriteAllText($full, $new, [Text.UTF8Encoding]::new($bom)); "EDIT $Path" }
}
$model = 'src/Core/Model/WorkOrder.cs'
$m = [regex]::Match((Get-Content $model -Raw), "${Field}MaxLength = (\d+);")
if (-not $m.Success) { throw "No ${Field}MaxLength constant in $model" }
$old = [int]$m.Groups[1].Value
$dir = 'src/Database/scripts/Update'
$done = Get-ChildItem $dir -Filter '*.sql' | Where-Object { (Get-Content $_.FullName -Raw) -match "\[$Column\] NVARCHAR\($Max\)" }
if ($old -eq $Max -and $done) { "SAME ${Field}MaxLength=$Max"; return }
Edit $model { param($s) $s -replace "${Field}MaxLength = \d+;", "${Field}MaxLength = $Max;" }
Edit src/DataAccess/Mappings/WorkOrderMap.cs { param($s) $s -replace "(Property\(e => e\.$Field\)[^;]*?)HasMaxLength\(\d+\)", "`$1HasMaxLength(WorkOrder.${Field}MaxLength)" }
Get-ChildItem -Recurse src/UI.Shared, src/UnitTests, src/IntegrationTests, src/AcceptanceTests -Include *.cs, *.razor |
  Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and (Select-String -Path $_.FullName -Pattern "${Field}MaxLength" -Quiet) } |
  ForEach-Object {
    $rel = $_.FullName.Substring((Get-Location).Path.Length + 1)
    # Files that use the constant may pin the old number in names, messages and asserts (e.g. "900 char", "LongerThan900", 901).
    Edit $rel { param($s) $s -replace "(?<![\d_.])$($old + 1)(?![\d_])", "$($Max + 1)" -replace "(?<![\d_.])$old(?![\d_])", "$Max" }
  }
if (-not $done) {
  $last = Get-ChildItem $dir -Filter '*.sql' | Sort-Object Name | Where-Object { (Get-Content $_.FullName -Raw) -match "\[$Column\] NVARCHAR\(\d+\)" } | Select-Object -Last 1
  $null_ = if ($last -and (Get-Content $last.FullName -Raw) -match "\[$Column\] NVARCHAR\(\d+\) NOT NULL") { 'NOT NULL' } else { 'NULL' }
  $next = [int](Get-ChildItem $dir -Filter '*.sql' | ForEach-Object { [int]($_.Name -replace '^(\d+)[_-].*', '$1') } | Measure-Object -Maximum).Maximum + 1
  $file = Join-Path $dir ('{0:D3}_ExtendWorkOrder{1}LengthTo{2}.sql' -f $next, $Field, $Max)
  @"
BEGIN TRANSACTION
GO
PRINT N'Altering [dbo].[WorkOrder] $Column column from NVARCHAR($old) to NVARCHAR($Max)'
GO
	ALTER TABLE [dbo].[WorkOrder] ALTER COLUMN [$Column] NVARCHAR($Max) $null_
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
GO
"@ | Set-Content $file
  "NEW  $file"
}
