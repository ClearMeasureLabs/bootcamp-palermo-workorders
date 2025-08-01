using ClearMeasure.Bootcamp.UnitTests;

namespace ClearMeasure.Bootcamp.IntegrationTests;

public class IntegratedTestBase
{
    protected TK Faker<TK>()
    {
        return TestHost.Faker<TK>();
    }

    public static void AssertAllProperties(object expected, object actual)
    {
        ObjectMother.AssertAllProperties(expected, actual);
    }
}