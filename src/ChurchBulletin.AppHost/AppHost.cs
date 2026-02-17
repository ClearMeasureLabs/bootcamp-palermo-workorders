var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddConnectionString("SqlConnectionString");
var appInsights = builder.AddConnectionString("AppInsights");

builder.AddProject<Projects.UI_Server>("ui-server")
    .WithReference(sql)
    .WithReference(appInsights)
    .WithEnvironment("AI_OpenAI_ApiKey", builder.Configuration["AI_OpenAI_ApiKey"])
    .WithEnvironment("AI_OpenAI_Url", builder.Configuration["AI_OpenAI_Url"])
    .WithEnvironment("AI_OpenAI_Model", builder.Configuration["AI_OpenAI_Model"]);

builder.Build().Run();
