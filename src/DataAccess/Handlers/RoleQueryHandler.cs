using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class RoleQueryHandler(DataContext context) : IRequestHandler<RoleGetAllQuery, Role[]>
{
	public async Task<Role[]> Handle(RoleGetAllQuery request, CancellationToken cancellationToken)
	{
		return await context.Set<Role>().OrderBy(r => r.Name).ToArrayAsync(cancellationToken);
	}
}
