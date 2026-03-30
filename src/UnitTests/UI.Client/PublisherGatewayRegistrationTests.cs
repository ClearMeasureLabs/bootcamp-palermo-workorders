using ClearMeasure.Bootcamp.UI.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client;

[TestFixture]
public class PublisherGatewayRegistrationTests
{
    [Test]
    public void ShouldConfigureNamedHttpClient_WithBaseAddressFromHostEnvironment()
    {
        const string baseAddress = "https://unit.test.example/app/";
        var services = new ServiceCollection();
        services.AddSingleton<IWebAssemblyHostEnvironment>(new StubWebAssemblyHostEnvironment(baseAddress));
        services.AddHttpClient(PublisherGateway.HttpClientName, (sp, client) =>
        {
            var env = sp.GetRequiredService<IWebAssemblyHostEnvironment>();
            client.BaseAddress = new Uri(env.BaseAddress);
        });
        services.AddTransient<IPublisherGateway>(sp =>
            new PublisherGateway(sp.GetRequiredService<IHttpClientFactory>()
                .CreateClient(PublisherGateway.HttpClientName)));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var factory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        using var client = factory.CreateClient(PublisherGateway.HttpClientName);
        client.BaseAddress.ShouldBe(new Uri(baseAddress));

        var gateway = scope.ServiceProvider.GetRequiredService<IPublisherGateway>();
        gateway.ShouldBeOfType<PublisherGateway>();
    }

    private sealed class StubWebAssemblyHostEnvironment(string baseAddress) : IWebAssemblyHostEnvironment
    {
        public string Environment { get; } = "Production";

        public string BaseAddress { get; } = baseAddress;
    }
}
