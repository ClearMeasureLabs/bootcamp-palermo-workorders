using System.Reflection;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class WorkOrderToolsEstimatedHoursTests
{
    [Test]
    public void SaveWorkOrder_ShouldExposeOptionalEstimatedHoursArgument()
    {
        var parameter = typeof(WorkOrderTools)
            .GetMethod(nameof(WorkOrderTools.SaveWorkOrder))!
            .GetParameters()
            .Single(p => p.Name == "estimatedHours");

        parameter.ParameterType.ShouldBe(typeof(decimal?));
        parameter.HasDefaultValue.ShouldBeTrue();
        parameter.DefaultValue.ShouldBeNull();
    }
}
