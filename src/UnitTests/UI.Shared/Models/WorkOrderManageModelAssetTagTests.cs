using System.ComponentModel.DataAnnotations;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

[TestFixture]
public class WorkOrderManageModelAssetTagTests
{
    [Test]
    public void ShouldAllowMissingAssetTag()
    {
        var model = ValidModel();
        model.AssetTag = null;

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldAllowEmptyAssetTag()
    {
        var model = ValidModel();
        model.AssetTag = string.Empty;

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldAcceptAssetTagAtMaxLength()
    {
        var model = ValidModel();
        model.AssetTag = new string('x', WorkOrder.AssetTagMaxLength);

        var results = Validate(model);

        results.ShouldBeEmpty();
    }

    [Test]
    public void ShouldRejectAssetTagLongerThanMaxLength()
    {
        var model = ValidModel();
        model.AssetTag = new string('x', WorkOrder.AssetTagMaxLength + 1);

        var results = Validate(model);

        results.ShouldContain(r => r.MemberNames.Contains(nameof(WorkOrderManageModel.AssetTag)));
    }

    private static WorkOrderManageModel ValidModel() => new()
    {
        Title = "Title",
        Description = "Description"
    };

    private static List<ValidationResult> Validate(WorkOrderManageModel model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }
}
