using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Shared;
using Lamar;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.UI.Server;

public class UiServiceRegistry : ServiceRegistry
{
    public UiServiceRegistry()
    {
        this.AddScoped<DbContext, DataContext>();

        this.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<UiServiceRegistry>());
        this.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<HealthCheck>());
        this.AddTransient<IBus, Bus>();

        // Register background service
        // this.AddHostedService<WorkOrderAutoAssignmentService>();

        Scan(scanner =>
        {
            scanner.WithDefaultConventions();
            scanner.AssemblyContainingType<CanConnectToDatabaseHealthCheck>();
            scanner.AssemblyContainingType<HealthCheck>();
            scanner.AssemblyContainingType<Is64BitProcessHealthCheck>();
            scanner.AssemblyContainingType<CanConnectToLlmServerHealthCheck>();
            scanner.ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>));
            scanner.ConnectImplementationsToTypesClosing(typeof(INotificationHandler<>));
        });

        this.AddHealthChecks()
            .AddCheck<CanConnectToLlmServerHealthCheck>("LlmGateway")
            .AddCheck<CanConnectToDatabaseHealthCheck>("DataAccess")
            .AddCheck<Is64BitProcessHealthCheck>("Server")
            .AddCheck<HealthCheck>("API");
    }
}