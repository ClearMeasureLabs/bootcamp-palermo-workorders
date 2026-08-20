using ClearMeasure.Bootcamp.Core.Queries;
using ChurchBulletin.ServiceDefaults;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.ServiceDefaults;

[TestFixture]
public class NamespaceAlignmentTests
{
    [Test]
    public void EmployeeGetAllQuery_ShouldLiveInCoreQueriesNamespace()
    {
        typeof(EmployeeGetAllQuery).Namespace.ShouldBe("ClearMeasure.Bootcamp.Core.Queries");
    }

    [Test]
    public void ServiceDefaultsExtensions_ShouldLiveInChurchBulletinServiceDefaultsNamespace()
    {
        typeof(Extensions).Namespace.ShouldBe("ChurchBulletin.ServiceDefaults");
    }

    [Test]
    public void LogEntry_ShouldLiveInChurchBulletinServiceDefaultsNamespace()
    {
        typeof(LogEntry).Namespace.ShouldBe("ChurchBulletin.ServiceDefaults");
    }

    [Test]
    public void CorrelationIdMiddleware_ShouldLiveInChurchBulletinServiceDefaultsNamespace()
    {
        typeof(CorrelationIdMiddleware).Namespace.ShouldBe("ChurchBulletin.ServiceDefaults");
    }
}
