var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker.Worker>();
var host = builder.Build();
host.Run();
