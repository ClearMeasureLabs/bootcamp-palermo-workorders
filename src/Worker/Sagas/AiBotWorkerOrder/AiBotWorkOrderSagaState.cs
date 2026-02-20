using ClearMeasure.Bootcamp.Core.Model;

namespace Worker.Sagas.AiBotWorkerOrder;

public class AiBotWorkOrderSagaState : ContainSagaData
{
    public Guid SagaId { get; set; }

    public string WorkOrderNumber { get; set; }

    public WorkOrder? WorkOrder { get; set; }
}