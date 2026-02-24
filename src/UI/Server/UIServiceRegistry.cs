using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Shared;
using Lamar;
using MediatR;
using Microsoft.EntityFrameworkCore;
using FunJeffreyCustomEventHealthCheck = ClearMeasure.Bootcamp.UI.Shared.FunJeffreyCustomEventHealthCheck;

namespace ClearMeasure.Bootcamp.UI.Server;

public class UiServiceRegistry : ServiceRegistry
{
    public UiServiceRegistry()
    {
        this.AddScoped<DbContext, DataContext>();

        this.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<UiServiceRegistry>());
        this.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<HealthCheck>());
        this.AddTransient<IBus>(provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            return new Bus(mediator);
        });

        // Register AI agent and background service
        this.AddTransient<WorkOrderReformatAgent>();
        this.AddHostedService<AutoReformatAgentService>();

        Scan(scanner =>
        {
            scanner.WithDefaultConventions();
            scanner.AssemblyContainingType<IBus>();
            scanner.AssemblyContainingType<CanConnectToDatabaseHealthCheck>();
            scanner.AssemblyContainingType<HealthCheck>();
            scanner.AssemblyContainingType<Is64BitProcessHealthCheck>();
            scanner.AssemblyContainingType<CanConnectToLlmServerHealthCheck>();
            scanner.AssemblyContainingType<FunJeffreyCustomEventHealthCheck>();
            scanner.ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>));
            scanner.ConnectImplementationsToTypesClosing(typeof(INotificationHandler<>));
        });

        this.AddHealthChecks()
            .AddCheck<CanConnectToLlmServerHealthCheck>("LlmGateway")
            .AddCheck<CanConnectToDatabaseHealthCheck>("DataAccess")
            .AddCheck<Is64BitProcessHealthCheck>("Server")
            .AddCheck<HealthCheck>("API")
            .AddCheck<FunJeffreyCustomEventHealthCheck>("Jeffrey");
    }
}