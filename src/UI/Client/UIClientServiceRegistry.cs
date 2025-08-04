using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Client.HealthChecks;
using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using Lamar;
using MediatR;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Palermo.BlazorMvc;

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
        this.AddSingleton<IChatClient>(provider =>
        {
            var endpoint = "http://localhost:11434/";
            var modelId = "llama3.2";

            return new OllamaChatClient(endpoint, modelId: modelId)
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();
        });

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