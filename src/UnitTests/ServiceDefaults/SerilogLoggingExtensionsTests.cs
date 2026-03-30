using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.ServiceDefaults;

[TestFixture]
public sealed class SerilogLoggingExtensionsTests
{
    [Test]
    public void WhenAddSerilogWithJsonConsole_ThenLoggerFactoryIsSerilog()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.AddSerilogWithJsonConsole();

        using var host = builder.Build();
        var factory = host.Services.GetRequiredService<ILoggerFactory>();

        factory.GetType().Name.ShouldBe("SerilogLoggerFactory");
    }
}
