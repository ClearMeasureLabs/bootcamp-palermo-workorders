using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

public class EditModeExtensionsTests
{
    [Test]
    public void ToSubmitLabel_New_ShouldReturnCreate()
    {
        EditMode.New.ToSubmitLabel().ShouldBe("Create");
    }

    [Test]
    public void ToSubmitLabel_Edit_ShouldReturnSave()
    {
        EditMode.Edit.ToSubmitLabel().ShouldBe("Save");
    }
}
