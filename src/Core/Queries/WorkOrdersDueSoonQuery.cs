using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Returns open work orders whose due date falls between today (America/Chicago) and today + Days inclusive.
/// </summary>
public record WorkOrdersDueSoonQuery(int Days) : IRequest<WorkOrder[]>, IRemotableRequest;
