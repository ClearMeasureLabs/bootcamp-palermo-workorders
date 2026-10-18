using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class EmployeeCountQueryHandler(DataContext context) :
    IRequestHandler<EmployeeCountQuery, int>
{
    public async Task<int> Handle(EmployeeCountQuery request,
        CancellationToken cancellationToken = default)
    {
        return await context.Set<Employee>()
            .AsNoTracking()
            .CountAsync(cancellationToken);
    }
}
