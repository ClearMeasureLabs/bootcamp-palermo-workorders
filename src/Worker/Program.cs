using Microsoft.Extensions.Logging;
using Serilog;
using Worker;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.AddServiceDefaults();
builder.Services.AddHostedService<WorkOrderEndpoint>();
var host = builder.Build();
try
{
    host.Run();
}
finally
{
    Log.CloseAndFlush();
}
