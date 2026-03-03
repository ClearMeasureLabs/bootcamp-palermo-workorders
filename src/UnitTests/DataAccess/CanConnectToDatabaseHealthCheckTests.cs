using ClearMeasure.Bootcamp.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess;

[TestFixture]
public class CanConnectToDatabaseHealthCheckTests
{
    [Test]
    public async Task WhenDatabaseResponds_ReturnsHealthy()
    {
        var mockDatabaseFacade = new Mock<DatabaseFacade>(MockBehavior.Strict, new Mock<DbContext>().Object);
        mockDatabaseFacade
            .Setup(db => db.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var mockContext = new Mock<DbContext>(new DbContextOptions<DbContext>());
        mockContext
            .Setup(ctx => ctx.Database)
            .Returns(mockDatabaseFacade.Object);

        CanConnectToDatabaseHealthCheck healthCheck = new(mockContext.Object);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Test]
    public async Task WhenDatabaseDoesNotRespond_ReturnsUnhealthy()
    {
        var mockDatabaseFacade = new Mock<DatabaseFacade>(MockBehavior.Strict, new Mock<DbContext>().Object);
        mockDatabaseFacade
            .Setup(db => db.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var mockContext = new Mock<DbContext>(new DbContextOptions<DbContext>());
        mockContext
            .Setup(ctx => ctx.Database)
            .Returns(mockDatabaseFacade.Object);

        CanConnectToDatabaseHealthCheck healthCheck = new(mockContext.Object);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Test]
    public async Task WhenDatabaseThrowsErrorWhenConnecting_ReturnsUnhealthy()
    {
        var mockDatabaseFacade = new Mock<DatabaseFacade>(MockBehavior.Strict, new Mock<DbContext>().Object);
        mockDatabaseFacade
            .Setup(db => db.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var mockContext = new Mock<DbContext>(new DbContextOptions<DbContext>());
        mockContext
            .Setup(ctx => ctx.Database)
            .Returns(mockDatabaseFacade.Object);

        CanConnectToDatabaseHealthCheck healthCheck = new(mockContext.Object);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }
}
