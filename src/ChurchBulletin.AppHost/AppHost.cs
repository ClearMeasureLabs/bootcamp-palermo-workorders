var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddConnectionString("SqlConnectionString");

builder.AddProject<Projects.UI_Server>("ui-server")
    .WithReference(sql);

builder.AddProject<Projects.Worker>("worker")
    .WithReference(sql);

builder.Build().Run();
