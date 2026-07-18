using ClearMeasure.Bootcamp.Core.Model;

namespace Worker.Sagas.AiBotWorkerOrder;

public class AiBotWorkRequestSagaState : ContainSagaData
{
    public Guid SagaId { get; set; }

    public string WorkRequestNumber { get; set; } = string.Empty;

    public WorkRequest WorkRequest { get; set; } = null!;
}