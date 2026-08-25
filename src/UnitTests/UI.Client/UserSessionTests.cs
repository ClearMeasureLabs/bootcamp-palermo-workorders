using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Client;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Bunit;
using ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client;

[TestFixture]
public class UserSessionTests
{
    [Test]
    public async Task GetCurrentUserAsync_ShouldReturnNull_WhenUsernameEmpty()
    {
        await using var ctx = new BunitContext();
        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        var session = new UserSession(
            new StubEmployeeBus(null),
            authProvider,
            ctx.Services.GetRequiredService<NavigationManager>());

        var user = await session.GetCurrentUserAsync();

        user.ShouldBeNull();
    }

    [Test]
    public async Task GetCurrentUserAsync_ShouldReturnEmployee_WhenValid()
    {
        await using var ctx = new BunitContext();
        var employee = new Employee("hsimpson", "Homer", "Simpson", "homer@example.com")
        {
            Id = Guid.NewGuid()
        };
        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");
        var stubBus = new StubEmployeeBus(employee);
        var session = new UserSession(
            stubBus,
            authProvider,
            ctx.Services.GetRequiredService<NavigationManager>());

        var user = await session.GetCurrentUserAsync();

        user.ShouldNotBeNull();
        user.UserName.ShouldBe("hsimpson");
        user.FirstName.ShouldBe("Homer");
        stubBus.LastQueriedUsername.ShouldBe("hsimpson");
    }

    [Test]
    public async Task GetCurrentUserAsync_ShouldRestoreStoredUsername_BeforeQueryingEmployee()
    {
        await using var ctx = new BunitContext();
        var employee = new Employee("tlovejoy", "Timothy", "Lovejoy", "lovejoy@example.com")
        {
            Id = Guid.NewGuid()
        };
        var store = new StubUserSessionStore { Username = "tlovejoy" };
        var authProvider = new CustomAuthenticationStateProvider(store);
        var stubBus = new StubEmployeeBus(employee);
        var session = new UserSession(
            stubBus,
            authProvider,
            ctx.Services.GetRequiredService<NavigationManager>());

        var user = await session.GetCurrentUserAsync();

        user.ShouldBe(employee);
        stubBus.LastQueriedUsername.ShouldBe("tlovejoy");
    }

    [Test]
    public async Task GetCurrentUserAsync_ShouldThrow_WhenEmployeeMissing()
    {
        await using var ctx = new BunitContext();
        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("unknown");
        var session = new UserSession(
            new StubEmployeeBus(null),
            authProvider,
            ctx.Services.GetRequiredService<NavigationManager>());

        var ex = await Should.ThrowAsync<Exception>(session.GetCurrentUserAsync);

        ex.Message.ShouldContain("doesn't exist");
    }

    [Test]
    public async Task LogOut_ShouldClearAuth_AndNavigateToLogin()
    {
        await using var ctx = new BunitContext();
        var authProvider = new CustomAuthenticationStateProvider(new StubUserSessionStore());
        await authProvider.Login("hsimpson");
        var navigationManager = ctx.Services.GetRequiredService<NavigationManager>();
        var session = new UserSession(new StubEmployeeBus(null), authProvider, navigationManager);

        await session.LogOut();

        var authState = await authProvider.GetAuthenticationStateAsync();
        authState.User.Identity!.IsAuthenticated.ShouldBeFalse();
        navigationManager.Uri.ShouldEndWith("/login");
    }

    private sealed class StubEmployeeBus(Employee? employee) : IBus
    {
        public string? LastQueriedUsername { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeByUserNameQuery query)
            {
                LastQueriedUsername = query.Username;
                return Task.FromResult((TResponse)(object)employee!);
            }

            throw new NotSupportedException(request.GetType().Name);
        }

        public Task<object?> Send(object request) => throw new NotSupportedException();

        public Task Publish(INotification notification) => throw new NotSupportedException();
    }
}
