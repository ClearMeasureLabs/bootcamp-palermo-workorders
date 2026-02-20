using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Client;
using ClearMeasure.Bootcamp.UI.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.UI;

[TestFixture]
public class WorkOrderManageValidationTests
{
    [Test]
    public async Task Post_WithEmptyTitle_ReturnsBadRequest()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var logger = new NullLogger<SingleApiController>();
        var controller = new SingleApiController(bus, logger);

        var employee = new Employee("test", "Test", "User", "test@test.com");
        var workOrder = new WorkOrder { Title = "", Description = "Test Description" };
        var command = new SaveDraftCommand(workOrder, employee);
        var message = new WebServiceMessage(command);

        var result = await controller.Post(message);

        result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)result;
        badRequest.Value.ToString().ShouldContain("The Title field is required.");
    }

    [Test]
    public async Task Post_WithEmptyDescription_ReturnsBadRequest()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var logger = new NullLogger<SingleApiController>();
        var controller = new SingleApiController(bus, logger);

        var employee = new Employee("test", "Test", "User", "test@test.com");
        var workOrder = new WorkOrder { Title = "Test Title", Description = "" };
        var command = new SaveDraftCommand(workOrder, employee);
        var message = new WebServiceMessage(command);

        var result = await controller.Post(message);

        result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)result;
        badRequest.Value.ToString().ShouldContain("The Description field is required.");
    }

    [Test]
    public async Task Post_WithBothTitleAndDescriptionEmpty_ReturnsBadRequestForTitle()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var logger = new NullLogger<SingleApiController>();
        var controller = new SingleApiController(bus, logger);

        var employee = new Employee("test", "Test", "User", "test@test.com");
        var workOrder = new WorkOrder { Title = "", Description = "" };
        var command = new SaveDraftCommand(workOrder, employee);
        var message = new WebServiceMessage(command);

        var result = await controller.Post(message);

        result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)result;
        badRequest.Value.ToString().ShouldContain("The Title field is required.");
    }

    [Test]
    public async Task Post_WithValidTitleAndDescription_ReturnsOk()
    {
        new DatabaseTests().Clean();

        var bus = TestHost.GetRequiredService<IBus>();
        var logger = new NullLogger<SingleApiController>();
        var controller = new SingleApiController(bus, logger);

        var employee = new Employee("test", "Test", "User", "test@test.com");
        var workOrder = new WorkOrder
        {
            Number = "TEST-001",
            Title = "Test Title",
            Description = "Test Description",
            Creator = employee
        };
        var command = new SaveDraftCommand(workOrder, employee);
        var message = new WebServiceMessage(command);

        var result = await controller.Post(message);

        result.ShouldBeOfType<OkObjectResult>();
    }
}
