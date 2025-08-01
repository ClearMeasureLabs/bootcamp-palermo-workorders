using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record EmployeeByUserNameQuery(string Username) : IRequest<Employee>, IRemotableRequest;