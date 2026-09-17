using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record WorkOrderCountByStatusQuery : IRequest<Dictionary<string, int>>, IRemotableRequest;
