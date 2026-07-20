using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record WorkOrderCountQuery : IRequest<int>, IRemotableRequest;
