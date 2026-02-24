using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.UI.Shared;
using Lamar;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClearMeasure.Bootcamp.McpServer;

public class McpServiceRegistry : ServiceRegistry
{
    public McpServiceRegistry()
    {
        this.AddScoped<DbContext, DataContext>();

        this.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<McpServiceRegistry>());
        this.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<DataContext>());

        this.AddTransient<IBus>(provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return new Bus(mediator);
        });

        this.AddTransient<IDatabaseConfiguration, DatabaseConfiguration>();
        this.AddSingleton<TimeProvider>(TimeProvider.System);

        Scan(scanner =>
        {
            scanner.WithDefaultConventions();
            scanner.AssemblyContainingType<IBus>();
            scanner.AssemblyContainingType<DataContext>();
            scanner.ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>));
            scanner.ConnectImplementationsToTypesClosing(typeof(INotificationHandler<>));
        });

        // Register after scanning to override the auto-discovered DistributedBus
        // which requires NServiceBus IMessageSession (not available in the MCP server)
        this.AddTransient<IDistributedBus, NullDistributedBus>();
    }
}
