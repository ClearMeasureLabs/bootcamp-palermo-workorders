using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.DataAccess.Messaging;
using ClearMeasure.HostedEndpoint;
using ClearMeasure.HostedEndpoint.Configuration;

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
        var routing = transport.Routing();
        routing.RouteToEndpoint(typeof(WorkOrderAssignedToBotEvent).Assembly, typeof(WorkOrderAssignedToBotEvent).Namespace, EndpointName);
    }

    // Register message handlers and services
    protected override void RegisterDependencyInjection(IServiceCollection services)
    {
        // TODO: register services and message handlers here
    }
}
