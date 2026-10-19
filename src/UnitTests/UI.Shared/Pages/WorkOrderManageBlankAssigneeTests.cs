using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderManageBlankAssigneeTests
{
    [Test]
    public async Task ShouldSaveDraftWithNoAssignee_WhenAssigneeClearedToBlankChoice()
    {
        var bus = new CapturingBus();

        var assignee = await WorkOrderManage.ResolveAssigneeForSaveAsync("", bus);

        assignee.ShouldBeNull();
        bus.EmployeeByUserNameQueryCount.ShouldBe(0);
    }

    [Test]
    public async Task ShouldResolveAssignee_WhenAssignedToUserNameIsSet()
    {
        var expected = new Employee("hsimpson", "Homer", "Simpson", "homer@example.com");
        var bus = new CapturingBus(expected);

        var assignee = await WorkOrderManage.ResolveAssigneeForSaveAsync("hsimpson", bus);

        assignee.ShouldBe(expected);
        bus.EmployeeByUserNameQueryCount.ShouldBe(1);
    }

    private class CapturingBus(Employee? employee = null) : Bus(null!)
    {
        public int EmployeeByUserNameQueryCount { get; private set; }

        public override Task Publish(INotification notification)
        {
            return Task.CompletedTask;
        }

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeByUserNameQuery)
            {
                EmployeeByUserNameQueryCount++;
                return Task.FromResult((TResponse)(object)employee!);
            }

            throw new NotImplementedException(request.GetType().Name);
        }
    }
}
