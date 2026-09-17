using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Server.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.Validation;

[TestFixture]
public class RoomGetAllQueryValidatorTests
{
    [Test]
    public void ShouldPass_WhenQueryHasNoFields()
    {
        var validator = new RoomGetAllQueryValidator();

        var result = validator.TestValidate(new RoomGetAllQuery());

        result.IsValid.ShouldBeTrue();
    }
}
