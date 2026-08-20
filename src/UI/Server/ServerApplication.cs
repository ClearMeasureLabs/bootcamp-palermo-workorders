using Asp.Versioning;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using ClearMeasure.Bootcamp.DataAccess.Messaging;
using ClearMeasure.Bootcamp.McpServer.Resources;
using ClearMeasure.Bootcamp.McpServer.Tools;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UI.Server.Grpc;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using ClearMeasure.Bootcamp.UI.Server.Notifications;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using ClearMeasure.Bootcamp.UI.Server.Testing;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using ChurchBulletin.ServiceDefaults;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Host bootstrap for UI.Server startup and pipeline configuration.
/// </summary>
public static class ServerApplication
{
    /// <summary>
    /// Builds and runs the web application host.
    /// </summary>
    public static async Task RunAsync(string[] args)
    {
        var app = BuildApplication(args);
        await app.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync();
        await app.RunAsync();
    }

    internal static WebApplication BuildApplication(
        string[] args,
        Action<WebApplicationBuilder>? configureBuilder = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        ConfigureWebHost(builder);
        RegisterServices(builder);
        configureBuilder?.Invoke(builder);
        RegisterNServiceBus(builder);
        var app = builder.Build();
        ConfigurePipeline(app);
        return app;
    }

    internal static bool ShouldUseLearningTransport(string? sqlConnectionString) =>
        !string.IsNullOrEmpty(sqlConnectionString)
        && sqlConnectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase);

    private static void ConfigureWebHost(WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ConfigureEndpointDefaults(listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            });
        });
    }

    private static void RegisterServices(WebApplicationBuilder builder)
    {
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
        builder.Services.AddMemoryCache();
        builder.Services.Configure<IdempotencyOptions>(
            builder.Configuration.GetSection(IdempotencyOptions.SectionName));
        builder.Services.AddSingleton<IdempotencyProbeState>();
        builder.Services.AddApiRateLimiting(builder.Configuration);
        builder.Services.AddApiRequestTimeouts(builder.Configuration);
        builder.Services.Configure<ApiKeyAuthenticationOptions>(
            builder.Configuration.GetSection(ApiKeyAuthenticationOptions.SectionName));
        builder.Services.Configure<DiagnosticsFeatureFlagsOptions>(
            builder.Configuration.GetSection(DiagnosticsFeatureFlagsOptions.SectionName));
        builder.Services.PostConfigure<ApiKeyAuthenticationOptions>(o =>
            o.ValidationKey = string.IsNullOrWhiteSpace(o.ValidationKey) ? null : o.ValidationKey.Trim());
        builder.Services.AddRequestDecompression();
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
        builder.Services.AddGrpc();
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
        builder.Services.AddApplicationInsightsTelemetry();
        builder.Services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new() { Name = "ChurchBulletin", Version = "1.0.0" };
            })
            .WithHttpTransport()
            .WithTools<WorkOrderTools>()
            .WithTools<EmployeeTools>()
            .WithResources<ReferenceResources>();
    }

    private static void RegisterNServiceBus(WebApplicationBuilder builder)
    {
        var endpointConfiguration = new NServiceBus.EndpointConfiguration("UI.Server");
        endpointConfiguration.UseSerialization<SystemJsonSerializer>();
        endpointConfiguration.EnableInstallers();
        endpointConfiguration.EnableOpenTelemetry();

        var sqlConnectionString = builder.Configuration.GetConnectionString("SqlConnectionString") ?? "";
        if (ShouldUseLearningTransport(sqlConnectionString))
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

        var conventions = new MessagingConventions();
        endpointConfiguration.Conventions().Add(conventions);
        builder.Host.UseNServiceBus(_ => endpointConfiguration);
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        app.UseSerilogShutdown();
        app.MapDefaultEndpoints();
        app.UseCorrelationId();
        app.UseWhen(
            context => ProblemDetailsPaths.IsMachineOriented(context.Request.Path),
            branch => branch.UseExceptionHandler(new ExceptionHandlerOptions
            {
                ExceptionHandler = context =>
                    ProblemDetailsExceptionHandler.HandleAsync(context, app.Environment)
            }));

        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRequestDecompression();
        app.UseResponseCompression();
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseWhen(
            context => ApiRateLimitingExtensions.ShouldApplyToPath(context.Request.Path),
            branch => branch.UseRequestTimeouts());
        app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
        app.UseMachineClientStatusCodeProblemDetails();

        if (app.Services.IsServerCorsActive())
        {
            app.UseCors(ServerCorsOptions.PolicyName);
        }

        app.UseWebSockets();
        app.UseMiddleware<RealtimeNotificationWebSocketMiddleware>();
        MapTestingEndpoints(app);
        app.UseRequestBodyBuffering();
        app.UseMiddleware<IdempotencyMiddleware>();
        app.UseMiddleware<WebServiceMessageValidationMiddleware>();
        app.UseMiddleware<RateLimitingMiddleware>();
        app.UseOutputCache();
        app.MapRazorPages();
        MapApiControllers(app);
        MapIdempotencyProbes(app);
        app.MapGrpcService<WorkOrdersGrpcService>();
        app.MapMcp("/mcp");
        app.MapFallback(FallbackToIndexHtml);
        app.MapGet("/_demo/setneedsreboot/{value:bool}", (bool value) =>
        {
            NeedsRebootHealthCheck.NeedsReboot = value;
            return Results.Text($"NeedsReboot set to {value}");
        });
        app.MapHealthChecks("_healthcheck");
        app.MapHealthChecks("_healthcheck/detailed", new HealthCheckOptions
        {
            ResponseWriter = DetailedHealthCheckResponseWriter.WriteAsync
        });
    }

    internal static void MapTestingEndpoints(WebApplication app)
    {
        if (!string.Equals(app.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        app.MapGet("/_test/compression-probe", () => Results.Text(new string('A', 4096), "text/plain; charset=utf-8"));
        app.MapGet(
            "/_test/realtime/connection-count",
            (IRealtimeNotificationHub hub) => Results.Json(new { count = hub.ConnectionCount }));
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
        app.MapPost("/__test/request-body-echo", async (HttpContext httpContext) =>
        {
            httpContext.Response.ContentType = "text/plain; charset=utf-8";
            using var reader = new StreamReader(httpContext.Request.Body);
            await httpContext.Response.WriteAsync(await reader.ReadToEndAsync());
        });
        app.MapGet(
                "/api/_test/request-timeout-probe",
                async (HttpContext httpContext) =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), httpContext.RequestAborted);
                    return Results.Ok();
                })
            .WithRequestTimeout(TimeSpan.FromMilliseconds(500));
    }

    private static void MapApiControllers(WebApplication app)
    {
        var apiControllers = app.MapControllers();
        ApiControllerMapping.ApplyRequestTimeout(app, apiControllers);
        ApiControllerMapping.ApplyCorsWhenActive(app, apiControllers);
    }

    private static void MapIdempotencyProbes(WebApplication app)
    {
        if (!string.Equals(app.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        static IResult Probe(IdempotencyProbeState state) =>
            Results.Text($"count={state.Next()}", "text/plain; charset=utf-8");
        app.MapPost("/api/_test/idempotency-probe", Probe);
        app.MapPut("/api/_test/idempotency-probe", Probe);
    }

    internal static async Task FallbackToIndexHtml(HttpContext context)
    {
        if (ProblemDetailsPaths.IsMachineOriented(context.Request.Path))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var fileInfo = env.WebRootFileProvider.GetFileInfo("index.html");
        if (!fileInfo.Exists)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        await using var stream = fileInfo.CreateReadStream();
        await stream.CopyToAsync(context.Response.Body);
    }
}
