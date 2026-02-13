var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.UI_Server>("ui-server");

builder.Build().Run();
