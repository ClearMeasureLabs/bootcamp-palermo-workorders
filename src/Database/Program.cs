using ClearMeasure.Bootcamp.Database.Console;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(config =>
{
    config.SetApplicationName("ChurchBulletin.Database");
    config.CaseSensitivity(CaseSensitivity.None); // Command names are case-insensitive

    // TODO [TO20251116] - Need to map the AliaSQL actions to DbUp commands. For now everything is a "Rebuild".
    config.AddCommand<BaselineDatabaseCommand>("baseline")
        .WithDescription("Mark all existing scripts as executed without running them (for existing databases)");
    
    config.AddCommand<RebuildDatabaseCommand>("rebuild")
        .WithDescription("Rebuild the database by running Create, Update, Everytime, and TestData scripts");

    config.AddCommand<RebuildDatabaseCommand>("update")
        .WithDescription("Update the database by running only the Update scripts");
});

return app.Run(args);