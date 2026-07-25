using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class LastLoginHandlerTests
{
    [Test]
    public async Task Should_SetLastLoginUtc_WhenUserLogsIn()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("loginuser", "Login", "User", "login@test.com");
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.SaveChanges();
        }

        var before = DateTimeOffset.UtcNow;
        var handler = TestHost.GetRequiredService<LastLoginHandler>();
        await handler.Handle(new UserLoggedInEvent("loginuser"), CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydrated = context.Set<Employee>().Single(e => e.UserName == "loginuser");
            rehydrated.LastLoginUtc.ShouldNotBeNull();
            rehydrated.LastLoginUtc!.Value.ShouldBeGreaterThanOrEqualTo(before.AddSeconds(-2));
            rehydrated.LastLoginUtc!.Value.ShouldBeLessThanOrEqualTo(after.AddSeconds(2));
        }
    }

    [Test]
    public async Task Should_UpdateLastLoginUtc_OnSubsequentLogins()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("repeatuser", "Repeat", "User", "repeat@test.com")
        {
            LastLoginUtc = DateTimeOffset.UtcNow.AddDays(-1)
        };
        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            context.SaveChanges();
        }

        var previous = employee.LastLoginUtc;
        await Task.Delay(50);

        var handler = TestHost.GetRequiredService<LastLoginHandler>();
        await handler.Handle(new UserLoggedInEvent("repeatuser"), CancellationToken.None);

        using (var context = TestHost.GetRequiredService<DbContext>())
        {
            var rehydrated = context.Set<Employee>().Single(e => e.UserName == "repeatuser");
            rehydrated.LastLoginUtc.ShouldNotBeNull();
            rehydrated.LastLoginUtc!.Value.ShouldBeGreaterThan(previous!.Value);
        }
    }
}
