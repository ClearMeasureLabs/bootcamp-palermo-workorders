using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class UserLoggedInEventValidatorTests
{
    [Test]
    public void ShouldPass_UserName_MaximumLength_WhenValid()
    {
        new UserLoggedInEventValidator().TestValidate(new UserLoggedInEvent("user")).ShouldNotHaveValidationErrorFor(x => x.UserName);
    }

    [Test]
    public void ShouldFail_UserName_MaximumLength_WhenInvalid()
    {
        new UserLoggedInEventValidator().TestValidate(new UserLoggedInEvent(new string('a', 101))).ShouldHaveValidationErrorFor(x => x.UserName);
    }
}
