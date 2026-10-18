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

    // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
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

    /// <summary>
    /// Optional middle name. When null or blank it is omitted from <see cref="GetFullName"/>.
    /// </summary>
    public string? MiddleName { get; set; }

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
        return string.IsNullOrWhiteSpace(MiddleName)
            ? $"{FirstName} {LastName}"
            : $"{FirstName} {MiddleName} {LastName}";
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

    /// <summary>
    /// Returns the domain portion of <see cref="EmailAddress"/> (after <c>@</c>), lower-cased.
    /// Returns an empty string when there is no <c>@</c>.
    /// </summary>
    public string GetEmailDomain()
    {
        var atIndex = EmailAddress.IndexOf('@');
        if (atIndex < 0)
        {
            return string.Empty;
        }

        return EmailAddress[(atIndex + 1)..].ToLowerInvariant();
    }

    public void AddRole(Role role)
    {
        Roles.Add(role);
    }
}