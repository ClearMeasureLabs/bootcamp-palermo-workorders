using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record OpenWorkOrdersInRoomQuery(string RoomNumber, string? ExcludeNumber)
    : IRequest<WorkOrder[]>, IRemotableRequest;
