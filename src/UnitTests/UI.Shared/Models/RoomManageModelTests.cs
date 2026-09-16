using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class RoomManageModelTests
{
    [Test]
    public void ShouldFailValidation_WhenNameIsEmpty()
    {
        var model = new RoomManageModel { Name = "" };

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(RoomManageModel.Name)));
    }

    [Test]
    public void ShouldFailValidation_WhenNameExceedsMaxLength()
    {
        var model = new RoomManageModel { Name = new string('R', Room.NameMaxLength + 1) };

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(RoomManageModel.Name)));
    }

    [Test]
    public void ShouldPassValidation_WhenNameIsValid()
    {
        var model = new RoomManageModel { Name = "Conference Room A" };

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    private static List<ValidationResult> Validate(RoomManageModel model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, true);
        return results;
    }
}
