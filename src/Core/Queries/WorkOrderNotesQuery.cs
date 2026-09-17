using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record WorkOrderNotesQuery(Guid WorkOrderId) : IRequest<WorkOrderNote[]>, IRemotableRequest;
