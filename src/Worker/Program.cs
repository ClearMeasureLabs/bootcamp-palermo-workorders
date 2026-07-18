using Microsoft.Extensions.Hosting;
using Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<WorkRequestEndpoint>();
var host = builder.Build();
host.UseSerilogShutdown();
host.Run();
