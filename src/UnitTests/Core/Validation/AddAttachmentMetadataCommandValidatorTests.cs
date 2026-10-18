using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Validation;
using FluentValidation.TestHelper;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class AddAttachmentMetadataCommandValidatorTests
{
    private AddAttachmentMetadataCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new AddAttachmentMetadataCommandValidator();
    }

    [Test]
    public void ShouldPass_WhenAllFieldsValid()
    {
        var command = CreateCommand();

        var result = _validator.TestValidate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldFail_WhenFileNameIsEmpty()
    {
        var command = CreateCommand(fileName: "");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Test]
    public void ShouldFail_WhenFileNameExceeds255Characters()
    {
        var command = CreateCommand(fileName: new string('a', 256));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Test]
    public void ShouldFail_WhenContentTypeIsEmpty()
    {
        var command = CreateCommand(contentType: "");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ContentType);
    }

    [Test]
    public void ShouldFail_WhenFileSizeIsNotGreaterThanZero()
    {
        var command = CreateCommand(fileSize: 0);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.FileSize);
    }

    private static AddAttachmentMetadataCommand CreateCommand(
        string fileName = "damage-photo.jpg",
        string contentType = "image/jpeg",
        long fileSize = 4096)
    {
        var workOrder = new WorkOrder { Id = Guid.NewGuid() };
        var uploader = new Employee("jpalermo", "Jeffrey", "Palermo", "jp@example.com") { Id = Guid.NewGuid() };
        return new AddAttachmentMetadataCommand(workOrder, uploader, fileName, contentType, fileSize);
    }
}
