using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess.Messaging;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.HostedEndpoint;
using ClearMeasure.HostedEndpoint.Configuration;
using Worker.Messaging;

namespace Worker;

public class WorkOrderEndpoint : ClearHostedEndpoint
{
    private const string EndpointName = "WorkOrderProcessing";
    private const string SchemaName = "nServiceBus";

    public WorkOrderEndpoint(IConfiguration configuration) : base(configuration)
    {
        EndpointOptions = new()
        {
            EndpointName = EndpointName,
            EnableInstallers = true,
            EnableMetrics = true,
            EnableOutbox = true,
            MaxConcurrency = Environment.ProcessorCount * 2,
            ImmediateRetryCount = 3,
            DelayedRetryCount = 3
        };

        SqlPersistenceOptions = new()
        {
            ConnectionString = Configuration.GetConnectionString("SqlConnectionString"),
            Schema = SchemaName,
            EnableSagaPersistence = true,
            EnableSubscriptionStorage = true
        };
    }

    // Configure endpoint options
    protected override EndpointOptions EndpointOptions { get; }

    // Configure SQL persistence for sagas
    protected override SqlPersistenceOptions SqlPersistenceOptions { get; }

    // Configure the message transport
    protected override void ConfigureTransport(EndpointConfiguration endpointConfiguration)
    {
        // OTEL
        endpointConfiguration.EnableOpenTelemetry();

        // transport
        var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
        transport.ConnectionString(SqlPersistenceOptions.ConnectionString);
        transport.DefaultSchema(SqlPersistenceOptions.Schema);
        transport.Transactions(TransportTransactionMode.TransactionScope);
        transport.Transport.TransportTransactionMode = TransportTransactionMode.ReceiveOnly;

        // message conventions
        var conventions = new MessagingConventions();
        endpointConfiguration.Conventions().Add(conventions);

        // routing
    }

    // Register services
    protected override void RegisterDependencyInjection(IServiceCollection services)
    {
        var apiUrl = Configuration["RemotableBus:ApiUrl"]
                     ?? throw new InvalidOperationException("RemotableBus:ApiUrl configuration is required.");

        services.AddHttpClient();

        services.AddSingleton<IBus>(sp =>
            new RemotableBus(sp.GetRequiredService<HttpClient>(), apiUrl));

        services.AddSingleton<ChatClientFactory>();
    }
}