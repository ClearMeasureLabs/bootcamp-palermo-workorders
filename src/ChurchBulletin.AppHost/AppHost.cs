var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddConnectionString("Sql");
var ollama = builder.AddConnectionString("Ollama");
var appInsights = builder.AddConnectionString("AppInsights");
var azureOpenAI = builder.AddConnectionString("AzureOpenAI");

var uiServer = builder.AddProject<Projects.UI_Server>("ui-server")
    .WithReference(sqlServer)
    .WithReference(ollama)
    .WithReference(appInsights)
    .WithReference(azureOpenAI);

builder.Build().Run();
