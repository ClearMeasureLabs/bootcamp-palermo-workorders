using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Returns employees eligible for display according to <see cref="EmployeeSpecification"/>.
/// </summary>
public class EmployeeGetAllQuery : IRequest<Employee[]>, IRemotableRequest
{
}
