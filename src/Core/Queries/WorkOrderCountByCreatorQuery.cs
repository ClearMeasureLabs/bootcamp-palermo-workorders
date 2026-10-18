using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record WorkOrderCountByCreatorQuery(string Username) : IRequest<int>, IRemotableRequest;
