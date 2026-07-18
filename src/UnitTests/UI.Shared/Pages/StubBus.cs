using ClearMeasure.Bootcamp.Core.Model;
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

        if (request is WorkRequestSpecificationQuery query)
        {
            return Task.FromResult<TResponse>((TResponse)(object)WorkRequestSpecificationQueryResponse());
        }

        if (request is WorkRequestAttachmentsQuery)
        {
            return Task.FromResult<TResponse>((TResponse)(object)Array.Empty<WorkRequestAttachment>());
        }

        if (request is WorkRequestByNumberQuery)
        {
            var workRequest = new WorkRequest
            {
                Id = Guid.NewGuid(),
                Number = "WO-001",
                Title = "Fix broken door",
                Status = WorkRequestStatus.Draft,
                Creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com")
            };
            return Task.FromResult<TResponse>((TResponse)(object)workRequest);
        }

        throw new NotImplementedException();
    }

    public Func<WorkRequest[]> WorkRequestSpecificationQueryResponse => () =>
    [
        new WorkRequest
        {
            Number = "WO-001",
            Title = "Fix broken door",
            Status = WorkRequestStatus.Draft,
            Creator = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com"),
            Assignee = new Employee("hsimpson", "Homer", "Simpson", "homer@example.com")
        },
        new WorkRequest
        {
            Number = "WO-002",
            Title = "Replace light bulb",
            Status = WorkRequestStatus.Assigned,
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
            new Employee("hsimpson", "HOMER", "SIMPSON", "homer@springfield.com"),
            new Employee("mburns", "Montgomery", "Burns", "burns@plant.com"),
            new Employee("nflanders", "Ned", "Flanders", "ned@flanders.com"),
            new Employee("jdoe", "mary jane", "SIMPSON", "mj@test.com")
        };
        return Task.FromResult<TResponse>((TResponse)(object)employees);
    }
}