using ClearMeasure.Bootcamp.Worker.Services;
using Microsoft.Extensions.Hosting;
using Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<WorkOrderEndpoint>();
builder.Services.AddHostedService<RecurringWorkOrderService>();
var host = builder.Build();
host.UseSerilogShutdown();
host.Run();
