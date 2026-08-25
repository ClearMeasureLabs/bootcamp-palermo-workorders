using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.Events;

/// <summary>
/// Raised after a successful transactional create of dated work orders.
/// </summary>
public record DatedWorkOrdersCreatedEvent(int Count) : INotification;
