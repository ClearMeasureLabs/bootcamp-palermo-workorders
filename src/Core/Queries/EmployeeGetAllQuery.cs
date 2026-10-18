using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public class EmployeeGetAllQuery : IRequest<Employee[]>, IRemotableRequest
{
    /// <summary>
    /// When set, only employees with a role of this name are returned (case-insensitive).
    /// </summary>
    // ReSharper disable once PropertyCanBeMadeInitOnly.Global -- remoting deserialization requires set
    public string? RoleName { get; set; }
}
