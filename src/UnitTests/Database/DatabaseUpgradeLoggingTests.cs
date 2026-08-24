using ClearMeasure.Bootcamp.Database.Console;
using DbUp.Engine.Output;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Shouldly;
using ChurchBulletin.ServiceDefaults;

namespace ClearMeasure.Bootcamp.UnitTests.Database;

[TestFixture]
public class DatabaseUpgradeLoggingTests
{
    [Test]
    public void ShouldSuppressAllOutput_WhenNullUpgradeLogUsed()
    {
        var log = new NullUpgradeLog();
        var writer = new StringWriter();
        var originalOut = Console.Out;
        var originalErr = Console.Error;
        try
        {
            Console.SetOut(writer);
            Console.SetError(writer);

            log.LogError(new InvalidOperationException("stack"), "upgrade failed {0}", "x");
            log.LogWarning("warn {0}", "y");
            log.LogInformation("info {0}", "z");

            writer.ToString().ShouldBeEmpty();
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }
    }

    [Test]
    public void ShouldOmitExceptionStack_WhenQuietLogLogsErrorWithException()
    {
        var log = new QuietLog();
        var writer = new StringWriter();
        var originalErr = Console.Error;
        try
        {
            Console.SetError(writer);

            log.LogError(new InvalidOperationException("inner"), "upgrade failed");

            var output = writer.ToString();
            output.ShouldContain("upgrade failed");
            output.ShouldContain("inner");
            output.ShouldNotContain("InvalidOperationException:");
            output.ShouldNotContain("at ");
        }
        finally
        {
            Console.SetError(originalErr);
        }
    }
}

[TestFixture]
public class SerilogTestingEnvironmentTests
{
    [Test]
    public void ShouldDisableWriteToProviders_WhenEnvironmentIsTesting()
    {
        var environment = new HostEnvironment { EnvironmentName = Environments.Development };
        SerilogExtensions.ShouldWriteToProviders(environment).ShouldBeTrue();

        environment.EnvironmentName = "Testing";
        SerilogExtensions.ShouldWriteToProviders(environment).ShouldBeFalse();
    }
}

internal sealed class HostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Production;
    public string ApplicationName { get; set; } = "test";
    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
}
