using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderDueDateStyleTests
{
    [Test]
    public void ShouldColorManageDueDateControlThroughDeepSelector()
    {
        var css = ReadScopedCss("WorkOrderManage.razor.css");

        css.ShouldContain(".due-date-field ::deep .due-date-today");
        css.ShouldContain(".due-date-field ::deep .due-date-overdue");
        css.ShouldNotContain("\n.due-date-today");
        css.ShouldNotContain("\n.due-date-overdue");
    }

    [Test]
    public void ShouldKeepSearchDueDateCellColors()
    {
        var css = ReadScopedCss("WorkOrderSearch.razor.css");

        css.ShouldContain(".due-date-today");
        css.ShouldContain(".due-date-overdue");
        css.ShouldContain("#fef08a");
        css.ShouldContain("#fecaca");
    }

    private static string ReadScopedCss(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "UI.Shared", "Pages", fileName));

        File.Exists(path).ShouldBeTrue($"Expected scoped stylesheet at {path}");
        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
