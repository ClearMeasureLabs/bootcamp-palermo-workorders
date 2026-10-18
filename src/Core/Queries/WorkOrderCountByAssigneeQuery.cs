using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record WorkOrderCountByAssigneeQuery(string Username) : IRequest<int>, IRemotableRequest;
