using System.Net;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UI.Server.Grpc;
using DomainWorkOrder = ClearMeasure.Bootcamp.Core.Model.WorkOrder;
using ClearMeasure.Bootcamp.UnitTests.Api;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class GrpcWorkOrderIntegrationTests
{
    private SqliteConnection? _sharedMemoryHold;
    private GrpcWebApplicationFactory? _factory;
    private HttpClient? _httpClient;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _sharedMemoryHold = new SqliteConnection(GrpcWebApplicationFactory.SqliteSharedMemoryConnectionString);
        _sharedMemoryHold.Open();
        _factory = new GrpcWebApplicationFactory();
        _httpClient = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _httpClient?.Dispose();
        _factory?.Dispose();
        _sharedMemoryHold?.Dispose();
    }

    private GrpcChannel CreateGrpcChannel()
    {
        var handler = _factory!.Server.CreateHandler();
        return GrpcChannel.ForAddress(_httpClient!.BaseAddress!, new GrpcChannelOptions
        {
            HttpHandler = handler,
            HttpVersion = new Version(2, 0),
            DisposeHttpClient = false
        });
    }

    [Test]
    public async Task Should_StartHost_When_GrpcConfigured()
    {
        using var channel = CreateGrpcChannel();
        var client = new WorkOrders.WorkOrdersClient(channel);
        var reply = await client.PingAsync(new PingRequest());
        reply.Message.ShouldBe("ok");
    }

    [Test]
    public async Task Should_ReturnExpectedPayload_When_UnaryGrpcCallSucceeded()
    {
        using (var scope = _factory!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            db.Database.EnsureCreated();
            var creator = new Employee("grpc-creator", "G", "Rpc", "g@t.test");
            db.Add(creator);
            var order = new DomainWorkOrder
            {
                Number = "GRPC-001",
                Title = "Test title",
                Description = "Test description",
                Instructions = "Bring extension cord",
                RoomNumber = "101",
                Status = WorkOrderStatus.Draft,
                Creator = creator,
                CreatedDate = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc)
            };
            db.Add(order);
            db.SaveChanges();
        }

        using var channel = CreateGrpcChannel();
        var client = new WorkOrders.WorkOrdersClient(channel);
        var reply = await client.GetWorkOrderByNumberAsync(new GetWorkOrderByNumberRequest { Number = "GRPC-001" });
        reply.WorkOrder.ShouldNotBeNull();
        reply.WorkOrder.Number.ShouldBe("GRPC-001");
        reply.WorkOrder.Title.ShouldBe("Test title");
        reply.WorkOrder.Description.ShouldBe("Test description");
        reply.WorkOrder.Instructions.ShouldBe("Bring extension cord");
        reply.WorkOrder.RoomNumber.ShouldBe("101");
        reply.WorkOrder.StatusKey.ShouldBe(WorkOrderStatus.Draft.Key);
        reply.WorkOrder.CreatorUsername.ShouldBe("grpc-creator");
        reply.WorkOrder.CreatedDateUtc.ShouldNotBeNull();
    }

    [Test]
    public void Should_MapGrpcStatus_When_InvalidArgumentOrNotFound()
    {
        using var channel = CreateGrpcChannel();
        var client = new WorkOrders.WorkOrdersClient(channel);

        var emptyEx = Should.Throw<RpcException>(async () =>
            await client.GetWorkOrderByNumberAsync(new GetWorkOrderByNumberRequest { Number = "   " }));
        emptyEx.StatusCode.ShouldBe(StatusCode.InvalidArgument);

        var notFoundEx = Should.Throw<RpcException>(async () =>
            await client.GetWorkOrderByNumberAsync(new GetWorkOrderByNumberRequest { Number = "missing-wo-999" }));
        notFoundEx.StatusCode.ShouldBe(StatusCode.NotFound);
    }

    [Test]
    public async Task Should_ExcludeGrpcFromApiRateLimiter_When_DesignRequires()
    {
        await using var hold = new SqliteConnection(GrpcWebApplicationFactory.RateLimitTestSqliteConnectionString);
        await hold.OpenAsync();
        await using var rateLimitedFactory =
            new RateLimitedApiWebApplicationFactory(GrpcWebApplicationFactory.RateLimitTestSqliteConnectionString);
        var http = rateLimitedFactory.CreateClient();

        var first = await http.GetAsync("/api/version");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var second = await http.GetAsync("/api/version");
        second.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

        using var channel = GrpcChannel.ForAddress(http.BaseAddress!, new GrpcChannelOptions
        {
            HttpHandler = rateLimitedFactory.Server.CreateHandler(),
            HttpVersion = new Version(2, 0),
            DisposeHttpClient = false
        });
        var grpcClient = new WorkOrders.WorkOrdersClient(channel);
        var ping = await grpcClient.PingAsync(new PingRequest());
        ping.Message.ShouldBe("ok");
    }

    [Test]
    public async Task Should_ServeRestAndGrpc_When_BothRegistered()
    {
        var rest = await _httpClient!.GetAsync("/api/health");
        rest.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var channel = CreateGrpcChannel();
        var grpcClient = new WorkOrders.WorkOrdersClient(channel);
        var ping = await grpcClient.PingAsync(new PingRequest());
        ping.Message.ShouldBe("ok");
    }

}
