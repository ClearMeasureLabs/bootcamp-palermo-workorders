using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.HostedEndpoint.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Worker;

namespace ClearMeasure.Bootcamp.UnitTests.Worker;

[TestFixture]
public class WorkOrderEndpointTests
{
    [Test]
    public void Constructor_ShouldConfigureEndpointAndSqlOptions()
    {
        var endpoint = new HarnessEndpoint(BuildConfig());

        endpoint.ExposedEndpointOptions.EndpointName.ShouldBe("WorkOrderProcessing");
        endpoint.ExposedEndpointOptions.EnableInstallers.ShouldBeTrue();
        endpoint.ExposedEndpointOptions.EnableOutbox.ShouldBeTrue();
        endpoint.ExposedSqlOptions.Schema.ShouldBe("nServiceBus");
        endpoint.ExposedSqlOptions.EnableSagaPersistence.ShouldBeTrue();
        endpoint.ExposedSqlOptions.ConnectionString.ShouldBe("Server=.;Database=test;");
    }

    [Test]
    public void RegisterDependencyInjection_WhenApiUrlMissing_ShouldThrow()
    {
        var endpoint = new HarnessEndpoint(new ConfigurationBuilder().Build());

        Should.Throw<InvalidOperationException>(() => endpoint.ExposeRegister(new ServiceCollection()))
            .Message.ShouldContain("RemotableBus:ApiUrl");
    }

    [Test]
    public void RegisterDependencyInjection_WhenApiUrlPresent_ShouldRegisterBusAndChatFactory()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var endpoint = new HarnessEndpoint(BuildConfig(apiUrl: "http://localhost/api/bus"));

        endpoint.ExposeRegister(services);
        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IBus>().ShouldBeOfType<global::Worker.Messaging.RemotableBus>();
        provider.GetRequiredService<ChatClientFactory>().ShouldNotBeNull();
    }

    private static IConfiguration BuildConfig(string? apiUrl = "http://localhost/api/bus")
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:SqlConnectionString"] = "Server=.;Database=test;"
        };
        if (apiUrl != null)
        {
            values["RemotableBus:ApiUrl"] = apiUrl;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private sealed class HarnessEndpoint : WorkOrderEndpoint
    {
        public HarnessEndpoint(IConfiguration configuration)
            : base(configuration)
        {
        }

        public EndpointOptions ExposedEndpointOptions => EndpointOptions;
        public SqlPersistenceOptions ExposedSqlOptions => SqlPersistenceOptions;

        public void ExposeRegister(IServiceCollection services) => RegisterDependencyInjection(services);
    }
}
