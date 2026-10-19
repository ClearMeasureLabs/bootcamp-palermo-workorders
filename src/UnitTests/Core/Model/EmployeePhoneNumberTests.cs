using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class EmployeePhoneNumberTests
{
    [Test]
    public void ShouldAllowOptionalPhoneNumber()
    {
        var employee = new Employee("user1", "First", "Last", "email@example.com")
        {
            PhoneNumber = "555-0123"
        };

        employee.PhoneNumber.ShouldBe("555-0123");
    }

    [Test]
    public void ShouldDefaultPhoneNumberToNull()
    {
        var employee = new Employee("user1", "First", "Last", "email@example.com");

        employee.PhoneNumber.ShouldBeNull();
    }
}
