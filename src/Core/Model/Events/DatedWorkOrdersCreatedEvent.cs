using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.Events;

public record DatedWorkOrdersCreatedEvent(int Count) : INotification;
