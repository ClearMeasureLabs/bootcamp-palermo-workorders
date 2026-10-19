using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class UpdatePreferredLanguageCommandHandlerTests
{
    [Test]
    public async Task ShouldPersistPreferredLanguageToDatabase()
    {
        new DatabaseTests().Clean();

        var employee = new Employee("upl_user", "First", "Last", "email@test.com")
        {
            PreferredLanguage = "en-US"
        };

        await using (var context = TestHost.GetRequiredService<DbContext>())
        {
            context.Add(employee);
            await context.SaveChangesAsync();
        }

        var dataContext = TestHost.GetRequiredService<DataContext>();
        var handler = new UpdatePreferredLanguageCommandHandler(dataContext);
        await handler.Handle(new UpdatePreferredLanguageCommand("upl_user", "fr-FR"));

        await using var verifyContext = TestHost.GetRequiredService<DbContext>();
        var saved = await verifyContext.Set<Employee>()
            .SingleAsync(e => e.UserName == "upl_user");
        saved.PreferredLanguage.ShouldBe("fr-FR");
    }
}
