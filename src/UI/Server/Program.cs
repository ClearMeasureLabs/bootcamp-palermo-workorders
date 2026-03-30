using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.ResponseCompression;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.DataAccess.Messaging;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.McpServer.Resources;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Configuration.AddEnvironmentVariables();
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
builder.Services.Configure<ApiKeyAuthenticationOptions>(
    builder.Configuration.GetSection(ApiKeyAuthenticationOptions.SectionName));
builder.Services.PostConfigure<ApiKeyAuthenticationOptions>(o =>
    o.ValidationKey = string.IsNullOrWhiteSpace(o.ValidationKey) ? null : o.ValidationKey.Trim());
builder.Services.Configure<RequestBodyBufferingOptions>(
    builder.Configuration.GetSection(RequestBodyBufferingOptions.SectionName));
builder.Services.AddServerCors(builder.Configuration);

builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(policy => policy.NoCache());
    options.AddPolicy(OutputCachePolicyNames.VersionMetadata, policy => policy
        .Expire(TimeSpan.FromMinutes(10))
        .SetVaryByQuery("*")
        .SetVaryByHeader("Accept"));
    options.AddPolicy(OutputCachePolicyNames.WeatherSample, policy => policy
        .Expire(TimeSpan.FromSeconds(30))
        .SetVaryByQuery("*")
        .SetVaryByHeader("Accept"));
});

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

app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

if (app.Services.IsServerCorsActive())
{
    app.UseCors(ServerCorsOptions.PolicyName);
}

if (string.Equals(app.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
{
    app.MapGet("/_test/compression-probe", () => Results.Text(new string('A', 4096), "text/plain; charset=utf-8"));
    app.MapPost("/_test/body-buffer-probe", async (HttpRequest request, CancellationToken cancellationToken) =>
    {
        using var firstReader = new StreamReader(request.Body, leaveOpen: true);
        var first = await firstReader.ReadToEndAsync(cancellationToken);
        if (request.Body.CanSeek)
        {
            request.Body.Position = 0;
        }

        using var secondReader = new StreamReader(request.Body, leaveOpen: true);
        var second = await secondReader.ReadToEndAsync(cancellationToken);
        return Results.Json(new { first, second });
    });
}

app.UseRequestBodyBuffering();

app.UseMiddleware<RateLimitingMiddleware>();
app.UseOutputCache();

app.MapRazorPages();
var apiControllers = app.MapControllers();
if (app.Services.IsServerCorsActive())
{
    apiControllers.RequireCors(ServerCorsOptions.PolicyName);
}

app.MapMcp("/mcp");
app.MapFallbackToFile("index.html");
app.MapHealthChecks("_healthcheck");

await app.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync();

app.Run();
