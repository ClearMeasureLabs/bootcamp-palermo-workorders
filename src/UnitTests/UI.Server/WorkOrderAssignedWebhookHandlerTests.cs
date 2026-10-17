using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class WorkOrderAssignedWebhookHandlerTests
{
    [Test]
    public async Task NotifyAsync_WhenUrlConfigured_PostsNumberTitleAssigneeJson()
    {
        var stubHandler = new StubHttpMessageHandler();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [WorkOrderAssignedWebhookHandler.ConfigKey] = "http://localhost/hooks/assigned"
            })
            .Build();
        var handler = new WorkOrderAssignedWebhookHandler(
            new StubHttpClientFactory(stubHandler),
            configuration,
            NullLogger<WorkOrderAssignedWebhookHandler>.Instance);
        var workOrder = new WorkOrder
        {
            Number = "WO-100",
            Title = "Fix lights",
            Assignee = new Employee("gwillie", "Gordon", "Willie", "g@example.com")
        };

        await handler.NotifyAsync(workOrder);

        stubHandler.LastRequest.ShouldNotBeNull();
        stubHandler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
        stubHandler.LastRequest.RequestUri!.ToString().ShouldBe("http://localhost/hooks/assigned");
        stubHandler.LastRequestBody.ShouldNotBeNull();
        using var doc = JsonDocument.Parse(stubHandler.LastRequestBody!);
        doc.RootElement.GetProperty("number").GetString().ShouldBe("WO-100");
        doc.RootElement.GetProperty("title").GetString().ShouldBe("Fix lights");
        doc.RootElement.GetProperty("assignee").GetString().ShouldBe("gwillie");
    }

    [Test]
    public async Task NotifyAsync_WhenUrlEmpty_SkipsPost()
    {
        var stubHandler = new StubHttpMessageHandler();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [WorkOrderAssignedWebhookHandler.ConfigKey] = ""
            })
            .Build();
        var handler = new WorkOrderAssignedWebhookHandler(
            new StubHttpClientFactory(stubHandler),
            configuration,
            NullLogger<WorkOrderAssignedWebhookHandler>.Instance);

        await handler.NotifyAsync(new WorkOrder { Number = "WO-1", Title = "x" });

        stubHandler.LastRequest.ShouldBeNull();
    }

    [Test]
    public async Task NotifyAsync_WhenHttpFails_DoesNotThrow()
    {
        var stubHandler = new StubHttpMessageHandler { ThrowOnSend = true };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [WorkOrderAssignedWebhookHandler.ConfigKey] = "http://localhost/hooks/assigned"
            })
            .Build();
        var handler = new WorkOrderAssignedWebhookHandler(
            new StubHttpClientFactory(stubHandler),
            configuration,
            NullLogger<WorkOrderAssignedWebhookHandler>.Instance);

        await Should.NotThrowAsync(async () =>
            await handler.NotifyAsync(new WorkOrder { Number = "WO-2", Title = "y" }));
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastRequestBody { get; private set; }

        public bool ThrowOnSend { get; init; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (ThrowOnSend)
            {
                throw new HttpRequestException("simulated failure");
            }

            LastRequest = request;
            if (request.Content != null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
