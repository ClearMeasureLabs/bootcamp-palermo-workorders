using ClearMeasure.Bootcamp.Database.Console;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spectre.Console.Cli;

var services = new ServiceCollection();
services.AddSingleton<DatabaseTasks>();
services.AddSingleton<IDatabaseTasks>(provider => provider.GetRequiredService<DatabaseTasks>());
services.TryAddSingleton<DropDatabaseCommand>();
services.TryAddSingleton<UpdateDatabaseCommand>();
services.TryAddSingleton<RebuildDatabaseCommand>();
services.TryAddSingleton<RebuildDatabaseCommand>();

ITypeRegistrar registrar = new ServiceCollectionRegistrar(services);

var app = new CommandApp(registrar);
app.Configure(config =>
{
    config.SetApplicationName("ChurchBulletin.Database");
    config.CaseSensitivity(CaseSensitivity.None); // Command names are case-insensitive
    config.AddCommand<BaselineDatabaseCommand>("baseline")
        .WithDescription("Mark all existing scripts as executed without running them (for existing databases)");

    config.AddCommand<RebuildDatabaseCommand>("rebuild")
        .WithDescription("Rebuild the database by running Create, Update, Everytime, and TestData scripts");

    config.AddCommand<UpdateDatabaseCommand>("update")
        .WithDescription("Update the database by running only the Update scripts");

    config.AddCommand<DropDatabaseCommand>("drop")
        .WithDescription("Drop the specified database if it exists");
});

return app.Run(args);