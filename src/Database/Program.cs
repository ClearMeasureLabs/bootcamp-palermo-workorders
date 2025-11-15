using ClearMeasure.Bootcamp.Database.Console;
using Spectre.Console.Cli;

var app = new CommandApp<RebuildDatabaseCommand>();
return app.Run(args);