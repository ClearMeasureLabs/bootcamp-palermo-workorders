using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record OpenWorkOrderCountByAssigneeQuery : IRequest<Dictionary<string, int>>, IRemotableRequest;
