using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class DiagnosticControllerTests
{
    [Test]
    public void ResetDbConnections_Should_CallResetConnectionPoolOnce_And_Return200WithMessage()
    {
        var databaseConfiguration = new StubDatabaseConfiguration();
        var controller = new DiagnosticController(databaseConfiguration)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.ResetDbConnections();

        databaseConfiguration.ResetConnectionPoolCallCount.ShouldBe(1);
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        ok.Value.ShouldBe("Connection pools cleared");
    }
}

file sealed class StubDatabaseConfiguration : IDatabaseConfiguration
{
    public int ResetConnectionPoolCallCount { get; private set; }

    public string GetConnectionString() => string.Empty;

    public void ResetConnectionPool() => ResetConnectionPoolCallCount++;
}
