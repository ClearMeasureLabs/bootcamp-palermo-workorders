namespace Worker.Sagas.AiBotWorkerOrder.Events;

public record AiBotCompletedWorkOrderEvent(Guid SagaId, string WorkOrderNumber)
{
}