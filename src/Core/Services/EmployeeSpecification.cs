namespace ClearMeasure.Bootcamp.Core.Services;

public class EmployeeSpecification
{
    public static readonly EmployeeSpecification All = new();

    public EmployeeSpecification()
    {
    }

    public EmployeeSpecification(bool canFulfill)
    {
        CanFulfill = canFulfill;
    }

    public bool CanFulfill { get; set; }
}