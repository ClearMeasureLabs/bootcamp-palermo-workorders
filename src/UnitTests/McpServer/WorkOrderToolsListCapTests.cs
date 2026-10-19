using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class WorkOrderToolsListCapTests
{
	[Test]
	public void McpServerOptions_MaxListResults_DefaultsTo100()
	{
		var options = new McpServerOptions();

		options.MaxListResults.ShouldBe(100);
	}

	[Test]
	public void FormatCappedWorkOrderList_WhenUnderCap_ReturnsAllWithoutTruncatedFlag()
	{
		var creator = new Employee("user1", "John", "Doe", "john@test.com");
		var older = new WorkOrder
		{
			Creator = creator,
			Number = "WO-001",
			Title = "Older",
			CreatedDate = new DateTime(2024, 1, 1)
		};
		var newer = new WorkOrder
		{
			Creator = creator,
			Number = "WO-002",
			Title = "Newer",
			CreatedDate = new DateTime(2024, 6, 1)
		};

		var result = WorkOrderTools.FormatCappedWorkOrderList([older, newer], maxListResults: 10);

		result.ShouldContain("WO-001");
		result.ShouldContain("WO-002");
		result.ShouldNotContain("truncated");
		using var doc = JsonDocument.Parse(result);
		doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
		doc.RootElement.GetArrayLength().ShouldBe(2);
		result.IndexOf("WO-002", StringComparison.Ordinal)
			.ShouldBeLessThan(result.IndexOf("WO-001", StringComparison.Ordinal));
	}

	[Test]
	public void FormatCappedWorkOrderList_WhenOverCap_ReturnsMaxNewestFirstAndTruncatedTrue()
	{
		var creator = new Employee("user1", "John", "Doe", "john@test.com");
		var orders = Enumerable.Range(1, 5).Select(i => new WorkOrder
		{
			Creator = creator,
			Number = $"WO-00{i}",
			Title = $"Order {i}",
			CreatedDate = new DateTime(2024, 1, i)
		}).ToArray();

		var result = WorkOrderTools.FormatCappedWorkOrderList(orders, maxListResults: 2);

		using var doc = JsonDocument.Parse(result);
		doc.RootElement.ValueKind.ShouldBe(JsonValueKind.Object);
		doc.RootElement.GetProperty("truncated").GetBoolean().ShouldBeTrue();
		doc.RootElement.GetProperty("workOrders").GetArrayLength().ShouldBe(2);
		result.ShouldContain("WO-005");
		result.ShouldContain("WO-004");
		result.ShouldNotContain("WO-001");
		result.ShouldNotContain("WO-002");
		result.ShouldNotContain("WO-003");
		result.IndexOf("WO-005", StringComparison.Ordinal)
			.ShouldBeLessThan(result.IndexOf("WO-004", StringComparison.Ordinal));
	}
}
