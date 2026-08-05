using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Query to find recurring work orders that are due for generation.
/// </summary>
public record RecurringWorkOrdersQuery : IRequest<RecurringWorkOrdersQueryResult>, IRemotableRequest
{
    /// <summary>
    /// The date/time to check against. Work orders with NextScheduledDate <= this value will be returned.
    /// </summary>
    public DateTime AsOfDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result containing work orders that are due for recurring generation.
/// </summary>
public record RecurringWorkOrdersQueryResult
{
    public WorkOrder[] DueWorkOrders { get; set; } = [];
}
