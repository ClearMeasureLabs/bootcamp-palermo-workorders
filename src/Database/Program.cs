using ClearMeasure.Bootcamp.Database.Console;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(config =>
{
    config.SetApplicationName("ChurchBulletin.Database");
    config.CaseSensitivity(CaseSensitivity.None); // Command names are case-insensitive
    
    config.AddCommand<RebuildDatabaseCommand>("rebuild")
        .WithDescription("Rebuild the database by running Create, Update, Everytime, and TestData scripts");
    
    config.AddCommand<UpdateDatabaseCommand>("update")
        .WithDescription("Update the database by running only the Update scripts");
});

return app.Run(args);