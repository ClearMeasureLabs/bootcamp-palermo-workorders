using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
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
        var query = context.Set<Employee>()
            .Include("Roles");
        var employees = await query.ToListAsync();
        if (EmployeeSpecification.All.CanFulfill)
        {
            employees = employees.Where(e => e.CanFulfilWorkOrder()).ToList();
        }

        return employees.OrderBy(e => e.LastName).ThenBy(e => e.FirstName).ToArray();
    }
}