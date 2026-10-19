using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public class EmployeeGetAllQuery : IRequest<Employee[]>, IRemotableRequest
{
    /// <summary>
    /// When true, inactive employees are included in the result.
    /// </summary>
    // ReSharper disable once PropertyCanBeMadeInitOnly.Global
    public bool IncludeInactive { get; set; }

    /// <summary>
    /// Username of an employee that must appear even when inactive (e.g. current assignee).
    /// </summary>
    // ReSharper disable once PropertyCanBeMadeInitOnly.Global
    public string? AlwaysIncludeUserName { get; set; }
}
