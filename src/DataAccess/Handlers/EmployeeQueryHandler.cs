using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class EmployeeQueryHandler(DataContext context)
    : IRequestHandler<EmployeeByUserNameQuery, Employee>,
        IRequestHandler<EmployeeGetAllQuery, Employee[]>
{
    public async Task<Employee> Handle(EmployeeByUserNameQuery request,
        CancellationToken cancellationToken = default)
    {
        var employee = await context.Set<Employee>()
            .Include("Roles")
            .SingleAsync(emp => emp.UserName == request.Username, cancellationToken);
        return employee;
    }

    public async Task<Employee[]> Handle(EmployeeGetAllQuery request, CancellationToken cancellationToken = default)
    {
        var query = context.Set<Employee>().Include("Roles").AsQueryable();
        if (!request.IncludeInactive)
        {
            var alwaysInclude = request.AlwaysIncludeUserName;
            query = query.Where(e => e.IsActive ||
                                    (alwaysInclude != null && e.UserName == alwaysInclude));
        }

        var employees = await query.ToListAsync(cancellationToken);

        return employees.OrderBy(e => e.LastName).ThenBy(e => e.FirstName).ToArray();
    }
}