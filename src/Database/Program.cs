using ClearMeasure.Bootcamp.Database.Console;
using Spectre.Console.Cli;

var app = new CommandApp<PerformDbUpMigration>();
return app.Run(args);