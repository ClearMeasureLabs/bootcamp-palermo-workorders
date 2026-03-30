using Asp.Versioning;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.DataAccess.Messaging;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.McpServer.Resources;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddProblemDetails();
builder.Services.AddControllersWithViews()
    .AddApplicationPart(typeof(DetailedHealthController).Assembly);
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
    options.UnsupportedApiVersionStatusCode = StatusCodes.Status400BadRequest;
}).AddMvc();
builder.Services.AddRazorPages();
builder.Host.UseLamar(registry => { registry.IncludeRegistry<UiServiceRegistry>(); });
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IDistributedBus, DistributedBus>();
builder.Services.AddApiRateLimiting(builder.Configuration);

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Add MCP server (HTTP transport at /mcp)
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "ChurchBulletin", Version = "1.0.0" };
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

app.UseSerilogShutdown();
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
app.UseCorrelationId();

app.UseWhen(
    context => ProblemDetailsPaths.IsMachineOriented(context.Request.Path),
    branch => branch.UseExceptionHandler(new ExceptionHandlerOptions
    {
        ExceptionHandler = context => ProblemDetailsExceptionHandler.HandleAsync(context, app.Environment)
    }));

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

app.UseResponseCompression();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseMachineClientStatusCodeProblemDetails();

if (string.Equals(app.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
{
    app.MapGet("/_test/compression-probe", () => Results.Text(new string('A', 4096), "text/plain; charset=utf-8"));
}

app.UseApiRateLimiting();

app.MapRazorPages();
app.MapControllers().RequireRateLimiting(ApiRateLimitingPolicyNames.ApiSlidingWindow);
app.MapMcp("/mcp");
app.MapFallback(async context =>
{
    if (ProblemDetailsPaths.IsMachineOriented(context.Request.Path))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
    var filePath = Path.Combine(env.WebRootPath ?? string.Empty, "index.html");
    if (!File.Exists(filePath))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(filePath);
});
app.MapHealthChecks("_healthcheck");

await app.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync();

app.Run();