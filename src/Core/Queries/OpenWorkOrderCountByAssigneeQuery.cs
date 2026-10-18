using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record OpenWorkOrderCountByAssigneeQuery(string Username) : IRequest<int>, IRemotableRequest;
