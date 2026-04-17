using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record EmployeeInProgressWorkOrderQuery(Employee Employee) : IRequest<WorkOrder?>, IRemotableRequest;
