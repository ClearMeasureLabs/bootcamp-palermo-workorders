namespace ClearMeasure.Bootcamp.Core.Model;

public class Employee : EntityBase<Employee>, IComparable<Employee>
{
    public Employee()
    {
        UserName = null!;
        EmailAddress = null!;
        FirstName = null!;
        LastName = null!;
    }

    public Employee(string userName, string firstName, string lastName, string emailAddress)
    {
        UserName = userName;
        FirstName = firstName;
        LastName = lastName;
        EmailAddress = emailAddress;
    }

    public override Guid Id { get; set; }

    public string UserName { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string EmailAddress { get; set; }

    public string PreferredLanguage { get; set; } = "en-US";

    public ISet<Role> Roles { get; init; } = new HashSet<Role>();

    public int CompareTo(Employee? other)
    {
        var compareResult = string.Compare(LastName, other!.LastName, StringComparison.Ordinal);
        if (compareResult == 0)
        {
            compareResult = string.Compare(FirstName, other.FirstName, StringComparison.Ordinal);
        }

        return compareResult;
    }

    public string GetFullName()
    {
        return string.Format("{0} {1}", FirstName, LastName);
    }

    public override string ToString()
    {
        return GetFullName();
    }

    public bool CanCreateWorkOrder() => Roles.Any(role => role.CanCreateWorkOrder);

    /// <summary>
    /// Returns whether this employee has at least one role with permission to fulfill work orders.
    /// </summary>
    public bool CanFulfillWorkOrder() => Roles.Any(role => role.CanFulfillWorkOrder);

    public void AddRole(Role role)
    {
        Roles.Add(role);
    }
}