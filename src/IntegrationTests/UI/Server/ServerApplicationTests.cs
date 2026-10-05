using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.UI.Server;

[TestFixture]
public class ServerApplicationTests
{
    [Test]
    public void ShouldUseLearningTransportWhenLocalDbConnectionString()
    {
        ServerApplication.ShouldUseLearningTransport("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=test")
            .ShouldBeTrue();
    }

    [Test]
    public void ShouldNotUseLearningTransportWhenSqlServerConnectionString()
    {
        ServerApplication.ShouldUseLearningTransport("server=localhost,1433;database=test")
            .ShouldBeFalse();
    }

    [Test]
    public void ShouldBuildApplicationWithoutThrowing()
    {
        // A command-line value is read before services are registered, so an
        // APPLICATIONINSIGHTS_CONNECTION_STRING in the environment cannot switch the exporter on.
        var app = ServerApplication.BuildApplication(["--APPLICATIONINSIGHTS_CONNECTION_STRING="], builder =>
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = "Data Source=:memory:",
                ["AI_OpenAI_ApiKey"] = "",
                ["AI_OpenAI_Url"] = "",
                ["AI_OpenAI_Model"] = ""
            });
            builder.Environment.EnvironmentName = "Testing";
        });

        app.ShouldNotBeNull();
    }
}
