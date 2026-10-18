using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record EmployeeByEmailQuery(string Email) : IRequest<Employee?>, IRemotableRequest;
