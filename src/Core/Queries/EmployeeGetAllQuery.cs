using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public class EmployeeGetAllQuery : IRequest<Employee[]>, IRemotableRequest;
