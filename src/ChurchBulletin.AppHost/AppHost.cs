var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddConnectionString("SqlConnectionString");
var appInsights = builder.AddConnectionString("AppInsights");
var ollama = builder.AddConnectionString("Ollama");
var openAi = builder.AddConnectionString("OpenAI");

builder.AddProject<Projects.UI_Server>("ui-server")
    .WithReference(sql)
    .WithReference(appInsights)
    .WithReference(ollama)
    .WithReference(openAi);

builder.AddProject<Projects.Worker>("worker")
    .WithReference(sql)
    .WithReference(appInsights)
    .WithReference(ollama)
    .WithReference(openAi);

builder.Build().Run();
