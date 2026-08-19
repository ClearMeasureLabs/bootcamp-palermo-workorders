using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
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
        var app = ServerApplication.BuildApplication([], builder =>
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = "Data Source=:memory:",
                ["AI_OpenAI_ApiKey"] = "",
                ["AI_OpenAI_Url"] = "",
                ["AI_OpenAI_Model"] = "",
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = ""
            });
            builder.Environment.EnvironmentName = "Testing";
        });

        app.ShouldNotBeNull();
    }
}
