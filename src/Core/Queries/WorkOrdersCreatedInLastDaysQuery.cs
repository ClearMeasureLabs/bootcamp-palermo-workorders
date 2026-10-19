using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Returns work orders whose <see cref="WorkOrder.CreatedDate"/> falls within the last
/// <see cref="Days"/> days (1–30), newest first.
/// </summary>
public record WorkOrdersCreatedInLastDaysQuery(int Days) : IRequest<WorkOrder[]>, IRemotableRequest;
