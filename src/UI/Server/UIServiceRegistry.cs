using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Lamar;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Client;
using ClearMeasure.Bootcamp.UI.Shared;

namespace ClearMeasure.Bootcamp.UI.Server;

public class UiServiceRegistry : ServiceRegistry
{
    public UiServiceRegistry()
    {
        this.AddScoped<DbContext, DataContext>();
        
        this.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<UiServiceRegistry>());
        this.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Api.HealthCheck>());
        this.AddTransient<IBus, Bus>();

        Scan(scanner =>
        {
            scanner.WithDefaultConventions();
            scanner.AssemblyContainingType<CanConnectToDatabaseHealthCheck>();
            scanner.AssemblyContainingType<Api.HealthCheck>();
            scanner.AssemblyContainingType<Is64BitProcessHealthCheck>();
            scanner.AssemblyContainingType<CanConnectToLlmServerHealthCheck>();
            scanner.ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>));
            scanner.ConnectImplementationsToTypesClosing(typeof(INotificationHandler<>));
        });

        this.AddHealthChecks()
            .AddCheck<CanConnectToLlmServerHealthCheck>("LlmGateway")
            .AddCheck<CanConnectToDatabaseHealthCheck>("DataAccess")
            .AddCheck<Is64BitProcessHealthCheck>("Server")
            .AddCheck<Api.HealthCheck>("API");
    }
}