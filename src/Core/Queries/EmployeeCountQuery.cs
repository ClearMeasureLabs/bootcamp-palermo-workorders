using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record EmployeeCountQuery : IRequest<int>, IRemotableRequest;
