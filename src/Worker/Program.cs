using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess.Messaging;
using Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddScoped<IDistributedBus, DistributedBus>();
builder.Services.AddHostedService<WorkOrderEndpoint>();
var host = builder.Build();
host.Run();
