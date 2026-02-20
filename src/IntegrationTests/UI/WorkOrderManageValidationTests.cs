using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Client;
using ClearMeasure.Bootcamp.UI.Server.Controllers;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.UI;

public class WorkOrderManageValidationTests : IntegratedTestBase
{
    [Test]
    public async Task Post_WithMissingTitle_ReturnsBadRequest()
    {
        new DatabaseTests().Clean();

        var currentUser = Faker<Employee>();
        var workOrder = Faker<WorkOrder>();
        workOrder.Creator = currentUser;
        workOrder.Title = null;
        workOrder.Description = "Test Description";

        var command = new SaveDraftCommand(workOrder, currentUser);
        var message = new WebServiceMessage(command);

        var controller = TestHost.GetRequiredService<SingleApiController>();
        var result = await controller.Post(message);

        result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.ShouldNotBeNull();
        var errorMessage = badRequestResult.Value?.ToString();
        errorMessage.ShouldNotBeNull();
        errorMessage.ShouldContain("Title is required");
    }

    [Test]
    public async Task Post_WithMissingDescription_ReturnsBadRequest()
    {
        new DatabaseTests().Clean();

        var currentUser = Faker<Employee>();
        var workOrder = Faker<WorkOrder>();
        workOrder.Creator = currentUser;
        workOrder.Title = "Test Title";
        workOrder.Description = null;

        var command = new SaveDraftCommand(workOrder, currentUser);
        var message = new WebServiceMessage(command);

        var controller = TestHost.GetRequiredService<SingleApiController>();
        var result = await controller.Post(message);

        result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.ShouldNotBeNull();
        var errorMessage = badRequestResult.Value?.ToString();
        errorMessage.ShouldNotBeNull();
        errorMessage.ShouldContain("Description is required");
    }

    [Test]
    public async Task Post_WithEmptyTitle_ReturnsBadRequest()
    {
        new DatabaseTests().Clean();

        var currentUser = Faker<Employee>();
        var workOrder = Faker<WorkOrder>();
        workOrder.Creator = currentUser;
        workOrder.Title = "";
        workOrder.Description = "Test Description";

        var command = new SaveDraftCommand(workOrder, currentUser);
        var message = new WebServiceMessage(command);

        var controller = TestHost.GetRequiredService<SingleApiController>();
        var result = await controller.Post(message);

        result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.ShouldNotBeNull();
        var errorMessage = badRequestResult.Value?.ToString();
        errorMessage.ShouldNotBeNull();
        errorMessage.ShouldContain("Title is required");
    }

    [Test]
    public async Task Post_WithEmptyDescription_ReturnsBadRequest()
    {
        new DatabaseTests().Clean();

        var currentUser = Faker<Employee>();
        var workOrder = Faker<WorkOrder>();
        workOrder.Creator = currentUser;
        workOrder.Title = "Test Title";
        workOrder.Description = "";

        var command = new SaveDraftCommand(workOrder, currentUser);
        var message = new WebServiceMessage(command);

        var controller = TestHost.GetRequiredService<SingleApiController>();
        var result = await controller.Post(message);

        result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.ShouldNotBeNull();
        var errorMessage = badRequestResult.Value?.ToString();
        errorMessage.ShouldNotBeNull();
        errorMessage.ShouldContain("Description is required");
    }
}
