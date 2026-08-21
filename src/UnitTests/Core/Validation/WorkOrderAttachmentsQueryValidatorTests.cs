using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class WorkOrderAttachmentsQueryValidatorTests
{
    private WorkOrderAttachmentsQueryValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new WorkOrderAttachmentsQueryValidator();
    }

    [Test]
    public void ShouldPass_WhenWorkOrderIdIsNotEmpty()
    {
        var query = new WorkOrderAttachmentsQuery(Guid.NewGuid());

        var result = _validator.TestValidate(query);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenWorkOrderIdIsEmpty()
    {
        var query = new WorkOrderAttachmentsQuery(Guid.Empty);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.WorkOrderId);
    }
}
