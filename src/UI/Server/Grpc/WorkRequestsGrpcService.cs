using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using Grpc.Core;

namespace ClearMeasure.Bootcamp.UI.Server.Grpc;

/// <summary>
/// gRPC API for work request reads; uses the same <see cref="IBus"/> pipeline as HTTP controllers.
/// </summary>
public class WorkRequestsGrpcService(IBus bus) : WorkRequests.WorkRequestsBase
{
    /// <inheritdoc />
    public override Task<PingReply> Ping(PingRequest request, ServerCallContext context)
    {
        return Task.FromResult(new PingReply { Message = "ok" });
    }

    /// <inheritdoc />
    public override async Task<GetWorkRequestByNumberReply> GetWorkRequestByNumber(
        GetWorkRequestByNumberRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Number))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Work request number is required."));
        }

        var workRequest = await bus.Send(new WorkRequestByNumberQuery(request.Number.Trim()));
        if (workRequest == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Work request '{request.Number}' was not found."));
        }

        return new GetWorkRequestByNumberReply { WorkRequest = MapWorkRequest(workRequest) };
    }

    private static WorkRequest MapWorkRequest(Core.Model.WorkRequest source)
    {
        var message = new WorkRequest
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
