using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record EmployeesByRoleQuery(string RoleName) : IRequest<Employee[]>, IRemotableRequest;
