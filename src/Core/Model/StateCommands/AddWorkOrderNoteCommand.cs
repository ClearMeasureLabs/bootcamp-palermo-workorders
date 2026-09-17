using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record AddWorkOrderNoteCommand(
    WorkOrder WorkOrder,
    Employee Author,
    string Text) : IRequest<WorkOrderNote>
{
    public WorkOrderNote CreateNote(DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(Text))
            throw new ArgumentException("Note text is required.", nameof(Text));

        return new WorkOrderNote
        {
            Id = Guid.NewGuid(),
            WorkOrderId = WorkOrder.Id,
            AuthorId = Author.Id,
            Text = Text,
            CreatedAt = createdAt
        };
    }
}
