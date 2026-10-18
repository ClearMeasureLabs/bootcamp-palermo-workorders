using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class RoleByNameQueryHandler(DataContext context)
    : IRequestHandler<RoleByNameQuery, Role?>
{
    public async Task<Role?> Handle(RoleByNameQuery request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.ToLower();
        return await context.Set<Role>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name.ToLower() == name, cancellationToken);
    }
}
