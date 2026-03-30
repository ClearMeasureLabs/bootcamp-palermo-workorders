using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Ensures SQLite schema exists when hosting UI.Server in the <c>Testing</c> environment (integration tests).
/// </summary>
internal sealed class TestingDatabaseStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            if (string.Equals(app.ApplicationServices.GetRequiredService<IHostEnvironment>().EnvironmentName, "Testing",
                    StringComparison.OrdinalIgnoreCase))
            {
                using var scope = app.ApplicationServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DataContext>();
                if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
                {
                    db.Database.EnsureCreated();
                }
            }

            next(app);
        };
    }
}
