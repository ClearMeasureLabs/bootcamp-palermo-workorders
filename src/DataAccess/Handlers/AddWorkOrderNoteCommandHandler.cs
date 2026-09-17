using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class AddWorkOrderNoteCommandHandler(DbContext dbContext, TimeProvider time)
    : IRequestHandler<AddWorkOrderNoteCommand, WorkOrderNote>
{
    public async Task<WorkOrderNote> Handle(AddWorkOrderNoteCommand request,
        CancellationToken cancellationToken = default)
    {
        var note = request.CreateNote(time.GetUtcNow().DateTime);
        dbContext.Add(note);
        await dbContext.SaveChangesAsync(cancellationToken);
        return note;
    }
}
