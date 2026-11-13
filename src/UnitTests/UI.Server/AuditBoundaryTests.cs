using System.Net;
using System.Security.Claims;
using System.Diagnostics;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.McpServer.Auditing;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class AuditBoundaryTests
{
    [TestCase(20)]
    [TestCase(200)]
    public void ShouldPreferSelectedCorrelationIdentifierWithBoundedLength(int length)
    {
        var context = new DefaultHttpContext { TraceIdentifier = "different-server-trace" };
        context.Items[ChurchBulletin.ServiceDefaults.CorrelationIdConstants.HttpContextItemKey] =
            new string('c', length);

        var metadata = AuditRequestMiddleware.CreateContext(context);

        metadata.CorrelationId.ShouldBe(new string('c', Math.Min(length, 64)));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ShouldExcludeAuthenticationIdentityFromBusTelemetry(bool login)
    {
        object notification = login
            ? new ClearMeasure.Bootcamp.Core.Model.Events.UserLoggedInEvent("private-identity")
            : new ClearMeasure.Bootcamp.Core.Model.Events.UserLoggedOutEvent("private-identity");
        using var activity = new Activity("AuditAuthenticationTest").Start();

        ClearMeasure.Bootcamp.UI.Shared.BusActivityTagger.AddScalarPropertyTags(notification, activity);

        activity.GetTagItem("bus.message.UserName").ShouldBeNull();
        activity.TagObjects.ShouldBeEmpty();
    }

    [TestCase("audit-read", "returned")]
    [TestCase("audit-fail", "exception")]
    [TestCase("create-work-order", "returned")]
    public async Task ShouldAuditActualMcpSdkInvocationsCentrally(string tool, string outcome)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var audit = new StubAuditLog();
        builder.Services.AddSingleton<IAuditLog>(audit);
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddMcpServer().WithHttpTransport().WithTools<StubAuditTools>()
            .WithRequestFilters(filters => filters.AddCallToolFilter(McpAuditFilter.Wrap));
        await using var app = builder.Build();
        app.UseMiddleware<AuditRequestMiddleware>();
        app.MapMcp("/mcp");
        await app.StartAsync();
        using var http = app.GetTestClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("audit-test");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await using var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri(http.BaseAddress!, "mcp") }, http);
        await using var client = await McpClient.CreateAsync(transport, cancellationToken: timeout.Token);

        await client.CallToolAsync(tool, new Dictionary<string, object?> { ["secret"] = "sensitive argument" },
            cancellationToken: timeout.Token);

        audit.NewValue.ShouldContain(tool);
        audit.NewValue.ShouldContain(outcome);
        audit.NewValue.ShouldNotContain("sensitive");
        audit.Metadata!.ActorUserName.ShouldBe("anonymous");
        audit.Metadata.UserAgent.ShouldBe("audit-test");
    }

    [TestCase("/api/audit")]
    [TestCase("/api/v1.0/audit")]
    public async Task ShouldProtectBothHttpAuditRoutesWithoutTrustingGeneralClientKey(string path)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AuditLog:AdministratorApiKey"] = "server-admin-key"
        });
        var audit = new StubAuditLog();
        builder.Services.AddSingleton<IAuditLog>(audit);
        builder.Services.AddControllers().AddApplicationPart(typeof(AuditController).Assembly);
        builder.Services.AddApiVersioning(options =>
        {
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
        }).AddMvc();
        await using var app = builder.Build();
        app.MapControllers();
        await app.StartAsync();
        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "server-admin-key");

        using var denied = await client.GetAsync(path);
        denied.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        audit.ReadCalls.ShouldBe(0);
        client.DefaultRequestHeaders.Add("X-Audit-Api-Key", "server-admin-key");
        using var allowed = await client.GetAsync(path);
        allowed.StatusCode.ShouldBe(HttpStatusCode.OK);
        audit.ReadCalls.ShouldBe(1);
    }

    [Test]
    public async Task ShouldCaptureBoundedMetadataWithoutTrustingIdentityHeaders()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-User-Name"] = "administrator";
        http.Request.Headers.UserAgent = new string('a', 1000);
        http.TraceIdentifier = new string('c', 300);
        http.Connection.RemoteIpAddress = IPAddress.Loopback;
        using var outer = AuditContext.Begin(new AuditContext("outer"));
        var middleware = new AuditRequestMiddleware(_ =>
        {
            AuditContext.Current.ActorUserName.ShouldBe("anonymous");
            AuditContext.Current.UserAgent!.Length.ShouldBe(500);
            AuditContext.Current.CorrelationId!.Length.ShouldBe(64);
            AuditContext.Current.IpAddress.ShouldBe("127.0.0.1");
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(http);

        AuditContext.Current.ActorUserName.ShouldBe("outer");
    }

    [Test]
    public async Task ShouldCaptureAuthenticatedIdentityAndRestoreOnFailure()
    {
        var http = new DefaultHttpContext
        {
            User = Principal("trusted", true)
        };
        using var outer = AuditContext.Begin(new AuditContext("outer"));
        var failure = new InvalidOperationException("business");
        var middleware = new AuditRequestMiddleware(_ =>
        {
            AuditContext.Current.ActorUserName.ShouldBe("trusted");
            throw failure;
        });

        (await Should.ThrowAsync<InvalidOperationException>(() => middleware.InvokeAsync(http))).ShouldBeSameAs(failure);
        AuditContext.Current.ActorUserName.ShouldBe("outer");
    }

    [TestCase(null, null)]
    [TestCase(null, "client-api-key")]
    [TestCase("admin-key", "client-api-key")]
    [TestCase("admin-key", "wrong")]
    public async Task ShouldDenyAuditAccessWithoutSeparateAdministratorCredential(string? expected, string? provided)
    {
        var audit = new StubAuditLog();
        var controller = Controller(audit, expected);
        controller.Request.Headers["X-Api-Key"] = "client-api-key";
        if (provided is not null)
            controller.Request.Headers["X-Audit-Api-Key"] = provided;

        (await controller.Get()).ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(403);
        audit.ReadCalls.ShouldBe(0);
    }

    [TestCase(true, true, 200)]
    [TestCase(false, true, 403)]
    [TestCase(true, false, 403)]
    public async Task ShouldRequireBothAuthenticationAndAdminRole(bool authenticated, bool adminRole, int status)
    {
        var controller = Controller(new StubAuditLog());
        controller.HttpContext.User = Principal("trusted", authenticated, adminRole);

        var result = await controller.Get();

        if (status == 200)
            result.ShouldBeOfType<OkObjectResult>();
        else
            result.ShouldBeOfType<StatusCodeResult>().StatusCode.ShouldBe(status);
    }

    [TestCase("2026-01-02T03:04:05Z")]
    [TestCase("2026-01-02")]
    public async Task ShouldReadWithSeparateKeyUtcFilterAndKeysetCursor(string from)
    {
        var audit = new StubAuditLog();
        var controller = Controller(audit, "admin-key");
        controller.Request.Headers["X-Audit-Api-Key"] = "admin-key";
        var id = Guid.NewGuid();

        (await controller.Get("WorkOrder", id, from, 50)).ShouldBeOfType<OkObjectResult>();

        audit.ReadCalls.ShouldBe(1);
        audit.From!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        audit.BeforeId.ShouldBe(50);
        audit.EntityId.ShouldBe(id);
        controller.Response.Headers.CacheControl.ToString().ShouldBe("no-store");
    }

    [TestCase("2026-01-02T03:04:05", null, null)]
    [TestCase("2026-01-02T03:04:05+01:00", null, null)]
    [TestCase("invalidZ", null, null)]
    [TestCase(null, 0, null)]
    [TestCase(null, -1, null)]
    [TestCase(null, null, " ")]
    public async Task ShouldRejectInvalidQueryFilters(string? from, long? beforeId, string? entityType)
    {
        var audit = new StubAuditLog();
        var controller = Controller(audit, "admin-key");
        controller.Request.Headers["X-Audit-Api-Key"] = "admin-key";

        (await controller.Get(entityType: entityType, from: from, beforeId: beforeId))
            .ShouldBeOfType<ObjectResult>().StatusCode.ShouldBe(400);
        audit.ReadCalls.ShouldBe(0);
    }

    [TestCase("/api/audit")]
    [TestCase("/api/v1.0/audit")]
    public void ShouldRequireGeneralApiKeyInAdditionToAuditAdministratorAuthorization(string path)
    {
        ApiKeyAuthenticationMiddleware.ShouldValidate(path,
            new ApiKeyAuthenticationOptions { Enabled = true, ValidationKey = "client-key" }).ShouldBeTrue();
    }

    [TestCase(false, "returned")]
    [TestCase(true, "error")]
    public async Task ShouldAuditReadToolAndErrorResultWithoutRecordingPayload(bool isError, string outcome)
    {
        var audit = new StubAuditLog();
        var result = new CallToolResult
        {
            IsError = isError,
            Content = [new TextContentBlock { Text = "secret result" }]
        };

        (await InvokeMcpAsync(audit, "GetWorkOrder", () => ValueTask.FromResult(result)))
            .ShouldBeSameAs(result);

        audit.Action.ShouldBe("McpToolInvoked");
        audit.NewValue.ShouldContain(outcome);
        audit.NewValue.ShouldContain("GetWorkOrder");
        audit.NewValue.ShouldNotContain("secret");
    }

    [Test]
    public async Task ShouldPreserveToolExceptionAndAuditSanitizedFailure()
    {
        var audit = new StubAuditLog();
        var failure = new InvalidOperationException("secret exception payload");

        var thrown = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await InvokeMcpAsync(audit, "Read", () => throw failure));

        thrown.ShouldBeSameAs(failure);
        audit.NewValue.ShouldContain("exception");
        audit.NewValue.ShouldNotContain("secret");
    }

    [Test]
    public async Task ShouldPropagateAuditFailureAfterSuccessfulToolInvocation()
    {
        var audit = new StubAuditLog { WriteFailure = new InvalidOperationException("storage unavailable") };

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await InvokeMcpAsync(audit, "Read",
                () => ValueTask.FromResult(new CallToolResult())));
    }

    [Test]
    public async Task ShouldPreserveBusinessFailureWhenAuditPersistenceAlsoFails()
    {
        var audit = new StubAuditLog { WriteFailure = new IOException("storage unavailable") };
        var failure = new InvalidOperationException("business failure");

        (await Should.ThrowAsync<InvalidOperationException>(async () =>
            await InvokeMcpAsync(audit, "Read", () => throw failure)))
            .ShouldBeSameAs(failure);
    }

    [Test]
    public async Task ShouldPurgeWithConfiguredUtcCutoffAndStopPromptly()
    {
        var audit = new StubAuditLog();
        using var provider = new ServiceCollection().AddScoped<IAuditLog>(_ => audit).BuildServiceProvider();
        using var worker = new AuditRetentionService(provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new AuditRetentionOptions { RetentionDays = 30 }), TimeProvider.System,
            NullLogger<AuditRetentionService>.Instance);
        var earliest = DateTime.UtcNow.AddDays(-30);

        await worker.StartAsync(CancellationToken.None);
        await audit.Purged.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StopAsync(stopTimeout.Token);

        audit.Cutoff.Kind.ShouldBe(DateTimeKind.Utc);
        audit.Cutoff.ShouldBeGreaterThanOrEqualTo(earliest);
        audit.Cutoff.ShouldBeLessThanOrEqualTo(DateTime.UtcNow.AddDays(-30));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ShouldIsolateMcpOutcomeWritesFromPendingBusinessScope(bool fail)
    {
        var unitsOfWork = new List<StubScopedUnitOfWork>();
        StubAuditLog? writer = null;
        using var services = new ServiceCollection()
            .AddScoped(_ =>
            {
                var unit = new StubScopedUnitOfWork();
                unitsOfWork.Add(unit);
                return unit;
            })
            .AddScoped<IAuditLog>(provider => writer = new StubAuditLog(
                provider.GetRequiredService<StubScopedUnitOfWork>()))
            .BuildServiceProvider();
        using var requestScope = services.CreateScope();
        var business = requestScope.ServiceProvider.GetRequiredService<StubScopedUnitOfWork>();
        var failure = new InvalidOperationException("business failed");
        using var metadata = AuditContext.Begin(new AuditContext("trusted"));
        Func<ValueTask<CallToolResult>> invoke = () =>
        {
            business.PendingBusinessChanges = true;
            if (fail) throw failure;
            return ValueTask.FromResult(new CallToolResult());
        };

        if (fail)
            (await Should.ThrowAsync<InvalidOperationException>(async () =>
                await McpAuditFilter.InvokeAsync(
                    requestScope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
                    NullLogger.Instance, "Read", invoke))).ShouldBeSameAs(failure);
        else
            await McpAuditFilter.InvokeAsync(
                requestScope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
                NullLogger.Instance, "Read", invoke);

        unitsOfWork.Count.ShouldBe(2);
        business.PendingBusinessChanges.ShouldBeTrue();
        business.Saved.ShouldBeFalse();
        unitsOfWork[1].Saved.ShouldBeTrue();
        unitsOfWork[1].PendingBusinessChanges.ShouldBeFalse();
        writer!.Metadata!.ActorUserName.ShouldBe("trusted");
    }

    [Test]
    public async Task ShouldPassCancellationToRetentionStorage()
    {
        var audit = new StubAuditLog();
        using var provider = new ServiceCollection().AddScoped<IAuditLog>(_ => audit).BuildServiceProvider();
        using var worker = new AuditRetentionService(provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new AuditRetentionOptions()), TimeProvider.System,
            NullLogger<AuditRetentionService>.Instance);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => worker.PurgeExpiredAsync(cancellation.Token));
    }

    [TestCase(false, 1440)]
    [TestCase(true, 5)]
    public async Task ShouldScheduleDailyRetentionOrLogAndRetryOnFailure(bool fail, int delayMinutes)
    {
        var audit = new StubAuditLog { PurgeFailure = fail ? new IOException("storage unavailable") : null };
        using var provider = new ServiceCollection().AddScoped<IAuditLog>(_ => audit).BuildServiceProvider();
        var clock = new StubDelayTimeProvider();
        var logger = new StubRetentionLogger();
        using var worker = new AuditRetentionService(provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new AuditRetentionOptions()), clock, logger);

        await worker.StartAsync(CancellationToken.None);
        var delay = await clock.DelayScheduled.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StopAsync(stopTimeout.Token);

        delay.ShouldBe(TimeSpan.FromMinutes(delayMinutes));
        logger.Errors.ShouldBe(fail ? 1 : 0);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(36501)]
    public void ShouldRejectInvalidRetentionConfiguration(int days)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AuditLog:RetentionDays"] = days.ToString()
        }).Build();
        using var provider = new ServiceCollection().AddAuditRetention(config).BuildServiceProvider();

        Should.Throw<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<AuditRetentionOptions>>().Value);
    }

    private static ClaimsPrincipal Principal(string name, bool authenticated, bool adminRole = false) =>
        new(new ClaimsIdentity(
            adminRole ? [new Claim(ClaimTypes.Name, name), new Claim(ClaimTypes.Role, "Admin")]
                : [new Claim(ClaimTypes.Name, name)],
            authenticated ? "trusted-server-auth" : null));

    private static AuditController Controller(IAuditLog audit, string? key = null) =>
        new(audit, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AuditLog:AdministratorApiKey"] = key
        }).Build())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    private static async ValueTask<CallToolResult> InvokeMcpAsync(
        IAuditLog audit, string toolName, Func<ValueTask<CallToolResult>> invoke)
    {
        using var services = new ServiceCollection().AddScoped<IAuditLog>(_ => audit).BuildServiceProvider();
        return await McpAuditFilter.InvokeAsync(services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger.Instance, toolName, invoke);
    }

    private sealed class StubScopedUnitOfWork
    {
        public bool PendingBusinessChanges { get; set; }
        public bool Saved { get; set; }
    }

    private sealed class StubAuditLog(StubScopedUnitOfWork? unitOfWork = null) : IAuditLog
    {
        public int ReadCalls { get; private set; }
        public DateTime? From { get; private set; }
        public long? BeforeId { get; private set; }
        public Guid? EntityId { get; private set; }
        public string? Action { get; private set; }
        public string NewValue { get; private set; } = "";
        public AuditContext? Metadata { get; private set; }
        public Exception? WriteFailure { get; init; }
        public Exception? PurgeFailure { get; init; }
        public DateTime Cutoff { get; private set; }
        public TaskCompletionSource Purged { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WriteAsync(string action, string entityType, Guid? entityId = null, string? previousValue = null,
            string? newValue = null, CancellationToken cancellationToken = default)
        {
            if (WriteFailure is not null) throw WriteFailure;
            if (unitOfWork is not null) unitOfWork.Saved = true;
            Action = action;
            Metadata = AuditContext.Current;
            NewValue = newValue ?? "";
            return Task.CompletedTask;
        }

        public Task<AuditLog[]> ReadAsync(string? entityType, Guid? entityId, DateTime? from, long? beforeId,
            CancellationToken cancellationToken = default)
        {
            ReadCalls++;
            From = from;
            BeforeId = beforeId;
            EntityId = entityId;
            return Task.FromResult(Array.Empty<AuditLog>());
        }

        public Task<int> PurgeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (PurgeFailure is not null) throw PurgeFailure;
            Cutoff = cutoffUtc;
            Purged.TrySetResult();
            return Task.FromResult(0);
        }

    }

    private sealed class StubDelayTimeProvider : TimeProvider
    {
        public TaskCompletionSource<TimeSpan> DelayScheduled { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new Timer(callback, state, Timeout.InfiniteTimeSpan, period);
            DelayScheduled.TrySetResult(dueTime);
            return timer;
        }
    }

    private sealed class StubRetentionLogger : ILogger<AuditRetentionService>
    {
        public int Errors { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error) Errors++;
        }
    }

    [McpServerToolType]
    public sealed class StubAuditTools
    {
        [McpServerTool(Name = "audit-read")]
        public static string Read(string secret) => secret;

        [McpServerTool(Name = "audit-fail")]
        public static string Fail(string secret) => throw new InvalidOperationException(secret);

        [McpServerTool(Name = "create-work-order")]
        public static string LegacyFailure(string secret) => $"Employee with username '{secret}' not found.";
    }
}
