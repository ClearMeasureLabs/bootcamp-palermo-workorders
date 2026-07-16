using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record WorkOrderAttachmentsQuery([property: TelemetryTag] Guid WorkOrderId) : IRequest<WorkOrderAttachment[]>, IRemotableRequest;
