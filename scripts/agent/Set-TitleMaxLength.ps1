# Canned recipe: set the WorkOrder Title max length end to end (domain constant, EF map, UI model, edit-screen
# maxlength, command validator, test data, integration tests, new DbUp script with auto-numbering, unit + bUnit tests).
# Idempotent. Prints one line per file touched.
param([int]$Max = 1000)
$ErrorActionPreference = 'Stop'
function Edit([string]$Path, [scriptblock]$Fn) {
  $bytes = [IO.File]::ReadAllBytes((Resolve-Path $Path))
  $bom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
  $old = [IO.File]::ReadAllText((Resolve-Path $Path)); $new = & $Fn $old
  if ($new -ne $old) { [IO.File]::WriteAllText((Resolve-Path $Path), $new, [Text.UTF8Encoding]::new($bom)); "EDIT $Path" } else { "SAME $Path" }
}
Edit src/Core/Model/WorkOrder.cs {
  param($s)
  if ($s -match 'TitleMaxLength = \d+') { return $s -replace 'TitleMaxLength = \d+', "TitleMaxLength = $Max" }
  $s -replace '(\s*/// <summary>\s*\r?\n\s*/// Maximum length of <see cref="RoomNumber"/>)', "`n    /// <summary>`n    /// Maximum length of <see cref=`"Title`"/> accepted by persistence and the work-order form.`n    /// </summary>`n    public const int TitleMaxLength = $Max;`n`$1"
}
Edit src/DataAccess/Mappings/WorkOrderMap.cs { param($s) $s -replace 'Property\(e => e\.Title\)\.IsRequired\(\)\.HasMaxLength\([^)]*\)', 'Property(e => e.Title).IsRequired().HasMaxLength(WorkOrder.TitleMaxLength)' }
Edit src/UI.Shared/Models/WorkOrderManageModel.cs {
  param($s)
  if ($s -match 'TitleMaxLength') { return $s }
  $s -replace '\[Required\] public string\? Title \{ get; set; \}', "[Required]`n    [StringLength(WorkOrder.TitleMaxLength, ErrorMessage = `"Title cannot exceed $Max characters.`")]`n    public string? Title { get; set; }"
}
Edit src/UI.Shared/Pages/WorkOrderManage.razor {
  param($s)
  if ($s -match 'id="Title"[^>]*maxlength') { return $s }
  $s -replace '(<InputText data-testid="@Elements\.Title" id="Title" @bind-Value="Model\.Title" class="form-control input-text")', '$1 maxlength="@WorkOrder.TitleMaxLength"'
}
Edit src/Core/Validation/CreateDatedWorkOrdersCommandValidator.cs {
  param($s)
  if ($s -match 'Title\)\.NotEmpty\(\)\.MaximumLength') { return $s }
  $s = $s -replace 'RuleFor\(x => x\.Title\)\.NotEmpty\(\);', 'RuleFor(x => x.Title).NotEmpty().MaximumLength(WorkOrder.TitleMaxLength);'
  if ($s -notmatch 'using ClearMeasure\.Bootcamp\.Core\.Model;') { $s = "using ClearMeasure.Bootcamp.Core.Model;`n" + $s }
  $s
}
Edit src/UnitTests/BogusOverrides.cs { param($s) $s -replace 'order\.Title = order\.Title\.ClampLength\(1, \d+\);[^\r\n]*', 'order.Title = order.Title.ClampLength(1, WorkOrder.TitleMaxLength);' }
Edit src/IntegrationTests/DataAccess/Mappings/WorkOrderMappingTests.cs {
  param($s)
  $s -replace "Title = new string\('B', 301\),[^\r\n]*", "Title = new string('B', WorkOrder.TitleMaxLength + 1)," `
     -replace "Title = new string\('B', 300\),", "Title = new string('B', WorkOrder.TitleMaxLength)," `
     -replace 'workOrder\.Title\.Length\.ShouldBe\(300\);', 'workOrder.Title.Length.ShouldBe(WorkOrder.TitleMaxLength);'
}
# DbUp script: next number, never edit existing scripts.
$dir = 'src/Database/scripts/Update'
$existing = Get-ChildItem $dir -Filter '*_ExtendWorkOrderTitleLengthTo*.sql' | Where-Object { (Get-Content $_.FullName -Raw) -match "NVARCHAR\($Max\)" }
if ($existing) { "SAME $($existing[0].FullName)" } else {
  $next = [int](Get-ChildItem $dir -Filter '*.sql' | ForEach-Object { [int]($_.Name -replace '^(\d+)[_-].*', '$1') } | Measure-Object -Maximum).Maximum + 1
  $prev = (Get-ChildItem $dir -Filter '*.sql' | Sort-Object Name | ForEach-Object { Get-Content $_.FullName -Raw } | Select-String -Pattern '\[Title\] NVARCHAR\((\d+)\)' -AllMatches | ForEach-Object { $_.Matches } | Select-Object -Last 1).Groups[1].Value
  $file = Join-Path $dir ('{0:D3}_ExtendWorkOrderTitleLengthTo{1}.sql' -f $next, $Max)
  @"
BEGIN TRANSACTION
GO
PRINT N'Altering [dbo].[WorkOrder] Title column from NVARCHAR($prev) to NVARCHAR($Max)'
GO
	ALTER TABLE [dbo].[WorkOrder] ALTER COLUMN [Title] NVARCHAR($Max) NOT NULL
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
PRINT 'The database update succeeded'
COMMIT TRANSACTION
GO
"@ | Set-Content $file
  "NEW  $file"
}
# Unit tests (model validation) + bUnit (maxlength attribute), derived from the RoomNumber precedents.
$mt = 'src/UnitTests/UI.Shared/Models/WorkOrderManageModelTitleTests.cs'
if (-not (Test-Path $mt)) {
  @"
using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelTitleTests
{
    [Test]
    public void ShouldAcceptTitleAtMaxLength()
    {
        var model = new WorkOrderManageModel { Title = new string('T', WorkOrder.TitleMaxLength), Description = "Description" };
        Validate(model).ShouldBeEmpty();
    }

    [Test]
    public void ShouldRejectTitleLongerThanMaxLength()
    {
        var model = new WorkOrderManageModel { Title = new string('T', WorkOrder.TitleMaxLength + 1), Description = "Description" };
        Validate(model).ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.Title)));
    }

    [Test]
    public void TitleMaxLength_ShouldBe$Max()
    {
        WorkOrder.TitleMaxLength.ShouldBe($Max);
    }

    private static List<ValidationResult> Validate(WorkOrderManageModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, true);
        return results;
    }
}
"@ | Set-Content $mt; "NEW  $mt"
}
$bt = 'src/UnitTests/UI.Shared/Pages/WorkOrderManageTitleFieldTests.cs'
if (-not (Test-Path $bt)) {
  (Get-Content src/UnitTests/UI.Shared/Pages/WorkOrderManageRoomFieldTests.cs -Raw) `
    -replace 'WorkOrderManageRoomFieldTests', 'WorkOrderManageTitleFieldTests' `
    -replace 'WorkOrderManage_ShouldRenderRoomAsTextareaWithMaxLength900', "WorkOrderManage_ShouldRenderTitleWithMaxLength$Max" `
    -replace 'Elements\.RoomNumber', 'Elements.Title' `
    -replace 'room\.TagName\.ShouldBe\("TEXTAREA"\);', 'room.TagName.ShouldBe("INPUT");' `
    -replace 'RoomNumberMaxLength', 'TitleMaxLength' `
    -replace '"WO-ROOM"', '"WO-TITLE"' | Set-Content $bt -NoNewline
  "NEW  $bt"
}
