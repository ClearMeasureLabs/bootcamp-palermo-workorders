using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class RoomQueryHandler(DataContext context)
    : IRequestHandler<RoomGetAllQuery, Room[]>
{
    public async Task<Room[]> Handle(RoomGetAllQuery request, CancellationToken cancellationToken = default)
    {
        var rooms = await context.Set<Room>().OrderBy(r => r.Name).ToArrayAsync(cancellationToken);
        return rooms;
    }
}
