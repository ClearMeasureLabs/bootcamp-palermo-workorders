using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelLocationNotesTests
{
    [Test]
    public void ShouldAllowMissingLocationNotes()
    {
        var model = ValidModel();
        model.LocationNotes = null;

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldAcceptLocationNotesAtMaxLength()
    {
        var model = ValidModel();
        model.LocationNotes = new string('L', WorkOrder.LocationNotesMaxLength);

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldRejectLocationNotesLongerThanMaxLength()
    {
        var model = ValidModel();
        model.LocationNotes = new string('L', WorkOrder.LocationNotesMaxLength + 1);

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.LocationNotes)));
    }

    [Test]
    public void ShouldMapLocationNotesFromWorkOrderOntoModel()
    {
        var workOrder = new WorkOrder
        {
            LocationNotes = "Near north stairwell, floor 2"
        };
        var model = ValidModel();
        model.LocationNotes = workOrder.LocationNotes;

        model.LocationNotes.ShouldBe("Near north stairwell, floor 2");
    }

    [Test]
    public void LocationNotesMaxLength_ShouldBe500()
    {
        WorkOrder.LocationNotesMaxLength.ShouldBe(500);
    }

    private static WorkOrderManageModel ValidModel()
    {
        return new WorkOrderManageModel
        {
            Title = "Title",
            Description = "Description"
        };
    }

    private static List<ValidationResult> Validate(WorkOrderManageModel model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, true);
        return results;
    }
}
