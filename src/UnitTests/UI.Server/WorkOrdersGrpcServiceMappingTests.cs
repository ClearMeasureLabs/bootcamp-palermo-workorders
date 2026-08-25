using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Server.Grpc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class WorkOrdersGrpcServiceMappingTests
{
    [Test]
    public void MapWorkOrder_MapsCoreFields()
    {
        var source = new ClearMeasure.Bootcamp.Core.Model.WorkOrder
        {
            Number = "WO-1",
            Title = "Title",
            Description = "Desc",
            RoomNumber = "101",
            Status = WorkOrderStatus.Draft,
            Creator = new Employee { UserName = "creator" },
            Assignee = new Employee { UserName = "assignee" },
            AssignedDate = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            CreatedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CompletedDate = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var mapped = WorkOrdersGrpcService.MapWorkOrder(source);

        mapped.Number.ShouldBe("WO-1");
        mapped.Title.ShouldBe("Title");
        mapped.Description.ShouldBe("Desc");
        mapped.RoomNumber.ShouldBe("101");
        mapped.StatusKey.ShouldBe(WorkOrderStatus.Draft.Key);
        mapped.CreatorUsername.ShouldBe("creator");
        mapped.AssigneeUsername.ShouldBe("assignee");
        mapped.AssignedDateUtc.ShouldNotBeNull();
        mapped.CreatedDateUtc.ShouldNotBeNull();
        mapped.CompletedDateUtc.ShouldNotBeNull();
        mapped.HasDueDate.ShouldBeFalse();
    }

    [Test]
    public void MapWorkOrder_MapsDueDateAsIsoDateString()
    {
        var source = new ClearMeasure.Bootcamp.Core.Model.WorkOrder
        {
            Number = "WO-3",
            Title = "T",
            Description = "D",
            Status = WorkOrderStatus.Assigned,
            DueDate = new DateOnly(2026, 8, 29)
        };

        var mapped = WorkOrdersGrpcService.MapWorkOrder(source);

        mapped.HasDueDate.ShouldBeTrue();
        mapped.DueDate.ShouldBe("2026-08-29");
    }

    [Test]
    public void MapWorkOrder_OmitsOptionalDates_WhenNull()
    {
        var source = new ClearMeasure.Bootcamp.Core.Model.WorkOrder
        {
            Number = "WO-2",
            Title = "T",
            Description = "D",
            Status = WorkOrderStatus.Draft
        };

        var mapped = WorkOrdersGrpcService.MapWorkOrder(source);

        mapped.AssignedDateUtc.ShouldBeNull();
        mapped.CreatedDateUtc.ShouldBeNull();
        mapped.CompletedDateUtc.ShouldBeNull();
        mapped.HasDueDate.ShouldBeFalse();
    }
}
