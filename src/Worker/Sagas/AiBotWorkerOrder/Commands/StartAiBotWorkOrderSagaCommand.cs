namespace Worker.Sagas.AiBotWorkerOrder.Commands;

public record StartAiBotWorkOrderSagaCommand(Guid SagaId, string WorkOrderNumber)
{
}