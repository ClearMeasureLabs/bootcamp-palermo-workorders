using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record WorkOrderByNumberQuery([property: TelemetryTag] string Number) : IRequest<WorkOrder?>, IRemotableRequest;