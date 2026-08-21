namespace ClearMeasure.Bootcamp.Core.Services;

public class EmployeeSpecification
{
    public static readonly EmployeeSpecification All = new();

    private EmployeeSpecification()
    {
    }

    public bool CanFulfill { get; set; }
}