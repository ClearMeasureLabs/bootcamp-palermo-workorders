using System.Collections.Concurrent;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.McpServer;
using MediatR;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpToolRateLimitingIntegrationTests
{
    private static readonly ConcurrentQueue<AuditLogRecord> AuditRecords = new();
    private WebApplication? _application;
    private string? _databasePath;

    [OneTimeSetUp]
    public async Task StartMcpHttpHost()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"mcp-ratelimit-{Guid.NewGuid():N}.db");
        _application = McpServerApplication.BuildApplication(["--http"], builder =>
        {
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = $"Data Source={_databasePath}",
                ["McpRateLimiting:Default:Capacity"] = "1",
                ["McpRateLimiting:Default:RefillTokens"] = "1",
                ["McpRateLimiting:Default:RefillPeriodSeconds"] = "3600",
                ["McpRateLimiting:Tools:list-work-orders:Capacity"] = "1",
                ["McpRateLimiting:Tools:list-work-orders:RefillTokens"] = "1",
                ["McpRateLimiting:Tools:list-work-orders:RefillPeriodSeconds"] = "3600"
            });
            builder.Services.AddSingleton<INotificationHandler<AuditLogRecord>>(
                new StubAuditLogRecordHandler(AuditRecords));
        });

        await _application.StartAsync();
        await using var scope = _application.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DbContext>().Database.EnsureCreatedAsync();
    }

    [OneTimeTearDown]
    public async Task StopMcpHttpHost()
    {
        if (_application != null)
        {
            await _application.StopAsync();
            await _application.DisposeAsync();
        }

        if (_databasePath != null && File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    [Test]
    public async Task ShouldRejectExcessToolCallsAndPublishAuditRecordForSession()
    {
        while (AuditRecords.TryDequeue(out _))
        {
        }

        var mcpUrl = GetMcpUrl();
        using var firstHttpClient = new HttpClient();
        var firstTransport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri(mcpUrl) },
            firstHttpClient);
        await using var firstClient = await McpClient.CreateAsync(firstTransport);

        var firstCall = await firstClient.CallToolAsync("list-work-orders");
        firstCall.IsError.ShouldNotBe(true);

        var rejectedCall = await firstClient.CallToolAsync("list-work-orders");
        rejectedCall.IsError.ShouldBe(true);
        rejectedCall.Content.OfType<TextContentBlock>().Single().Text.ShouldContain("MCP tool rate limit exceeded");

        using var secondHttpClient = new HttpClient();
        var secondTransport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri(mcpUrl) },
            secondHttpClient);
        await using var secondClient = await McpClient.CreateAsync(secondTransport);
        var secondSessionCall = await secondClient.CallToolAsync("list-work-orders");
        secondSessionCall.IsError.ShouldNotBe(true);

        AuditRecords.TryDequeue(out var auditRecord).ShouldBeTrue();
        auditRecord!.Reason.ShouldBe("MCP tool rate limit exceeded.");
        auditRecord.ToolName.ShouldBe("list-work-orders");
    }

    private string GetMcpUrl()
    {
        var server = _application!.Services.GetRequiredService<IServer>();
        var address = server.Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        return $"{address}/mcp";
    }

    private sealed class StubAuditLogRecordHandler(ConcurrentQueue<AuditLogRecord> records)
        : INotificationHandler<AuditLogRecord>
    {
        public Task Handle(AuditLogRecord notification, CancellationToken cancellationToken)
        {
            records.Enqueue(notification);
            return Task.CompletedTask;
        }
    }
}
