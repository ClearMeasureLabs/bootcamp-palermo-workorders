using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record WorkRequestAttachmentsQuery(Guid WorkRequestId) : IRequest<WorkRequestAttachment[]>, IRemotableRequest;
