using ClearMeasure.Bootcamp.DataAccess.Messaging;
using ClearMeasure.HostedEndpoint;
using ClearMeasure.HostedEndpoint.Configuration;

namespace Worker;

public class WorkOrderEndpoint : ClearHostedEndpoint
{
    public WorkOrderEndpoint(IConfiguration configuration) : base(configuration)
    {
        EndpointOptions = new()
        {
            EndpointName = "WorkOrderProcessing",
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
            Schema = "nServiceBus",
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
        // endpoint options
        endpointConfiguration.SendOnly();

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

    // Register message handlers and services
    protected override void RegisterDependencyInjection(IServiceCollection services)
    {
        // TODO: register services and message handlers here
    }
}
