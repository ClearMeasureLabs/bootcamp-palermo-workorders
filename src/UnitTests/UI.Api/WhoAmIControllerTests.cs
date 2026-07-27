using System.Security.Claims;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class WhoAmIControllerTests
{
    [Test]
    public async Task Get_Should_ReturnJsonWithEmployeeFields_When_UserAuthenticated()
    {
        var role = new Role("Maintenance", false, true);
        var employee = new Employee("testuser", "Test", "User", "test@example.com")
        {
            PreferredLanguage = "en-US"
        };
        employee.AddRole(role);
        var controller = new WhoAmIController(new StubUserSession(employee))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity("test"))
                }
            }
        };

        var result = await controller.Get(CancellationToken.None);

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldNotBeNull();
        content.ContentType!.ShouldContain("application/json");
        var payload = System.Text.Json.JsonSerializer.Deserialize<WhoAmIResponse>(
            content.Content!,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.UserName.ShouldBe("testuser");
        payload.FirstName.ShouldBe("Test");
        payload.LastName.ShouldBe("User");
        payload.EmailAddress.ShouldBe("test@example.com");
        payload.PreferredLanguage.ShouldBe("en-US");
        payload.Roles.Count.ShouldBe(1);
        payload.Roles[0].Name.ShouldBe("Maintenance");
        payload.Roles[0].CanCreateWorkOrder.ShouldBeFalse();
        payload.Roles[0].CanFulfillWorkOrder.ShouldBeTrue();
    }

    [Test]
    public async Task Get_Should_Return401_When_UserNotAuthenticated()
    {
        var controller = new WhoAmIController(new StubUserSession(null))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Get(CancellationToken.None);

        result.ShouldBeOfType<UnauthorizedResult>();
    }

    private sealed class StubUserSession(Employee? currentUser) : IUserSession
    {
        public Task<Employee?> GetCurrentUserAsync() => Task.FromResult(currentUser);
    }
}
