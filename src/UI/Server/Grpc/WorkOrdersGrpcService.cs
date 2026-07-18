using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using Grpc.Core;

namespace ClearMeasure.Bootcamp.UI.Server.Grpc;

/// <summary>
/// gRPC API for work order reads; uses the same <see cref="IBus"/> pipeline as HTTP controllers.
/// </summary>
public class WorkOrdersGrpcService(IBus bus) : WorkOrders.WorkOrdersBase
{
    /// <inheritdoc />
    public override Task<PingReply> Ping(PingRequest request, ServerCallContext context)
    {
        return Task.FromResult(new PingReply { Message = "ok" });
    }

    /// <inheritdoc />
    public override async Task<GetWorkOrderByNumberReply> GetWorkOrderByNumber(
        GetWorkOrderByNumberRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Number))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Work order number is required."));
        }

        var workOrder = await bus.Send(new WorkOrderByNumberQuery(request.Number.Trim()));
        if (workOrder == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Work order '{request.Number}' was not found."));
        }

        return new GetWorkOrderByNumberReply { WorkOrder = MapWorkOrder(workOrder) };
    }

    private static WorkOrder MapWorkOrder(Core.Model.WorkOrder source)
    {
        var message = new WorkOrder
        {
            Number = source.Number ?? "",
            Title = source.Title ?? "",
            Description = source.Description ?? "",
            RoomNumber = source.RoomNumber ?? "",
            StatusKey = source.Status.Key,
            CreatorUsername = source.Creator?.UserName ?? "",
            AssigneeUsername = source.Assignee?.UserName ?? ""
        };

        if (source.AssignedDate.HasValue)
        {
            message.AssignedDateUtc = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                DateTime.SpecifyKind(source.AssignedDate.Value, DateTimeKind.Utc));
        }

        if (source.CreatedDate.HasValue)
        {
            message.CreatedDateUtc = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                DateTime.SpecifyKind(source.CreatedDate.Value, DateTimeKind.Utc));
        }

        if (source.CompletedDate.HasValue)
        {
            message.CompletedDateUtc = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                DateTime.SpecifyKind(source.CompletedDate.Value, DateTimeKind.Utc));
        }

        return message;
    }
}
