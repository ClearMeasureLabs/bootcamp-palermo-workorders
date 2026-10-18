using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Returns the number of work orders that have the given status key.
/// </summary>
public record WorkOrderCountForStatusQuery(string StatusKey) : IRequest<int>, IRemotableRequest;
