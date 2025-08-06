using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Client.HealthChecks;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using Lamar;
using MediatR;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI;
using Palermo.BlazorMvc;
using static System.Net.WebRequestMethods;

namespace ClearMeasure.Bootcamp.UI.Client;

// ReSharper disable once InconsistentNaming
public class UIClientServiceRegistry : ServiceRegistry
{
    public UIClientServiceRegistry()
    {
        this.AddScoped<CustomAuthenticationStateProvider>();
        this.AddScoped<AuthenticationStateProvider>(provider =>
            provider.GetRequiredService<CustomAuthenticationStateProvider>());

        this.AddScoped<IUiBus>(provider => new MvcBus(NullLogger<MvcBus>.Instance));
        this.AddScoped<IUserSession, UserSession>();
        this.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<RemotableBus>());
        this.AddTransient<IBus, RemotableBus>();

        this.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<UIClientServiceRegistry>());
        this.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CanConnectToLlmServerHealthCheck>());
        
        
        this.AddSingleton<ChatClientFactory>();
        this.AddTransient<WorkOrderTool>();

        Scan(scanner =>
        {
            scanner.WithDefaultConventions();
            scanner.AssemblyContainingType<UIClientServiceRegistry>();
            scanner.AssemblyContainingType<IRemotableRequest>();
            scanner.ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>));
            scanner.ConnectImplementationsToTypesClosing(typeof(INotificationHandler<>));
        });

        this.AddHealthChecks().AddCheck<HealthCheckTracer>("UI.Client");
        this.AddHealthChecks().AddCheck<RemotableBusHealthCheck>("Remotable Bus");
        this.AddHealthChecks().AddCheck<ServerHealthCheck>("Server health check");
    }
}