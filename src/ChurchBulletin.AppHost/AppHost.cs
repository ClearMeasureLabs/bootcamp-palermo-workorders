var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddConnectionString("SqlConnectionString");
var appInsights = builder.AddConnectionString("AppInsights");

builder.AddProject<Projects.UI_Server>("ui-server")
    .WithReference(sql)
    .WithReference(appInsights);

builder.AddProject<Projects.Worker>("worker")
    .WithReference(sql)
    .WithReference(appInsights);

builder.Build().Run();
