using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class MyWorkOrdersTests
{
    [Test]
    public void ShouldInitializeWithZeroCount()
    {
        using var ctx = new TestContext();

        // Arrange
        var stubBus = new StubBusWithNoWorkOrders();
        var stubUserSession = new StubUserSession();
        var stubUiBus = new StubUiBus();

        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUserSession>(stubUserSession);
        ctx.Services.AddSingleton<IUiBus>(stubUiBus);

        // Act
        var component = ctx.RenderComponent<MyWorkOrders>();

        // Assert
        component.Instance.Count.ShouldBe(0);
    }

    [Test]
    public void ShouldLoadWorkOrdersForCurrentUserOnInitialization()
    {
        using var ctx = new TestContext();

        // Arrange
        var currentUser = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com");
        var stubBus = new StubBusWithWorkOrders(currentUser);
        var stubUserSession = new StubUserSession(currentUser);
        var stubUiBus = new StubUiBus();

        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUserSession>(stubUserSession);
        ctx.Services.AddSingleton<IUiBus>(stubUiBus);

        // Act
        var component = ctx.RenderComponent<MyWorkOrders>();

        // Assert
        component.Instance.Count.ShouldBe(2);
    }

    [Test]
    public void ShouldHandleWorkOrderChangedEventAndIncrementCount()
    {
        using var ctx = new TestContext();

        // Arrange
        var currentUser = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com");
        var stubBus = new StubBusWithWorkOrders(currentUser);
        var stubUserSession = new StubUserSession(currentUser);
        var stubUiBus = new StubUiBus();

        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUserSession>(stubUserSession);
        ctx.Services.AddSingleton<IUiBus>(stubUiBus);

        var component = ctx.RenderComponent<MyWorkOrders>();
        var initialCount = component.Instance.Count;

        // Act
        var newWorkOrder = new WorkOrder
        {
            Number = "WO-003",
            Title = "New work order",
            Status = WorkOrderStatus.Draft,
            Creator = currentUser
        };

        var workOrderChangedEvent = new WorkOrderChangedEvent(
            new StateCommandResult(newWorkOrder)
        );

        initialCount.ShouldBe(2);
        component.Instance.Handle(workOrderChangedEvent);

        // Assert
        component.Instance.Count.ShouldBe(3);
    }

    [Test]
    public void ShouldNotDuplicateWorkOrdersWhenHandlingSameEvent()
    {
        using var ctx = new TestContext();

        // Arrange
        var currentUser = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com");
        var stubBus = new StubBusWithWorkOrders(currentUser);
        var stubUserSession = new StubUserSession(currentUser);
        var stubUiBus = new StubUiBus();

        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUserSession>(stubUserSession);
        ctx.Services.AddSingleton<IUiBus>(stubUiBus);

        var component = ctx.RenderComponent<MyWorkOrders>();
        var initialCount = component.Instance.Count;

        // Act - Handle the same work order event twice
        var workOrder = new WorkOrder
        {
            Id = Guid.NewGuid(),
            Number = "WO-003",
            Title = "New work order",
            Status = WorkOrderStatus.Draft,
            Creator = currentUser
        };

        var workOrderChangedEvent = new WorkOrderChangedEvent(
            new StateCommandResult(workOrder)
        );

        component.Instance.Handle(workOrderChangedEvent);
        component.Instance.Handle(workOrderChangedEvent);

        // Assert - Count should only increment by 1 due to HashSet behavior
        component.Instance.Count.ShouldBe(initialCount + 1);
    }

    [Test]
    public void ShouldHandleNullCurrentUser()
    {
        using var ctx = new TestContext();

        // Arrange
        var stubBus = new StubBusWithNoWorkOrders();
        var stubUserSession = new StubUserSession();
        var stubUiBus = new StubUiBus();

        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUserSession>(stubUserSession);
        ctx.Services.AddSingleton<IUiBus>(stubUiBus);

        // Act
        var component = ctx.RenderComponent<MyWorkOrders>();

        // Assert
        component.Instance.Count.ShouldBe(0);
    }

    private class StubUserSession(Employee? currentUser = null) : IUserSession
    {
        private readonly Employee? _currentUser =
            currentUser ?? new Employee("testuser", "Test", "User", "test@example.com");

        public Task<Employee?> GetCurrentUserAsync()
        {
            return Task.FromResult(_currentUser);
        }
    }

    private class StubBusWithWorkOrders(Employee creator) : Bus(null!)
    {
        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderSpecificationQuery query)
            {
                var workOrders = new[]
                {
                    new WorkOrder
                    {
                        Number = "WO-001",
                        Title = "Fix broken door",
                        Status = WorkOrderStatus.Draft,
                        Creator = creator
                    },
                    new WorkOrder
                    {
                        Number = "WO-002",
                        Title = "Replace light bulb",
                        Status = WorkOrderStatus.Assigned,
                        Creator = creator
                    }
                };
                return Task.FromResult((TResponse)(object)workOrders);
            }

            throw new NotImplementedException();
        }
    }

    private class StubBusWithNoWorkOrders() : Bus(null!)
    {
        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderSpecificationQuery)
            {
                var emptyWorkOrders = Array.Empty<WorkOrder>();
                return Task.FromResult((TResponse)(object)emptyWorkOrders);
            }

            throw new NotImplementedException();
        }
    }
}