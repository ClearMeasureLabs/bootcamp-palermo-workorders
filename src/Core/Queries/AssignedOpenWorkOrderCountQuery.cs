using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Returns how many work orders assigned to the given user are Assigned or In Progress.
/// </summary>
public record AssignedOpenWorkOrderCountQuery(string UserName) : IRequest<int>, IRemotableRequest;
