using ChurchBulletin.ServiceDefaults;
using Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<WorkOrderEndpoint>();
var host = builder.Build();
host.UseSerilogShutdown();
host.Run();
