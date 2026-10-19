using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderManageAssigneeFilterTests
{
    [Test]
    public void ShouldExcludeInactiveEmployeesFromAssigneeOptions()
    {
        var employees = new[]
        {
            new Employee("active1", "A", "One", "a1@test.com"),
            new Employee("inactive1", "I", "One", "i1@test.com") { IsActive = false }
        };

        var filtered = AssigneeEmployeeFilter.Filter(employees, currentAssigneeUserName: null)
            .Select(e => e.UserName)
            .ToArray();

        filtered.ShouldBe(["active1"]);
    }

    [Test]
    public void ShouldKeepCurrentlyAssignedInactiveEmployeeVisible()
    {
        var employees = new[]
        {
            new Employee("active1", "A", "One", "a1@test.com"),
            new Employee("inactive1", "I", "One", "i1@test.com") { IsActive = false }
        };

        var filtered = AssigneeEmployeeFilter.Filter(employees, "inactive1")
            .Select(e => e.UserName)
            .ToArray();

        filtered.ShouldBe(["active1", "inactive1"]);
    }
}
