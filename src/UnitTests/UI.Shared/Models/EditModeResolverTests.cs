using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class EditModeResolverTests
{
    [TestCase("edit")]
    [TestCase("Edit")]
    [TestCase("EDIT")]
    [TestCase("eDiT")]
    public void FromQuery_WhenModeIsEditIgnoreCase_ShouldReturnEdit(string mode)
    {
        EditModeResolver.FromQuery(mode).ShouldBe(EditMode.Edit);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("new")]
    [TestCase("view")]
    public void FromQuery_WhenModeIsNotEdit_ShouldReturnNew(string? mode)
    {
        EditModeResolver.FromQuery(mode).ShouldBe(EditMode.New);
    }
}
