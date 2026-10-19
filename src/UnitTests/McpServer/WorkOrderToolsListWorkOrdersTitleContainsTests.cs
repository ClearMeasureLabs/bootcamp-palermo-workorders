using System.Reflection;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class WorkOrderToolsListWorkOrdersTitleContainsTests
{
    [Test]
    public void ListWorkOrders_ShouldAcceptOptionalTitleContainsArgument()
    {
        var method = typeof(WorkOrderTools).GetMethod(nameof(WorkOrderTools.ListWorkOrders));
        method.ShouldNotBeNull();

        var parameter = method.GetParameters().SingleOrDefault(p => p.Name == "titleContains");
        parameter.ShouldNotBeNull();
        parameter.ParameterType.ShouldBe(typeof(string));
        parameter.HasDefaultValue.ShouldBeTrue();
        parameter.DefaultValue.ShouldBeNull();
    }
}
