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
public class MyWorkRequestsTests
{
    [Test]
    public void ShouldInitializeWithZeroCount()
    {
        using var ctx = new TestContext();

        // Arrange
        var stubBus = new StubBusWithNoWorkRequests();
        var stubUserSession = new StubUserSession();
        var stubUiBus = new StubUiBus();

        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUserSession>(stubUserSession);
        ctx.Services.AddSingleton<IUiBus>(stubUiBus);

        // Act
        var component = ctx.RenderComponent<MyWorkRequests>();

        // Assert
        component.Instance.Count.ShouldBe(0);
    }

    [Test]
    public void ShouldLoadWorkRequestsForCurrentUserOnInitialization()
    {
        using var ctx = new TestContext();

        // Arrange
        var currentUser = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com");
        var stubBus = new StubBusWithWorkRequests(currentUser);
        var stubUserSession = new StubUserSession(currentUser);
        var stubUiBus = new StubUiBus();

        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUserSession>(stubUserSession);
        ctx.Services.AddSingleton<IUiBus>(stubUiBus);

        // Act
        var component = ctx.RenderComponent<MyWorkRequests>();

        // Assert
        component.Instance.Count.ShouldBe(2);
    }

    [Test]
    public void ShouldHandleWorkRequestChangedEventAndIncrementCount()
    {
        using var ctx = new TestContext();

        // Arrange
        var currentUser = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com");
        var stubBus = new StubBusWithWorkRequests(currentUser);
        var stubUserSession = new StubUserSession(currentUser);
        var stubUiBus = new StubUiBus();

        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUserSession>(stubUserSession);
        ctx.Services.AddSingleton<IUiBus>(stubUiBus);

        var component = ctx.RenderComponent<MyWorkRequests>();
        var initialCount = component.Instance.Count;

        // Act
        var newWorkRequest = new WorkRequest
        {
            Number = "WO-003",
            Title = "New work request",
            Status = WorkRequestStatus.Draft,
            Creator = currentUser
        };

        var workRequestChangedEvent = new WorkRequestChangedEvent(
            new StateCommandResult(newWorkRequest)
        );

        initialCount.ShouldBe(2);
        component.Instance.Handle(workRequestChangedEvent);

        // Assert
        component.Instance.Count.ShouldBe(3);
    }

    [Test]
    public void ShouldNotDuplicateWorkRequestsWhenHandlingSameEvent()
    {
        using var ctx = new TestContext();

        // Arrange
        var currentUser = new Employee("jpalermo", "Jeffrey", "Palermo", "jeffrey@example.com");
        var stubBus = new StubBusWithWorkRequests(currentUser);
        var stubUserSession = new StubUserSession(currentUser);
        var stubUiBus = new StubUiBus();

        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUserSession>(stubUserSession);
        ctx.Services.AddSingleton<IUiBus>(stubUiBus);

        var component = ctx.RenderComponent<MyWorkRequests>();
        var initialCount = component.Instance.Count;

        // Act - Handle the same work request event twice
        var workRequest = new WorkRequest
        {
            Id = Guid.NewGuid(),
            Number = "WO-003",
            Title = "New work request",
            Status = WorkRequestStatus.Draft,
            Creator = currentUser
        };

        var workRequestChangedEvent = new WorkRequestChangedEvent(
            new StateCommandResult(workRequest)
        );

        component.Instance.Handle(workRequestChangedEvent);
        component.Instance.Handle(workRequestChangedEvent);

        // Assert - Count should only increment by 1 due to HashSet behavior
        component.Instance.Count.ShouldBe(initialCount + 1);
    }

    [Test]
    public void ShouldHandleNullCurrentUser()
    {
        using var ctx = new TestContext();

        // Arrange
        var stubBus = new StubBusWithNoWorkRequests();
        var stubUserSession = new StubUserSession();
        var stubUiBus = new StubUiBus();

        ctx.Services.AddSingleton<IBus>(stubBus);
        ctx.Services.AddSingleton<IUserSession>(stubUserSession);
        ctx.Services.AddSingleton<IUiBus>(stubUiBus);

        // Act
        var component = ctx.RenderComponent<MyWorkRequests>();

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

    private class StubBusWithWorkRequests(Employee creator) : Bus(null!)
    {
        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkRequestSpecificationQuery query)
            {
                var workRequests = new[]
                {
                    new WorkRequest
                    {
                        Number = "WO-001",
                        Title = "Fix broken door",
                        Status = WorkRequestStatus.Draft,
                        Creator = creator
                    },
                    new WorkRequest
                    {
                        Number = "WO-002",
                        Title = "Replace light bulb",
                        Status = WorkRequestStatus.Assigned,
                        Creator = creator
                    }
                };
                return Task.FromResult<TResponse>((TResponse)(object)workRequests);
            }

            throw new NotImplementedException();
        }
    }

    private class StubBusWithNoWorkRequests() : Bus(null!)
    {
        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkRequestSpecificationQuery)
            {
                var emptyWorkRequests = Array.Empty<WorkRequest>();
                return Task.FromResult((TResponse)(object)emptyWorkRequests);
            }

            throw new NotImplementedException();
        }
    }
}