using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public class EmployeeGetAllQuery : IRequest<Employee[]>, IRemotableRequest
{
    /// <summary>
    /// When set, only employees with a role of this name are returned (case-insensitive).
    /// </summary>
    public string? RoleName { get; init; }
}
