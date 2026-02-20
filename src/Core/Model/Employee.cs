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

    public bool CanCreateWorkOrder()
    {
        foreach (var role in Roles)
        {
            if (role.CanCreateWorkOrder)
            {
                return true;
            }
        }

        return false;
    }

    public bool CanFulfillWorkOrder()
    {
        foreach (var role in Roles)
        {
            if (role.CanFulfillWorkOrder)
            {
                return true;
            }
        }

        return false;
    }

    public void AddRole(Role role)
    {
        Roles.Add(role);
    }

    public string GetNotificationEmail(DayOfWeek day)
    {
        return EmailAddress;
    }
}