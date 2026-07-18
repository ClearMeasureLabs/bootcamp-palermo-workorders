namespace ClearMeasure.Bootcamp.Core.Services.Impl;

public class WorkRequestNumberGenerator : IWorkRequestNumberGenerator
{
    public string GenerateNumber()
    {
        return Guid.NewGuid().ToString().Substring(0, 7).ToUpper();
    }
}