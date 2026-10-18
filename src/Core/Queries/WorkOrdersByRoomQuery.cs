using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record WorkOrdersByRoomQuery(string RoomNumber) : IRequest<WorkOrder[]>, IRemotableRequest;
