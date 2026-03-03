namespace ClearMeasure.Bootcamp.Core.Services.Impl;

public class WorkOrderNumberGenerator : IWorkOrderNumberGenerator
{
    private string _number = "";
    public string GenerateNumber()
    {
        var num = Guid.NewGuid().ToString().Substring(0, 7).ToUpper();
        _number = num;
        return num;
    }

    public string GetNumber() 
    {
        return _number;
    }
}