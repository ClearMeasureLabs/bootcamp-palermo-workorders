using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests;

[TestFixture]
public class TestHostConfigurationTester
{
    [Test]
    public void ShouldReadVariableFromConfigFile()
    {
        var config = TestHost.GetRequiredService<IConfiguration>();
        var key = config.GetValue<string>("ConnectionStrings:SqlConnectionString");
        key.ShouldNotBeNullOrEmpty();
        Console.WriteLine(key);
    }

    [Test]
    public void ShouldReadVariableFromEnvironmentVariable()
    {
        var keyName = "ConnectionStrings:TestConnectionString";
        var config = TestHost.GetRequiredService<IConfiguration>();
        var testValue = "test value" + new Random();
        config.GetValue<string>(keyName).ShouldNotBe(testValue);
        Console.WriteLine(testValue);

        Environment.SetEnvironmentVariable(keyName, testValue, EnvironmentVariableTarget.Process);
        var foundVariable = Environment.GetEnvironmentVariable(keyName);
        foundVariable.ShouldBe(testValue);

        config = TestHost.GetRequiredService<IConfiguration>();
        (config as IConfigurationRoot)?.Reload();
        var key = config.GetValue<string>(keyName);
        key.ShouldBe(testValue);

        config.GetConnectionString("TestConnectionString").ShouldBe(testValue);
    }
}