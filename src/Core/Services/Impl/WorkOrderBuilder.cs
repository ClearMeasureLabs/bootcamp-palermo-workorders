using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services.Impl;

public class WorkOrderBuilder(IWorkOrderNumberGenerator numberGenerator)
    : IWorkOrderBuilder
{
    public WorkOrder CreateNewWorkOrder(Employee creator)
    {
        var workOrder = new WorkOrder
        {
            Number = numberGenerator.GenerateNumber(),
            Creator = creator,
            Status = WorkOrderStatus.Draft
        };
        return workOrder;
    }
}