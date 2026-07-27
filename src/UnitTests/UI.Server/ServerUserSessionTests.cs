using System.Security.Claims;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Server.Authentication;
using MediatR;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ServerUserSessionTests
{
    [Test]
    public async Task GetCurrentUserAsync_Should_LoadEmployee_When_ClaimPresent()
    {
        var employee = new Employee("testuser", "Test", "User", "test@example.com");
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "testuser")],
                EmployeeAuthenticationDefaults.Scheme))
        };
        var accessor = new StubHttpContextAccessor(httpContext);
        var session = new ServerUserSession(accessor, new StubBus(employee));

        var result = await session.GetCurrentUserAsync();

        result.ShouldNotBeNull();
        result!.UserName.ShouldBe("testuser");
    }

    [Test]
    public async Task GetCurrentUserAsync_Should_ReturnNull_When_NoClaim()
    {
        var accessor = new StubHttpContextAccessor(new DefaultHttpContext());
        var session = new ServerUserSession(accessor, new StubBus(null));

        var result = await session.GetCurrentUserAsync();

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetCurrentUserAsync_Should_ReturnNull_When_NoHttpContext()
    {
        var accessor = new StubHttpContextAccessor(null);
        var session = new ServerUserSession(accessor, new StubBus(null));

        var result = await session.GetCurrentUserAsync();

        result.ShouldBeNull();
    }

    private sealed class StubHttpContextAccessor(HttpContext? httpContext) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = httpContext;
    }

    private sealed class StubBus(Employee? employee) : IBus
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is EmployeeByUserNameQuery)
            {
                if (employee is null)
                {
                    throw new InvalidOperationException("Employee not found.");
                }

                return Task.FromResult((TResponse)(object)employee);
            }

            throw new NotImplementedException();
        }

        public Task<object?> Send(object request) => throw new NotImplementedException();

        public Task Publish(INotification notification) => Task.CompletedTask;
    }
}
