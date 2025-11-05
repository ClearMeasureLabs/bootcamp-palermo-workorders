using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderManageTests
{
    [Test]
    public void ShouldNotLookupEmployeeWhenAssigneeClearedAndSavingDraft()
    {
        using var ctx = new TestContext();

        var currentUser = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com");
        var stubBus = new WorkOrderManageStubBus(currentUser);

        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        ctx.Services.AddSingleton<IUserSession>(new StubUserSession(currentUser));
        ctx.Services.AddSingleton<IWorkOrderBuilder>(new StubWorkOrderBuilder(currentUser));

        var component = ctx.RenderComponent<WorkOrderManage>();
        component.WaitForAssertion(() => component.Instance.Model.WorkOrderNumber.ShouldNotBeNull());

        component.Find("#Title").Change("Test Title");
        component.Find("#Description").Change("Test Description");
        component.Find("#AssignedToUserName").Change(string.Empty);

        var saveButton = component.Find($"[data-testid=\"{WorkOrderManage.Elements.CommandButton}Save\"]");
        saveButton.Click();

        component.WaitForAssertion(() =>
        {
            stubBus.EmployeeLookupRequested.ShouldBeFalse();
            stubBus.StateCommandExecuted.ShouldBeTrue();
        });
    }

    private sealed class StubUserSession(Employee currentUser) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync()
        {
            return Task.FromResult<Employee?>(currentUser);
        }
    }

    private sealed class StubWorkOrderBuilder : IWorkOrderBuilder
    {
        private readonly Employee _creator;

        public StubWorkOrderBuilder(Employee creator)
        {
            _creator = creator;
        }

        public WorkOrder CreateNewWorkOrder(Employee creator)
        {
            return new WorkOrder
            {
                Number = "WO-TEST",
                Creator = _creator,
                Status = WorkOrderStatus.Draft
            };
        }
    }

    private sealed class WorkOrderManageStubBus(Employee currentUser) : Bus(null!)
    {
        public bool EmployeeLookupRequested { get; private set; }
        public bool StateCommandExecuted { get; private set; }

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            switch (request)
            {
                case EmployeeGetAllQuery:
                    var employees = new[]
                    {
                        currentUser,
                        new Employee("hsimpson", "Homer", "Simpson", "homer@example.com"),
                        new Employee("nflanders", "Ned", "Flanders", "ned@example.com")
                    };
                    return Task.FromResult((TResponse)(object)employees);

                case EmployeeByUserNameQuery:
                    EmployeeLookupRequested = true;
                    var employee = new Employee("hsimpson", "Homer", "Simpson", "homer@example.com");
                    return Task.FromResult((TResponse)(object)employee);

                case StateCommandBase stateCommand:
                    StateCommandExecuted = true;
                    return Task.FromResult((TResponse)(object)new StateCommandResult(stateCommand.WorkOrder));

                default:
                    throw new NotImplementedException($"Unhandled request type {request.GetType().Name}");
            }
        }
    }
}
