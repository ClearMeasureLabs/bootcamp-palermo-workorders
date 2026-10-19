using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Returns how many work orders assigned to the given employee are in Assigned and In Progress status.
/// </summary>
/// <param name="Username">Employee username to measure workload for.</param>
public record EmployeeWorkloadQuery(string Username) : IRequest<EmployeeWorkloadResult?>, IRemotableRequest;

/// <summary>
/// Assigned and In Progress work-order counts for one employee.
/// </summary>
/// <param name="Username">Employee username.</param>
/// <param name="AssignedCount">Work orders in Assigned status for this employee.</param>
/// <param name="InProgressCount">Work orders in In Progress status for this employee.</param>
public record EmployeeWorkloadResult(string Username, int AssignedCount, int InProgressCount);
