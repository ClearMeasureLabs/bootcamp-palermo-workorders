using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record WorkRequestByNumberQuery(string Number) : IRequest<WorkRequest?>, IRemotableRequest;