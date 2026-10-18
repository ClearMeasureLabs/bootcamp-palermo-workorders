using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class EmployeesByRoleQueryHandler(DataContext context)
    : IRequestHandler<EmployeesByRoleQuery, Employee[]>
{
    public async Task<Employee[]> Handle(EmployeesByRoleQuery request,
        CancellationToken cancellationToken = default)
    {
        var roleName = request.RoleName.ToLower();
        var employees = await context.Set<Employee>()
            .Include("Roles")
            .Where(emp => emp.Roles.Any(role => role.Name.ToLower() == roleName))
            .ToListAsync(cancellationToken);

        return employees.OrderBy(e => e.LastName).ThenBy(e => e.FirstName).ToArray();
    }
}
