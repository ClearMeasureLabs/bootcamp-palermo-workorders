using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Returns the count of work orders whose status is not Complete (Cancelled counts as open).
/// </summary>
public record OpenWorkOrderCountQuery : IRequest<int>, IRemotableRequest;
