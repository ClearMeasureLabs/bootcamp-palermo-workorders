using System.ComponentModel;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.LlmGateway;

public static class WorkOrderTool
{
    [Description("Can get or retrieve WorkOrder given the work order number. Retrieves the employee creator and assignee, or who the work order is assigned to")]
    public static WorkOrder GetWorkOrderByNumber(string workOrderNumber)
    {
        return new WorkOrder() { Number = workOrderNumber };
    }
}