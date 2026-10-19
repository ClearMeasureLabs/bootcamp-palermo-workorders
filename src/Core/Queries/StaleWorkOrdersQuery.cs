using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record StaleWorkOrdersQuery(int Days) : IRequest<WorkOrder[]>, IRemotableRequest;
