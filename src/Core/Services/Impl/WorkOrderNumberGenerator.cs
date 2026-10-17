namespace ClearMeasure.Bootcamp.Core.Services.Impl;

public sealed class WorkOrderNumberGenerator : IWorkOrderNumberGenerator
{
    public string GenerateNumber()
    {
        return Guid.NewGuid().ToString().Substring(0, 7).ToUpper();
    }
}