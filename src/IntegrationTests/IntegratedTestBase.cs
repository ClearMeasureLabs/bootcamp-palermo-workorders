namespace ClearMeasure.Bootcamp.IntegrationTests;

public class IntegratedTestBase
{
    protected TK Faker<TK>()
    {
        return TestHost.Faker<TK>();
    }
}