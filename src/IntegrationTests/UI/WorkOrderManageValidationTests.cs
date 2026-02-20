using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Client;
using ClearMeasure.Bootcamp.UI.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.UI;

[TestFixture]
public class WorkOrderManageValidationTests : IntegratedTestBase
{
    [Test]
    public async Task Post_SaveDraftCommandWithEmptyTitle_ReturnsBadRequest()
    {
        // Arrange
        var bus = TestHost.GetRequiredService<IBus>();
        var controller = new SingleApiController(bus, new NullLogger<SingleApiController>());

        var employee = Faker<Employee>();
        var workOrder = new WorkOrder
        {
            Title = "",
            Description = "Valid description",
            Creator = employee
        };

        var command = new SaveDraftCommand(workOrder, employee);
        var message = new WebServiceMessage(command);

        // Act
        var result = await controller.Post(message);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)result;
        var errorMessage = badRequest.Value as string;
        errorMessage.ShouldNotBeNull();
        errorMessage.ShouldContain("The Title field is required");
    }

    [Test]
    public async Task Post_SaveDraftCommandWithEmptyDescription_ReturnsBadRequest()
    {
        // Arrange
        var bus = TestHost.GetRequiredService<IBus>();
        var controller = new SingleApiController(bus, new NullLogger<SingleApiController>());

        var employee = Faker<Employee>();
        var workOrder = new WorkOrder
        {
            Title = "Valid title",
            Description = "",
            Creator = employee
        };

        var command = new SaveDraftCommand(workOrder, employee);
        var message = new WebServiceMessage(command);

        // Act
        var result = await controller.Post(message);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)result;
        var errorMessage = badRequest.Value as string;
        errorMessage.ShouldNotBeNull();
        errorMessage.ShouldContain("The Description field is required");
    }

    [Test]
    public async Task Post_SaveDraftCommandWithEmptyTitleAndDescription_ReturnsBadRequestWithBothErrors()
    {
        // Arrange
        var bus = TestHost.GetRequiredService<IBus>();
        var controller = new SingleApiController(bus, new NullLogger<SingleApiController>());

        var employee = Faker<Employee>();
        var workOrder = new WorkOrder
        {
            Title = "",
            Description = "",
            Creator = employee
        };

        var command = new SaveDraftCommand(workOrder, employee);
        var message = new WebServiceMessage(command);

        // Act
        var result = await controller.Post(message);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)result;
        var errorMessage = badRequest.Value as string;
        errorMessage.ShouldNotBeNull();
        errorMessage.ShouldContain("The Title field is required");
        errorMessage.ShouldContain("The Description field is required");
    }

    [Test]
    public async Task Post_SaveDraftCommandWithValidData_ReturnsOk()
    {
        // Arrange
        new DatabaseTests().Clean();
        var bus = TestHost.GetRequiredService<IBus>();
        var controller = new SingleApiController(bus, new NullLogger<SingleApiController>());

        var employee = Faker<Employee>();
        var workOrder = new WorkOrder
        {
            Title = "Valid title",
            Description = "Valid description",
            Creator = employee,
            Number = "WO-TEST-001"
        };

        var command = new SaveDraftCommand(workOrder, employee);
        var message = new WebServiceMessage(command);

        // Act
        var result = await controller.Post(message);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
    }
}
