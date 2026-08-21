using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class DatabaseConfigurationTests
{
    [Test]
    public void GetConnectionString_Should_ReturnConfiguredValue()
    {
        var configuration = BuildConfiguration("Server=.;Database=Test;TrustServerCertificate=true;");
        var sut = new DatabaseConfiguration(configuration);

        sut.GetConnectionString().ShouldBe("Server=.;Database=Test;TrustServerCertificate=true;");
    }

    [Test]
    public void GetConnectionString_Should_Throw_When_SqlConnectionStringMissing()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var sut = new DatabaseConfiguration(configuration);

        Should.Throw<InvalidOperationException>(() => sut.GetConnectionString())
            .Message.ShouldContain("SqlConnectionString");
    }

    [Test]
    public void ResetConnectionPool_Should_NotThrow_When_SqliteConnectionString()
    {
        var configuration = BuildConfiguration("Data Source=:memory:");
        var sut = new DatabaseConfiguration(configuration);

        Should.NotThrow(() => sut.ResetConnectionPool());
    }

    [Test]
    public void ResetConnectionPool_Should_NotThrow_When_SqlServerConnectionString()
    {
        var configuration = BuildConfiguration("Server=.;Database=Test;TrustServerCertificate=true;");
        var sut = new DatabaseConfiguration(configuration);

        Should.NotThrow(() => sut.ResetConnectionPool());
    }

    private static IConfiguration BuildConfiguration(string connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = connectionString
            })
            .Build();
}
