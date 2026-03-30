using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.ServiceDefaults;

[TestFixture]
public class SerilogRegistrationTests
{
    [Test]
    public void ShouldUseSerilogLoggerFactoryWhenServiceDefaultsAreAdded()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddServiceDefaults();
        using var host = builder.Build();

        var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
        loggerFactory.GetType().FullName.ShouldNotBeNull();
        loggerFactory.GetType().FullName!.ShouldContain("SerilogLoggerFactory");
    }
}
