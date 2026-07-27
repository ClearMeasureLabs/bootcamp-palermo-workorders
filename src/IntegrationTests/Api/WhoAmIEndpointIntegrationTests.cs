using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.Api;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class WhoAmIEndpointIntegrationTests
{
    private SqliteConnection? _sharedMemoryHold;
    private WhoAmIWebApplicationFactory? _factory;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _sharedMemoryHold = new SqliteConnection(WhoAmIWebApplicationFactory.SqliteConnectionString);
        _sharedMemoryHold.Open();
        _factory = new WhoAmIWebApplicationFactory();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _factory?.Dispose();
        _sharedMemoryHold?.Dispose();
    }

    [Test]
    public async Task Should_Return401_When_GetWhoAmIWithoutAuthentication()
    {
        using var client = _factory!.CreateClient();

        var unversioned = await client.GetAsync("/api/whoami");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var versioned = await client.GetAsync("/api/v1.0/whoami");
        versioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Should_Return200AndEmployeeJson_When_AuthenticatedViaAuthLogin()
    {
        var testTag = Guid.NewGuid().ToString("N")[..8];
        var employee = SeedTestEmployee(testTag);
        using var client = _factory!.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { userName = employee.UserName });
        login.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var response = await client.GetAsync("/api/whoami");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<WhoAmIResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.UserName.ShouldBe(employee.UserName);
        payload.FirstName.ShouldBe("Test");
        payload.LastName.ShouldBe("User");
        payload.EmailAddress.ShouldBe($"testuser-{testTag}@example.com");
        payload.PreferredLanguage.ShouldBe("en-US");
        payload.Roles.Count.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task Should_Return200AndEmployeeJson_When_GetVersionedWhoAmI()
    {
        var testTag = Guid.NewGuid().ToString("N")[..8];
        var employee = SeedTestEmployee(testTag);
        using var client = _factory!.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { userName = employee.UserName });
        login.EnsureSuccessStatusCode();

        var response = await client.GetAsync("/api/v1.0/whoami");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<WhoAmIResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.UserName.ShouldBe(employee.UserName);
    }

    [Test]
    public async Task Should_Return401AfterLogout_When_SessionCleared()
    {
        var testTag = Guid.NewGuid().ToString("N")[..8];
        var employee = SeedTestEmployee(testTag);
        using var client = _factory!.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { userName = employee.UserName });
        login.EnsureSuccessStatusCode();

        var logout = await client.PostAsync("/api/auth/logout", null);
        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var whoami = await client.GetAsync("/api/whoami");
        whoami.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Should_Return400_When_LoginWithUnknownUsername()
    {
        using var client = _factory!.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName = "nonexistent-user" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("Invalid login");
    }

    [Test]
    public async Task Should_Return204_When_LogoutWithoutAuthentication()
    {
        using var client = _factory!.CreateClient();

        var response = await client.PostAsync("/api/auth/logout", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabledAndWhoAmIProtected()
    {
        var testTag = Guid.NewGuid().ToString("N")[..8];
        await using var connection = new SqliteConnection(WhoAmIApiKeyProtectedWebApplicationFactory.SqliteConnectionString);
        await connection.OpenAsync();
        await using var factory = new WhoAmIApiKeyProtectedWebApplicationFactory();
        SeedTestEmployee(testTag, factory);
        using var client = factory.CreateClient();

        var unauth = await client.GetAsync("/api/whoami");
        unauth.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var withKey = factory.CreateClient();
        withKey.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var login = await withKey.PostAsJsonAsync("/api/auth/login", new { userName = $"testuser-{testTag}" });
        login.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var whoami = await withKey.GetAsync("/api/whoami");
        whoami.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_RespectRateLimiting_When_WhoAmICalledRepeatedly()
    {
        var testTag = Guid.NewGuid().ToString("N")[..8];
        await using var connection = new SqliteConnection(WhoAmIWebApplicationFactory.SqliteConnectionString);
        await connection.OpenAsync();
        await using var factory = new RateLimitedApiWebApplicationFactory(WhoAmIWebApplicationFactory.SqliteConnectionString);
        SeedTestEmployee(testTag, factory);
        using var client = factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { userName = $"testuser-{testTag}" });
        login.EnsureSuccessStatusCode();

        var first = await client.GetAsync("/api/whoami");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);

        var second = await client.GetAsync("/api/whoami");
        second.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task Should_Return401_When_EmployeeDeletedAfterLogin()
    {
        var testTag = Guid.NewGuid().ToString("N")[..8];
        var employee = SeedTestEmployee(testTag);
        using var client = _factory!.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { userName = employee.UserName });
        login.EnsureSuccessStatusCode();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            var tracked = await db.Set<Employee>().SingleAsync(e => e.UserName == employee.UserName);
            db.Remove(tracked);
            await db.SaveChangesAsync();
        }

        var whoami = await client.GetAsync("/api/whoami");
        whoami.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Should_IncludeMultipleRoles_When_EmployeeHasMultipleRoles()
    {
        var testTag = Guid.NewGuid().ToString("N")[..8];
        var employee = SeedTestEmployeeWithMultipleRoles(testTag);
        using var client = _factory!.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { userName = employee.UserName });
        login.EnsureSuccessStatusCode();

        var response = await client.GetAsync("/api/whoami");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<WhoAmIResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Roles.Count.ShouldBe(2);
        payload.Roles.ShouldContain(r => r.Name == $"Maintenance-{testTag}" && r.CanFulfillWorkOrder);
        payload.Roles.ShouldContain(r => r.Name == $"Manager-{testTag}" && r.CanCreateWorkOrder);
    }

    private Employee SeedTestEmployee(string testTag, WebApplicationFactory<UiServerWebApplicationMarker>? factory = null)
    {
        var targetFactory = factory ?? _factory!;
        using var scope = targetFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        db.Database.EnsureCreated();
        var role = new Role($"TestRole-{testTag}", true, true);
        var employee = new Employee(
            $"testuser-{testTag}",
            "Test",
            "User",
            $"testuser-{testTag}@example.com")
        {
            PreferredLanguage = "en-US"
        };
        employee.AddRole(role);
        db.Add(role);
        db.Add(employee);
        db.SaveChanges();
        return employee;
    }

    private Employee SeedTestEmployeeWithMultipleRoles(string testTag)
    {
        using var scope = _factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        db.Database.EnsureCreated();
        var maintenance = new Role($"Maintenance-{testTag}", false, true);
        var manager = new Role($"Manager-{testTag}", true, false);
        var employee = new Employee(
            $"multirole-{testTag}",
            "Test",
            "User",
            $"multirole-{testTag}@example.com")
        {
            PreferredLanguage = "en-US"
        };
        employee.AddRole(maintenance);
        employee.AddRole(manager);
        db.Add(maintenance);
        db.Add(manager);
        db.Add(employee);
        db.SaveChanges();
        return employee;
    }
}
