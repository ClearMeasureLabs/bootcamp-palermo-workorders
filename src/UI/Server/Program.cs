using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess.Messaging;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.McpServer.Resources;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Host.UseLamar(registry => { registry.IncludeRegistry<UiServiceRegistry>(); });
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IDistributedBus, DistributedBus>();

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Add MCP server with HTTP transport for remote access
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = "ChurchBulletin",
            Version = "1.0.0"
        };
    })
    .WithHttpTransport()
    .WithTools<WorkOrderTools>()
    .WithTools<EmployeeTools>()
    .WithResources<ReferenceResources>();

// Add NServiceBus endpoint
var endpointConfiguration = new NServiceBus.EndpointConfiguration("UI.Server");
endpointConfiguration.UseSerialization<SystemJsonSerializer>();
endpointConfiguration.EnableInstallers();
endpointConfiguration.EnableOpenTelemetry();

// transport
var sqlConnectionString = builder.Configuration.GetConnectionString("SqlConnectionString") ?? "";
if (sqlConnectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
{
    endpointConfiguration.UseTransport<LearningTransport>();
}
else
{
    var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
    transport.ConnectionString(sqlConnectionString);
    transport.DefaultSchema("nServiceBus");
    transport.Transactions(TransportTransactionMode.TransactionScope);
}

// message conventions
var conventions = new MessagingConventions();
endpointConfiguration.Conventions().Add(conventions);

builder.Host.UseNServiceBus(_ => endpointConfiguration);

// Build application
var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();
app.MapControllers();
app.MapMcp();
app.MapFallbackToFile("index.html");
app.MapHealthChecks("_healthcheck");

await app.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync();

app.Run();