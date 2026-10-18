using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record WorkOrderCountByAssigneeQuery : IRequest<Dictionary<string, int>>, IRemotableRequest;
