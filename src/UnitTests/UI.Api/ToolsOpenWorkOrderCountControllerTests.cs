using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsOpenWorkOrderCountControllerTests
{
    [Test]
    public async Task Get_ShouldReturnSumOfDraftAssignedAndInProgress()
    {
        var bus = new StubCountBus(new Dictionary<string, int>
        {
            [WorkOrderStatus.Draft.Key] = 2,
            [WorkOrderStatus.Assigned.Key] = 1,
            [WorkOrderStatus.InProgress.Key] = 3,
            [WorkOrderStatus.Complete.Key] = 9,
            [WorkOrderStatus.Cancelled.Key] = 4
        });
        var controller = new ToolsOpenWorkOrderCountController(bus);

        var result = await controller.Get();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<OpenWorkOrderCountResponse>();
        payload.Open.ShouldBe(6);
    }

    [Test]
    public async Task Get_ShouldReturnZero_WhenNoOpenWorkOrders()
    {
        var bus = new StubCountBus(new Dictionary<string, int>
        {
            [WorkOrderStatus.Draft.Key] = 0,
            [WorkOrderStatus.Assigned.Key] = 0,
            [WorkOrderStatus.InProgress.Key] = 0,
            [WorkOrderStatus.Complete.Key] = 5,
            [WorkOrderStatus.Cancelled.Key] = 2
        });
        var controller = new ToolsOpenWorkOrderCountController(bus);

        var result = await controller.Get();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<OpenWorkOrderCountResponse>();
        payload.Open.ShouldBe(0);
    }

    private sealed class StubCountBus(Dictionary<string, int> counts) : IBus
    {
        public Task<object?> Send(object request) => throw new NotImplementedException();

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderCountByStatusQuery)
            {
                return Task.FromResult((TResponse)(object)counts);
            }

            throw new NotImplementedException();
        }

        public Task Publish(INotification notification) => Task.CompletedTask;
    }
}
