using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UI.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.UI;

[TestFixture]
public class WorkOrderManageValidationTests
{
    [Test]
    public async Task SaveDraftCommand_WithoutTitle_ReturnsBadRequest()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var controller = new SingleApiController(bus, new NullLogger<SingleApiController>());

        var workOrder = new WorkOrder
        {
            Number = "WO-TEST-001",
            Title = "",
            Description = "Valid description",
            Creator = new Employee("test", "Test", "User", "test@example.com")
        };

        var command = new SaveDraftCommand(workOrder);
        var message = new WebServiceMessage(command);

        var result = await controller.Post(message);

        result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)result;
        badRequest.Value.ShouldBe("Title is required");
    }

    [Test]
    public async Task SaveDraftCommand_WithoutDescription_ReturnsBadRequest()
    {
        var bus = TestHost.GetRequiredService<IBus>();
        var controller = new SingleApiController(bus, new NullLogger<SingleApiController>());

        var workOrder = new WorkOrder
        {
            Number = "WO-TEST-002",
            Title = "Valid title",
            Description = "",
            Creator = new Employee("test", "Test", "User", "test@example.com")
        };

        var command = new SaveDraftCommand(workOrder);
        var message = new WebServiceMessage(command);

        var result = await controller.Post(message);

        result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)result;
        badRequest.Value.ShouldBe("Description is required");
    }

    [Test]
    public async Task SaveDraftCommand_WithValidData_ReturnsOk()
    {
        new DatabaseTests().Clean();

        var bus = TestHost.GetRequiredService<IBus>();
        var controller = new SingleApiController(bus, new NullLogger<SingleApiController>());

        var employee = new Employee("testuser", "Test", "User", "test@example.com");
        using (var context = TestHost.GetRequiredService<DataContext>())
        {
            context.Add(employee);
            context.SaveChanges();
        }

        var workOrder = new WorkOrder
        {
            Number = "WO-TEST-003",
            Title = "Valid title",
            Description = "Valid description",
            Creator = employee
        };

        var command = new SaveDraftCommand(workOrder);
        var message = new WebServiceMessage(command);

        var result = await controller.Post(message);

        result.ShouldBeOfType<OkObjectResult>();
    }
}
