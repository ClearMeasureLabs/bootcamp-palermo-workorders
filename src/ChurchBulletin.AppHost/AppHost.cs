using ClearMeasure.Bootcamp.ChurchBulletin.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddConnectionString("SqlConnectionString");

// Aspire 17: AddProject<TProject> is obsolete (CS0618); the app host references projects by path.
// WithSingleProcessRebuild: strips the -mt Aspire injects on .NET 11, which the WebAssembly SDK's
// ReadWasmProps task factory cannot run out of process (microsoft/aspire#20709).
builder.AddDotnetProject("ui-server", "../UI/Server/UI.Server.csproj")
    .WithReference(sql)
    .WithSingleProcessRebuild();

builder.AddDotnetProject("worker", "../Worker/Worker.csproj")
    .WithReference(sql);

builder.Build().Run();
