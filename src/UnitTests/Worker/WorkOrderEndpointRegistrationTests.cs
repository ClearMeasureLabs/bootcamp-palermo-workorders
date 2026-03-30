using ClearMeasure.Bootcamp.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Worker;
using Worker.Messaging;

namespace ClearMeasure.Bootcamp.UnitTests.Worker;

[TestFixture]
public class WorkOrderEndpointRegistrationTests
{
    [Test]
    public void Should_RegisterRemotableBusWithIHttpClientFactory_When_EndpointRegistersServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = "Server=.;Database=test;",
                ["RemotableBus:ApiUrl"] = "https://example.test/api/bus"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddHttpClient();

        var endpoint = new TestWorkOrderEndpoint(configuration);
        endpoint.InvokeRegisterDependencyInjection(services);

        using var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBus>();
        bus.ShouldBeOfType<RemotableBus>();
    }

    private sealed class TestWorkOrderEndpoint : WorkOrderEndpoint
    {
        public TestWorkOrderEndpoint(IConfiguration configuration) : base(configuration)
        {
        }

        public void InvokeRegisterDependencyInjection(IServiceCollection services)
        {
            RegisterDependencyInjection(services);
        }
    }
}
