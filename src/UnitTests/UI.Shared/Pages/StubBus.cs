using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

public class StubBus() : Bus(null!)
{
    public override Task Publish(INotification notification)
    {
        return Task.CompletedTask;
    }

    public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        if (request is EmployeeGetAllQuery)
        {
            return (Task<TResponse>)EmployeeGetAllQueryResponse<TResponse>();
        }

        if (request is EmployeeByUserNameQuery)
        {
            return (Task<TResponse>)EmployeeByUserNameQueryResponse<TResponse>();
        }

        if (request is WorkOrderSpecificationQuery query)
        {
            return Task.FromResult<TResponse>((TResponse)(object)WorkOrderSpecificationQueryResponse());
        }

        throw new NotImplementedException();
    }

    public Func<WorkOrder[]> WorkOrderSpecificationQueryResponse => () =>
    [
        new WorkOrder
        {
            Number = "WO-001",
            Title = "Fix broken door",
            Status = WorkOrderStatus.Draft,
            Creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com"),
            Assignee = new Employee("hsimpson", "Homer", "Simpson", "homer@example.com")
        },
        new WorkOrder
        {
            Number = "WO-002",
            Title = "Replace light bulb",
            Status = WorkOrderStatus.Assigned,
            Creator = new Employee("mburns", "Montgomery", "Burns", "burns@example.com"),
            Assignee = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com")
        }
    ];

    public static Task EmployeeByUserNameQueryResponse<TResponse>()
    {
        var employee = new Employee("hsimpson", "Homer", "Simpson", "homer@springfield.com");
        return Task.FromResult<TResponse>((TResponse)(object)employee);
    }

    private Task EmployeeGetAllQueryResponse<TResponse>()
    {
        var employees = new[]
        {
            new Employee("hsimpson", "Homer", "Simpson", "homer@springfield.com"),
            new Employee("mburns", "Montgomery", "Burns", "burns@plant.com"),
            new Employee("nflanders", "Ned", "Flanders", "ned@flanders.com")
        };
        return Task.FromResult<TResponse>((TResponse)(object)employees);
    }
}