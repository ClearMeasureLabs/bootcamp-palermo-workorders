using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record OverdueWorkOrderCountQuery(DateOnly AsOfDate) : IRequest<int>, IRemotableRequest;
