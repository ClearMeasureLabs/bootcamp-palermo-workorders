using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class AuthControllerTests
{
    [Test]
    public async Task Login_Should_Return204_When_ValidUsername()
    {
        var employee = new Employee("validuser", "Valid", "User", "valid@example.com");
        var signInService = new StubEmployeeSignInService();
        var controller = new AuthController(new StubBus(employee), signInService)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Login(new LoginRequest("validuser"));

        result.ShouldBeOfType<NoContentResult>();
        signInService.LastSignedInUserName.ShouldBe("validuser");
    }

    [Test]
    public async Task Login_Should_Return400_When_UnknownUsername()
    {
        var controller = new AuthController(new StubBus(null), new StubEmployeeSignInService())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Login(new LoginRequest("unknown"));

        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.Value.ShouldBeOfType<ProblemDetails>();
    }

    [Test]
    public async Task Logout_Should_Return204_When_Called()
    {
        var signInService = new StubEmployeeSignInService();
        var controller = new AuthController(new StubBus(null), signInService)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Logout();

        result.ShouldBeOfType<NoContentResult>();
        signInService.SignOutCalled.ShouldBeTrue();
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

    private sealed class StubEmployeeSignInService : IEmployeeSignInService
    {
        public string? LastSignedInUserName { get; private set; }

        public bool SignOutCalled { get; private set; }

        public Task SignInAsync(string userName)
        {
            LastSignedInUserName = userName;
            return Task.CompletedTask;
        }

        public Task SignOutAsync()
        {
            SignOutCalled = true;
            return Task.CompletedTask;
        }
    }
}
