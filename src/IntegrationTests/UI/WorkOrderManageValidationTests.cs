using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.IntegrationTests.DataAccess;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using ClearMeasure.Bootcamp.UI.Client;
using ClearMeasure.Bootcamp.UI.Server.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace ClearMeasure.Bootcamp.IntegrationTests.UI;

public class WorkOrderManageValidationTests : IntegratedTestBase
{
    [Test]
    public async Task ServerSideValidation_WithEmptyTitle_ShouldReturnBadRequest()
    {
        new DatabaseTests().Clean();

        var currentUser = Faker<Employee>();
        currentUser.Id = Guid.NewGuid();
        var context = TestHost.GetRequiredService<DbContext>();
        context.Add(currentUser);
        await context.SaveChangesAsync();

        var workOrder = Faker<WorkOrder>();
        workOrder.Id = Guid.Empty;
        workOrder.Creator = currentUser;
        workOrder.Title = "";
        workOrder.Description = "Valid description";

        var command = new SaveDraftCommand(workOrder, currentUser);
        var message = new WebServiceMessage(command);
        
        var controller = TestHost.GetRequiredService<SingleApiController>();
        var result = await controller.Post(message);

        result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.ShouldNotBeNull();
        badRequestResult.Value.ToString().ShouldContain("Title is required");
    }

    [Test]
    public async Task ServerSideValidation_WithEmptyDescription_ShouldReturnBadRequest()
    {
        new DatabaseTests().Clean();

        var currentUser = Faker<Employee>();
        currentUser.Id = Guid.NewGuid();
        var context = TestHost.GetRequiredService<DbContext>();
        context.Add(currentUser);
        await context.SaveChangesAsync();

        var workOrder = Faker<WorkOrder>();
        workOrder.Id = Guid.Empty;
        workOrder.Creator = currentUser;
        workOrder.Title = "Valid title";
        workOrder.Description = "";

        var command = new SaveDraftCommand(workOrder, currentUser);
        var message = new WebServiceMessage(command);
        
        var controller = TestHost.GetRequiredService<SingleApiController>();
        var result = await controller.Post(message);

        result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.ShouldNotBeNull();
        badRequestResult.Value.ToString().ShouldContain("Description is required");
    }

    [Test]
    public async Task ServerSideValidation_WithEmptyTitleAndDescription_ShouldReturnBadRequest()
    {
        new DatabaseTests().Clean();

        var currentUser = Faker<Employee>();
        currentUser.Id = Guid.NewGuid();
        var context = TestHost.GetRequiredService<DbContext>();
        context.Add(currentUser);
        await context.SaveChangesAsync();

        var workOrder = Faker<WorkOrder>();
        workOrder.Id = Guid.Empty;
        workOrder.Creator = currentUser;
        workOrder.Title = "";
        workOrder.Description = "";

        var command = new SaveDraftCommand(workOrder, currentUser);
        var message = new WebServiceMessage(command);
        
        var controller = TestHost.GetRequiredService<SingleApiController>();
        var result = await controller.Post(message);

        result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.ShouldNotBeNull();
        var errorMessage = badRequestResult.Value.ToString();
        errorMessage.ShouldContain("Title is required");
        errorMessage.ShouldContain("Description is required");
    }

    [Test]
    public async Task ServerSideValidation_WithValidTitleAndDescription_ShouldReturnOk()
    {
        new DatabaseTests().Clean();

        var currentUser = Faker<Employee>();
        currentUser.Id = Guid.NewGuid();
        var context = TestHost.GetRequiredService<DbContext>();
        context.Add(currentUser);
        await context.SaveChangesAsync();

        var workOrder = Faker<WorkOrder>();
        workOrder.Id = Guid.Empty;
        workOrder.Creator = currentUser;
        workOrder.Title = "Valid title";
        workOrder.Description = "Valid description";

        var command = new SaveDraftCommand(workOrder, currentUser);
        var message = new WebServiceMessage(command);
        
        var controller = TestHost.GetRequiredService<SingleApiController>();
        var result = await controller.Post(message);

        result.ShouldBeOfType<OkObjectResult>();
    }
}
