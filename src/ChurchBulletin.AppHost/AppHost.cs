var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddConnectionString("SqlConnectionString");

// Aspire 17: AddProject<TProject> is obsolete (CS0618); the app host references projects by path.
builder.AddDotnetProject("ui-server", "../UI/Server/UI.Server.csproj")
    .WithReference(sql);

builder.AddDotnetProject("worker", "../Worker/Worker.csproj")
    .WithReference(sql);

builder.Build().Run();
