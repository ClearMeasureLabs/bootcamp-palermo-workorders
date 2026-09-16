using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class RoomCommandHandler(DbContext dbContext)
    : IRequestHandler<SaveRoomCommand, Room>,
        IRequestHandler<DeleteRoomCommand, Unit>
{
    public async Task<Room> Handle(SaveRoomCommand request, CancellationToken cancellationToken = default)
    {
        var room = request.Room;
        if (room.Id == Guid.Empty)
        {
            room.Id = Guid.NewGuid();
        }

        var existing = await dbContext.FindAsync<Room>([room.Id], cancellationToken);
        if (existing == null)
        {
            dbContext.Add(room);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(room);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return room;
    }

    public async Task<Unit> Handle(DeleteRoomCommand request, CancellationToken cancellationToken = default)
    {
        var room = await dbContext.Set<Room>()
            .SingleAsync(r => r.Id == request.RoomId, cancellationToken);
        dbContext.Remove(room);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
