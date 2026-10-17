using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Services.Impl;

public class WorkOrderNumberGenerator : IWorkOrderNumberGenerator
{
    public string GenerateNumber()
    {
        return Guid.NewGuid().ToString().Substring(0, WorkOrder.NumberMaxLength).ToUpper();
    }
}