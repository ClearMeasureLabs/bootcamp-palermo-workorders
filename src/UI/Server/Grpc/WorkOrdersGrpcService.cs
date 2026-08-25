using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using Grpc.Core;
using Google.Protobuf.WellKnownTypes;

namespace ClearMeasure.Bootcamp.UI.Server.Grpc;

/// <summary>
/// gRPC API for work order reads; uses the same <see cref="IBus"/> pipeline as HTTP controllers.
/// </summary>
public class WorkOrdersGrpcService(IBus bus) : WorkOrders.WorkOrdersBase
{
    /// <inheritdoc />
    public override Task<PingReply> Ping(PingRequest request, ServerCallContext context) =>
        Task.FromResult(new PingReply { Message = "ok" });

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

    internal static WorkOrder MapWorkOrder(Core.Model.WorkOrder source)
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

        GrpcWorkOrderDateMapper.ApplyOptionalDates(message, source);
        return message;
    }
}

internal static class GrpcWorkOrderDateMapper
{
    internal static void ApplyOptionalDates(WorkOrder message, Core.Model.WorkOrder source)
    {
        message.AssignedDateUtc = ToUtcTimestamp(source.AssignedDate);
        message.CreatedDateUtc = ToUtcTimestamp(source.CreatedDate);
        message.CompletedDateUtc = ToUtcTimestamp(source.CompletedDate);
        if (source.DueDate.HasValue)
        {
            message.DueDate = source.DueDate.Value.ToString("yyyy-MM-dd");
        }
    }

    private static Timestamp? ToUtcTimestamp(DateTime? value) =>
        value.HasValue
            ? Timestamp.FromDateTime(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
            : null;
}
