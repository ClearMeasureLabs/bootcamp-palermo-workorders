using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests;

[TestFixture]
public class TestHostConfigurationTester
{
    [Test]
    public void ShouldReadVariableFromConfigFile()
    {
        IConfiguration config = TestHost.GetRequiredService<IConfiguration>();
        string? key = config.GetValue<string>("ConnectionStrings:SqlConnectionString");
        key.ShouldNotBeNullOrEmpty();
        Console.WriteLine(key);
    }

    [Test]
    public void ShouldReadVariableFromEnvironmentVariable()
    {
        string keyName = "ConnectionStrings:TestConnectionString";
        IConfiguration config = TestHost.GetRequiredService<IConfiguration>();
        var testValue = "test value" + new Random().ToString();
        config.GetValue<string>(keyName).ShouldNotBe(testValue);
        Console.WriteLine(testValue);

        Environment.SetEnvironmentVariable(keyName, testValue, EnvironmentVariableTarget.Process);
        string? foundVariable = Environment.GetEnvironmentVariable(keyName);
        foundVariable.ShouldBe(testValue);

        config = TestHost.GetRequiredService<IConfiguration>();
        (config as IConfigurationRoot)?.Reload();
        string? key = config.GetValue<string>(keyName);
        key.ShouldBe(testValue);

        config.GetConnectionString("TestConnectionString").ShouldBe(testValue);

    }
}