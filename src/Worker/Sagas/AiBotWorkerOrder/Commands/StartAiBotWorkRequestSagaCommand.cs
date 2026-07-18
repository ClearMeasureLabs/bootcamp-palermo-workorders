namespace Worker.Sagas.AiBotWorkerOrder.Commands;

public record StartAiBotWorkRequestSagaCommand(Guid SagaId, string WorkRequestNumber)
{
}