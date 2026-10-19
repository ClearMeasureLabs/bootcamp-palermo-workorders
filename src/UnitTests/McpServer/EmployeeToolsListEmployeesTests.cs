using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.UI.Shared;
using MediatR;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class EmployeeToolsListEmployeesTests
{
    [Test]
    public async Task ShouldListOnlyActiveEmployeesByDefault()
    {
        var bus = new StubEmployeeBus(
        [
            new Employee("active1", "A", "One", "a1@test.com"),
            new Employee("inactive1", "I", "One", "i1@test.com") { IsActive = false }
        ]);

        var result = await EmployeeTools.ListEmployees(bus);

        result.ShouldContain("active1");
        result.ShouldNotContain("inactive1");
    }

    [Test]
    public async Task ShouldIncludeInactiveEmployeesWhenIncludeInactiveIsTrue()
    {
        var bus = new StubEmployeeBus(
        [
            new Employee("active1", "A", "One", "a1@test.com"),
            new Employee("inactive1", "I", "One", "i1@test.com") { IsActive = false }
        ]);

        var result = await EmployeeTools.ListEmployees(bus, includeInactive: true);

        result.ShouldContain("active1");
        result.ShouldContain("inactive1");
    }

    private sealed class StubEmployeeBus(Employee[] employees) : Bus(null!)
    {
        public override Task Publish(INotification notification)
        {
            return Task.CompletedTask;
        }

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeGetAllQuery getAll)
            {
                var result = getAll.IncludeInactive
                    ? employees
                    : employees.Where(e => e.IsActive).ToArray();
                return Task.FromResult((TResponse)(object)result);
            }

            throw new NotImplementedException();
        }
    }
}
