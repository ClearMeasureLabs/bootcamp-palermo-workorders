using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class RoleByNameQueryHandlerTests
{
    [Test]
    public async Task ShouldReturnRole_WhenNameMatchesIgnoringCase()
    {
        new DatabaseTests().Clean();

        var role = new Role("FacilityAdmin", true, true);
        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(role);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new RoleByNameQueryHandler(dataContext);
        var result = await handler.Handle(new RoleByNameQuery("facilityadmin"));

        result.ShouldNotBeNull();
        result.Id.ShouldBe(role.Id);
        result.Name.ShouldBe("FacilityAdmin");
    }

    [Test]
    public async Task ShouldReturnNull_WhenNameNotFound()
    {
        new DatabaseTests().Clean();

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new RoleByNameQueryHandler(dataContext);
        var result = await handler.Handle(new RoleByNameQuery("NoSuchRole"));

        result.ShouldBeNull();
    }
}
