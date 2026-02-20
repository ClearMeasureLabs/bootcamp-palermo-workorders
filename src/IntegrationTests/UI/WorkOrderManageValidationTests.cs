using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.UI;

[TestFixture]
public class WorkOrderManageValidationTests : IntegratedTestBase
{
    [Test]
    public async Task Post_WhenTitleIsEmpty_ShouldReturnBadRequest()
    {
        var currentUser = ObjectMother.GetDefaultEmployee();
        var workOrder = ObjectMother.GetDefaultWorkOrder();
        workOrder.Title = "";
        workOrder.Description = "Valid Description";
        
        var command = new SaveDraftCommand(workOrder, currentUser);
        var controller = new SingleApiController(Bus);
        
        var result = await controller.Post(new WebServiceMessage(command));
        
        result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.ShouldNotBeNull();
        badRequestResult.Value.ToString().ShouldContain("Title is required");
    }

    [Test]
    public async Task Post_WhenDescriptionIsEmpty_ShouldReturnBadRequest()
    {
        var currentUser = ObjectMother.GetDefaultEmployee();
        var workOrder = ObjectMother.GetDefaultWorkOrder();
        workOrder.Title = "Valid Title";
        workOrder.Description = "";
        
        var command = new SaveDraftCommand(workOrder, currentUser);
        var controller = new SingleApiController(Bus);
        
        var result = await controller.Post(new WebServiceMessage(command));
        
        result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.ShouldNotBeNull();
        badRequestResult.Value.ToString().ShouldContain("Description is required");
    }

    [Test]
    public async Task Post_WhenTitleAndDescriptionAreBothEmpty_ShouldReturnBadRequest()
    {
        var currentUser = ObjectMother.GetDefaultEmployee();
        var workOrder = ObjectMother.GetDefaultWorkOrder();
        workOrder.Title = "";
        workOrder.Description = "";
        
        var command = new SaveDraftCommand(workOrder, currentUser);
        var controller = new SingleApiController(Bus);
        
        var result = await controller.Post(new WebServiceMessage(command));
        
        result.ShouldBeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result;
        badRequestResult.Value.ShouldNotBeNull();
        var errorMessage = badRequestResult.Value.ToString();
        errorMessage.ShouldNotBeNull();
        errorMessage.ShouldContain("Title is required");
        errorMessage.ShouldContain("Description is required");
    }

    [Test]
    public async Task Post_WhenTitleAndDescriptionAreValid_ShouldReturnOk()
    {
        var currentUser = ObjectMother.GetDefaultEmployee();
        var workOrder = ObjectMother.GetDefaultWorkOrder();
        workOrder.Title = "Valid Title";
        workOrder.Description = "Valid Description";
        
        var command = new SaveDraftCommand(workOrder, currentUser);
        var controller = new SingleApiController(Bus);
        
        var result = await controller.Post(new WebServiceMessage(command));
        
        result.ShouldBeOfType<OkObjectResult>();
    }
}
